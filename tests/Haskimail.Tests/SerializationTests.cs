using System.Text.Json;
using Haskimail.Models;

namespace Haskimail.Tests;

/// <summary>Форма тел запросов: имена полей, пропуск null, значения enum, полиморфные пакеты.</summary>
public class SerializationTests
{
    [Fact]
    public async Task Email_body_uses_api_field_names_and_skips_nulls()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""{"To":"ivan@example.com","SubmittedAt":"2026-10-07 14:30:12","MessageID":"6f1c","ErrorCode":0,"Message":"OK"}""");

        var result = await client.Email.SendAsync(new EmailMessage
        {
            From = "Магазин <shop@example.com>",
            To = "ivan@example.com",
            Subject = "Заказ №1042",
            HtmlBody = "<p>Привет & пока</p>",
            Headers = [new("X-Order-Id", "1042")],
            Metadata = new() { ["order_id"] = "1042" },
            TrackLinks = true,
            TrackOpens = false,
            MessageStream = "2081",
            Attachments = [Attachment.FromBytes("a.txt", "hi"u8.ToArray(), "text/plain", "cid:logo")],
        });

        var body = http.Last.Json;
        Assert.Contains("Магазин", http.Last.Body);            // кириллица без \uXXXX
        Assert.Contains("<p>Привет & пока</p>", http.Last.Body); // HTML без экранирования
        Assert.Equal("1042", body.GetProperty("Headers")[0].GetProperty("Value").GetString());
        Assert.Equal("1042", body.GetProperty("Metadata").GetProperty("order_id").GetString());
        Assert.True(body.GetProperty("TrackLinks").GetBoolean());
        Assert.False(body.GetProperty("TrackOpens").GetBoolean());
        Assert.Equal("aGk=", body.GetProperty("Attachments")[0].GetProperty("Content").GetString());
        Assert.Equal("cid:logo", body.GetProperty("Attachments")[0].GetProperty("ContentID").GetString());
        Assert.False(body.TryGetProperty("TextBody", out _));
        Assert.False(body.TryGetProperty("Cc", out _));
        Assert.False(body.TryGetProperty("$type", out _));

        Assert.True(result.IsSuccess);
        Assert.Equal("6f1c", result.MessageId);
        Assert.Equal(new DateTimeOffset(2026, 10, 7, 14, 30, 12, TimeSpan.FromHours(3)), result.SubmittedAt);
    }

    [Fact]
    public async Task Batch_mixes_plain_and_templated_messages()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""
            [{"To":"ivan@example.com","SubmittedAt":"2026-10-13 14:30:12","MessageID":"id1","ErrorCode":0,"Message":"OK"},
             {"To":"anna@example.com","SubmittedAt":"2026-10-13 14:30:12","MessageID":null,"ErrorCode":6,"Message":"Invalid recipient email address"}]
            """);

        var results = await client.Email.SendBatchAsync(
        [
            new EmailMessage { From = "a@b.ru", To = "ivan@example.com", TextBody = "hi" },
            new TemplatedEmailMessage { From = "a@b.ru", To = "anna@example.com", TemplateAlias = "welcome", TemplateModel = TemplatePlaceholders.From(("name", "Анна")) },
        ]);

        var body = http.Last.Json;
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal("hi", body[0].GetProperty("TextBody").GetString());
        Assert.Equal("welcome", body[1].GetProperty("TemplateAlias").GetString());
        Assert.Equal("Анна", body[1].GetProperty("TemplateModel").GetProperty("{{name}}").GetString());
        Assert.False(body[0].TryGetProperty("$type", out _));

        Assert.True(results[0].IsSuccess);
        Assert.False(results[1].IsSuccess);
        Assert.Null(results[1].MessageId);
        var failure = Assert.Single(results.GetFailures());
        Assert.Equal(1, failure.Index);
        var ex = Assert.Throws<HaskimailBatchException>(() => results.EnsureAllSucceeded());
        Assert.Contains("1 из 2", ex.Message);
    }

    [Fact]
    public async Task Batch_with_templates_is_wrapped_in_messages()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""[{"To":"x@y.ru","ErrorCode":0,"Message":"OK","MessageID":"a, b"}]""");

        var results = await client.Templates.SendBatchAsync([new TemplatedEmailMessage { From = "a@b.ru", To = "x@y.ru", TemplateId = 4521 }]);

        Assert.Equal(4521, http.Last.Json.GetProperty("Messages")[0].GetProperty("TemplateId").GetInt64());
        Assert.Equal(["a", "b"], results[0].MessageIds);
    }

    [Fact]
    public async Task Server_requests_write_extensible_enums_as_strings()
    {
        var (client, http) = TestClient.Create();

        await client.Servers.CreateAsync(new CreateServerRequest { Name = "Магазин", TrackLinks = LinkTrackingMode.HtmlAndText, DeliveryType = DeliveryType.Sandbox, InboundSpamThreshold = 10 });
        var create = http.Last.Json;
        await client.Servers.UpdateAsync(1, new UpdateServerRequest { Color = ServerColor.Green, EnableSmtpApiErrorHooks = true });
        var update = http.Last.Json;

        Assert.Equal("purple", create.GetProperty("Color").GetString());
        Assert.Equal("HtmlAndText", create.GetProperty("TrackLinks").GetString());
        Assert.Equal("Sandbox", create.GetProperty("DeliveryType").GetString());
        Assert.Equal(10, create.GetProperty("InboundSpamThreshold").GetInt32());
        Assert.False(create.TryGetProperty("SmtpApiActivated", out _));

        Assert.Equal("green", update.GetProperty("Color").GetString());
        Assert.True(update.GetProperty("EnableSmtpApiErrorHooks").GetBoolean());
        Assert.False(update.TryGetProperty("Name", out _));
        Assert.False(update.TryGetProperty("TrackLinks", out _));
    }

    [Fact]
    public async Task Stream_and_template_requests()
    {
        var (client, http) = TestClient.Create();

        await client.MessageStreams.CreateAsync(new CreateMessageStreamRequest { Name = "Рассылки", MessageStreamType = MessageStreamType.Broadcasts });
        var stream = http.Last.Json;
        await client.MessageStreams.UpdateAsync("2480", "Заказы");
        var rename = http.Last.Json;
        await client.Templates.PushAsync(new PushTemplatesRequest { SourceServerId = 1, DestinationServerId = 2 });
        var push = http.Last.Json;
        await client.Domains.CreateAsync(" example.ru ");
        var domain = http.Last.Json;
        await client.InboundRules.CreateAsync("spam@example.org");
        var rule = http.Last.Json;

        Assert.Equal("Broadcasts", stream.GetProperty("MessageStreamType").GetString());
        Assert.False(stream.TryGetProperty("ServerID", out _));
        Assert.Equal("Заказы", rename.GetProperty("Name").GetString());
        Assert.Equal(1, push.GetProperty("SourceServerID").GetInt32());
        Assert.False(push.GetProperty("PerformChanges").GetBoolean());
        Assert.Equal("example.ru", domain.GetProperty("Name").GetString());
        Assert.Equal("spam@example.org", rule.GetProperty("Rule").GetString());
    }

    [Fact]
    public async Task Webhook_triggers_send_only_what_was_set()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""{"ID":905,"Url":"https://example.com/h","MessageStream":"2081","Triggers":{"Delivery":{"Enabled":true},"Bounce":{"Enabled":true},"SpamComplaint":{"Enabled":false},"Open":{"Enabled":false},"Click":{"Enabled":false},"SubscriptionChange":{"Enabled":false}}}""");

        var webhook = await client.Webhooks.CreateAsync(new CreateWebhookRequest
        {
            Url = "https://example.com/h",
            Triggers = new WebhookTriggers { Delivery = WebhookTrigger.On, Bounce = WebhookTrigger.On },
        });

        var triggers = http.Last.Json.GetProperty("Triggers");
        Assert.True(triggers.GetProperty("Delivery").GetProperty("Enabled").GetBoolean());
        Assert.False(triggers.TryGetProperty("Open", out _));
        Assert.False(http.Last.Json.TryGetProperty("MessageStream", out _));
        Assert.Equal(905, webhook.Id);
        Assert.False(webhook.Triggers.Open!.Enabled);
    }

    [Fact]
    public async Task Suppressions_body()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""{"Suppressions":[{"EmailAddress":"a@b.ru","Status":"Suppressed","Message":""},{"EmailAddress":"bad","Status":"Failed","Message":"An invalid email address was provided."}]}""");

        var result = await client.Suppressions.AddAsync("2480", [" a@b.ru ", "bad"]);

        var entries = http.Last.Json.GetProperty("Suppressions");
        Assert.Equal("a@b.ru", entries[0].GetProperty("EmailAddress").GetString());
        Assert.False(result.Suppressions[0].IsFailed);
        Assert.True(result.Suppressions[1].IsFailed);
    }

    [Fact]
    public async Task Unknown_fields_and_enum_values_do_not_break_parsing()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""{"ID":"1","ServerID":2,"Name":"X","MessageStreamType":"newsletters","ArchivedAt":"2026-10-07 15:00:00","SomethingNew":{"a":1}}""");

        var stream = await client.MessageStreams.GetAsync("1");

        Assert.Equal("newsletters", stream.MessageStreamType.ToString());
        Assert.NotEqual(MessageStreamType.Broadcasts, stream.MessageStreamType);
        Assert.True(stream.IsArchived);
    }
}
