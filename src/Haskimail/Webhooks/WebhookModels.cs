using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Webhooks
{
    /// <summary>Тип события вебхука (по полю <c>event</c>).</summary>
    public enum WebhookEventType
    {
        /// <summary>Неизвестное событие — смотрите <see cref="WebhookEvent.Event"/>.</summary>
        Unknown = 0,

        /// <summary>Письмо доставлено.</summary>
        Delivered,

        /// <summary>Возврат.</summary>
        Bounced,

        /// <summary>Открытие.</summary>
        Opened,

        /// <summary>Клик по ссылке.</summary>
        Clicked,

        /// <summary>Жалоба на спам.</summary>
        SpamComplaint,

        /// <summary>Изменение стоп-листа.</summary>
        SubscriptionChange,
    }

    /// <summary>Категория возврата в вебхуке (<c>bounce_category</c>).</summary>
    public static class BounceCategories
    {
        /// <summary>Жёсткий возврат.</summary>
        public const string Hard = "hrd";

        /// <summary>Мягкий возврат.</summary>
        public const string Soft = "sft";

        /// <summary>Блокировка провайдером.</summary>
        public const string Block = "blk";

        /// <summary>Отклонено как спам.</summary>
        public const string Spam = "spm";
    }

    /// <summary>
    /// Событие вебхука: доставка, возврат, открытие, клик, жалоба на спам или изменение подписки.
    /// Формат общий; поля, не относящиеся к событию, приходят пустыми или отсутствуют.
    /// </summary>
    public sealed class WebhookEvent
    {
        /// <summary>Имя события как есть: delivered, bounced, opened, CLICKED, COMPLAINED, subscriptionchange.</summary>
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        /// <summary>Тип события.</summary>
        [JsonIgnore]
        public WebhookEventType EventType => ParseType(Event);

        /// <summary>Получатель.</summary>
        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }

        /// <summary>Совпадает с <see cref="Recipient"/>.</summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>MessageID из ответа на отправку. Пусто, если событие не связано с письмом.</summary>
        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }

        /// <summary>Тег письма (Tag).</summary>
        [JsonPropertyName("campaign_id")]
        public string? CampaignId { get; set; }

        /// <summary>Metadata письма. API присылает её то JSON-строкой, то объектом — здесь всегда словарь.</summary>
        [JsonPropertyName("custom_vars")]
        [JsonConverter(typeof(StringDictionaryConverter))]
        public IReadOnlyDictionary<string, string>? CustomVars { get; set; }

        /// <summary>ID сервера, отправившего письмо.</summary>
        [JsonPropertyName("server_id")]
        [JsonConverter(typeof(FlexibleInt64Converter))]
        public long? ServerId { get; set; }

        /// <summary>ID канала (приходит строкой или числом).</summary>
        [JsonPropertyName("stream_id")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? StreamId { get; set; }

        /// <summary>Время события (московское).</summary>
        [JsonPropertyName("event_time")]
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? EventTime { get; set; }

        /// <summary>То же время в UTC.</summary>
        [JsonPropertyName("event_time_utc")]
        [JsonConverter(typeof(UtcDateTimeOffsetConverter))]
        public DateTimeOffset? EventTimeUtc { get; set; }

        /// <summary>ID вашего аккаунта.</summary>
        [JsonPropertyName("user_id")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? UserId { get; set; }

        /// <summary>Внутренний ID получателя.</summary>
        [JsonPropertyName("member_id")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MemberId { get; set; }

        /// <summary>Подпись события. Проверяйте через <see cref="HaskimailWebhooks.VerifySignature(WebhookEvent, string)"/>.</summary>
        [JsonPropertyName("secret")]
        public string? Secret { get; set; }

        /// <summary>Код ответа почтового сервера, например «550» (только возвраты).</summary>
        [JsonPropertyName("code")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Code { get; set; }

        /// <summary>Ответ почтового сервера (только возвраты).</summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>Совпадает с <see cref="Error"/>.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>hrd, sft, blk или spm (<see cref="BounceCategories"/>).</summary>
        [JsonPropertyName("bounce_category")]
        public string? BounceCategory { get; set; }

        /// <summary>Исходная ссылка (только клики).</summary>
        [JsonPropertyName("url_original")]
        public string? UrlOriginal { get; set; }

        /// <summary>Ссылка после подстановки персональных данных (только клики).</summary>
        [JsonPropertyName("url_pers")]
        public string? UrlPersonalized { get; set; }

        /// <summary>Причина изменения стоп-листа (только изменение подписки).</summary>
        [JsonPropertyName("SuppressionReason")]
        public SuppressionReason? SuppressionReason { get; set; }

        /// <summary>true — адрес добавлен в стоп-лист, false — удалён (только изменение подписки).</summary>
        [JsonPropertyName("SuppressSending")]
        [JsonConverter(typeof(FlexibleBooleanConverter))]
        public bool? SuppressSending { get; set; }

        /// <summary>
        /// Ключ для дедупликации: события могут прийти повторно, документация советует сравнивать
        /// event, message_id, recipient и event_time.
        /// </summary>
        [JsonIgnore]
        public string DeduplicationKey =>
            string.Join("|", Event ?? string.Empty, MessageId ?? string.Empty, Recipient ?? string.Empty,
                EventTime?.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);

        internal static WebhookEventType ParseType(string? name)
        {
            return (name?.Trim().ToUpperInvariant()) switch
            {
                "DELIVERED" => WebhookEventType.Delivered,
                "BOUNCED" => WebhookEventType.Bounced,
                "OPENED" => WebhookEventType.Opened,
                "CLICKED" => WebhookEventType.Clicked,
                "COMPLAINED" => WebhookEventType.SpamComplaint,
                "SUBSCRIPTIONCHANGE" => WebhookEventType.SubscriptionChange,
                _ => WebhookEventType.Unknown,
            };
        }
    }

    /// <summary>Вложение входящего письма.</summary>
    public sealed class InboundAttachment
    {
        /// <summary>Название.</summary>
        public string? Name { get; set; }

        /// <summary>Содержимое в Base64.</summary>
        public string? Content { get; set; }

        /// <summary>MIME-тип.</summary>
        public string? ContentType { get; set; }

        /// <summary>Размер в байтах.</summary>
        public long ContentLength { get; set; }

        /// <summary>CID встроенной картинки.</summary>
        [JsonPropertyName("ContentID")]
        public string? ContentId { get; set; }

        /// <summary>Раскодированное содержимое.</summary>
        public byte[] GetBytes() => string.IsNullOrEmpty(Content) ? [] : Convert.FromBase64String(Content);
    }

    /// <summary>Входящее письмо, которое Haskimail присылает на InboundHookUrl сервера.</summary>
    public sealed class InboundWebhookMessage
    {
        /// <summary>Имя отправителя.</summary>
        public string? FromName { get; set; }

        /// <summary>ID канала.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MessageStream { get; set; }

        /// <summary>Отправитель.</summary>
        public string? From { get; set; }

        /// <summary>Отправитель, разобранный по частям.</summary>
        public EmailAddressInfo? FromFull { get; set; }

        /// <summary>Заголовок To как есть.</summary>
        public string? To { get; set; }

        /// <summary>Получатели, разобранные по частям.</summary>
        public IReadOnlyList<EmailAddressInfo> ToFull { get; set; } = [];

        /// <summary>Заголовок Cc как есть.</summary>
        public string? Cc { get; set; }

        /// <summary>Получатели копии, разобранные по частям.</summary>
        public IReadOnlyList<EmailAddressInfo> CcFull { get; set; } = [];

        /// <summary>Заголовок Bcc как есть.</summary>
        public string? Bcc { get; set; }

        /// <summary>Получатели скрытой копии, разобранные по частям.</summary>
        public IReadOnlyList<EmailAddressInfo> BccFull { get; set; } = [];

        /// <summary>Адрес, на который письмо фактически доставлено.</summary>
        public string? OriginalRecipient { get; set; }

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>ID письма.</summary>
        [JsonPropertyName("MessageID")]
        public string? MessageId { get; set; }

        /// <summary>Адрес для ответа.</summary>
        public string? ReplyTo { get; set; }

        /// <summary>Часть адреса после «+» — удобно для привязки ответа к тикету или заказу.</summary>
        public string? MailboxHash { get; set; }

        /// <summary>Заголовок Date как есть (RFC 2822).</summary>
        public string? Date { get; set; }

        /// <summary>Текстовая версия.</summary>
        public string? TextBody { get; set; }

        /// <summary>HTML-версия.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>Текст ответа без процитированного предыдущего письма.</summary>
        public string? StrippedTextReply { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }

        /// <summary>Заголовки письма.</summary>
        public IReadOnlyList<MessageHeader> Headers { get; set; } = [];

        /// <summary>Вложения с содержимым.</summary>
        public IReadOnlyList<InboundAttachment> Attachments { get; set; } = [];

        /// <summary>Исходник письма — только если на сервере включён RawEmailEnabled.</summary>
        public string? RawEmail { get; set; }
    }
}
