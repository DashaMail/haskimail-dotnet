using System.Net;
using System.Text;
using System.Text.Json;

namespace Haskimail.Tests;

/// <summary>Записанный запрос (тело читается сразу — сам запрос после отправки освобождается).</summary>
internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string? Body, string? ContentType)
{
    public string PathAndQuery => Uri.PathAndQuery;

    public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;

    public JsonElement Json => JsonDocument.Parse(Body ?? "null").RootElement;
}

/// <summary>Подменённый HTTP: отдаёт ответы из очереди и записывает запросы.</summary>
internal sealed class MockHttp : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public RecordedRequest Last => Requests[^1];

    /// <summary>Ответ по умолчанию, когда очередь пуста.</summary>
    public string DefaultBody { get; set; } = "{}";

    public MockHttp Respond(HttpStatusCode status, string body, Action<HttpResponseMessage>? configure = null)
    {
        _responses.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            configure?.Invoke(response);
            return Task.FromResult(response);
        });
        return this;
    }

    public MockHttp Respond(string body) => Respond(HttpStatusCode.OK, body);

    public MockHttp RespondFixture(string fixture) => Respond(Fixtures.Read(fixture));

    public MockHttp Throw(Exception exception)
    {
        _responses.Enqueue((_, _) => Task.FromException<HttpResponseMessage>(exception));
        return this;
    }

    /// <summary>Ответ, который «висит», пока запрос не отменят (для проверки таймаутов).</summary>
    public MockHttp Hang()
    {
        _responses.Enqueue(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("недостижимо");
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, headers, body, request.Content?.Headers.ContentType?.ToString()));

        if (_responses.Count > 0) return await _responses.Dequeue()(request, cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(DefaultBody, Encoding.UTF8, "application/json") };
    }
}

internal static class TestClient
{
    public const string ServerToken = "server-token";
    public const string AccountToken = "account-token";

    /// <summary>Клиент с обоими ключами и быстрыми повторами.</summary>
    public static (HaskimailClient Client, MockHttp Http) Create(Action<HaskimailClientOptions>? configure = null)
    {
        var http = new MockHttp();
        var options = new HaskimailClientOptions
        {
            ServerToken = ServerToken,
            AccountToken = AccountToken,
            Retry = new HaskimailRetryOptions { MaxRetries = 2, BaseDelay = TimeSpan.FromMilliseconds(1), MaxDelay = TimeSpan.FromMilliseconds(20) },
        };
        configure?.Invoke(options);
        return (new HaskimailClient(new HttpClient(http), options), http);
    }
}

internal static class Fixtures
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
