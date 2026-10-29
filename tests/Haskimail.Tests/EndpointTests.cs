using System.Reflection;
using Haskimail.Api;
using Haskimail.Models;

namespace Haskimail.Tests;

/// <summary>
/// Каждый метод SDK против документации: HTTP-метод, путь, строка запроса и ключ.
/// Отдельный тест проверяет, что в таблице нет пропусков — новый метод без строки здесь уронит сборку.
/// </summary>
public class EndpointTests
{
    private const string S = "server";
    private const string A = "account";

    public sealed record Endpoint(string Signature, string Method, string PathAndQuery, string Token, string Response, Func<IHaskimailClient, Task> Call)
    {
        public override string ToString() => Signature;
    }

    private static readonly EmailMessage Mail = new() { From = "shop@example.com", To = "ivan@example.com", TextBody = "Привет" };
    private static readonly TemplatedEmailMessage TemplatedMail = new() { From = "shop@example.com", To = "ivan@example.com", TemplateId = 4521 };

    public static readonly Endpoint[] All =
    [
        // Отправка
        new("IEmailApi.SendAsync(EmailMessage)", "POST", "/email", S, "{}", c => c.Email.SendAsync(Mail)),
        new("IEmailApi.SendBatchAsync(IEnumerable<EmailMessageBase>)", "POST", "/email/batch", S, "[]", c => c.Email.SendBatchAsync([Mail])),

        // Шаблоны
        new("ITemplatesApi.SendAsync(TemplatedEmailMessage)", "POST", "/email/withTemplate", S, "{}", c => c.Templates.SendAsync(TemplatedMail)),
        new("ITemplatesApi.SendBatchAsync(IEnumerable<TemplatedEmailMessage>)", "POST", "/email/batchWithTemplates", S, "[]", c => c.Templates.SendBatchAsync([TemplatedMail])),
        new("ITemplatesApi.PushAsync(PushTemplatesRequest)", "PUT", "/templates/push", A, "{}", c => c.Templates.PushAsync(new PushTemplatesRequest { SourceServerId = 1, DestinationServerId = 2 })),
        new("ITemplatesApi.GetAsync(Int64)", "GET", "/templates/4521", S, "{}", c => c.Templates.GetAsync(4521)),
        new("ITemplatesApi.GetAsync(String)", "GET", "/templates/%D0%B7%D0%B0%D0%BA%D0%B0%D0%B7%2F1", S, "{}", c => c.Templates.GetAsync("заказ/1")),
        new("ITemplatesApi.CreateAsync(CreateTemplateRequest)", "POST", "/templates", S, "{}", c => c.Templates.CreateAsync(new CreateTemplateRequest { Name = "Заказ" })),
        new("ITemplatesApi.UpdateAsync(String,UpdateTemplateRequest)", "PUT", "/templates/welcome", S, "{}", c => c.Templates.UpdateAsync("welcome", new UpdateTemplateRequest())),
        new("ITemplatesApi.UpdateAsync(Int64,UpdateTemplateRequest)", "PUT", "/templates/4521", S, "{}", c => c.Templates.UpdateAsync(4521, new UpdateTemplateRequest())),
        new("ITemplatesApi.ListAsync(Int32?,Int32?)", "GET", "/templates?count=10&offset=20", S, "{}", c => c.Templates.ListAsync(10, 20)),
        new("ITemplatesApi.DeleteAsync(String)", "DELETE", "/templates/welcome", S, "{}", c => c.Templates.DeleteAsync("welcome")),
        new("ITemplatesApi.DeleteAsync(Int64)", "DELETE", "/templates/4521", S, "{}", c => c.Templates.DeleteAsync(4521)),
        new("ITemplatesApi.ValidateAsync(ValidateTemplateRequest)", "POST", "/templates/validate", S, "{}", c => c.Templates.ValidateAsync(new ValidateTemplateRequest { Subject = "x" })),

        // Сообщения
        new("IMessagesApi.SearchOutboundAsync(OutboundMessageSearch)", "GET",
            "/messages?count=50&offset=5&recipient=a%2Bb%40c.ru&fromemail=shop%40example.com&tag=orders&subject=%D0%97%D0%B0%D0%BA%D0%B0%D0%B7%20%E2%84%961&messagestream=2081&fromdate=2026-10-01&todate=2026-10-02%2012%3A30%3A00",
            S, "{}", c => c.Messages.SearchOutboundAsync(new OutboundMessageSearch
            {
                Count = 50, Offset = 5, Recipient = "a+b@c.ru", FromEmail = "shop@example.com", Tag = "orders", Subject = "Заказ №1",
                MessageStream = "2081", FromDate = new DateTime(2026, 10, 1), ToDate = new DateTime(2026, 10, 2, 12, 30, 0),
            })),
        new("IMessagesApi.GetOutboundDetailsAsync(String)", "GET", "/messages/abc123/details", S, "{}", c => c.Messages.GetOutboundDetailsAsync("abc123")),
        new("IMessagesApi.ListOpensAsync(MessageEventSearch)", "GET", "/messages/opens?count=2&recipient=ivan%40example.com&fromdate=2026-10-01", S, "{}",
            c => c.Messages.ListOpensAsync(new MessageEventSearch { Count = 2, Recipient = "ivan@example.com", FromDate = new DateTime(2026, 10, 1) })),
        new("IMessagesApi.GetOpensForMessageAsync(String,MessageEventFilter)", "GET", "/messages/opens/abc123?tag=orders", S, "{}",
            c => c.Messages.GetOpensForMessageAsync("abc123", new MessageEventFilter { Tag = "orders" })),
        new("IMessagesApi.ListClicksAsync(MessageEventSearch)", "GET", "/messages/clicks?messagestream=2081&todate=2026-10-02", S, "{}",
            c => c.Messages.ListClicksAsync(new MessageEventSearch { MessageStream = "2081", ToDate = new DateTime(2026, 10, 2) })),
        new("IMessagesApi.GetClicksForMessageAsync(String,MessageEventFilter)", "GET", "/messages/clicks/abc123", S, "{}", c => c.Messages.GetClicksForMessageAsync("abc123")),
        new("IMessagesApi.SearchInboundAsync(InboundMessageSearch)", "GET",
            "/messages/inbound?count=10&mailboxhash=ticket-42&status=blocked&fromdate=2026-09-01T12%3A00%3A00", S, "{}",
            c => c.Messages.SearchInboundAsync(new InboundMessageSearch { Count = 10, MailboxHash = "ticket-42", Status = InboundMessageStatus.Blocked, FromDate = new DateTime(2026, 9, 1, 12, 0, 0) })),
        new("IMessagesApi.GetInboundDetailsAsync(String)", "GET", "/messages/inbound/22c7/details", S, "{}", c => c.Messages.GetInboundDetailsAsync("22c7")),
        new("IMessagesApi.BypassInboundAsync(String)", "PUT", "/messages/inbound/22c7/bypass", S, "{}", c => c.Messages.BypassInboundAsync("22c7")),
        new("IMessagesApi.RetryInboundAsync(String)", "PUT", "/messages/inbound/22c7/retry", S, "{}", c => c.Messages.RetryInboundAsync("22c7")),

        // Возвраты
        new("IBouncesApi.GetDeliveryStatsAsync()", "GET", "/deliverystats", S, "{}", c => c.Bounces.GetDeliveryStatsAsync()),
        new("IBouncesApi.ListAsync(BounceSearch)", "GET", "/bounces?count=5&type=HardBounce&emailFilter=old%40example.com&messageID=m1&messagestream=2081&fromdate=2026-10-01&todate=2026-10-31", S, "{}",
            c => c.Bounces.ListAsync(new BounceSearch { Count = 5, Type = BounceType.HardBounce, EmailFilter = "old@example.com", MessageId = "m1", MessageStream = "2081", FromDate = new DateTime(2026, 10, 1, 15, 0, 0), ToDate = new DateTime(2026, 10, 31) })),

        // Серверы
        new("IServersApi.GetAsync(Int64)", "GET", "/servers/2459", A, "{}", c => c.Servers.GetAsync(2459)),
        new("IServersApi.CreateAsync(CreateServerRequest)", "POST", "/servers", A, "{}", c => c.Servers.CreateAsync(new CreateServerRequest { Name = "Магазин" })),
        new("IServersApi.UpdateAsync(Int64,UpdateServerRequest)", "PUT", "/servers/2459", A, "{}", c => c.Servers.UpdateAsync(2459, new UpdateServerRequest { Name = "Новое" })),
        new("IServersApi.ListAsync(Int32?,Int32?)", "GET", "/servers?count=50", A, "{}", c => c.Servers.ListAsync(50)),
        new("IServersApi.DeleteAsync(Int64)", "DELETE", "/servers/2459", A, "{}", c => c.Servers.DeleteAsync(2459)),

        // Каналы
        new("IMessageStreamsApi.ListAsync()", "GET", "/message-streams", S, "{}", c => c.MessageStreams.ListAsync()),
        new("IMessageStreamsApi.GetAsync(String)", "GET", "/message-streams/2480", S, "{}", c => c.MessageStreams.GetAsync("2480")),
        new("IMessageStreamsApi.UpdateAsync(String,String)", "PATCH", "/message-streams/2480", S, "{}", c => c.MessageStreams.UpdateAsync("2480", "Заказы")),
        new("IMessageStreamsApi.CreateAsync(CreateMessageStreamRequest)", "POST", "/message-streams", S, "{}", c => c.MessageStreams.CreateAsync(new CreateMessageStreamRequest { Name = "Рассылки", MessageStreamType = MessageStreamType.Broadcasts })),
        new("IMessageStreamsApi.ArchiveAsync(String)", "POST", "/message-streams/2480/archive", S, "{}", c => c.MessageStreams.ArchiveAsync("2480")),
        new("IMessageStreamsApi.UnarchiveAsync(String)", "POST", "/message-streams/2480/unarchive", S, "{}", c => c.MessageStreams.UnarchiveAsync("2480")),

        // Домены
        new("IDomainsApi.ListAsync(Int32?,Int32?)", "GET", "/domains?offset=10", A, "{}", c => c.Domains.ListAsync(offset: 10)),
        new("IDomainsApi.GetAsync(Int64)", "GET", "/domains/23779", A, "{}", c => c.Domains.GetAsync(23779)),
        new("IDomainsApi.CreateAsync(String)", "POST", "/domains", A, "{}", c => c.Domains.CreateAsync("example.ru")),
        new("IDomainsApi.DeleteAsync(Int64)", "DELETE", "/domains/23779", A, "{}", c => c.Domains.DeleteAsync(23779)),
        new("IDomainsApi.VerifyDkimAsync(Int64)", "PUT", "/domains/23779/verifyDkim", A, "{}", c => c.Domains.VerifyDkimAsync(23779)),
        new("IDomainsApi.VerifySpfAsync(Int64)", "PUT", "/domains/23779/verifyspf", A, "{}", c => c.Domains.VerifySpfAsync(23779)),

        // Статистика
        new("IStatsApi.GetOverviewAsync(StatsFilter)", "GET", "/stats?fromdate=2026-10-01&todate=2026-10-31&messagestream=2480", S, "{}",
            c => c.Stats.GetOverviewAsync(new StatsFilter { FromDate = new DateTime(2026, 10, 1), ToDate = new DateTime(2026, 10, 31), MessageStream = "2480" })),
        new("IStatsApi.GetSentCountsAsync(StatsFilter)", "GET", "/stats/sends", S, "{}", c => c.Stats.GetSentCountsAsync()),
        new("IStatsApi.GetBounceCountsAsync(StatsFilter)", "GET", "/stats/bounces", S, "{}", c => c.Stats.GetBounceCountsAsync()),
        new("IStatsApi.GetSpamComplaintsAsync(StatsFilter)", "GET", "/stats/spam", S, "{}", c => c.Stats.GetSpamComplaintsAsync()),
        new("IStatsApi.GetOpenCountsAsync(StatsFilter)", "GET", "/stats/opens", S, "{}", c => c.Stats.GetOpenCountsAsync()),
        new("IStatsApi.GetClickCountsAsync(StatsFilter)", "GET", "/stats/clicks", S, "{}", c => c.Stats.GetClickCountsAsync()),
        new("IStatsApi.GetOpenPlatformsAsync(StatsFilter)", "GET", "/stats/opens/platforms", S, "{}", c => c.Stats.GetOpenPlatformsAsync()),
        new("IStatsApi.GetOpenEmailClientsAsync(StatsFilter)", "GET", "/stats/opens/emailclients", S, "{}", c => c.Stats.GetOpenEmailClientsAsync()),
        new("IStatsApi.GetClickBrowsersAsync(StatsFilter)", "GET", "/stats/clicks/browserfamilies", S, "{}", c => c.Stats.GetClickBrowsersAsync()),
        new("IStatsApi.GetClickPlatformsAsync(StatsFilter)", "GET", "/stats/clicks/platforms", S, "{}", c => c.Stats.GetClickPlatformsAsync()),

        // Вебхуки
        new("IWebhooksApi.ListAsync()", "GET", "/webhooks", S, "{}", c => c.Webhooks.ListAsync()),
        new("IWebhooksApi.GetAsync(Int64)", "GET", "/webhooks/905", S, "{}", c => c.Webhooks.GetAsync(905)),
        new("IWebhooksApi.CreateAsync(CreateWebhookRequest)", "POST", "/webhooks", S, "{}", c => c.Webhooks.CreateAsync(new CreateWebhookRequest { Url = "https://example.com/h" })),
        new("IWebhooksApi.UpdateAsync(Int64,UpdateWebhookRequest)", "PUT", "/webhooks/905", S, "{}", c => c.Webhooks.UpdateAsync(905, new UpdateWebhookRequest { Url = "https://example.com/h2" })),
        new("IWebhooksApi.DeleteAsync(Int64)", "DELETE", "/webhooks/905", S, "{}", c => c.Webhooks.DeleteAsync(905)),

        // Правила для входящих
        new("IInboundRulesApi.ListAsync(Int32?,Int32?)", "GET", "/triggers/inboundrules?count=100", S, "{}", c => c.InboundRules.ListAsync(100)),
        new("IInboundRulesApi.CreateAsync(String)", "POST", "/triggers/inboundrules", S, "{}", c => c.InboundRules.CreateAsync("spam@example.org")),
        new("IInboundRulesApi.DeleteAsync(Int64)", "DELETE", "/triggers/inboundrules/5", S, "{}", c => c.InboundRules.DeleteAsync(5)),

        // Стоп-листы
        new("ISuppressionsApi.ListAsync(String)", "GET", "/message-streams/2480/suppressions/dump", S, "{}", c => c.Suppressions.ListAsync("2480")),
        new("ISuppressionsApi.AddAsync(String,IEnumerable<String>)", "POST", "/message-streams/2480/suppressions", S, "{}", c => c.Suppressions.AddAsync("2480", ["a@b.ru"])),
        new("ISuppressionsApi.DeleteAsync(String,IEnumerable<String>)", "POST", "/message-streams/2480/suppressions/delete", S, "{}", c => c.Suppressions.DeleteAsync("2480", ["a@b.ru"])),
    ];

    public static TheoryData<Endpoint> Cases()
    {
        var data = new TheoryData<Endpoint>();
        foreach (var e in All) data.Add(e);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Calls_documented_endpoint(Endpoint endpoint)
    {
        var (client, http) = TestClient.Create();
        http.DefaultBody = endpoint.Response;

        await endpoint.Call(client);

        var request = Assert.Single(http.Requests);
        Assert.Equal(endpoint.Method, request.Method.Method);
        Assert.Equal(endpoint.PathAndQuery, request.PathAndQuery);
        Assert.Equal("application/json", request.Header("Accept"));

        if (endpoint.Token == S)
        {
            Assert.Equal(TestClient.ServerToken, request.Header("X-Haskimail-Server-Token"));
            Assert.Null(request.Header("X-Haskimail-Account-Token"));
        }
        else
        {
            Assert.Equal(TestClient.AccountToken, request.Header("X-Haskimail-Account-Token"));
            Assert.Null(request.Header("X-Haskimail-Server-Token"));
        }

        if (request.Body != null) Assert.Equal("application/json; charset=utf-8", request.ContentType);
    }

    [Fact]
    public void Every_public_api_method_is_covered()
    {
        var api = typeof(IHaskimailClient).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t.Namespace == "Haskimail.Api")
            .SelectMany(t => t.GetMethods().Select(m => (Type: t, Method: m)))
            // Автопагинация проверяется в PagingTests, изменение шаблона через делегат — в ModelTests.
            .Where(x => !x.Method.ReturnType.Name.StartsWith("IAsyncEnumerable", StringComparison.Ordinal))
            .Where(x => !x.Method.GetParameters().Any(p => p.ParameterType.Name.StartsWith("Action", StringComparison.Ordinal)))
            .Select(x => Signature(x.Type, x.Method))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        var covered = All.Select(e => e.Signature).OrderBy(s => s, StringComparer.Ordinal).ToArray();

        Assert.Equal(api, covered);
        // 61 метод справочника API Haskimail + у шаблонов Get/Update/Delete есть перегрузки по ID и по названию.
        Assert.Equal(61 + 3, covered.Length);
    }

    [Fact]
    public async Task Methods_accepting_both_keys_fall_back_to_account_key()
    {
        var (client, http) = TestClient.Create(o => o.ServerToken = null);

        await client.Stats.GetOverviewAsync();
        await client.Messages.SearchOutboundAsync();

        Assert.All(http.Requests, r =>
        {
            Assert.Equal(TestClient.AccountToken, r.Header("X-Haskimail-Account-Token"));
            Assert.Null(r.Header("X-Haskimail-Server-Token"));
        });
    }

    [Fact]
    public async Task Templates_can_be_managed_with_account_key_only()
    {
        var (client, http) = TestClient.Create(o => o.ServerToken = null);

        await client.Templates.ListAsync();
        await client.Templates.GetAsync(4521);
        await client.Templates.UpdateAsync(4521, new UpdateTemplateRequest());
        await client.Templates.DeleteAsync("welcome");
        await client.Templates.CreateAsync(new CreateTemplateRequest { Name = "Заказ", AssociatedServerId = 2459 });

        Assert.Equal(5, http.Requests.Count);
        Assert.All(http.Requests, r => Assert.Equal(TestClient.AccountToken, r.Header("X-Haskimail-Account-Token")));
    }

    [Fact]
    public async Task Inbound_messages_require_server_key()
    {
        var (client, http) = TestClient.Create(o => o.ServerToken = null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Messages.SearchInboundAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Messages.GetInboundDetailsAsync("22c7"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Messages.BypassInboundAsync("22c7"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Messages.RetryInboundAsync("22c7"));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.Messages.SearchInboundAllAsync()) { }
        });

        Assert.Empty(http.Requests); // запрос с ключом аккаунта сервер отклонил бы с 401
    }

    [Fact]
    public async Task Missing_required_key_fails_before_request()
    {
        var (accountOnly, http1) = TestClient.Create(o => o.ServerToken = null);
        var (serverOnly, http2) = TestClient.Create(o => o.AccountToken = null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => accountOnly.Email.SendAsync(Mail));
        await Assert.ThrowsAsync<InvalidOperationException>(() => serverOnly.Domains.ListAsync());

        Assert.Empty(http1.Requests);
        Assert.Empty(http2.Requests);
    }

    private static string Signature(Type type, MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Where(p => p.ParameterType != typeof(CancellationToken))
            .Select(p => TypeName(p.ParameterType));
        return $"{type.Name}.{method.Name}({string.Join(",", parameters)})";
    }

    private static string TypeName(Type t)
    {
        if (Nullable.GetUnderlyingType(t) is { } inner) return TypeName(inner) + "?";
        if (!t.IsGenericType) return t.Name;
        var name = t.Name[..t.Name.IndexOf('`')];
        return $"{name}<{string.Join(",", t.GetGenericArguments().Select(TypeName))}>";
    }
}
