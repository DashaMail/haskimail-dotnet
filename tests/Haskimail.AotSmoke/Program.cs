using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Haskimail;
using Haskimail.Models;
using Haskimail.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Нативный бинарник гоняет сериализацию и разбор всех основных моделей через подменённый HTTP.
var handler = new Replay();
var client = new HaskimailClient(new HttpClient(handler), new HaskimailClientOptions { ServerToken = "s", AccountToken = "a" });

handler.Next = """{"To":"ivan@example.com","SubmittedAt":"2026-10-07 14:30:12","MessageID":"6f1c","ErrorCode":0,"Message":"OK"}""";
var sent = await client.Email.SendAsync(new EmailMessage { From = "Магазин <shop@example.com>", To = "ivan@example.com", HtmlBody = "<p>Привет</p>", Metadata = new() { ["k"] = "v" } });
Check(sent.IsSuccess && sent.SubmittedAt!.Value.Offset == TimeSpan.FromHours(3), "send");
Check(handler.LastBody!.Contains("\"HtmlBody\":\"<p>Привет</p>\""), "send body");

handler.Next = """[{"To":"a@b.ru","ErrorCode":0,"MessageID":"1"},{"To":"c@d.ru","ErrorCode":6,"MessageID":null}]""";
var batch = await client.Email.SendBatchAsync(
[
    new EmailMessage { From = "a@b.ru", To = "a@b.ru", TextBody = "x" },
    new TemplatedEmailMessage { From = "a@b.ru", To = "c@d.ru", TemplateId = 1, TemplateModel = TemplatePlaceholders.From(("name", "Иван")) },
]);
Check(batch.GetFailures().Count == 1 && handler.LastBody!.Contains("\"TemplateModel\":{\"{{name}}\":\"Иван\"}"), "batch");

handler.Next = """{"TotalCount":1,"Servers":[{"ID":2459,"Color":"blue","TrackLinks":"None","DeliveryType":"Live","ApiTokens":["t"]}]}""";
var servers = await client.Servers.ListAsync();
Check(servers.Servers[0].Color == ServerColor.Blue, "servers");

handler.Next = """{"Days":[{"Date":"2026-10-05","Apple Mail":6}],"Apple Mail":6}""";
var clients = await client.Stats.GetOpenEmailClientsAsync();
Check(clients.Totals[0].Value == 6, "breakdown");

handler.Next = """{"TotalCount":1,"MessageStreams":[{"ID":"2480","ServerID":1,"MessageStreamType":"transactional","ArchivedAt":null}]}""";
var streams = await client.MessageStreams.ListAsync();
Check(streams.MessageStreams[0].MessageStreamType == MessageStreamType.Transactional, "streams");

handler.Next = """{"TotalCount":2,"Bounces":[{"Type":"HardBounce","BouncedAt":"2026-10-17 00:28:04"},{"Type":"SpmBounce"}]}""";
var bounces = new List<Bounce>();
await foreach (var b in client.Bounces.ListAllAsync()) bounces.Add(b);
Check(bounces.Count == 2 && bounces[1].Type!.Value.IsSpam, "paging");

handler.Status = HttpStatusCode.UnprocessableEntity;
handler.Next = """{"ErrorCode":701,"Message":"Сообщение не найдено"}""";
try { await client.Messages.GetOutboundDetailsAsync("x"); Check(false, "error"); }
catch (HaskimailValidationException ex) { Check(ex.ErrorCode == 701, "error"); }
handler.Status = HttpStatusCode.OK;

var e = HaskimailWebhooks.ParseEvent("""{"event":"CLICKED","recipient":"a@b.ru","message_id":"m","custom_vars":{"k":"v"},"stream_id":25,"SuppressSending":"true"}""");
Check(e.EventType == WebhookEventType.Clicked && e.CustomVars!["k"] == "v" && e.StreamId == "25", "webhook");
Check(HaskimailWebhooks.VerifySignature(e.Recipient, e.MessageId, HaskimailWebhooks.ComputeSignature("a@b.ru", "m", "key"), "key"), "signature");

var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Haskimail:ServerToken"] = "cfg" }).Build();
var services = new ServiceCollection();
services.AddHaskimail(config.GetSection("Haskimail")).ConfigurePrimaryHttpMessageHandler(() => handler);
using var provider = services.BuildServiceProvider();
handler.Next = """{"TotalCount":0,"Templates":[]}""";
await provider.GetRequiredService<IHaskimailClient>().Templates.ListAsync();
Check(handler.LastServerToken == "cfg", "di");

Console.WriteLine("AOT smoke: OK");
return 0;

static void Check(bool ok, string what)
{
    if (!ok) { Console.Error.WriteLine("AOT smoke FAILED: " + what); Environment.Exit(1); }
}

sealed class Replay : HttpMessageHandler
{
    public string Next { get; set; } = "{}";
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string? LastBody { get; private set; }
    public string? LastServerToken { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        LastServerToken = request.Headers.TryGetValues("X-Haskimail-Server-Token", out var v) ? v.First() : null;
        return new HttpResponseMessage(Status) { Content = new StringContent(Next, Encoding.UTF8, "application/json") };
    }
}
