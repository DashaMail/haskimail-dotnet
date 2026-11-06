using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Haskimail.Models;

namespace Haskimail
{
    /// <summary>Базовое исключение SDK Haskimail. Ловите его, чтобы обработать любую ошибку обращения к API.</summary>
    public class HaskimailException : Exception
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailException(string message) : base(message) { }

        /// <summary>Создаёт исключение.</summary>
        public HaskimailException(string message, Exception? innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// API ответил ошибкой (HTTP не 2xx). Тело ошибки Haskimail — <c>{"ErrorCode": N, "Message": "..."}</c>.
    /// Для типичных статусов выбрасываются наследники: <see cref="HaskimailAuthenticationException"/>,
    /// <see cref="HaskimailForbiddenException"/>, <see cref="HaskimailNotFoundException"/>,
    /// <see cref="HaskimailRequestTooLargeException"/>, <see cref="HaskimailValidationException"/>,
    /// <see cref="HaskimailRateLimitException"/>, <see cref="HaskimailServerException"/>.
    /// </summary>
    public class HaskimailApiException : HaskimailException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailApiException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(BuildMessage(statusCode, errorCode, apiMessage, requestMethod, requestPath))
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            ApiMessage = apiMessage ?? string.Empty;
            ResponseBody = responseBody;
            RequestMethod = requestMethod;
            RequestPath = requestPath;
        }

        /// <summary>HTTP-статус ответа.</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>Код ошибки Haskimail (<see cref="HaskimailErrorCodes"/>); 0, если в ответе его не было.</summary>
        public int ErrorCode { get; }

        /// <summary>Текст ошибки из ответа API.</summary>
        public string ApiMessage { get; }

        /// <summary>Сырое тело ответа.</summary>
        public string? ResponseBody { get; }

        /// <summary>HTTP-метод запроса (без ключей и тела).</summary>
        public string? RequestMethod { get; }

        /// <summary>Путь запроса (без строки запроса).</summary>
        public string? RequestPath { get; }

        /// <summary>
        /// Временная ошибка: запрос имеет смысл повторить позже
        /// (HTTP 408, 429, 500, 502, 503, 504; коды 36 и 100).
        /// </summary>
        public bool IsTransient =>
            IsTransientStatus(StatusCode) ||
            ErrorCode == HaskimailErrorCodes.TemporarySendingError ||
            ErrorCode == HaskimailErrorCodes.InternalError;

        /// <summary>
        /// Запрошенный объект не найден: HTTP 404 или один из кодов «не найдено»
        /// (12, 510, 701, 812, 1101, 1226). Haskimail сообщает об отсутствии объекта
        /// таким кодом и при HTTP 422, поэтому одного статуса недостаточно.
        /// </summary>
        public bool IsNotFound =>
            StatusCode == HttpStatusCode.NotFound || ErrorCode is
                HaskimailErrorCodes.NotFoundOrIncompleteRequest or
                HaskimailErrorCodes.DomainOrServerNotFound or
                HaskimailErrorCodes.MessageNotFound or
                HaskimailErrorCodes.InboundRuleNotFound or
                HaskimailErrorCodes.TemplateNotFound or
                HaskimailErrorCodes.MessageStreamNotFound;

        internal static bool IsTransientStatus(HttpStatusCode status)
        {
            var code = (int)status;
            return code == 408 || code == 429 || code == 500 || code == 502 || code == 503 || code == 504;
        }

        internal static HaskimailApiException Create(HttpStatusCode status, int errorCode, string message, string? body, string method, string path)
        {
            return (int)status switch
            {
                401 => new HaskimailAuthenticationException(status, errorCode, message, body, method, path),
                403 => new HaskimailForbiddenException(status, errorCode, message, body, method, path),
                404 => new HaskimailNotFoundException(status, errorCode, message, body, method, path),
                413 => new HaskimailRequestTooLargeException(status, errorCode, message, body, method, path),
                422 => new HaskimailValidationException(status, errorCode, message, body, method, path),
                429 => new HaskimailRateLimitException(status, errorCode, message, body, method, path),
                _ => (int)status >= 500
                                        ? new HaskimailServerException(status, errorCode, message, body, method, path)
                                        : new HaskimailApiException(status, errorCode, message, body, method, path),
            };
        }

        private static string BuildMessage(HttpStatusCode statusCode, int errorCode, string? apiMessage, string? method, string? path)
        {
            var text = string.IsNullOrWhiteSpace(apiMessage) ? statusCode.ToString() : apiMessage;
            var request = method != null && path != null ? $" [{method} {path}]" : string.Empty;
            return errorCode != 0
                ? $"Haskimail API: ошибка {errorCode} (HTTP {(int)statusCode}){request}: {text}"
                : $"Haskimail API: HTTP {(int)statusCode}{request}: {text}";
        }
    }

    /// <summary>HTTP 401: ключ не передан, неверен или не подходит для метода (код 10).</summary>
    public sealed class HaskimailAuthenticationException : HaskimailApiException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailAuthenticationException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(statusCode, errorCode, apiMessage, responseBody, requestMethod, requestPath) { }
    }

    /// <summary>HTTP 403: IP-адрес не входит в список разрешённых для аккаунта.</summary>
    public sealed class HaskimailForbiddenException : HaskimailApiException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailForbiddenException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(statusCode, errorCode, apiMessage, responseBody, requestMethod, requestPath) { }
    }

    /// <summary>HTTP 404: неизвестный метод или объект (сервер, шаблон, канал, вебхук…).</summary>
    public sealed class HaskimailNotFoundException : HaskimailApiException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailNotFoundException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(statusCode, errorCode, apiMessage, responseBody, requestMethod, requestPath) { }
    }

    /// <summary>HTTP 413: запрос больше 50 МБ. Ничего не отправлено.</summary>
    public sealed class HaskimailRequestTooLargeException : HaskimailApiException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailRequestTooLargeException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(statusCode, errorCode, apiMessage, responseBody, requestMethod, requestPath) { }
    }

    /// <summary>
    /// HTTP 422: API отклонил данные. Причина — в <see cref="HaskimailApiException.ErrorCode"/>
    /// (например, 6 — неверный адрес, 52 — отправитель не подтверждён, 701 — письмо не найдено).
    /// </summary>
    public sealed class HaskimailValidationException : HaskimailApiException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailValidationException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(statusCode, errorCode, apiMessage, responseBody, requestMethod, requestPath) { }
    }

    /// <summary>HTTP 429: слишком много запросов.</summary>
    public sealed class HaskimailRateLimitException : HaskimailApiException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailRateLimitException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(statusCode, errorCode, apiMessage, responseBody, requestMethod, requestPath) { }
    }

    /// <summary>HTTP 5xx: ошибка на стороне Haskimail или шлюза. Обычно временная.</summary>
    public sealed class HaskimailServerException : HaskimailApiException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailServerException(HttpStatusCode statusCode, int errorCode, string apiMessage, string? responseBody, string? requestMethod = null, string? requestPath = null)
            : base(statusCode, errorCode, apiMessage, responseBody, requestMethod, requestPath) { }
    }

    /// <summary>Запрос не уложился в <see cref="HaskimailClientOptions.Timeout"/> (с учётом повторов).</summary>
    public sealed class HaskimailTimeoutException : HaskimailException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailTimeoutException(string message, Exception? innerException) : base(message, innerException) { }
    }

    /// <summary>Не удалось связаться с API: DNS, соединение, TLS.</summary>
    public sealed class HaskimailConnectionException : HaskimailException
    {
        /// <summary>Создаёт исключение.</summary>
        public HaskimailConnectionException(string message, Exception? innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Часть писем пакета отклонена. Выбрасывается только из <see cref="SendResultExtensions.EnsureAllSucceeded"/>:
    /// сама пакетная отправка частичный отказ ошибкой не считает.
    /// </summary>
    public sealed class HaskimailBatchException : HaskimailException
    {
        /// <summary>Создаёт исключение по результатам пакета.</summary>
        public HaskimailBatchException(IReadOnlyList<SendResult> results, IReadOnlyList<(int Index, SendResult Result)> failures)
            : base(BuildMessage(results, failures))
        {
            Results = results;
            Failures = failures;
        }

        /// <summary>Результаты по всем письмам пакета.</summary>
        public IReadOnlyList<SendResult> Results { get; }

        /// <summary>Отклонённые письма и их номера в пакете.</summary>
        public IReadOnlyList<(int Index, SendResult Result)> Failures { get; }

        private static string BuildMessage(IReadOnlyList<SendResult> results, IReadOnlyList<(int Index, SendResult Result)> failures)
        {
            var sample = string.Join("; ", failures.Take(3).Select(f => $"[{f.Index}] ({f.Result.To}): {f.Result.ErrorCode} {f.Result.Message}"));
            return $"Haskimail: отклонено писем — {failures.Count} из {results.Count}. {sample}{(failures.Count > 3 ? "; …" : string.Empty)}";
        }
    }
}
