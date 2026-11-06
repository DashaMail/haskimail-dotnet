using System.Net;
using System.Net.Http.Headers;
using Haskimail.Models;

namespace Haskimail.Tests;

public class ErrorAndRetryTests
{
    private static readonly EmailMessage Mail = new() { From = "a@b.ru", To = "c@d.ru", TextBody = "x" };

    [Theory]
    [InlineData(401, typeof(HaskimailAuthenticationException))]
    [InlineData(403, typeof(HaskimailForbiddenException))]
    [InlineData(404, typeof(HaskimailNotFoundException))]
    [InlineData(413, typeof(HaskimailRequestTooLargeException))]
    [InlineData(422, typeof(HaskimailValidationException))]
    [InlineData(429, typeof(HaskimailRateLimitException))]
    [InlineData(500, typeof(HaskimailServerException))]
    [InlineData(502, typeof(HaskimailServerException))]
    [InlineData(400, typeof(HaskimailApiException))]
    public async Task Status_maps_to_exception_type(int status, Type expected)
    {
        var (client, http) = TestClient.Create(o => o.Retry = HaskimailRetryOptions.None);
        http.Respond((HttpStatusCode)status, """{"ErrorCode":10,"Message":"Bad key"}""");

        var ex = await Assert.ThrowsAnyAsync<HaskimailApiException>(() => client.Templates.ListAsync());

        Assert.IsType(expected, ex);
        Assert.Equal(status, (int)ex.StatusCode);
        Assert.Equal(10, ex.ErrorCode);
        Assert.Equal("Bad key", ex.ApiMessage);
        Assert.Contains("[GET /templates]", ex.Message);
    }

    [Theory]
      [InlineData(404, 0)]
      [InlineData(422, HaskimailErrorCodes.NotFoundOrIncompleteRequest)]
      [InlineData(422, HaskimailErrorCodes.DomainOrServerNotFound)]
      [InlineData(422, HaskimailErrorCodes.MessageNotFound)]
      [InlineData(422, HaskimailErrorCodes.InboundRuleNotFound)]
      [InlineData(422, HaskimailErrorCodes.TemplateNotFound)]
      [InlineData(422, HaskimailErrorCodes.MessageStreamNotFound)]
      public async Task IsNotFound_recognises_both_http_404_and_not_found_error_codes(int status, int errorCode)
      {
          var (client, http) = TestClient.Create(o => o.Retry = HaskimailRetryOptions.None);
          var body = errorCode == 0 ? "" : "{\"ErrorCode\":" + errorCode + ",\"Message\":\"x\"}";
          http.Respond((HttpStatusCode)status, body);

          var ex = await Assert.ThrowsAnyAsync<HaskimailApiException>(() => client.Messages.GetOutboundDetailsAsync("m"));

          Assert.Equal(status, (int)ex.StatusCode);
          Assert.Equal(errorCode, ex.ErrorCode);
          Assert.True(ex.IsNotFound);
      }

      [Fact]
      public async Task IsNotFound_is_false_for_other_codes()
      {
          var (client, http) = TestClient.Create(o => o.Retry = HaskimailRetryOptions.None);
          http.Respond((HttpStatusCode)422, """{"ErrorCode":6,"Message":"bad address"}""");

          var ex = await Assert.ThrowsAsync<HaskimailValidationException>(() => client.Email.SendAsync(Mail));

          Assert.False(ex.IsNotFound);
      }

      [Fact]
      public async Task Send_rejection_422_carries_error_code()
      {
          var (client, http) = TestClient.Create();
          http.Respond((HttpStatusCode)422, """{"ErrorCode":1226,"Message":"MessageStream doesn't match api key"}""");

          var ex = await Assert.ThrowsAsync<HaskimailValidationException>(() => client.Email.SendAsync(Mail));

          Assert.Equal(HaskimailErrorCodes.MessageStreamNotFound, ex.ErrorCode);
          Assert.False(ex.IsTransient);
          Assert.Single(http.Requests); // 422 не повторяется
      }

    [Fact]
    public async Task Non_json_error_body_is_preserved()
    {
        var (client, http) = TestClient.Create(o => o.Retry = HaskimailRetryOptions.None);
        http.Respond(HttpStatusCode.BadGateway, "<html><body>502 Bad Gateway</body></html>");

        var ex = await Assert.ThrowsAsync<HaskimailServerException>(() => client.Stats.GetOverviewAsync());

        Assert.Equal(0, ex.ErrorCode);
        Assert.Contains("502 Bad Gateway", ex.ApiMessage);
        Assert.True(ex.IsTransient);
    }

    [Fact]
    public async Task Error_code_as_string_is_understood()
    {
        var (client, http) = TestClient.Create();
        http.Respond((HttpStatusCode)422, """{"ErrorCode":"701","Message":"x"}""");

        var ex = await Assert.ThrowsAsync<HaskimailValidationException>(() => client.Messages.GetOutboundDetailsAsync("m"));
        Assert.Equal(701, ex.ErrorCode);
    }

    [Fact]
    public async Task Get_is_retried_on_bad_gateway()
    {
        var (client, http) = TestClient.Create();
        http.Respond(HttpStatusCode.BadGateway, "").Respond(HttpStatusCode.GatewayTimeout, "").RespondFixture("stats-overview.json");

        var overview = await client.Stats.GetOverviewAsync();

        Assert.Equal(71, overview.Sent);
        Assert.Equal(3, http.Requests.Count);
    }

    [Fact]
    public async Task Retries_stop_after_max_and_throw_last_error()
    {
        var (client, http) = TestClient.Create();
        http.Respond(HttpStatusCode.ServiceUnavailable, "").Respond(HttpStatusCode.ServiceUnavailable, "").Respond(HttpStatusCode.ServiceUnavailable, """{"ErrorCode":100,"Message":"down"}""");

        var ex = await Assert.ThrowsAsync<HaskimailServerException>(() => client.Servers.ListAsync());

        Assert.Equal(3, http.Requests.Count); // 1 + MaxRetries(2)
        Assert.Equal("down", ex.ApiMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task Sending_is_not_retried_when_the_email_may_have_been_accepted(HttpStatusCode status)
    {
        var (client, http) = TestClient.Create();
        http.Respond(status, "");

        await Assert.ThrowsAsync<HaskimailServerException>(() => client.Email.SendAsync(Mail));

        Assert.Single(http.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task Sending_is_retried_when_the_api_refused_before_processing(HttpStatusCode status)
    {
        var (client, http) = TestClient.Create();
        http.Respond(status, "").Respond("""{"ErrorCode":0,"Message":"OK","MessageID":"1"}""");

        var result = await client.Email.SendAsync(Mail);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, http.Requests.Count);
        Assert.Equal(http.Requests[0].Body, http.Requests[1].Body); // тело отправлено повторно целиком
    }

    [Fact]
    public async Task Retry_after_header_is_honored_within_max_delay()
    {
        var (client, http) = TestClient.Create(o => o.Retry = new HaskimailRetryOptions { MaxRetries = 1, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.FromMilliseconds(300) });
        http.Respond(HttpStatusCode.TooManyRequests, "", r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)))
            .Respond("{}");

        var started = DateTime.UtcNow;
        await client.Templates.ListAsync();
        var elapsed = DateTime.UtcNow - started;

        Assert.InRange(elapsed, TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(5)); // 30 с урезано до MaxDelay
    }

    [Fact]
    public async Task Timeout_throws_haskimail_timeout_after_retries_for_reads()
    {
        var (client, http) = TestClient.Create(o => o.Timeout = TimeSpan.FromMilliseconds(50));
        http.Hang().Hang().Hang();

        await Assert.ThrowsAsync<HaskimailTimeoutException>(() => client.Templates.ListAsync());
        Assert.Equal(3, http.Requests.Count);
    }

    [Fact]
    public async Task Timeout_of_a_send_is_not_retried()
    {
        var (client, http) = TestClient.Create(o => o.Timeout = TimeSpan.FromMilliseconds(50));
        http.Hang();

        await Assert.ThrowsAsync<HaskimailTimeoutException>(() => client.Email.SendAsync(Mail));
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_wrapped_or_retried()
    {
        var (client, http) = TestClient.Create();
        http.Hang();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Templates.ListAsync(cancellationToken: cts.Token));
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task Network_error_of_a_read_is_retried_then_wrapped()
    {
        var (client, http) = TestClient.Create();
        http.Throw(new HttpRequestException("reset")).Throw(new HttpRequestException("reset")).Throw(new HttpRequestException("reset"));

        var ex = await Assert.ThrowsAsync<HaskimailConnectionException>(() => client.Domains.ListAsync());

        Assert.Equal(3, http.Requests.Count);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task Network_error_of_a_send_is_not_retried_unless_connection_failed()
    {
        var (client, http) = TestClient.Create();
        http.Throw(new HttpRequestException("reset"));

        await Assert.ThrowsAsync<HaskimailConnectionException>(() => client.Email.SendAsync(Mail));
        Assert.Single(http.Requests);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public async Task Send_is_retried_when_connection_was_never_established()
    {
        // Работает только в сборках net8.0+: там HttpRequestError различает «не подключились» и «оборвалось».
        if (typeof(HaskimailClient).Assembly.GetName().Version is null || IsNetStandardBuild()) return;

        var (client, http) = TestClient.Create();
        http.Throw(new HttpRequestException(HttpRequestError.ConnectionError, "refused")).Respond("""{"ErrorCode":0,"Message":"OK"}""");

        var result = await client.Email.SendAsync(Mail);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, http.Requests.Count);
    }

    private static bool IsNetStandardBuild() =>
        typeof(HaskimailClient).Assembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
            .Cast<System.Runtime.Versioning.TargetFrameworkAttribute>()
            .Any(a => a.FrameworkName.StartsWith(".NETStandard", StringComparison.Ordinal));
#endif

    [Fact]
    public async Task Empty_success_body_is_reported()
    {
        var (client, http) = TestClient.Create();
        http.Respond("");

        var ex = await Assert.ThrowsAsync<HaskimailException>(() => client.Templates.ListAsync());
        Assert.Contains("пустой ответ", ex.Message);
    }

    [Fact]
    public async Task Malformed_success_body_is_reported()
    {
        var (client, http) = TestClient.Create();
        http.Respond("{not json");

        var ex = await Assert.ThrowsAsync<HaskimailException>(() => client.Templates.ListAsync());
        Assert.IsAssignableFrom<System.Text.Json.JsonException>(ex.InnerException);
    }
}
