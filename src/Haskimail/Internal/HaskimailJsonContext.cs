using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haskimail.Models;
using Haskimail.Webhooks;

namespace Haskimail.Internal
{
    /// <summary>
    /// Сериализация без рефлексии (source generator): работает с Native AOT и тримминга не боится.
    /// Имена полей API — PascalCase, поэтому политика именования не задаётся. null-поля в запросах не отправляются,
    /// неизвестные поля ответа игнорируются (новые поля API ничего не ломают).
    /// </summary>
    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true)]
    // Отправка
    [JsonSerializable(typeof(EmailMessage))]
    [JsonSerializable(typeof(TemplatedEmailMessage))]
    [JsonSerializable(typeof(List<EmailMessageBase>))]
    [JsonSerializable(typeof(BatchWithTemplatesRequest))]
    [JsonSerializable(typeof(SendResult))]
    [JsonSerializable(typeof(IReadOnlyList<SendResult>))]
    // Общее
    [JsonSerializable(typeof(ApiResult))]
    // Шаблоны
    [JsonSerializable(typeof(Template))]
    [JsonSerializable(typeof(TemplateSummary))]
    [JsonSerializable(typeof(TemplateList))]
    [JsonSerializable(typeof(CreateTemplateRequest))]
    [JsonSerializable(typeof(UpdateTemplateRequest))]
    [JsonSerializable(typeof(PushTemplatesRequest))]
    [JsonSerializable(typeof(ValidateTemplateRequest))]
    [JsonSerializable(typeof(TemplateValidationResult))]
    // Сообщения
    [JsonSerializable(typeof(OutboundMessage))]
    [JsonSerializable(typeof(OutboundMessageList))]
    [JsonSerializable(typeof(MessageOpenList))]
    [JsonSerializable(typeof(MessageClickList))]
    [JsonSerializable(typeof(InboundMessageList))]
    [JsonSerializable(typeof(InboundMessageDetails))]
    // Возвраты
    [JsonSerializable(typeof(DeliveryStats))]
    [JsonSerializable(typeof(BounceList))]
    // Серверы
    [JsonSerializable(typeof(Server))]
    [JsonSerializable(typeof(ServerList))]
    [JsonSerializable(typeof(CreateServerRequest))]
    [JsonSerializable(typeof(UpdateServerRequest))]
    [JsonSerializable(typeof(IssueServerTokenRequest))]
    [JsonSerializable(typeof(ServerToken))]
    // Каналы
    [JsonSerializable(typeof(MessageStream))]
    [JsonSerializable(typeof(MessageStreamList))]
    [JsonSerializable(typeof(CreateMessageStreamRequest))]
    [JsonSerializable(typeof(RenameMessageStreamRequest))]
    [JsonSerializable(typeof(ArchiveMessageStreamResult))]
    // Домены
    [JsonSerializable(typeof(DomainList))]
    [JsonSerializable(typeof(DomainDetails))]
    [JsonSerializable(typeof(CreateDomainRequest))]
    [JsonSerializable(typeof(DkimVerificationResult))]
    [JsonSerializable(typeof(SpfVerificationResult))]
    // Статистика
    [JsonSerializable(typeof(OutboundOverview))]
    [JsonSerializable(typeof(DailyStats<SentDay>))]
    [JsonSerializable(typeof(DailyStats<BounceDay>))]
    [JsonSerializable(typeof(DailyStats<SpamComplaintDay>))]
    [JsonSerializable(typeof(DailyStats<OpenDay>))]
    [JsonSerializable(typeof(DailyStats<ClickDay>))]
    [JsonSerializable(typeof(PlatformStats))]
    [JsonSerializable(typeof(JsonElement))]
    // Вебхуки, правила, стоп-листы
    [JsonSerializable(typeof(Webhook))]
    [JsonSerializable(typeof(WebhookList))]
    [JsonSerializable(typeof(CreateWebhookRequest))]
    [JsonSerializable(typeof(UpdateWebhookRequest))]
    [JsonSerializable(typeof(InboundRule))]
    [JsonSerializable(typeof(InboundRuleList))]
    [JsonSerializable(typeof(CreateInboundRuleRequest))]
    [JsonSerializable(typeof(SuppressionList))]
    [JsonSerializable(typeof(SuppressionRequest))]
    [JsonSerializable(typeof(SuppressionChangeResult))]
    // Входящие запросы вебхуков
    [JsonSerializable(typeof(WebhookEvent))]
    [JsonSerializable(typeof(List<WebhookEvent>))]
    [JsonSerializable(typeof(InboundWebhookMessage))]
    internal sealed partial class HaskimailJsonContext : JsonSerializerContext
    {
        /// <summary>
        /// Экземпляр с кодировщиком, который не экранирует кириллицу и HTML: тело письма короче,
        /// а JSON остаётся корректным.
        /// </summary>
        public static HaskimailJsonContext Api { get; } = new HaskimailJsonContext(new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    internal sealed class BatchWithTemplatesRequest
    {
        public List<TemplatedEmailMessage> Messages { get; set; } = [];
    }
}
