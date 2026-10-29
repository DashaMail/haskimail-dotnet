using Haskimail.Models;

namespace Haskimail.Tests;

/// <summary>
/// Тест против настоящего API. По умолчанию пропускается; включается переменными окружения:
/// <list type="bullet">
/// <item><c>HASKIMAIL_LIVE=1</c> — отправка с тестовым ключом HASKIMAIL_API_TEST (письма не создаются и не доставляются, аккаунт не нужен);</item>
/// <item><c>HASKIMAIL_SERVER_TOKEN</c> — дополнительно чтение данных вашего сервера (только GET-запросы).</item>
/// </list>
/// </summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute(bool requiresServerToken = false)
    {
        if (Environment.GetEnvironmentVariable("HASKIMAIL_LIVE") != "1")
            Skip = "Живые тесты выключены: задайте HASKIMAIL_LIVE=1.";
        else if (requiresServerToken && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HASKIMAIL_SERVER_TOKEN")))
            Skip = "Нужен HASKIMAIL_SERVER_TOKEN.";
    }
}

[Trait("Category", "Live")]
public class LiveTests
{
    private static HaskimailClient TestTokenClient() => new(HaskimailClient.TestServerToken);

    [LiveFact]
    public async Task Test_token_accepts_a_valid_email()
    {
        using var client = TestTokenClient();

        var result = await client.Email.SendAsync(new EmailMessage
        {
            From = "Haskimail.NET <sdk-test@example.com>",
            To = "ivan@example.com, \"Иванова, Анна\" <anna@example.com>",
            Subject = "Проверка SDK 🚀",
            HtmlBody = "<p>Привет & пока</p>",
            TextBody = "Привет",
            Tag = "sdk-live-test",
            Metadata = new() { ["run"] = Guid.NewGuid().ToString("N") },
            Headers = [new("X-Sdk-Test", "1")],
            Attachments = [Attachment.FromBytes("note.txt", "hi"u8.ToArray(), "text/plain")],
        });

        Assert.Equal(0, result.ErrorCode);
        Assert.NotNull(result.SubmittedAt);
    }

    [LiveFact]
    public async Task Test_token_reports_errors_per_batch_item()
    {
        using var client = TestTokenClient();

        var results = await client.Email.SendBatchAsync(
        [
            new EmailMessage { From = "sdk-test@example.com", To = "ivan@example.com", TextBody = "ok" },
            new EmailMessage { From = "sdk-test@example.com", To = "not-an-email", TextBody = "bad" },
        ]);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess, results[0].Message);
        Assert.False(results[1].IsSuccess);
    }

    [LiveFact]
    public async Task Test_token_rejects_an_invalid_email_with_api_exception()
    {
        using var client = TestTokenClient();

        var ex = await Assert.ThrowsAnyAsync<HaskimailApiException>(() =>
            client.Email.SendAsync(new EmailMessage { From = "sdk-test@example.com", To = "not-an-email", TextBody = "x" }));

        Assert.NotEqual(0, ex.ErrorCode);
    }

    [LiveFact(requiresServerToken: true)]
    public async Task Server_token_reads_account_data()
    {
        using var client = new HaskimailClient(Environment.GetEnvironmentVariable("HASKIMAIL_SERVER_TOKEN")!);

        var streams = await client.MessageStreams.ListAsync();
        var overview = await client.Stats.GetOverviewAsync();
        var messages = await client.Messages.SearchOutboundAsync(new OutboundMessageSearch { Count = 5 });
        var clients = await client.Stats.GetOpenEmailClientsAsync();

        Assert.NotEmpty(streams.MessageStreams);
        Assert.True(overview.Sent >= 0);
        Assert.True(messages.TotalCount >= messages.Messages.Count);
        Assert.NotNull(clients.Totals);
    }
}
