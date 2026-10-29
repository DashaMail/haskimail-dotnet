using System.Text;
using Haskimail.Models;

namespace Haskimail.Tests;

public class PagingTests
{
    private static string Messages(int total, int from, int count)
    {
        var items = Enumerable.Range(from, count).Select(i => $$"""{"Recipient":"u{{i}}@example.com","MessageID":"m{{i}}"}""");
        return $$"""{"TotalCount":{{total}},"Messages":[{{string.Join(",", items)}}]}""";
    }

    [Fact]
    public async Task Walks_pages_until_total_count()
    {
        var (client, http) = TestClient.Create();
        http.Respond(Messages(5, 0, 2)).Respond(Messages(5, 2, 2)).Respond(Messages(5, 4, 1));

        var all = new List<OutboundMessage>();
        await foreach (var m in client.Messages.SearchOutboundAllAsync(new OutboundMessageSearch { Count = 2, Tag = "orders" }))
            all.Add(m);

        Assert.Equal(["m0", "m1", "m2", "m3", "m4"], all.Select(m => m.MessageId));
        Assert.Equal(["/messages?count=2&offset=0&tag=orders", "/messages?count=2&offset=2&tag=orders", "/messages?count=2&offset=4&tag=orders"],
            http.Requests.Select(r => r.PathAndQuery));
    }

    [Fact]
    public async Task Stops_at_the_ten_thousand_window()
    {
        var (client, http) = TestClient.Create();
        http.Respond(Messages(20000, 9500, 400)).Respond(Messages(20000, 9900, 100));

        var count = 0;
        await foreach (var _ in client.Messages.SearchOutboundAllAsync(new OutboundMessageSearch { Count = 400, Offset = 9500 }))
            count++;

        Assert.Equal(500, count);
        Assert.Equal("/messages?count=100&offset=9900", http.Requests[1].PathAndQuery);
        Assert.Equal(2, http.Requests.Count);
    }

    [Fact]
    public async Task Stops_on_short_page()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""{"TotalCount":100,"Templates":[{"TemplateId":1,"Name":"a"}]}""");

        var list = new List<TemplateSummary>();
        await foreach (var t in client.Templates.ListAllAsync(pageSize: 10)) list.Add(t);

        Assert.Single(list);
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task Is_lazy_and_cancellable()
    {
        var (client, http) = TestClient.Create();
        http.DefaultBody = """{"TotalCount":1000,"Bounces":[{"Email":"a@b.ru"},{"Email":"c@d.ru"}]}""";

        var sequence = client.Bounces.ListAllAsync(new BounceSearch { Count = 2 });
        Assert.Empty(http.Requests); // ничего не запрошено, пока не начали перебор

        using var cts = new CancellationTokenSource();
        var seen = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in sequence.WithCancellation(cts.Token))
            {
                if (++seen == 3) cts.Cancel();
            }
        });
        Assert.Equal(2, http.Requests.Count);
    }

    [Fact]
    public async Task Other_collections_page_through_their_lists()
    {
        var (client, http) = TestClient.Create();
        http.Respond("""{"TotalCount":1,"Servers":[{"ID":1}]}""")
            .Respond("""{"TotalCount":1,"Domains":[{"ID":2}]}""")
            .Respond("""{"TotalCount":1,"InboundRules":[{"ID":3,"Rule":"x.ru"}]}""")
            .Respond("""{"TotalCount":1,"Opens":[{"MessageID":"o"}]}""")
            .Respond("""{"TotalCount":1,"Clicks":[{"MessageID":"c"}]}""")
            .Respond("""{"TotalCount":1,"InboundMessages":[{"MessageID":"i"}]}""");

        Assert.Equal(1, (await ToListAsync(client.Servers.ListAllAsync()))[0].Id);
        Assert.Equal(2, (await ToListAsync(client.Domains.ListAllAsync()))[0].Id);
        Assert.Equal(3, (await ToListAsync(client.InboundRules.ListAllAsync()))[0].Id);
        Assert.Equal("o", (await ToListAsync(client.Messages.ListAllOpensAsync()))[0].MessageId);
        Assert.Equal("c", (await ToListAsync(client.Messages.ListAllClicksAsync()))[0].MessageId);
        Assert.Equal("i", (await ToListAsync(client.Messages.SearchInboundAllAsync()))[0].MessageId);

        Assert.Equal(["/servers", "/domains", "/triggers/inboundrules", "/messages/opens", "/messages/clicks", "/messages/inbound"],
            http.Requests.Select(r => r.Uri.AbsolutePath));
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source) list.Add(item);
        return list;
    }
}
