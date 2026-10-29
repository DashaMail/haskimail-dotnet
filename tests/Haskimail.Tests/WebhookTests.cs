using System.Security.Cryptography;
using System.Text;
using Haskimail.Models;
using Haskimail.Webhooks;

namespace Haskimail.Tests;

/// <summary>Примеры тел вебхуков — из документации Haskimail, вместе с её несостыковками.</summary>
public class WebhookTests
{
    private const string Delivery = """
        {"event":"delivered","recipient":"ivan@example.com","email":"ivan@example.com","message_id":"a41d7c9e2b6f4e0d8c3b5a1f9e7d2c60",
         "code":"","description":"","error":"","bounce_category":"","user_id":"48213","campaign_id":"orders","member_id":"0",
         "event_time":"2026-10-07 14:30:15","event_time_utc":"2026-10-07 11:30:15","secret":"5f2b0c1d9a7e4c3b8d6f1a2e3c4b5d6e",
         "custom_vars":"{\"order_id\":\"1042\",\"n\":5}","server_id":11834,"stream_id":"2081"}
        """;

    private const string Bounce = """
        {"event":"bounced","recipient":"old-address@example.com","email":"old-address@example.com","message_id":"a41d",
         "code":"550","description":"550 5.1.1 The email account that you tried to reach does not exist",
         "error":"550 5.1.1 The email account that you tried to reach does not exist","bounce_category":"hrd",
         "user_id":"48213","campaign_id":"orders","member_id":"0","event_time":"2026-10-07 14:30:15","event_time_utc":"2026-10-07 11:30:15",
         "secret":"x","custom_vars":"{\"order_id\":\"1042\"}","server_id":11834,"stream_id":"2081"}
        """;

    private const string Click = """
        {"event":"CLICKED","message_id":"883953f4-6105-42a2-a16a-77a8eac79483","campaign_id":"Test",
         "custom_vars" : {"a_key" : "a_value","b_key": "b_value"},"server_id": 23,"stream_id": 25,
         "recipient": "ivan@example.com","event_time": "2019-11-05 16:33:54","url_original": "https://haskimail.ru","url_pers": "https://haskimail.ru"}
        """;

    private const string Complaint = """
        {"event":"COMPLAINED","message_id":"883953f4","campaign_id":"Test","custom_vars":{"a_key":"a_value"},"server_id":23,"stream_id":25,
         "recipient":"ivan@example.com","event_time":"2019-11-05 16:33:54"}
        """;

    private const string SubscriptionChange = """
        {"event":"subscriptionchange","recipient":"angry@example.com","email":"angry@example.com","message_id":"a41d","code":"",
         "user_id":"48213","campaign_id":"orders","member_id":"0","event_time":"2026-10-07 14:30:15","event_time_utc":"2026-10-07 11:30:15",
         "secret":"x","custom_vars":"{\"order_id\":\"1042\"}","server_id":11834,"stream_id":"2081",
         "SuppressionReason":"SpamComplaint","SuppressSending":"true"}
        """;

    private const string Inbound = """
        {"FromName":"Иван Петров","MessageStream":"2081","From":"ivan@example.com",
         "FromFull":{"Email":"ivan@example.com","Name":"Иван Петров","MailboxHash":""},
         "To":"\"Поддержка\" <451d+ticket-42@inbound.haskimail.ru>",
         "ToFull":[{"Email":"451d+ticket-42@inbound.haskimail.ru","Name":"Поддержка","MailboxHash":"ticket-42"}],
         "Cc":"","CcFull":[],"Bcc":"","BccFull":[],"OriginalRecipient":"451d+ticket-42@inbound.haskimail.ru","Subject":"Re: Заказ №42",
         "MessageID":"22c74902-a0c1-4511-804f-341342852c90","ReplyTo":"","MailboxHash":"ticket-42","Date":"Mon, 29 Sep 2026 14:05:13 +0300",
         "TextBody":"Спасибо, всё получил!","HtmlBody":"<p>Спасибо, всё получил!</p>","StrippedTextReply":"Спасибо, всё получил!","Tag":"",
         "Headers":[{"Name":"X-Spam-Score","Value":"0.3"}],
         "Attachments":[{"Name":"check.txt","Content":"aGk=","ContentType":"text/plain","ContentLength":2,"ContentID":""}]}
        """;

    [Fact]
    public void Delivery_event()
    {
        var e = HaskimailWebhooks.ParseEvent(Delivery);

        Assert.Equal(WebhookEventType.Delivered, e.EventType);
        Assert.Equal("1042", e.CustomVars!["order_id"]);
        Assert.Equal("5", e.CustomVars["n"]);
        Assert.Equal(11834, e.ServerId);
        Assert.Equal("2081", e.StreamId);
        Assert.Equal(e.EventTime, e.EventTimeUtc); // один и тот же момент, МСК и UTC
        Assert.Equal(TimeSpan.FromHours(3), e.EventTime!.Value.Offset);
    }

    [Fact]
    public void Bounce_event()
    {
        var e = HaskimailWebhooks.ParseEvent(Bounce);

        Assert.Equal(WebhookEventType.Bounced, e.EventType);
        Assert.Equal(BounceCategories.Hard, e.BounceCategory);
        Assert.Equal("550", e.Code);
        Assert.StartsWith("550 5.1.1", e.Error);
    }

    [Fact]
    public void Click_event_with_object_custom_vars_and_numeric_stream()
    {
        var e = HaskimailWebhooks.ParseEvent(Click);

        Assert.Equal(WebhookEventType.Clicked, e.EventType);
        Assert.Equal("a_value", e.CustomVars!["a_key"]);
        Assert.Equal("25", e.StreamId);
        Assert.Equal("https://haskimail.ru", e.UrlOriginal);
        Assert.Null(e.EventTimeUtc);
    }

    [Fact]
    public void Complaint_and_subscription_change()
    {
        var complaint = HaskimailWebhooks.ParseEvent(Complaint);
        var change = HaskimailWebhooks.ParseEvent(SubscriptionChange);

        Assert.Equal(WebhookEventType.SpamComplaint, complaint.EventType);
        Assert.Equal(WebhookEventType.SubscriptionChange, change.EventType);
        Assert.True(change.SuppressSending);
        Assert.Equal(SuppressionReason.SpamComplaint, change.SuppressionReason);
    }

    [Fact]
    public void Tolerates_odd_values()
    {
        var e = HaskimailWebhooks.ParseEvent("""{"event":"something-new","server_id":"11834","custom_vars":"","code":550,"SuppressSending":"0","event_time":""}""");

        Assert.Equal(WebhookEventType.Unknown, e.EventType);
        Assert.Equal("something-new", e.Event);
        Assert.Equal(11834, e.ServerId);
        Assert.Null(e.CustomVars);
        Assert.Equal("550", e.Code);
        Assert.False(e.SuppressSending);
        Assert.Null(e.EventTime);
    }

    [Fact]
    public void Array_of_events()
    {
        var events = HaskimailWebhooks.ParseEvents("[" + Delivery + "," + Click + "]");
        var single = HaskimailWebhooks.ParseEvents("  " + Bounce);

        Assert.Equal([WebhookEventType.Delivered, WebhookEventType.Clicked], events.Select(e => e.EventType));
        Assert.Single(single);
    }

    [Fact]
    public void Invalid_body_throws_haskimail_exception()
    {
        Assert.Throws<HaskimailException>(() => HaskimailWebhooks.ParseEvent("not json"));
        Assert.Throws<HaskimailException>(() => HaskimailWebhooks.ParseEvent(" "));
        Assert.Throws<HaskimailException>(() => HaskimailWebhooks.ParseInbound("null"));
    }

    [Fact]
    public void Signature_is_md5_of_recipient_message_id_and_key()
    {
        var expected = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("ivan@example.coma41dключ"))).ToLowerInvariant();

        Assert.Equal(expected, HaskimailWebhooks.ComputeSignature("ivan@example.com", "a41d", "ключ"));

        var e = new WebhookEvent { Recipient = "ivan@example.com", MessageId = "a41d", Secret = expected.ToUpperInvariant() };
        Assert.True(HaskimailWebhooks.VerifySignature(e, "ключ"));
        Assert.False(HaskimailWebhooks.VerifySignature(e, "другой"));
        Assert.False(HaskimailWebhooks.VerifySignature(new WebhookEvent { Recipient = "x" }, "ключ"));
        Assert.True(HaskimailWebhooks.VerifySignature("x", null,
            Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("xключ"))), "ключ")); // пустой message_id
    }

    [Fact]
    public void Deduplication_key()
    {
        var a = HaskimailWebhooks.ParseEvent(Delivery);
        var b = HaskimailWebhooks.ParseEvent(Delivery);
        var c = HaskimailWebhooks.ParseEvent(Bounce);

        Assert.Equal(a.DeduplicationKey, b.DeduplicationKey);
        Assert.NotEqual(a.DeduplicationKey, c.DeduplicationKey);
    }

    [Fact]
    public async Task Inbound_message_from_stream()
    {
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(Inbound));

        var m = await HaskimailWebhooks.ParseInboundAsync(body);

        Assert.True(body.CanRead); // поток не закрыт — его можно прочитать ещё раз
        Assert.Equal("ticket-42", m.MailboxHash);
        Assert.Equal("Поддержка", m.ToFull[0].Name);
        Assert.Empty(m.CcFull);
        Assert.Equal("0.3", m.Headers[0].Value);
        Assert.Equal("hi", Encoding.UTF8.GetString(m.Attachments[0].GetBytes()));
        Assert.Null(m.RawEmail);
    }

    [Fact]
    public async Task Event_from_stream()
    {
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(Delivery));
        var e = await HaskimailWebhooks.ParseEventAsync(body);
        Assert.Equal("ivan@example.com", e.Recipient);
    }
}
