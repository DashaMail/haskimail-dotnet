using System;
using System.Net.Http;
using System.Threading;
using Haskimail.Api;
using Haskimail.Internal;

namespace Haskimail
{
    /// <summary>Клиент REST API Haskimail. Интерфейс удобен для подмены в тестах.</summary>
    public interface IHaskimailClient
    {
        /// <summary>Отправка писем.</summary>
        IEmailApi Email { get; }

        /// <summary>Шаблоны и отправка по шаблонам.</summary>
        ITemplatesApi Templates { get; }

        /// <summary>Отправленные и входящие письма, открытия, клики.</summary>
        IMessagesApi Messages { get; }

        /// <summary>Возвраты.</summary>
        IBouncesApi Bounces { get; }

        /// <summary>SMTP-серверы (ключ аккаунта).</summary>
        IServersApi Servers { get; }

        /// <summary>Каналы.</summary>
        IMessageStreamsApi MessageStreams { get; }

        /// <summary>Домены (ключ аккаунта).</summary>
        IDomainsApi Domains { get; }

        /// <summary>Статистика.</summary>
        IStatsApi Stats { get; }

        /// <summary>Настройка вебхуков.</summary>
        IWebhooksApi Webhooks { get; }

        /// <summary>Правила для входящих.</summary>
        IInboundRulesApi InboundRules { get; }

        /// <summary>Стоп-листы.</summary>
        ISuppressionsApi Suppressions { get; }
    }

    /// <summary>
    /// Клиент REST API Haskimail. Потокобезопасен: создайте один экземпляр на приложение
    /// или получайте из DI (пакет Haskimail.NET.Extensions.DependencyInjection).
    /// </summary>
    /// <example>
    /// <code>
    /// using var client = new HaskimailClient("ключ-сервера");
    /// var result = await client.Email.SendAsync(new EmailMessage
    /// {
    ///     From = "Магазин &lt;shop@example.com&gt;",
    ///     To = "ivan@example.com",
    ///     Subject = "Заказ оформлен",
    ///     TextBody = "Спасибо за заказ!",
    /// });
    /// </code>
    /// </example>
    public sealed class HaskimailClient : IHaskimailClient, IDisposable
    {
        /// <summary>
        /// Тестовый ключ сервера: запрос только проверяется, письмо не создаётся и не доставляется.
        /// Работает лишь с методами отправки.
        /// </summary>
        public const string TestServerToken = "HASKIMAIL_API_TEST";

        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        /// <summary>Клиент с ключом сервера и, при необходимости, ключом аккаунта.</summary>
        public HaskimailClient(string serverToken, string? accountToken = null)
            : this(new HaskimailClientOptions { ServerToken = serverToken, AccountToken = accountToken })
        {
        }

        /// <summary>Клиент со своим HttpClient.</summary>
        public HaskimailClient(HaskimailClientOptions options)
            : this(CreateHttpClient(), options, ownsHttpClient: true)
        {
        }

        /// <summary>
        /// Клиент поверх внешнего HttpClient — например, из IHttpClientFactory. Внешний HttpClient клиент не освобождает.
        /// </summary>
        public HaskimailClient(HttpClient httpClient, HaskimailClientOptions options)
            : this(httpClient, options, ownsHttpClient: false)
        {
        }

        private HaskimailClient(HttpClient httpClient, HaskimailClientOptions options, bool ownsHttpClient)
        {
            _httpClient = Guard.NotNull(httpClient, nameof(httpClient));
            _ownsHttpClient = ownsHttpClient;
            var snapshot = Guard.NotNull(options, nameof(options)).Snapshot();

            var api = new ApiConnection(httpClient, snapshot);
            Email = new EmailApi(api);
            Templates = new TemplatesApi(api);
            Messages = new MessagesApi(api);
            Bounces = new BouncesApi(api);
            Servers = new ServersApi(api);
            MessageStreams = new MessageStreamsApi(api);
            Domains = new DomainsApi(api);
            Stats = new StatsApi(api);
            Webhooks = new WebhooksApi(api);
            InboundRules = new InboundRulesApi(api);
            Suppressions = new SuppressionsApi(api);
        }

        /// <inheritdoc />
        public IEmailApi Email { get; }

        /// <inheritdoc />
        public ITemplatesApi Templates { get; }

        /// <inheritdoc />
        public IMessagesApi Messages { get; }

        /// <inheritdoc />
        public IBouncesApi Bounces { get; }

        /// <inheritdoc />
        public IServersApi Servers { get; }

        /// <inheritdoc />
        public IMessageStreamsApi MessageStreams { get; }

        /// <inheritdoc />
        public IDomainsApi Domains { get; }

        /// <inheritdoc />
        public IStatsApi Stats { get; }

        /// <inheritdoc />
        public IWebhooksApi Webhooks { get; }

        /// <inheritdoc />
        public IInboundRulesApi InboundRules { get; }

        /// <inheritdoc />
        public ISuppressionsApi Suppressions { get; }

        /// <summary>Освобождает внутренний HttpClient (внешний не трогает).</summary>
        public void Dispose()
        {
            if (_ownsHttpClient) _httpClient.Dispose();
        }

        private static HttpClient CreateHttpClient()
        {
#if NET5_0_OR_GREATER
            // Пул соединений с ограниченным временем жизни — клиент корректно переживает смену DNS при долгой работе.
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            };
            return new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
#else
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            };
            return new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
#endif
        }
    }
}
