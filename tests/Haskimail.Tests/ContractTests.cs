using System.Net;
using Haskimail.Models;

namespace Haskimail.Tests;

/// <summary>Разбор реальных (обезличенных) ответов API — см. Fixtures/README.md.</summary>
public class ContractTests
{
    private static readonly TimeSpan Msk = TimeSpan.FromHours(3);

    [Fact]
    public async Task Servers()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("servers.json");

        var list = await client.Servers.ListAsync();

        var server = Assert.Single(list.Servers);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal(2459, server.Id);
        Assert.Equal(ServerColor.Blue, server.Color);
        Assert.Equal(LinkTrackingMode.None, server.TrackLinks);
        Assert.Equal(DeliveryType.Live, server.DeliveryType);
        Assert.True(server.SmtpApiActivated);
        Assert.Equal("", server.InboundHookUrl);
        Assert.Single(server.ApiTokens);
        Assert.EndsWith("@inbound.haskimail.ru", server.InboundAddress);
    }

    [Fact]
    public async Task Message_streams_with_lowercase_types()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("message-streams.json");

        var list = await client.MessageStreams.ListAsync();

        Assert.Equal(2, list.MessageStreams.Count);
        Assert.Equal(MessageStreamType.Transactional, list.MessageStreams[0].MessageStreamType);
        Assert.Equal(MessageStreamType.Inbound, list.MessageStreams[1].MessageStreamType);
        Assert.Equal("transactional", list.MessageStreams[0].MessageStreamType.ToString()); // исходное значение сохраняется
        Assert.Equal("2480", list.MessageStreams[0].Id);
        Assert.False(list.MessageStreams[0].IsArchived);
    }

    [Fact]
    public async Task Empty_lists()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("templates-empty.json").RespondFixture("webhooks-empty.json")
            .RespondFixture("inbound-empty.json").RespondFixture("inbound-rules-empty.json");

        Assert.Empty((await client.Templates.ListAsync()).Templates);
        Assert.Empty((await client.Webhooks.ListAsync()).Webhooks);
        Assert.Empty((await client.Messages.SearchInboundAsync()).InboundMessages);
        Assert.Empty((await client.InboundRules.ListAsync()).InboundRules);
    }

    [Fact]
    public async Task Domains_and_dns_records()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("domains.json").RespondFixture("domain.json");

        var list = await client.Domains.ListAsync();
        var domain = await client.Domains.GetAsync(23779);

        Assert.Equal(2, list.TotalCount);
        Assert.False(list.Domains[0].DkimVerified);
        Assert.True(list.Domains[1].SpfVerified);
        Assert.True(domain.DkimVerified);
        Assert.Equal("dm._domainkey.example.ru.", domain.DkimTextValue!.Name);
        Assert.Equal("TXT", domain.DkimTextValue.RecordType);
        Assert.True(domain.DkimTextValue.IsValid);
        Assert.Contains("include:_spf.haskisender.ru", domain.SpfTextValue!.Value);
    }

    [Fact]
    public async Task Outbound_messages_with_emoji_and_moscow_time()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("messages.json").RespondFixture("message-details.json");

        var list = await client.Messages.SearchOutboundAsync(new OutboundMessageSearch { Count = 3 });
        var details = await client.Messages.GetOutboundDetailsAsync("32c5adc11df90c6d2a53a792dc6a22ea");

        Assert.Equal(71, list.TotalCount);
        Assert.Equal(3, list.Messages.Count);
        Assert.Equal("🛸 Вас выбрали для миссии на Марс (почти)", list.Messages[0].Subject);
        Assert.Equal(new DateTimeOffset(2026, 10, 28, 11, 58, 50, Msk), list.Messages[0].ReceivedAt);
        Assert.Equal("0", list.Messages[2].Tag);
        Assert.Equal("Sent", details.Status);
        Assert.Equal("32c5adc11df90c6d2a53a792dc6a22ea", details.MessageId);
    }

    [Fact]
    public async Task Opens_and_clicks()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("opens.json").RespondFixture("clicks.json");

        var opens = await client.Messages.ListOpensAsync();
        var clicks = await client.Messages.ListClicksAsync();

        var open = Assert.Single(opens.Opens);
        Assert.Equal("Open", open.RecordType);
        Assert.Equal("MailRuBot", open.Browser);
        Assert.Equal("26", open.Region);
        Assert.Equal("", open.From);
        Assert.Equal(2, clicks.Clicks.Count);
        Assert.Equal("https://haskimail.ru/orders/1042", clicks.Clicks[0].OriginalLink);
        Assert.Equal("CZ", clicks.Clicks[0].Country);
    }

    [Fact]
    public async Task Bounces_and_delivery_stats()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("bounces.json").RespondFixture("delivery-stats.json");

        var bounces = await client.Bounces.ListAsync();
        var stats = await client.Bounces.GetDeliveryStatsAsync();

        var bounce = Assert.Single(bounces.Bounces);
        Assert.Equal(BounceType.HardBounce, bounce.Type);
        Assert.Equal("675", bounce.TypeCode);
        Assert.Equal(2459, bounce.ServerId);
        Assert.Equal(new DateTimeOffset(2026, 10, 17, 0, 28, 4, Msk), bounce.BouncedAt);

        Assert.Equal(5, stats.Bounces.Count);
        Assert.Null(stats.Bounces[0].Type);
        Assert.Equal("All", stats.Bounces[0].Name);
        Assert.True(stats.Bounces[2].Type!.Value.IsBlock); // BulkBounce
        Assert.True(stats.Bounces[4].Type!.Value.IsSpam);  // SpamBlock
    }

    [Fact]
    public async Task Stats_overview_with_integer_rate_and_legacy_field()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("stats-overview.json");

        var overview = await client.Stats.GetOverviewAsync();

        Assert.Equal(71, overview.Sent);
        Assert.Equal(38.03, overview.OpenRate);
        Assert.Equal(0, overview.SpamComplaintsRate);
    }

    [Fact]
    public async Task Stats_by_day_and_platform()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("stats-sends.json").RespondFixture("stats-bounces.json").RespondFixture("stats-platforms.json");

        var sends = await client.Stats.GetSentCountsAsync();
        var bounces = await client.Stats.GetBounceCountsAsync();
        var platforms = await client.Stats.GetOpenPlatformsAsync();

        Assert.Equal(new DateTime(2026, 10, 21), sends.Days[2].Date);
        Assert.Equal(36, sends.Days[2].Sent);
        Assert.Equal(1, bounces.Days[0].HardBounce);
        Assert.Equal(17, platforms.WebMail);
        Assert.Equal(8, platforms.Days[2].WebMail);
    }

    [Fact]
    public async Task Stats_with_dynamic_keys()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("stats-emailclients.json").RespondFixture("stats-browsers.json");

        var clients = await client.Stats.GetOpenEmailClientsAsync();
        var browsers = await client.Stats.GetClickBrowsersAsync();

        Assert.Equal(4, clients.Days.Count);
        Assert.Equal(6, clients.Days[2].Counts["Apple Mail"]);
        Assert.Equal(new DateTime(2026, 10, 5), clients.Days[2].Date);
        Assert.Equal(["Yandex Mail", "Apple Mail", "Mail.ru", "Gmail"], clients.Totals.Select(t => t.Key));
        Assert.Equal(2, browsers.Totals.First(t => t.Key == "Chrome Mobile").Value);
    }

    [Fact]
    public async Task Suppressions()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("suppressions.json");

        var list = await client.Suppressions.ListAsync("2480");

        var s = Assert.Single(list.Suppressions);
        Assert.Equal(SuppressionReason.HardBounce, s.SuppressionReason);
        Assert.Equal(new DateTimeOffset(2026, 7, 22, 15, 30, 1, Msk), s.CreatedAt);
    }

    [Fact]
    public async Task Template_validation()
    {
        var (client, http) = TestClient.Create();
        http.RespondFixture("template-validation.json");

        var result = await client.Templates.ValidateAsync(new ValidateTemplateRequest
        {
            Subject = "Заказ {{order}} для {{name}}",
            TestRenderModel = TemplatePlaceholders.From(("order", "1042")),
        });

        Assert.False(result.AllContentIsValid);
        Assert.Equal("Заказ 1042 для {{name}}", result.Subject!.RenderedContent);
        Assert.Equal(2, result.HtmlBody!.ValidationErrors.Count);
        Assert.Equal(22, result.HtmlBody.ValidationErrors[1].CharacterPosition);
        Assert.Null(result.TextBody);
        Assert.Equal("name_Value", result.SuggestedTemplateModel!["{{name}}"]);
        Assert.Equal("1042", http.Last.Json.GetProperty("TestRenderModel").GetProperty("{{order}}").GetString());
    }

    [Fact]
    public async Task Real_error_responses()
    {
        var (client, http) = TestClient.Create();
        http.Respond((HttpStatusCode)422, Fixtures.Read("error-message-not-found.json"))
            .Respond(HttpStatusCode.NotFound, Fixtures.Read("error-template-not-found.json"));

        var notFoundMessage = await Assert.ThrowsAsync<HaskimailValidationException>(() => client.Messages.GetOutboundDetailsAsync("nope"));
        var notFoundTemplate = await Assert.ThrowsAsync<HaskimailNotFoundException>(() => client.Templates.GetAsync(999999999));

        Assert.Equal(HaskimailErrorCodes.MessageNotFound, notFoundMessage.ErrorCode);
        Assert.Equal("Сообщение не найдено", notFoundMessage.ApiMessage);
        Assert.Equal(HaskimailErrorCodes.TemplateNotFound, notFoundTemplate.ErrorCode);
        Assert.Equal("GET", notFoundTemplate.RequestMethod);
        Assert.Equal("/templates/999999999", notFoundTemplate.RequestPath);
    }
}
