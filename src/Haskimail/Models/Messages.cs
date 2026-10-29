using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Haskimail.Internal;

namespace Haskimail.Models
{
    /// <summary>Фильтр поиска исходящих писем. Даты — московское время.</summary>
    public sealed class OutboundMessageSearch
    {
        /// <summary>1–500, по умолчанию 100. count + offset ≤ 10 000.</summary>
        public int? Count { get; set; }

        /// <summary>Сколько записей пропустить. По умолчанию 0.</summary>
        public int? Offset { get; set; }

        /// <summary>Точное совпадение.</summary>
        public string? Recipient { get; set; }

        /// <summary>Отправитель, точное совпадение.</summary>
        public string? FromEmail { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }

        /// <summary>Часть темы.</summary>
        public string? Subject { get; set; }

        /// <summary>ID канала.</summary>
        public string? MessageStream { get; set; }

        /// <summary>Начало периода. По умолчанию — месяц назад.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Конец периода включительно; дата без времени — до конца дня. По умолчанию — сейчас.</summary>
        public DateTime? ToDate { get; set; }
    }

    /// <summary>Фильтр открытий или кликов одного письма (за всё время, без дат).</summary>
    public class MessageEventFilter
    {
        /// <summary>1–500, по умолчанию 100. count + offset ≤ 10 000.</summary>
        public int? Count { get; set; }

        /// <summary>Сколько записей пропустить. По умолчанию 0.</summary>
        public int? Offset { get; set; }

        /// <summary>Получатель, точное совпадение.</summary>
        public string? Recipient { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }

        /// <summary>ID канала.</summary>
        public string? MessageStream { get; set; }
    }

    /// <summary>Фильтр списка открытий или кликов. Даты — московское время.</summary>
    public sealed class MessageEventSearch : MessageEventFilter
    {
        /// <summary>Начало периода. По умолчанию — месяц назад.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Конец периода включительно; дата без времени — до конца дня. По умолчанию — сейчас.</summary>
        public DateTime? ToDate { get; set; }
    }

    /// <summary>Фильтр входящих писем (хранятся 45 дней). Даты — московское время.</summary>
    public sealed class InboundMessageSearch
    {
        /// <summary>1–500, по умолчанию 100.</summary>
        public int? Count { get; set; }

        /// <summary>Сколько записей пропустить. По умолчанию 0.</summary>
        public int? Offset { get; set; }

        /// <summary>Адрес, на который доставлено письмо (OriginalRecipient).</summary>
        public string? Recipient { get; set; }

        /// <summary>Email отправителя.</summary>
        public string? FromEmail { get; set; }

        /// <summary>Часть темы.</summary>
        public string? Subject { get; set; }

        /// <summary>Значение MailboxHash (часть адреса после «+»).</summary>
        public string? MailboxHash { get; set; }

        /// <summary>Статус письма.</summary>
        public InboundMessageStatus? Status { get; set; }

        /// <summary>Получено начиная с этого момента.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Получено до этого момента включительно; дата без времени — до конца дня.</summary>
        public DateTime? ToDate { get; set; }
    }

    /// <summary>Отправленное письмо (запись на одного получателя).</summary>
    public sealed class OutboundMessage
    {
        /// <summary>Получатель.</summary>
        public string? Recipient { get; set; }

        /// <summary>Время приёма письма (московское).</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? ReceivedAt { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }

        /// <summary>Отправитель.</summary>
        public string? From { get; set; }

        /// <summary>ID письма.</summary>
        [JsonPropertyName("MessageID")]
        public string? MessageId { get; set; }

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>ID канала («» или «0» у старых писем).</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MessageStream { get; set; }

        /// <summary>Сейчас всегда «Sent».</summary>
        public string? Status { get; set; }
    }

    /// <summary>Страница отправленных писем.</summary>
    public sealed class OutboundMessageList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Письма.</summary>
        public IReadOnlyList<OutboundMessage> Messages { get; set; } = [];
    }

    /// <summary>Открытие письма. Подробности хранятся 45 дней.</summary>
    public class MessageOpen
    {
        /// <summary>Получатель.</summary>
        public string? Recipient { get; set; }

        /// <summary>Время события (московское).</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? ReceivedAt { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }

        /// <summary>Отправитель.</summary>
        public string? From { get; set; }

        /// <summary>ID письма.</summary>
        [JsonPropertyName("MessageID")]
        public string? MessageId { get; set; }

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>ID канала.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MessageStream { get; set; }

        /// <summary>«Open» или «Click».</summary>
        public string? RecordType { get; set; }

        /// <summary>Операционная система.</summary>
        public string? Os { get; set; }

        /// <summary>Браузер или почтовый клиент.</summary>
        public string? Browser { get; set; }

        /// <summary>Веб-почта, через которую открыто письмо (например, «mail»); пусто, если не веб-почта.</summary>
        public string? Webservice { get; set; }

        /// <summary>User-Agent.</summary>
        public string? UserAgent { get; set; }

        /// <summary>Язык (заголовок Accept-Language).</summary>
        public string? Language { get; set; }

        /// <summary>Код региона.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Region { get; set; }

        /// <summary>Код страны.</summary>
        public string? Country { get; set; }
    }

    /// <summary>Клик по ссылке.</summary>
    public sealed class MessageClick : MessageOpen
    {
        /// <summary>Ссылка, по которой перешёл получатель.</summary>
        public string? OriginalLink { get; set; }
    }

    /// <summary>Страница открытий.</summary>
    public sealed class MessageOpenList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Открытия.</summary>
        public IReadOnlyList<MessageOpen> Opens { get; set; } = [];
    }

    /// <summary>Страница кликов.</summary>
    public sealed class MessageClickList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Клики.</summary>
        public IReadOnlyList<MessageClick> Clicks { get; set; } = [];
    }

    /// <summary>Адрес с разобранными частями.</summary>
    public sealed class EmailAddressInfo
    {
        /// <summary>Адрес.</summary>
        public string? Email { get; set; }

        /// <summary>Имя.</summary>
        public string? Name { get; set; }

        /// <summary>Часть адреса после «+».</summary>
        public string? MailboxHash { get; set; }
    }

    /// <summary>Входящее письмо в списке.</summary>
    public sealed class InboundMessageSummary
    {
        /// <summary>Отправитель.</summary>
        public string? From { get; set; }

        /// <summary>Имя отправителя.</summary>
        public string? FromName { get; set; }

        /// <summary>Отправитель, разобранный по частям.</summary>
        public EmailAddressInfo? FromFull { get; set; }

        /// <summary>Получатели.</summary>
        public string? To { get; set; }

        /// <summary>Адрес, на который письмо фактически доставлено.</summary>
        public string? OriginalRecipient { get; set; }

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>Дата письма.</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? Date { get; set; }

        /// <summary>Часть адреса после «+».</summary>
        public string? MailboxHash { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }

        /// <summary>ID письма.</summary>
        [JsonPropertyName("MessageID")]
        public string? MessageId { get; set; }

        /// <summary>ID канала.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MessageStream { get; set; }

        /// <summary>Статус обработки.</summary>
        public InboundMessageStatus? Status { get; set; }

        /// <summary>Число вложений.</summary>
        public int AttachmentsCount { get; set; }
    }

    /// <summary>Страница входящих писем.</summary>
    public sealed class InboundMessageList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Письма.</summary>
        public IReadOnlyList<InboundMessageSummary> InboundMessages { get; set; } = [];
    }

    /// <summary>Сведения о вложении входящего письма (без содержимого).</summary>
    public sealed class InboundAttachmentInfo
    {
        /// <summary>Название.</summary>
        public string? Name { get; set; }

        /// <summary>MIME-тип.</summary>
        public string? ContentType { get; set; }

        /// <summary>Размер в байтах.</summary>
        public long ContentLength { get; set; }

        /// <summary>CID встроенной картинки.</summary>
        [JsonPropertyName("ContentID")]
        public string? ContentId { get; set; }
    }

    /// <summary>Входящее письмо целиком (без содержимого вложений).</summary>
    public sealed class InboundMessageDetails
    {
        /// <summary>Имя отправителя.</summary>
        public string? FromName { get; set; }

        /// <summary>ID канала.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MessageStream { get; set; }

        /// <summary>Отправитель.</summary>
        public string? From { get; set; }

        /// <summary>Адрес, на который письмо фактически доставлено.</summary>
        public string? OriginalRecipient { get; set; }

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>ID письма.</summary>
        [JsonPropertyName("MessageID")]
        public string? MessageId { get; set; }

        /// <summary>Часть адреса после «+».</summary>
        public string? MailboxHash { get; set; }

        /// <summary>Текстовая версия.</summary>
        public string? TextBody { get; set; }

        /// <summary>HTML-версия.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>Текст ответа без процитированного предыдущего письма.</summary>
        public string? StrippedTextReply { get; set; }

        /// <summary>Сведения о вложениях.</summary>
        public IReadOnlyList<InboundAttachmentInfo> Attachments { get; set; } = [];

        /// <summary>Статус обработки.</summary>
        public InboundMessageStatus? Status { get; set; }

        /// <summary>Причина блокировки; пусто, если письмо не заблокировано.</summary>
        public string? BlockedReason { get; set; }

        /// <summary>Причина ошибки доставки на вебхук; пусто, если ошибки не было.</summary>
        public string? FailureReason { get; set; }

        /// <summary>Время получения.</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? ReceivedAt { get; set; }

        /// <summary>Время доставки на вебхук; null, если ещё не доставлено.</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? ProcessedAt { get; set; }

        /// <summary>Время отправки в обход правил; null, если обход не выполнялся.</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? BypassedAt { get; set; }

        /// <summary>Email пользователя или «API»; пусто, если обход не выполнялся.</summary>
        public string? BypassedBy { get; set; }

        /// <summary>Причина блокировки, которую обошли.</summary>
        public string? BypassedBlockReason { get; set; }
    }
}
