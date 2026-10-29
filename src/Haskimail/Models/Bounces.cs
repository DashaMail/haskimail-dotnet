using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Haskimail.Internal;

namespace Haskimail.Models
{
    /// <summary>Количество возвратов одного типа.</summary>
    public sealed class BounceTypeCount
    {
        /// <summary>Тип возврата; null у итоговой записи «All».</summary>
        public BounceType? Type { get; set; }

        /// <summary>Название типа («All» — итог).</summary>
        public string? Name { get; set; }

        /// <summary>Количество.</summary>
        public int Count { get; set; }
    }

    /// <summary>Возвраты сервера по типам за последние 45 дней. Первый элемент («All») — итог.</summary>
    public sealed class DeliveryStats
    {
        /// <summary>Счётчики по типам; первый — итог «All».</summary>
        public IReadOnlyList<BounceTypeCount> Bounces { get; set; } = [];
    }

    /// <summary>Фильтр возвратов. Даты — московское время.</summary>
    public sealed class BounceSearch
    {
        /// <summary>По умолчанию 500.</summary>
        public int? Count { get; set; }

        /// <summary>Сколько записей пропустить. По умолчанию 0.</summary>
        public int? Offset { get; set; }

        /// <summary>Тип возврата.</summary>
        public BounceType? Type { get; set; }

        /// <summary>Email получателя, точное совпадение.</summary>
        public string? EmailFilter { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }

        /// <summary>ID письма.</summary>
        public string? MessageId { get; set; }

        /// <summary>ID канала.</summary>
        public string? MessageStream { get; set; }

        /// <summary>Начало периода (учитывается только дата). По умолчанию — месяц назад.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Конец периода включительно (учитывается только дата). По умолчанию — сегодня.</summary>
        public DateTime? ToDate { get; set; }
    }

    /// <summary>Возврат.</summary>
    public sealed class Bounce
    {
        /// <summary>Тип возврата.</summary>
        public BounceType? Type { get; set; }

        /// <summary>Название типа.</summary>
        public string? Name { get; set; }

        /// <summary>Адрес, на который письмо не доставлено.</summary>
        public string? Email { get; set; }

        /// <summary>Всегда «Bounce».</summary>
        public string? RecordType { get; set; }

        /// <summary>ID письма.</summary>
        [JsonPropertyName("MessageID")]
        public string? MessageId { get; set; }

        /// <summary>ID сервера.</summary>
        [JsonPropertyName("ServerID")]
        public long ServerId { get; set; }

        /// <summary>ID канала.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? MessageStream { get; set; }

        /// <summary>Ответ сервера получателя.</summary>
        public string? Description { get; set; }

        /// <summary>Время возврата (московское).</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? BouncedAt { get; set; }

        /// <summary>Код причины, например SMTP-код «550».</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? TypeCode { get; set; }

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>Тег письма.</summary>
        public string? Tag { get; set; }
    }

    /// <summary>Страница возвратов.</summary>
    public sealed class BounceList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Возвраты.</summary>
        public IReadOnlyList<Bounce> Bounces { get; set; } = [];
    }
}
