using System.Net;
using Haskimail.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Haskimail.Tests;

public class ClientTests
{
    [Fact]
    public void Options_are_validated()
    {
        Assert.Throws<ArgumentException>(() => new HaskimailClient(new HaskimailClientOptions()));
        Assert.Throws<ArgumentException>(() => new HaskimailClient(new HaskimailClientOptions { ServerToken = "x", BaseUrl = "not a url" }));
        Assert.Throws<ArgumentException>(() => new HaskimailClient(new HaskimailClientOptions { ServerToken = "x", BaseUrl = "ftp://api.haskimail.ru" }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HaskimailClient(new HaskimailClientOptions { ServerToken = "x", Timeout = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HaskimailClient(new HaskimailClientOptions { ServerToken = "x", Retry = new HaskimailRetryOptions { MaxRetries = -1 } }));
        Assert.Throws<ArgumentNullException>(() => new HaskimailClient((HaskimailClientOptions)null!));
    }

    [Fact]
    public async Task Options_are_snapshotted_at_construction()
    {
        var http = new MockHttp();
        var options = new HaskimailClientOptions { ServerToken = " token ", BaseUrl = "https://proxy.local/haski/" };
        var client = new HaskimailClient(new HttpClient(http), options);

        options.ServerToken = "changed";
        options.BaseUrl = "https://elsewhere";
        await client.Templates.ListAsync();

        Assert.Equal("https://proxy.local/haski/templates", http.Last.Uri.ToString());
        Assert.Equal("token", http.Last.Header("X-Haskimail-Server-Token"));
    }

    [Fact]
    public async Task User_agent_identifies_sdk_and_app()
    {
        var (client, http) = TestClient.Create(o => o.UserAgentSuffix = "MyShop/2.1");

        await client.Templates.ListAsync();

        var ua = http.Last.Header("User-Agent")!;
        Assert.StartsWith("Haskimail.NET/", ua);
        Assert.EndsWith("MyShop/2.1", ua);
    }

    [Fact]
    public async Task External_http_client_is_not_disposed()
    {
        var http = new MockHttp();
        var httpClient = new HttpClient(http);
        using (var client = new HaskimailClient(httpClient, new HaskimailClientOptions { ServerToken = "x" }))
        {
            await client.Templates.ListAsync();
        }

        var response = await httpClient.GetAsync(new Uri("https://example.com/"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Owned_client_can_be_created_and_disposed()
    {
        using var client = new HaskimailClient(HaskimailClient.TestServerToken);
        Assert.NotNull(client.Email);
    }

    [Fact]
    public async Task Update_template_with_delegate_does_not_escape_name_and_subject_again()
    {
        // Название и тема могут прийти экранированными (шаблоны, созданные через API старыми версиями сервера).
        var (client, http) = TestClient.Create();
        http.Respond("""{"TemplateId":7,"Name":"Счёт &amp; оплата &laquo;Плюс&raquo;","Subject":"Заказ &laquo;{{order}}&raquo; &mdash; оплачен","HtmlBody":"<p>&amp;</p>","TextBody":"t"}""")
            .Respond("""{"TemplateId":7,"Name":"x"}""");

        await client.Templates.UpdateAsync("7", t => t.TextBody = "новый текст");

        var body = http.Requests[1].Json;
        Assert.Equal("Счёт & оплата «Плюс»", body.GetProperty("Name").GetString());
        Assert.Equal("Заказ «{{order}}» — оплачен", body.GetProperty("Subject").GetString());
        Assert.Equal("<p>&amp;</p>", body.GetProperty("HtmlBody").GetString()); // тело сервер не экранирует — не трогаем
        Assert.Equal("новый текст", body.GetProperty("TextBody").GetString());
    }

    [Fact]
    public void Template_exposes_decoded_name_and_subject()
    {
        var template = new Template { Name = "A &amp; B", Subject = "&laquo;Привет&raquo;" };

        Assert.Equal("A & B", template.DecodedName);
        Assert.Equal("«Привет»", template.DecodedSubject);
        Assert.Equal("A & B", UpdateTemplateRequest.FromTemplate(template).Name);
        Assert.Equal("A & B", new TemplateSummary { Name = "A &amp; B" }.DecodedName);
        Assert.Null(new Template().DecodedName);
    }

    [Fact]
    public async Task Update_template_with_delegate_reads_then_writes_all_fields_by_id()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""{"TemplateId":4521,"Name":"Заказ","Subject":"Старая","HtmlBody":"<p>h</p>","TextBody":"t","AssociatedServerId":1}""")
            .Respond("""{"TemplateId":4521,"Name":"Заказ оформлен"}""");

        var result = await client.Templates.UpdateAsync("Заказ", t => { t.Name = "Заказ оформлен"; t.Subject = "Новая"; });

        Assert.Equal("GET", http.Requests[0].Method.Method);
        Assert.Equal("/templates/%D0%97%D0%B0%D0%BA%D0%B0%D0%B7", http.Requests[0].PathAndQuery);
        Assert.Equal("/templates/4521", http.Requests[1].PathAndQuery); // по ID: название уже изменилось
        var body = http.Requests[1].Json;
        Assert.Equal("Новая", body.GetProperty("Subject").GetString());
        Assert.Equal("<p>h</p>", body.GetProperty("HtmlBody").GetString());
        Assert.Equal("t", body.GetProperty("TextBody").GetString());
        Assert.Equal("Заказ оформлен", result.Name);
    }
}

public class ModelTests
{
    [Fact]
    public void Extensible_enums_compare_case_insensitively_and_keep_unknown_values()
    {
        Assert.Equal(MessageStreamType.Transactional, new MessageStreamType("transactional"));
        Assert.True(MessageStreamType.Broadcasts == new MessageStreamType("BROADCASTS"));
        Assert.Equal(MessageStreamType.Inbound.GetHashCode(), new MessageStreamType("inbound").GetHashCode());
        Assert.NotEqual(BounceType.HardBounce, BounceType.SoftBounce);
        Assert.Equal("newtype", new BounceType("newtype").ToString());
        Assert.Equal("", default(ServerColor).ToString());
        Assert.True(BounceType.BlkBounce.IsBlock && BounceType.BulkBounce.IsBlock && !BounceType.HardBounce.IsBlock);
        Assert.True(BounceType.SpmBounce.IsSpam && BounceType.SpamBlock.IsSpam);
        Assert.Throws<ArgumentNullException>(() => new LinkTrackingMode(null!));
    }

    [Fact]
    public void Template_placeholders()
    {
        var model = TemplatePlaceholders.From(("name", "Иван"), ("{{order}}", "1042"), (" city ", "Москва"));

        Assert.Equal("Иван", model["{{name}}"]);
        Assert.Equal("1042", model["{{order}}"]);
        Assert.Equal("Москва", model["{{city}}"]);

        var custom = TemplatePlaceholders.From(new Dictionary<string, string> { ["name"] = "Анна" }, "%", "%");
        Assert.Equal("Анна", custom["%name%"]);
    }

    [Fact]
    public void Send_result_helpers()
    {
        var ok = new SendResult { ErrorCode = 0, MessageId = "a, b,,c" };
        Assert.True(ok.IsSuccess);
        Assert.Equal(["a", "b", "c"], ok.MessageIds);
        Assert.Empty(new SendResult { MessageId = null }.MessageIds);

        IReadOnlyList<SendResult> all = [ok, ok];
        Assert.Same(all, all.EnsureAllSucceeded());
    }

    [Fact]
    public void Attachments()
    {
        var path = Path.Combine(Path.GetTempPath(), $"haskimail-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "hi");
        try
        {
            var fromFile = Attachment.FromFile(path, "text/plain");
            using var stream = new MemoryStream("hi"u8.ToArray());
            var fromStream = Attachment.FromStream("a.txt", stream, "text/plain", "cid1");

            Assert.Equal(Path.GetFileName(path), fromFile.Name);
            Assert.Equal("aGk=", fromFile.Content);
            Assert.Equal("aGk=", fromStream.Content);
            Assert.Equal("cid1", fromStream.ContentId);
            Assert.Contains("exe", Attachment.ForbiddenExtensions);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Test_addresses_match_documentation()
    {
        Assert.Equal("hardbounce@bounce-testing.haskimail.ru", HaskimailTestAddresses.HardBounce);
        Assert.Equal("spamnotification@bounce-testing.haskimail.ru", HaskimailTestAddresses.SpamComplaint);
    }
}

public class DependencyInjectionTests
{
    [Fact]
    public async Task Registers_typed_client_from_delegate()
    {
        var http = new MockHttp();
        var services = new ServiceCollection();
        services.AddHaskimail(o => o.ServerToken = "di-token").ConfigurePrimaryHttpMessageHandler(() => http);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHaskimailClient>();
        await client.Templates.ListAsync();

        Assert.Equal("di-token", http.Last.Header("X-Haskimail-Server-Token"));
    }

    [Fact]
    public async Task Binds_options_from_configuration()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Haskimail:ServerToken"] = "cfg-server",
            ["Haskimail:AccountToken"] = "cfg-account",
            ["Haskimail:BaseUrl"] = "https://proxy.local",
            ["Haskimail:Timeout"] = "00:00:07",
            ["Haskimail:ValidateRequests"] = "false",
            ["Haskimail:Retry:MaxRetries"] = "4",
            ["Haskimail:Retry:BaseDelay"] = "00:00:00.010",
        }).Build();

        var options = new HaskimailClientOptions();
        HaskimailServiceCollectionExtensions.Bind(config.GetSection("Haskimail"), options);

        Assert.Equal("cfg-server", options.ServerToken);
        Assert.Equal("cfg-account", options.AccountToken);
        Assert.Equal(TimeSpan.FromSeconds(7), options.Timeout);
        Assert.False(options.ValidateRequests);
        Assert.Equal(4, options.Retry.MaxRetries);
        Assert.Equal(TimeSpan.FromMilliseconds(10), options.Retry.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), options.Retry.MaxDelay); // не задано — по умолчанию

        var http = new MockHttp();
        var services = new ServiceCollection();
        services.AddHaskimail(config.GetSection("Haskimail")).ConfigurePrimaryHttpMessageHandler(() => http);
        using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IHaskimailClient>().Domains.ListAsync();

        Assert.Equal("https://proxy.local/domains", http.Last.Uri.ToString());
        Assert.Equal("cfg-account", http.Last.Header("X-Haskimail-Account-Token"));
    }

    [Fact]
    public void Invalid_configuration_value_names_the_key()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Timeout"] = "soon" }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => HaskimailServiceCollectionExtensions.Bind(config, new HaskimailClientOptions()));
        Assert.Contains("Timeout", ex.Message);
    }

    [Fact]
    public void Missing_tokens_fail_on_resolve()
    {
        var services = new ServiceCollection();
        services.AddHaskimail(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IHaskimailClient>());
    }
}
