// Пример: отправка письма, пакета и чтение статистики.
//
//   dotnet run                       — проверка с тестовым ключом (HASKIMAIL_API_TEST): письма не доставляются
//   HASKIMAIL_SERVER_TOKEN=... dotnet run -- --live ivan@example.com — настоящая отправка

using Haskimail;
using Haskimail.Models;

var token = Environment.GetEnvironmentVariable("HASKIMAIL_SERVER_TOKEN");
var live = args.Contains("--live") && !string.IsNullOrEmpty(token);
var recipient = args.SkipWhile(a => a != "--live").Skip(1).FirstOrDefault() ?? "ivan@example.com";

using var client = new HaskimailClient(new HaskimailClientOptions
{
    ServerToken = live ? token : HaskimailClient.TestServerToken,
    UserAgentSuffix = "Haskimail.Samples.Console",
});

Console.WriteLine(live ? "Режим: настоящая отправка" : "Режим: тестовый ключ, письма не доставляются");

try
{
    // 1. Одно письмо
    var result = await client.Email.SendAsync(new EmailMessage
    {
        From = "Магазин <shop@example.com>",
        To = recipient,
        Subject = "Заказ №1042 оформлен",
        HtmlBody = "<p>Здравствуйте! Заказ <b>№1042</b> подтверждён.</p>",
        TextBody = "Здравствуйте! Заказ №1042 подтверждён.",
        Tag = "orders",
        Metadata = new() { ["order_id"] = "1042" },
    });
    Console.WriteLine($"Письмо принято: {result.MessageId} в {result.SubmittedAt:HH:mm:ss}");

    // 2. Пакет: частичный отказ — не исключение, смотрим результат по каждому письму
    var batch = await client.Email.SendBatchAsync(
    [
        new EmailMessage { From = "shop@example.com", To = recipient, Subject = "Первое", TextBody = "1" },
        new EmailMessage { From = "shop@example.com", To = "not-an-email", Subject = "Второе", TextBody = "2" },
    ]);
    foreach (var (index, failure) in batch.GetFailures())
        Console.WriteLine($"Письмо [{index}] отклонено: {failure.ErrorCode} {failure.Message}");

    // 3. Чтение (только с настоящим ключом — тестовый работает лишь для отправки)
    if (live)
    {
        var overview = await client.Stats.GetOverviewAsync();
        Console.WriteLine($"За месяц: отправлено {overview.Sent}, открываемость {overview.OpenRate}%");

        await foreach (var message in client.Messages.SearchOutboundAllAsync(new OutboundMessageSearch { Tag = "orders", Count = 100 }))
            Console.WriteLine($"{message.ReceivedAt:dd.MM HH:mm}  {message.Recipient}  {message.Subject}");
    }
}
catch (HaskimailValidationException ex)
{
    Console.WriteLine($"API отклонил запрос: код {ex.ErrorCode} — {ex.ApiMessage}");
}
catch (HaskimailException ex)
{
    Console.WriteLine($"Ошибка Haskimail: {ex.Message}");
}
