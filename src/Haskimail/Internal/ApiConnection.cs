using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Haskimail.Internal
{
    /// <summary>Каким ключом авторизуется метод.</summary>
    internal enum TokenType
    {
        /// <summary>Только ключ сервера.</summary>
        Server,

        /// <summary>Только ключ аккаунта.</summary>
        Account,

        /// <summary>Ключ сервера, а если он не задан — ключ аккаунта.</summary>
        ServerOrAccount,
    }

    /// <summary>Описание вызова API.</summary>
    internal sealed class ApiCall
    {
        private ApiCall(HttpMethod method, string path, TokenType token, bool idempotent, QueryBuilder? query)
        {
            Method = method;
            Path = path;
            Token = token;
            Idempotent = idempotent;
            Query = query;
        }

        public HttpMethod Method { get; }
        public string Path { get; }
        public TokenType Token { get; }

        /// <summary>Повтор не меняет результат — значит, при сбое запрос можно безопасно повторить.</summary>
        public bool Idempotent { get; }

        public QueryBuilder? Query { get; }

        public static ApiCall Get(string path, TokenType token, QueryBuilder? query = null) => new(HttpMethod.Get, path, token, true, query);
        public static ApiCall Post(string path, TokenType token, bool idempotent = false) => new(HttpMethod.Post, path, token, idempotent, null);
        public static ApiCall Put(string path, TokenType token, bool idempotent = true) => new(HttpMethod.Put, path, token, idempotent, null);
        public static ApiCall Patch(string path, TokenType token) => new(Polyfills.Patch, path, token, true, null);
        public static ApiCall Delete(string path, TokenType token) => new(HttpMethod.Delete, path, token, true, null);
    }

    /// <summary>
    /// HTTP-уровень: авторизация, сериализация, таймауты, повторы и разбор ошибок.
    /// Потокобезопасен; состояние — только неизменяемые настройки.
    /// </summary>
    internal sealed class ApiConnection
    {
        internal const string ServerTokenHeader = "X-Haskimail-Server-Token";
        internal const string AccountTokenHeader = "X-Haskimail-Account-Token";

        private static readonly MediaTypeWithQualityHeaderValue AcceptJson = new("application/json");
        private static readonly MediaTypeHeaderValue ContentTypeJson = new("application/json") { CharSet = "utf-8" };

        private readonly HttpClient _http;
        private readonly HaskimailClientOptions _options;
        private readonly string _userAgent;

        public ApiConnection(HttpClient http, HaskimailClientOptions options)
        {
            _http = http;
            _options = options;
            _userAgent = BuildUserAgent(options.UserAgentSuffix);
        }

        public HaskimailClientOptions Options => _options;

        public Task<TResponse> SendAsync<TResponse>(ApiCall call, JsonTypeInfo<TResponse> responseType, CancellationToken cancellationToken) =>
            SendCoreAsync(call, null, responseType, cancellationToken);

        public Task<TResponse> SendAsync<TRequest, TResponse>(ApiCall call, TRequest body, JsonTypeInfo<TRequest> requestType, JsonTypeInfo<TResponse> responseType, CancellationToken cancellationToken) =>
            SendCoreAsync(call, JsonSerializer.SerializeToUtf8Bytes(body, requestType), responseType, cancellationToken);

        private async Task<TResponse> SendCoreAsync<TResponse>(ApiCall call, byte[]? payload, JsonTypeInfo<TResponse> responseType, CancellationToken cancellationToken)
        {
            var (headerName, headerValue) = ResolveToken(call.Token);
            var url = _options.BaseUrl + call.Path + (call.Query?.ToString() ?? string.Empty);
            var retry = _options.Retry;

            for (var attempt = 0; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = new HttpRequestMessage(call.Method, url);
                request.Headers.Accept.Add(AcceptJson);
                request.Headers.TryAddWithoutValidation(headerName, headerValue);
                request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
                if (payload != null)
                {
                    request.Content = new ByteArrayContent(payload);
                    request.Content.Headers.ContentType = ContentTypeJson;
                }

                using var timeout = CreateTimeout(cancellationToken);
                HttpResponseMessage response;
                try
                {
                    response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Сработал наш таймаут (или таймаут HttpClient), а не отмена вызывающим кодом.
                    if (call.Idempotent && attempt < retry.MaxRetries)
                    {
                        await DelayAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new HaskimailTimeoutException($"Haskimail API не ответил за {_options.Timeout.TotalSeconds:0.#} с [{call.Method} {call.Path}].", ex);
                }
                catch (HttpRequestException ex)
                {
                    if (attempt < retry.MaxRetries && (call.Idempotent || IsNotSent(ex)))
                    {
                        await DelayAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new HaskimailConnectionException($"Не удалось связаться с Haskimail API [{call.Method} {call.Path}]: {ex.Message}", ex);
                }

                using (response)
                {
                    var bytes = response.Content is null
                        ? []
                        : await Polyfills.ReadBytesAsync(response.Content, cancellationToken).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                        return Deserialize(bytes, responseType, call);

                    if (attempt < retry.MaxRetries && ShouldRetry(response.StatusCode, call.Idempotent))
                    {
                        await DelayAsync(attempt, GetRetryAfter(response), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw CreateApiException(response.StatusCode, bytes, call);
                }
            }
        }

        private CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_options.Timeout != Timeout.InfiniteTimeSpan) cts.CancelAfter(_options.Timeout);
            return cts;
        }

        /// <summary>
        /// Повтор запроса, который мог быть выполнен (например, отправка письма), допустим только когда API
        /// явно отказал до обработки: 429 (лимит) и 503 (сервис недоступен).
        /// </summary>
        internal static bool ShouldRetry(HttpStatusCode status, bool idempotent) =>
            idempotent ? HaskimailApiException.IsTransientStatus(status) : (int)status == 429 || (int)status == 503;

        /// <summary>Соединение не установлено — запрос гарантированно не ушёл на сервер.</summary>
        private static bool IsNotSent(HttpRequestException ex)
        {
#if NET8_0_OR_GREATER
            return ex.HttpRequestError == HttpRequestError.NameResolutionError
                || ex.HttpRequestError == HttpRequestError.ConnectionError
                || ex.HttpRequestError == HttpRequestError.SecureConnectionError;
#else
            _ = ex;
            return false;
#endif
        }

        private Task DelayAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken)
        {
            var retry = _options.Retry;
            TimeSpan delay;
            if (retryAfter.HasValue)
            {
                delay = retryAfter.Value > retry.MaxDelay ? retry.MaxDelay : retryAfter.Value;
            }
            else
            {
                // Экспонента с «равным» джиттером: половина фиксирована, половина случайна.
                var exponential = retry.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt);
                var capped = Math.Min(exponential, retry.MaxDelay.TotalMilliseconds);
                delay = TimeSpan.FromMilliseconds(capped / 2 + Polyfills.NextRandomDouble() * capped / 2);
            }
            return delay <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(delay, cancellationToken);
        }

        private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
        {
            var header = response.Headers.RetryAfter;
            if (header is null) return null;
            if (header.Delta.HasValue) return header.Delta.Value;
            if (header.Date.HasValue)
            {
                var wait = header.Date.Value - DateTimeOffset.UtcNow;
                return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
            }
            return null;
        }

        private static TResponse Deserialize<TResponse>(byte[] bytes, JsonTypeInfo<TResponse> type, ApiCall call)
        {
            if (bytes.Length == 0)
                throw new HaskimailException($"Haskimail API вернул пустой ответ [{call.Method} {call.Path}].");
            try
            {
                var result = JsonSerializer.Deserialize(bytes, type) ?? throw new HaskimailException($"Haskimail API вернул null [{call.Method} {call.Path}].");
                return result;
            }
            catch (JsonException ex)
            {
                throw new HaskimailException($"Не удалось разобрать ответ Haskimail API [{call.Method} {call.Path}]: {ex.Message}", ex);
            }
        }

        internal static HaskimailApiException CreateApiException(HttpStatusCode status, byte[] bytes, ApiCall call)
        {
            var body = bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
            var errorCode = 0;
            var message = string.Empty;
            if (body.Length > 0)
            {
                try
                {
                    using var doc = JsonDocument.Parse(bytes);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in doc.RootElement.EnumerateObject())
                        {
                            if (string.Equals(p.Name, "ErrorCode", StringComparison.OrdinalIgnoreCase))
                            {
                                if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var code)) errorCode = code;
                                else if (p.Value.ValueKind == JsonValueKind.String) int.TryParse(p.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out errorCode);
                            }
                            else if (string.Equals(p.Name, "Message", StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                            {
                                message = p.Value.GetString() ?? string.Empty;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Не JSON (например, HTML-страница шлюза) — покажем начало как есть.
                    message = body.Length > 300 ? new StringBuilder(body, 0, 300, 301).Append('…').ToString() : body;
                }
            }
            return HaskimailApiException.Create(status, errorCode, message, body, call.Method.Method, call.Path);
        }

        private (string Header, string Value) ResolveToken(TokenType token)
        {
            return token switch
            {
                TokenType.Server => (ServerTokenHeader, _options.ServerToken
                                        ?? throw new InvalidOperationException("Для этого метода нужен ключ сервера: задайте HaskimailClientOptions.ServerToken.")),
                TokenType.Account => (AccountTokenHeader, _options.AccountToken
                                        ?? throw new InvalidOperationException("Для этого метода нужен ключ аккаунта: задайте HaskimailClientOptions.AccountToken.")),
                _ => _options.ServerToken != null
                                        ? (ServerTokenHeader, _options.ServerToken)
                                        : (AccountTokenHeader, _options.AccountToken!),
            };
        }

        private static string BuildUserAgent(string? suffix)
        {
            var version = typeof(ApiConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
            var plus = version.IndexOf('+');
            if (plus >= 0) version = version.Substring(0, plus);
            var ua = $"Haskimail.NET/{version} ({RuntimeInformation.FrameworkDescription.Trim()})";
            return string.IsNullOrWhiteSpace(suffix) ? ua : ua + " " + suffix!.Trim();
        }
    }
}
