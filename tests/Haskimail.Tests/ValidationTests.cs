using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Tests;

/// <summary>Проверки до отправки — документированные ограничения Haskimail.</summary>
public class ValidationTests
{
    private static EmailMessage Valid() => new() { From = "shop@example.com", To = "ivan@example.com", TextBody = "x" };

    private static async Task<ArgumentException> Rejects(EmailMessage message, Action<HaskimailClientOptions>? configure = null)
    {
        var (client, http) = TestClient.Create(configure);
        var ex = await Assert.ThrowsAnyAsync<ArgumentException>(() => client.Email.SendAsync(message));
        Assert.Empty(http.Requests);
        return ex;
    }

    [Fact] public Task No_sender() => Rejects(new EmailMessage { To = "a@b.ru", TextBody = "x" });

    [Fact] public Task No_recipients() => Rejects(new EmailMessage { From = "a@b.ru", TextBody = "x" });

    [Fact] public Task No_body() => Rejects(new EmailMessage { From = "a@b.ru", To = "c@d.ru" });

    [Fact]
    public async Task Template_without_id_or_alias()
    {
        var (client, _) = TestClient.Create();
        await Assert.ThrowsAsync<ArgumentException>(() => client.Templates.SendAsync(new TemplatedEmailMessage { From = "a@b.ru", To = "c@d.ru" }));
    }

    [Fact]
    public async Task Length_limits()
    {
        var m1 = Valid(); m1.From = new string('a', 251) + "@b.ru"; // 256 символов
        var m2 = Valid(); m2.Subject = new string('я', 2001);
        var m3 = Valid(); m3.Tag = new string('t', 1001);

        Assert.Contains("From", (await Rejects(m1)).Message);
        Assert.Contains("Subject", (await Rejects(m2)).Message);
        Assert.Contains("Tag", (await Rejects(m3)).Message);
    }

    [Fact]
    public async Task Limits_count_utf16_units_like_haskimail()
    {
        // Эмодзи вне BMP — две единицы UTF-16: 1000 эмодзи = 2000 единиц, ровно на границе.
        var (client, http) = TestClient.Create();
        var ok = Valid(); ok.Subject = string.Concat(Enumerable.Repeat("🚀", 1000));
        await client.Email.SendAsync(ok);
        Assert.Single(http.Requests);

        var tooLong = Valid(); tooLong.Subject = ok.Subject + "!";
        await Rejects(tooLong);
    }

    [Fact]
    public async Task Body_size_limit_is_in_utf8_bytes()
    {
        var m = Valid();
        m.TextBody = null;
        m.HtmlBody = new string('ж', 2 * 1024 * 1024 + 1); // 2 байта на символ → больше 4 МБ? нет: 4 МБ + 2 байта — проходит
        var (client, http) = TestClient.Create();
        await client.Email.SendAsync(m);
        Assert.Single(http.Requests);

        m.HtmlBody = new string('ж', 3 * 1024 * 1024); // 6 МБ
        Assert.Contains("HtmlBody", (await Rejects(m)).Message);
    }

    [Fact]
    public async Task At_most_fifty_recipients_across_to_cc_bcc()
    {
        var m = Valid();
        m.To = string.Join(", ", Enumerable.Range(0, 30).Select(i => $"u{i}@example.com"));
        m.Cc = string.Join(",", Enumerable.Range(0, 15).Select(i => $"c{i}@example.com"));
        m.Bcc = string.Join(",", Enumerable.Range(0, 6).Select(i => $"b{i}@example.com"));

        Assert.Contains("51", (await Rejects(m)).Message);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("a@b.ru", 1)]
    [InlineData("a@b.ru, c@d.ru", 2)]
    [InlineData("a@b.ru,, c@d.ru,", 2)]
    [InlineData("\"Иванов, Иван\" <ivan@example.com>, anna@example.com", 2)]
    [InlineData("\"Кавычка \\\" и, запятая\" <a@b.ru>", 1)]
    [InlineData("Name <weird,local@b.ru>", 1)]
    public void Counts_addresses_respecting_quotes_and_brackets(string? list, int expected) =>
        Assert.Equal(expected, RequestValidator.CountAddresses(list));

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("SCRIPT.VBS")]
    [InlineData("archive.tar.msi")]
    public async Task Forbidden_attachment_types(string name)
    {
        var m = Valid();
        m.Attachments = [Attachment.FromBytes(name, [1], "application/octet-stream")];
        Assert.Contains(name, (await Rejects(m)).Message);
    }

    [Fact]
      public async Task Attachments_over_ten_megabytes_after_base64()
      {
          var m = Valid();
          m.Attachments =
          [
              new Attachment { Name = "a.pdf", ContentType = "application/pdf", Content = new string('A', 6 * 1024 * 1024) },
              new Attachment { Name = "b.pdf", ContentType = "application/pdf", Content = new string('A', 5 * 1024 * 1024) },
          ];
          Assert.Contains("10 МБ", (await Rejects(m)).Message);
      }

      [Fact]
      public async Task Whole_message_over_ten_megabytes_counted_together_with_the_body()
      {
          var m = Valid();
          // 6 МБ текста (в пределах лимита тела 5 МБ? нет — превышает, поэтому текст короче).
          m.TextBody = new string('ж', 3 * 1024 * 1024); // 6 МБ — сам по себе уже над лимитом тела
          m.Attachments = [new Attachment { Name = "a.pdf", ContentType = "application/pdf", Content = new string('A', 4 * 1024 * 1024) + "==" }];
          // Тело (6 МБ) само по себе превышает 5 МБ, поэтому отвергается как тело.
          Assert.Contains("TextBody", (await Rejects(m)).Message);

          // Тело и вложения порознь в пределах лимитов, но вместе — больше 10 МБ.
          var combined = Valid();
          combined.TextBody = new string('ж', 2 * 1024 * 1024); // 4 МБ текста
          combined.Attachments =
          [
              new Attachment { Name = "a.pdf", ContentType = "application/pdf", Content = new string('A', 4 * 1024 * 1024) },
              new Attachment { Name = "b.pdf", ContentType = "application/pdf", Content = new string('A', 3 * 1024 * 1024) },
          ];
          var ex = await Rejects(combined);
          Assert.Contains("10 МБ", ex.Message);
          Assert.DoesNotContain("HtmlBody", ex.Message);
          Assert.DoesNotContain("TextBody", ex.Message);
      }

    [Fact]
    public async Task Header_without_name()
    {
        var m = Valid();
        m.Headers = [new MessageHeader("", "x")];
        await Rejects(m);
    }

    [Fact]
    public async Task Deep_checks_can_be_disabled_but_basic_ones_stay()
    {
        var (client, http) = TestClient.Create(o => o.ValidateRequests = false);
        var m = Valid();
        m.Subject = new string('x', 5000);
        m.Attachments = [Attachment.FromBytes("run.exe", [1], "application/octet-stream")];

        await client.Email.SendAsync(m);
        Assert.Single(http.Requests);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Email.SendAsync(new EmailMessage { From = "a@b.ru", To = "c@d.ru" }));
    }

    [Fact]
    public async Task Batch_size_bounds_and_item_index_in_message()
    {
        var (client, http) = TestClient.Create();

        await Assert.ThrowsAsync<ArgumentException>(() => client.Email.SendBatchAsync([]));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Email.SendBatchAsync(Enumerable.Range(0, 501).Select(_ => (EmailMessageBase)Valid())));
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.Email.SendBatchAsync([Valid(), new EmailMessage { From = "a@b.ru", To = "c@d.ru" }]));

        Assert.StartsWith("Письмо [1]", ex.Message);
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task Other_argument_checks()
    {
        var (client, http) = TestClient.Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Messages.SearchOutboundAsync(new OutboundMessageSearch { Count = 501 }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Messages.ListOpensAsync(new MessageEventSearch { Count = 500, Offset = 9600 }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Servers.UpdateAsync(1, new UpdateServerRequest { InboundSpamThreshold = 7 }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.MessageStreams.CreateAsync(new CreateMessageStreamRequest { Name = "x", MessageStreamType = MessageStreamType.Inbound }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Templates.ValidateAsync(new ValidateTemplateRequest()));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Suppressions.AddAsync("1", []));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Templates.GetAsync(" "));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Email.SendAsync(null!));

        Assert.Empty(http.Requests);
    }
}
