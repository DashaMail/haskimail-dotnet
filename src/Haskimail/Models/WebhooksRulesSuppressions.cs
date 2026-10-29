using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Haskimail.Internal;

namespace Haskimail.Models
{
    // ---------------- Вебхуки (настройка) ----------------

    /// <summary>Включено ли событие вебхука.</summary>
    public sealed class WebhookTrigger
    {
        /// <summary>Создаёт настройку события.</summary>
        public WebhookTrigger() { }

        /// <summary>Создаёт настройку события.</summary>
        public WebhookTrigger(bool enabled) { Enabled = enabled; }

        /// <summary>Событие включено.</summary>
        public bool Enabled { get; set; }

        /// <summary>Включить событие.</summary>
        public static WebhookTrigger On => new(true);

        /// <summary>Выключить событие.</summary>
        public static WebhookTrigger Off => new(false);
    }

    /// <summary>
    /// События вебхука. При создании незаданные события выключены; при изменении — остаются как были.
    /// </summary>
    public sealed class WebhookTriggers
    {
        /// <summary>Доставка письма.</summary>
        public WebhookTrigger? Delivery { get; set; }

        /// <summary>Возврат.</summary>
        public WebhookTrigger? Bounce { get; set; }

        /// <summary>Жалоба на спам.</summary>
        public WebhookTrigger? SpamComplaint { get; set; }

        /// <summary>Открытие.</summary>
        public WebhookTrigger? Open { get; set; }

        /// <summary>Клик по ссылке.</summary>
        public WebhookTrigger? Click { get; set; }

        /// <summary>Изменение стоп-листа.</summary>
        public WebhookTrigger? SubscriptionChange { get; set; }
    }

    /// <summary>Вебхук канала.</summary>
    public sealed class Webhook
    {
        /// <summary>ID вебхука.</summary>
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        /// <summary>URL, на который приходят события.</summary>
        public string? Url { get; set; }

        /// <summary>ID канала.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MessageStream { get; set; }

        /// <summary>Включённые события.</summary>
        public WebhookTriggers Triggers { get; set; } = new WebhookTriggers();
    }

    /// <summary>Вебхуки.</summary>
    public sealed class WebhookList
    {
        /// <summary>Вебхуки.</summary>
        public IReadOnlyList<Webhook> Webhooks { get; set; } = [];
    }

    /// <summary>Создание вебхука.</summary>
    public sealed class CreateWebhookRequest
    {
        /// <summary>
        /// URL для POST-запросов с событиями. Рекомендуется HTTPS; для Basic-авторизации:
        /// <c>https://user:password@example.com/webhook</c>.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Канал. Без него — транзакционный канал сервера по умолчанию; с ключом аккаунта обязателен.</summary>
        public string? MessageStream { get; set; }

        /// <summary>События; незаданные выключены.</summary>
        public WebhookTriggers? Triggers { get; set; }
    }

    /// <summary>Изменение вебхука: только меняемые поля. Канал изменить нельзя.</summary>
    public sealed class UpdateWebhookRequest
    {
        /// <summary>Новый URL.</summary>
        public string? Url { get; set; }

        /// <summary>События, которые нужно включить или выключить.</summary>
        public WebhookTriggers? Triggers { get; set; }
    }

    // ---------------- Правила для входящих ----------------

    /// <summary>Правило блокировки входящих.</summary>
    public sealed class InboundRule
    {
        /// <summary>ID правила.</summary>
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        /// <summary>Email-адрес или домен.</summary>
        public string? Rule { get; set; }
    }

    /// <summary>Страница правил.</summary>
    public sealed class InboundRuleList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Правила.</summary>
        public IReadOnlyList<InboundRule> InboundRules { get; set; } = [];
    }

    internal sealed class CreateInboundRuleRequest
    {
        public string Rule { get; set; } = string.Empty;
    }

    // ---------------- Стоп-листы ----------------

    /// <summary>Адрес в стоп-листе.</summary>
    public sealed class Suppression
    {
        /// <summary>Адрес.</summary>
        public string? EmailAddress { get; set; }

        /// <summary>Причина.</summary>
        public SuppressionReason? SuppressionReason { get; set; }

        /// <summary>Всегда «Recipient».</summary>
        public string? Origin { get; set; }

        /// <summary>Когда адрес попал в стоп-лист (московское время).</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? CreatedAt { get; set; }
    }

    /// <summary>Стоп-лист канала.</summary>
    public sealed class SuppressionList
    {
        /// <summary>Адреса.</summary>
        public IReadOnlyList<Suppression> Suppressions { get; set; } = [];
    }

    /// <summary>Результат по одному адресу.</summary>
    public sealed class SuppressionStatus
    {
        /// <summary>Адрес.</summary>
        public string? EmailAddress { get; set; }

        /// <summary>«Suppressed» (добавлен), «Deleted» (удалён) или «Failed».</summary>
        public string? Status { get; set; }

        /// <summary>Причина неудачи; пусто при успехе.</summary>
        public string? Message { get; set; }

        /// <summary>Операция для адреса не выполнена.</summary>
        [JsonIgnore]
        public bool IsFailed => string.Equals(Status, "Failed", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Результат изменения стоп-листа.</summary>
    public sealed class SuppressionChangeResult
    {
        /// <summary>Результат по каждому адресу, в порядке запроса.</summary>
        public IReadOnlyList<SuppressionStatus> Suppressions { get; set; } = [];
    }

    internal sealed class SuppressionEntry
    {
        public string EmailAddress { get; set; } = string.Empty;
    }

    internal sealed class SuppressionRequest
    {
        public List<SuppressionEntry> Suppressions { get; set; } = [];
    }
}
