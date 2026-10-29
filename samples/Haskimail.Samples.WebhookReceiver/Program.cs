// Пример: приём вебхуков Haskimail в ASP.NET Core и отправка через DI.
//
// appsettings.json:
//   "Haskimail": { "ServerToken": "...", "WebhookSigningKey": "..." }
//
// Проверка без настоящих событий: отправьте письмо на HaskimailTestAddresses.HardBounce —
// Haskimail сразу пришлёт событие bounced на ваш вебхук.

using System.Collections.Concurrent;
using Haskimail;
using Haskimail.Models;
using Haskimail.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHaskimail(builder.Configuration.GetSection("Haskimail"));

var app = builder.Build();
var signingKey = app.Configuration["Haskimail:WebhookSigningKey"];
var seen = new ConcurrentDictionary<string, byte>(); // в реальном приложении — Redis или таблица БД

// События: доставка, возврат, открытие, клик, жалоба, изменение подписки
app.MapPost("/haskimail/events", async (HttpRequest request, ILogger<Program> log) =>
{
    WebhookEvent e;
    try
    {
        e = await HaskimailWebhooks.ParseEventAsync(request.Body, request.HttpContext.RequestAborted);
    }
    catch (HaskimailException ex)
    {
        log.LogWarning(ex, "Некорректное тело вебхука");
        return Results.BadRequest(); // 4xx — Haskimail не будет повторять
    }

    if (signingKey is not null && !HaskimailWebhooks.VerifySignature(e, signingKey))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (!seen.TryAdd(e.DeduplicationKey, 0))
        return Results.Ok(); // повтор того же события

    switch (e.EventType)
    {
        case WebhookEventType.Delivered:
            log.LogInformation("Доставлено {Recipient} ({MessageId})", e.Recipient, e.MessageId);
            break;
        case WebhookEventType.Bounced:
            log.LogWarning("Возврат {Recipient}: {Category} {Code} {Error}", e.Recipient, e.BounceCategory, e.Code, e.Error);
            break;
        case WebhookEventType.Clicked:
            log.LogInformation("Клик {Recipient}: {Url}", e.Recipient, e.UrlOriginal);
            break;
        case WebhookEventType.SubscriptionChange:
            log.LogInformation("{Recipient}: стоп-лист {Action} ({Reason})", e.Recipient, e.SuppressSending == true ? "добавлен" : "удалён", e.SuppressionReason);
            break;
        default:
            log.LogInformation("Событие {Event} для {Recipient}", e.Event, e.Recipient);
            break;
    }

    return Results.Ok(); // отвечайте быстро: Haskimail ждёт не больше 10 секунд
});

// Входящие письма (InboundHookUrl сервера)
app.MapPost("/haskimail/inbound", async (HttpRequest request, ILogger<Program> log) =>
{
    var mail = await HaskimailWebhooks.ParseInboundAsync(request.Body, request.HttpContext.RequestAborted);
    log.LogInformation("Ответ от {From} по тикету {Ticket}: {Text}", mail.From, mail.MailboxHash, mail.StrippedTextReply);
    return Results.Ok();
});

// Отправка через клиент из DI
app.MapPost("/send-test", async (IHaskimailClient haskimail) =>
{
    var result = await haskimail.Email.SendAsync(new EmailMessage
    {
        From = "shop@example.com",
        To = HaskimailTestAddresses.HardBounce,
        Subject = "Проверка вебхука возвратов",
        TextBody = "Это письмо не уйдёт в интернет — Haskimail сразу пришлёт событие bounced.",
    });
    return Results.Ok(new { result.MessageId });
});

app.Run();
