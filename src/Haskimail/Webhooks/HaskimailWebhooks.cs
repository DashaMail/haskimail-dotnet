using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;

namespace Haskimail.Webhooks
{
    /// <summary>Разбор и проверка запросов, которые Haskimail присылает на ваши вебхуки.</summary>
    /// <remarks>
    /// Haskimail ждёт ответ до 10 секунд. На 5xx повторяет примерно раз в минуту, до 60 попыток;
    /// на 4xx и при отсутствии ответа не повторяет. Отвечайте сразу, обработку выполняйте после.
    /// События могут прийти повторно — дедуплицируйте по <see cref="WebhookEvent.DeduplicationKey"/>.
    /// </remarks>
    public static class HaskimailWebhooks
    {
        /// <summary>Разобрать событие: доставка, возврат, открытие, клик, жалоба, изменение подписки.</summary>
        /// <exception cref="HaskimailException">Тело не является событием Haskimail.</exception>
        public static WebhookEvent ParseEvent(string json) =>
            Parse(Guard.NotNull(json, nameof(json)), HaskimailJsonContext.Api.WebhookEvent, "событие вебхука");

        /// <summary>Разобрать тело, в котором может быть одно событие или массив событий.</summary>
        public static IReadOnlyList<WebhookEvent> ParseEvents(string json)
        {
            Guard.NotNull(json, nameof(json));
            var trimmed = json.TrimStart();
            return trimmed.Length > 0 && trimmed[0] == '['
                ? Parse(json, HaskimailJsonContext.Api.ListWebhookEvent, "события вебхука")
                : new[] { ParseEvent(json) };
        }

        /// <summary>Прочитать тело запроса (например, <c>Request.Body</c>) и разобрать событие. Поток не закрывается.</summary>
        public static async Task<WebhookEvent> ParseEventAsync(Stream body, CancellationToken cancellationToken = default) =>
            ParseEvent(await ReadAsync(body, cancellationToken).ConfigureAwait(false));

        /// <summary>Разобрать входящее письмо (вебхук для входящих).</summary>
        public static InboundWebhookMessage ParseInbound(string json) =>
            Parse(Guard.NotNull(json, nameof(json)), HaskimailJsonContext.Api.InboundWebhookMessage, "входящее письмо");

        /// <summary>Прочитать тело запроса и разобрать входящее письмо. Поток не закрывается.</summary>
        public static async Task<InboundWebhookMessage> ParseInboundAsync(Stream body, CancellationToken cancellationToken = default) =>
            ParseInbound(await ReadAsync(body, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Подпись события: MD5 (hex, нижний регистр) от <c>recipient + message_id + ключ</c> без разделителей.
        /// Ключ подписи — во вкладке «Вебхуки» канала в интерфейсе Haskimail; он один на весь аккаунт.
        /// </summary>
        public static string ComputeSignature(string? recipient, string? messageId, string signingKey)
        {
            Guard.NotEmpty(signingKey, nameof(signingKey));
            return Polyfills.Md5HexLower((recipient ?? string.Empty) + (messageId ?? string.Empty) + signingKey);
        }

        /// <summary>Проверить подпись события (сравнение за постоянное время, регистр hex не важен).</summary>
        public static bool VerifySignature(WebhookEvent webhookEvent, string signingKey)
        {
            Guard.NotNull(webhookEvent, nameof(webhookEvent));
            return VerifySignature(webhookEvent.Recipient, webhookEvent.MessageId, webhookEvent.Secret, signingKey);
        }

        /// <inheritdoc cref="VerifySignature(WebhookEvent, string)"/>
        public static bool VerifySignature(string? recipient, string? messageId, string? secret, string signingKey)
        {
            if (string.IsNullOrWhiteSpace(secret)) return false;
            var expected = Encoding.ASCII.GetBytes(ComputeSignature(recipient, messageId, signingKey));
            var actual = Encoding.ASCII.GetBytes(secret!.Trim().ToLowerInvariant());
            return Polyfills.FixedTimeEquals(expected, actual);
        }

        private static T Parse<T>(string json, JsonTypeInfo<T> type, string what)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new HaskimailException($"Пустое тело вебхука: ожидалось {what}.");
            try
            {
                return JsonSerializer.Deserialize(json, type) ?? throw new HaskimailException($"Пустое тело вебхука: ожидалось {what}.");
            }
            catch (JsonException ex)
            {
                throw new HaskimailException($"Не удалось разобрать {what}: {ex.Message}", ex);
            }
        }

        private static async Task<string> ReadAsync(Stream body, CancellationToken cancellationToken)
        {
            Guard.NotNull(body, nameof(body));
            using var reader = new StreamReader(body, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            return await Polyfills.ReadToEndAsync(reader, cancellationToken).ConfigureAwait(false);
        }
    }
}
