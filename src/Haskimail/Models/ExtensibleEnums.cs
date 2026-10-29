using System;
using System.Text.Json.Serialization;
using Haskimail.Internal;

// «Расширяемые enum»: значения API как структуры со строкой внутри.
// В отличие от обычного enum, новое значение, добавленное в API, не ломает разбор ответа —
// оно просто сохраняется как есть. Сравнение без учёта регистра: API отвечает «transactional»,
// а принимает «Transactional».

namespace Haskimail.Models
{
    internal static class StringValue
    {
        public static bool Equal(string? a, string? b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        public static int Hash(string? a) => StringComparer.OrdinalIgnoreCase.GetHashCode(a ?? string.Empty);
        public static string NotNull(string value, string paramName) => value ?? throw new ArgumentNullException(paramName);
    }

    /// <summary>Тип канала.</summary>
    [JsonConverter(typeof(MessageStreamTypeConverter))]
    public readonly struct MessageStreamType : IEquatable<MessageStreamType>
    {
        private readonly string? _value;

        /// <summary>Значение API, в том числе неизвестное SDK.</summary>
        public MessageStreamType(string value) { _value = StringValue.NotNull(value, nameof(value)); }

        /// <summary>Индивидуальные письма в ответ на действия пользователя.</summary>
        public static MessageStreamType Transactional { get; } = new MessageStreamType("Transactional");

        /// <summary>Массовые рассылки.</summary>
        public static MessageStreamType Broadcasts { get; } = new MessageStreamType("Broadcasts");

        /// <summary>Входящие. Создаётся автоматически, по одному на сервер.</summary>
        public static MessageStreamType Inbound { get; } = new MessageStreamType("Inbound");

        /// <inheritdoc/>
        public bool Equals(MessageStreamType other) => StringValue.Equal(_value, other._value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MessageStreamType other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => StringValue.Hash(_value);

        /// <inheritdoc/>
        public override string ToString() => _value ?? string.Empty;

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator ==(MessageStreamType left, MessageStreamType right) => left.Equals(right);

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator !=(MessageStreamType left, MessageStreamType right) => !left.Equals(right);
    }

    /// <summary>Режим отслеживания ссылок на уровне сервера.</summary>
    [JsonConverter(typeof(LinkTrackingModeConverter))]
    public readonly struct LinkTrackingMode : IEquatable<LinkTrackingMode>
    {
        private readonly string? _value;

        /// <summary>Значение API, в том числе неизвестное SDK.</summary>
        public LinkTrackingMode(string value) { _value = StringValue.NotNull(value, nameof(value)); }

        /// <summary>Ссылки не отслеживаются (по умолчанию).</summary>
        public static LinkTrackingMode None { get; } = new LinkTrackingMode("None");

        /// <summary>Ссылки заменяются в HTML и текстовой части.</summary>
        public static LinkTrackingMode HtmlAndText { get; } = new LinkTrackingMode("HtmlAndText");

        /// <summary>Ссылки заменяются только в HTML.</summary>
        public static LinkTrackingMode HtmlOnly { get; } = new LinkTrackingMode("HtmlOnly");

        /// <summary>Ссылки заменяются только в текстовой части.</summary>
        public static LinkTrackingMode TextOnly { get; } = new LinkTrackingMode("TextOnly");

        /// <inheritdoc/>
        public bool Equals(LinkTrackingMode other) => StringValue.Equal(_value, other._value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is LinkTrackingMode other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => StringValue.Hash(_value);

        /// <inheritdoc/>
        public override string ToString() => _value ?? string.Empty;

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator ==(LinkTrackingMode left, LinkTrackingMode right) => left.Equals(right);

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator !=(LinkTrackingMode left, LinkTrackingMode right) => !left.Equals(right);
    }

    /// <summary>Тип доставки сервера. Задаётся только при создании.</summary>
    [JsonConverter(typeof(DeliveryTypeConverter))]
    public readonly struct DeliveryType : IEquatable<DeliveryType>
    {
        private readonly string? _value;

        /// <summary>Значение API, в том числе неизвестное SDK.</summary>
        public DeliveryType(string value) { _value = StringValue.NotNull(value, nameof(value)); }

        /// <summary>Обычная доставка.</summary>
        public static DeliveryType Live { get; } = new DeliveryType("Live");

        /// <summary>Песочница: письма проходят проверки и видны в статистике и вебхуках, но не доставляются.</summary>
        public static DeliveryType Sandbox { get; } = new DeliveryType("Sandbox");

        /// <inheritdoc/>
        public bool Equals(DeliveryType other) => StringValue.Equal(_value, other._value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DeliveryType other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => StringValue.Hash(_value);

        /// <inheritdoc/>
        public override string ToString() => _value ?? string.Empty;

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator ==(DeliveryType left, DeliveryType right) => left.Equals(right);

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator !=(DeliveryType left, DeliveryType right) => !left.Equals(right);
    }

    /// <summary>Цвет сервера в списке. Неизвестный цвет API превращает в purple.</summary>
    [JsonConverter(typeof(ServerColorConverter))]
    public readonly struct ServerColor : IEquatable<ServerColor>
    {
        private readonly string? _value;

        /// <summary>Значение API, в том числе неизвестное SDK.</summary>
        public ServerColor(string value) { _value = StringValue.NotNull(value, nameof(value)); }

        /// <summary>Фиолетовый.</summary>
        public static ServerColor Purple { get; } = new ServerColor("purple");

        /// <summary>Синий.</summary>
        public static ServerColor Blue { get; } = new ServerColor("blue");

        /// <summary>Бирюзовый.</summary>
        public static ServerColor Turquoise { get; } = new ServerColor("turquoise");

        /// <summary>Зелёный.</summary>
        public static ServerColor Green { get; } = new ServerColor("green");

        /// <summary>Красный.</summary>
        public static ServerColor Red { get; } = new ServerColor("red");

        /// <summary>Оранжевый.</summary>
        public static ServerColor Orange { get; } = new ServerColor("orange");

        /// <summary>Жёлтый.</summary>
        public static ServerColor Yellow { get; } = new ServerColor("yellow");

        /// <summary>Серый.</summary>
        public static ServerColor Grey { get; } = new ServerColor("grey");

        /// <inheritdoc/>
        public bool Equals(ServerColor other) => StringValue.Equal(_value, other._value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is ServerColor other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => StringValue.Hash(_value);

        /// <inheritdoc/>
        public override string ToString() => _value ?? string.Empty;

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator ==(ServerColor left, ServerColor right) => left.Equals(right);

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator !=(ServerColor left, ServerColor right) => !left.Equals(right);
    }

    /// <summary>
    /// Тип возврата. В списке возвратов блокировка и спам называются BlkBounce и SpmBounce,
    /// в статистике доставки — BulkBounce и SpamBlock; фильтр принимает оба варианта.
    /// </summary>
    [JsonConverter(typeof(BounceTypeConverter))]
    public readonly struct BounceType : IEquatable<BounceType>
    {
        private readonly string? _value;

        /// <summary>Значение API, в том числе неизвестное SDK.</summary>
        public BounceType(string value) { _value = StringValue.NotNull(value, nameof(value)); }

        /// <summary>Постоянная ошибка: адреса нет или ящик недоступен навсегда.</summary>
        public static BounceType HardBounce { get; } = new BounceType("HardBounce");

        /// <summary>Временная ошибка: ящик переполнен или сервер получателя недоступен.</summary>
        public static BounceType SoftBounce { get; } = new BounceType("SoftBounce");

        /// <summary>Блокировка политикой сервера получателя или из-за репутации (название в списке возвратов).</summary>
        public static BounceType BlkBounce { get; } = new BounceType("BlkBounce");

        /// <summary>Отклонено как спам (название в списке возвратов).</summary>
        public static BounceType SpmBounce { get; } = new BounceType("SpmBounce");

        /// <summary>Блокировка (название в статистике доставки).</summary>
        public static BounceType BulkBounce { get; } = new BounceType("BulkBounce");

        /// <summary>Отклонено как спам (название в статистике доставки).</summary>
        public static BounceType SpamBlock { get; } = new BounceType("SpamBlock");

        /// <summary>Блокировка — в любом из двух написаний API.</summary>
        public bool IsBlock => Equals(BlkBounce) || Equals(BulkBounce);

        /// <summary>Отказ как спам — в любом из двух написаний API.</summary>
        public bool IsSpam => Equals(SpmBounce) || Equals(SpamBlock);

        /// <inheritdoc/>
        public bool Equals(BounceType other) => StringValue.Equal(_value, other._value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is BounceType other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => StringValue.Hash(_value);

        /// <inheritdoc/>
        public override string ToString() => _value ?? string.Empty;

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator ==(BounceType left, BounceType right) => left.Equals(right);

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator !=(BounceType left, BounceType right) => !left.Equals(right);
    }

    /// <summary>Причина попадания в стоп-лист.</summary>
    [JsonConverter(typeof(SuppressionReasonConverter))]
    public readonly struct SuppressionReason : IEquatable<SuppressionReason>
    {
        private readonly string? _value;

        /// <summary>Значение API, в том числе неизвестное SDK.</summary>
        public SuppressionReason(string value) { _value = StringValue.NotNull(value, nameof(value)); }

        /// <summary>Жёсткий возврат.</summary>
        public static SuppressionReason HardBounce { get; } = new SuppressionReason("HardBounce");

        /// <summary>Жалоба на спам.</summary>
        public static SuppressionReason SpamComplaint { get; } = new SuppressionReason("SpamComplaint");

        /// <summary>Добавлен вручную.</summary>
        public static SuppressionReason ManualSuppression { get; } = new SuppressionReason("ManualSuppression");

        /// <inheritdoc/>
        public bool Equals(SuppressionReason other) => StringValue.Equal(_value, other._value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is SuppressionReason other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => StringValue.Hash(_value);

        /// <inheritdoc/>
        public override string ToString() => _value ?? string.Empty;

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator ==(SuppressionReason left, SuppressionReason right) => left.Equals(right);

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator !=(SuppressionReason left, SuppressionReason right) => !left.Equals(right);
    }

    /// <summary>Статус входящего письма.</summary>
    [JsonConverter(typeof(InboundMessageStatusConverter))]
    public readonly struct InboundMessageStatus : IEquatable<InboundMessageStatus>
    {
        private readonly string? _value;

        /// <summary>Значение API, в том числе неизвестное SDK.</summary>
        public InboundMessageStatus(string value) { _value = StringValue.NotNull(value, nameof(value)); }

        /// <summary>В очереди.</summary>
        public static InboundMessageStatus Queued { get; } = new InboundMessageStatus("queued");

        /// <summary>Доставлено на вебхук.</summary>
        public static InboundMessageStatus Processed { get; } = new InboundMessageStatus("processed");

        /// <summary>Заблокировано правилом или спам-фильтром — можно отправить в обход (BypassInboundAsync).</summary>
        public static InboundMessageStatus Blocked { get; } = new InboundMessageStatus("blocked");

        /// <summary>Вебхук не принял письмо — можно повторить (RetryInboundAsync).</summary>
        public static InboundMessageStatus Failed { get; } = new InboundMessageStatus("failed");

        /// <summary>Запланирована повторная доставка.</summary>
        public static InboundMessageStatus Scheduled { get; } = new InboundMessageStatus("scheduled");

        /// <inheritdoc/>
        public bool Equals(InboundMessageStatus other) => StringValue.Equal(_value, other._value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is InboundMessageStatus other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => StringValue.Hash(_value);

        /// <inheritdoc/>
        public override string ToString() => _value ?? string.Empty;

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator ==(InboundMessageStatus left, InboundMessageStatus right) => left.Equals(right);

        /// <summary>Сравнение без учёта регистра.</summary>
        public static bool operator !=(InboundMessageStatus left, InboundMessageStatus right) => !left.Equals(right);
    }

    internal sealed class MessageStreamTypeConverter : StringValueConverter<MessageStreamType>
    {
        protected override MessageStreamType Create(string value) => new(value);
    }

    internal sealed class LinkTrackingModeConverter : StringValueConverter<LinkTrackingMode>
    {
        protected override LinkTrackingMode Create(string value) => new(value);
    }

    internal sealed class DeliveryTypeConverter : StringValueConverter<DeliveryType>
    {
        protected override DeliveryType Create(string value) => new(value);
    }

    internal sealed class ServerColorConverter : StringValueConverter<ServerColor>
    {
        protected override ServerColor Create(string value) => new(value);
    }

    internal sealed class BounceTypeConverter : StringValueConverter<BounceType>
    {
        protected override BounceType Create(string value) => new(value);
    }

    internal sealed class SuppressionReasonConverter : StringValueConverter<SuppressionReason>
    {
        protected override SuppressionReason Create(string value) => new(value);
    }

    internal sealed class InboundMessageStatusConverter : StringValueConverter<InboundMessageStatus>
    {
        protected override InboundMessageStatus Create(string value) => new(value);
    }
}
