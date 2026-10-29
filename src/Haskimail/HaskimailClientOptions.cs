using System;

namespace Haskimail
{
    /// <summary>Настройки клиента Haskimail.</summary>
    public sealed class HaskimailClientOptions
    {
        /// <summary>Адрес API по умолчанию.</summary>
        public const string DefaultBaseUrl = "https://api.haskimail.ru";

        /// <summary>
        /// Ключ сервера (<c>X-Haskimail-Server-Token</c>): отправка и данные сервера — сообщения, возвраты, шаблоны,
        /// каналы, статистика, вебхуки, правила, стоп-листы. Для проверки без доставки — <see cref="HaskimailClient.TestServerToken"/>.
        /// </summary>
        public string? ServerToken { get; set; }

        /// <summary>
        /// Ключ аккаунта (<c>X-Haskimail-Account-Token</c>): серверы, домены, перенос шаблонов.
        /// Методы, которые принимают оба ключа, без ключа сервера работают по всем серверам аккаунта.
        /// </summary>
        public string? AccountToken { get; set; }

        /// <summary>Базовый адрес API.</summary>
        public string BaseUrl { get; set; } = DefaultBaseUrl;

        /// <summary>
        /// Таймаут одной попытки запроса. Работает и с внешним HttpClient (например, из IHttpClientFactory).
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

        /// <summary>Политика повторов при временных ошибках.</summary>
        public HaskimailRetryOptions Retry { get; set; } = new HaskimailRetryOptions();

        /// <summary>
        /// Проверять запросы на документированные ограничения до отправки (лимиты длины, число получателей,
        /// запрещённые вложения). Ошибки — <see cref="ArgumentException"/> без обращения к API.
        /// </summary>
        public bool ValidateRequests { get; set; } = true;

        /// <summary>Дополнение к заголовку User-Agent, например название вашего приложения.</summary>
        public string? UserAgentSuffix { get; set; }

        internal HaskimailClientOptions Snapshot()
        {
            if (string.IsNullOrWhiteSpace(ServerToken) && string.IsNullOrWhiteSpace(AccountToken))
                throw new ArgumentException("Задайте ServerToken и/или AccountToken.", nameof(ServerToken));
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException($"BaseUrl должен быть абсолютным http(s)-адресом, получено «{BaseUrl}».", nameof(BaseUrl));
            if (Timeout <= TimeSpan.Zero && Timeout != System.Threading.Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(Timeout), Timeout, "Таймаут должен быть положительным.");

            var retry = Retry ?? new HaskimailRetryOptions();
            retry.Validate();

            return new HaskimailClientOptions
            {
                ServerToken = string.IsNullOrWhiteSpace(ServerToken) ? null : ServerToken!.Trim(),
                AccountToken = string.IsNullOrWhiteSpace(AccountToken) ? null : AccountToken!.Trim(),
                BaseUrl = BaseUrl.TrimEnd('/'),
                Timeout = Timeout,
                Retry = retry.Clone(),
                ValidateRequests = ValidateRequests,
                UserAgentSuffix = UserAgentSuffix,
            };
        }
    }

    /// <summary>
    /// Повторы при временных ошибках: экспоненциальная задержка с джиттером и учётом заголовка Retry-After.
    /// </summary>
    /// <remarks>
    /// Идемпотентные запросы (чтение, изменение, удаление) повторяются при сетевых сбоях, таймаутах и HTTP 408/429/500/502/503/504.
    /// Отправка писем и создание объектов повторяются, только если API точно не принял запрос (HTTP 429/503 или сбой
    /// соединения до отправки) — иначе повтор мог бы отправить письмо дважды.
    /// </remarks>
    public sealed class HaskimailRetryOptions
    {
        /// <summary>Сколько раз повторить запрос после первой попытки. 0 — без повторов.</summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>Начальная задержка; удваивается с каждой попыткой.</summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>Максимальная задержка между попытками (в том числе для Retry-After).</summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Без повторов.</summary>
        public static HaskimailRetryOptions None => new() { MaxRetries = 0 };

        internal void Validate()
        {
            if (MaxRetries < 0) throw new ArgumentOutOfRangeException(nameof(MaxRetries), MaxRetries, "Не может быть отрицательным.");
            if (BaseDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(BaseDelay), BaseDelay, "Не может быть отрицательной.");
            if (MaxDelay < BaseDelay) throw new ArgumentOutOfRangeException(nameof(MaxDelay), MaxDelay, "Не может быть меньше BaseDelay.");
        }

        internal HaskimailRetryOptions Clone() =>
            new()
            { MaxRetries = MaxRetries, BaseDelay = BaseDelay, MaxDelay = MaxDelay };
    }
}
