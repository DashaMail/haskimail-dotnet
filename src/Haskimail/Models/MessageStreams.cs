using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Haskimail.Internal;

namespace Haskimail.Models
{
    /// <summary>Канал отправки или приёма писем.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711", Justification = "Название сущности Haskimail API (MessageStream).")]
    public sealed class MessageStream
    {
        /// <summary>ID канала — передавайте его как MessageStream при отправке.</summary>
        [JsonPropertyName("ID")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Id { get; set; }

        /// <summary>ID сервера.</summary>
        [JsonPropertyName("ServerID")]
        public long ServerId { get; set; }

        /// <summary>Название.</summary>
        public string? Name { get; set; }

        /// <summary>Тип: transactional, broadcasts или inbound.</summary>
        public MessageStreamType MessageStreamType { get; set; }

        /// <summary>Время архивации (московское); null, если канал активен.</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? ArchivedAt { get; set; }

        /// <summary>Канал в архиве.</summary>
        [JsonIgnore]
        public bool IsArchived => ArchivedAt.HasValue;
    }

    /// <summary>Каналы сервера.</summary>
    public sealed class MessageStreamList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Включая архивные каналы.</summary>
        public IReadOnlyList<MessageStream> MessageStreams { get; set; } = [];
    }

    /// <summary>Создание канала.</summary>
    public sealed class CreateMessageStreamRequest
    {
        /// <summary>С ключом сервера можно не указывать; с ключом аккаунта обязателен.</summary>
        [JsonPropertyName("ServerID")]
        public long? ServerId { get; set; }

        /// <summary>Название канала.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Только Transactional или Broadcasts.</summary>
        public MessageStreamType MessageStreamType { get; set; } = MessageStreamType.Transactional;
    }

    /// <summary>Результат архивации канала.</summary>
    public sealed class ArchiveMessageStreamResult
    {
        /// <summary>ID канала.</summary>
        [JsonPropertyName("ID")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Id { get; set; }

        /// <summary>ID сервера.</summary>
        [JsonPropertyName("ServerID")]
        public long ServerId { get; set; }

        /// <summary>Время архивации (московское).</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? ArchivedAt { get; set; }

        /// <summary>Когда канал будет удалён окончательно — через 45 дней после архивации.</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? ExpectedPurgeDate { get; set; }
    }

    internal sealed class RenameMessageStreamRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
