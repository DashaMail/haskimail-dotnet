using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Haskimail
{
    /// <summary>Регистрация клиента Haskimail в контейнере зависимостей.</summary>
    public static class HaskimailServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует <see cref="IHaskimailClient"/> поверх IHttpClientFactory.
        /// Возвращает <see cref="IHttpClientBuilder"/> — к нему можно добавить свои обработчики.
        /// </summary>
        /// <example>
        /// <code>
        /// services.AddHaskimail(o => o.ServerToken = configuration["Haskimail:ServerToken"]);
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddHaskimail(this IServiceCollection services, Action<HaskimailClientOptions> configure)
        {
            ThrowIfNull(services, nameof(services));
            ThrowIfNull(configure, nameof(configure));
            services.AddOptions<HaskimailClientOptions>().Configure(configure);
            return AddClient(services);
        }

        /// <summary>
        /// Регистрирует <see cref="IHaskimailClient"/> с настройками из секции конфигурации, например:
        /// <c>{"Haskimail": {"ServerToken": "...", "AccountToken": "...", "Timeout": "00:00:30", "Retry": {"MaxRetries": 3}}}</c>.
        /// Привязка явная, без рефлексии — совместима с Native AOT.
        /// </summary>
        public static IHttpClientBuilder AddHaskimail(this IServiceCollection services, IConfiguration configuration)
        {
            ThrowIfNull(services, nameof(services));
            ThrowIfNull(configuration, nameof(configuration));
            services.AddOptions<HaskimailClientOptions>().Configure(options => Bind(configuration, options));
            return AddClient(services);
        }

        private static IHttpClientBuilder AddClient(IServiceCollection services)
        {
            services.AddOptions<HaskimailClientOptions>()
                .Validate(o => !string.IsNullOrWhiteSpace(o.ServerToken) || !string.IsNullOrWhiteSpace(o.AccountToken),
                    "Haskimail: задайте ServerToken и/или AccountToken.");

            return services
                .AddHttpClient<IHaskimailClient, HaskimailClient>((httpClient, provider) =>
                    new HaskimailClient(httpClient, provider.GetRequiredService<IOptions<HaskimailClientOptions>>().Value))
                // Таймаут попытки и повторы обеспечивает сам клиент (HaskimailClientOptions.Timeout и Retry).
                .ConfigureHttpClient(httpClient => httpClient.Timeout = Timeout.InfiniteTimeSpan);
        }

        internal static void Bind(IConfiguration section, HaskimailClientOptions options)
        {
            options.ServerToken = section["ServerToken"] ?? options.ServerToken;
            options.AccountToken = section["AccountToken"] ?? options.AccountToken;
            options.BaseUrl = section["BaseUrl"] ?? options.BaseUrl;
            options.UserAgentSuffix = section["UserAgentSuffix"] ?? options.UserAgentSuffix;
            options.Timeout = ReadTimeSpan(section, "Timeout") ?? options.Timeout;
            options.ValidateRequests = ReadBool(section, "ValidateRequests") ?? options.ValidateRequests;

            var retry = section.GetSection("Retry");
            options.Retry.MaxRetries = ReadInt(retry, "MaxRetries") ?? options.Retry.MaxRetries;
            options.Retry.BaseDelay = ReadTimeSpan(retry, "BaseDelay") ?? options.Retry.BaseDelay;
            options.Retry.MaxDelay = ReadTimeSpan(retry, "MaxDelay") ?? options.Retry.MaxDelay;
        }

        private static TimeSpan? ReadTimeSpan(IConfiguration section, string key) =>
            Read(section, key, s => TimeSpan.Parse(s, CultureInfo.InvariantCulture), "интервал вида 00:00:30");

        private static int? ReadInt(IConfiguration section, string key) =>
            Read(section, key, s => int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture), "целое число");

        private static bool? ReadBool(IConfiguration section, string key) =>
            Read(section, key, bool.Parse, "true или false");

        private static T? Read<T>(IConfiguration section, string key, Func<string, T> parse, string expected) where T : struct
        {
            var value = section[key];
            if (string.IsNullOrWhiteSpace(value)) return null;
            try
            {
                return parse(value!.Trim());
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Haskimail: в настройке «{key}» ожидается {expected}, получено «{value}».", ex);
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException($"Haskimail: значение настройки «{key}» вне допустимого диапазона: «{value}».", ex);
            }
        }

        private static void ThrowIfNull(object? value, string paramName)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(value, paramName);
#else
            if (value is null) throw new ArgumentNullException(paramName);
#endif
        }
    }
}
