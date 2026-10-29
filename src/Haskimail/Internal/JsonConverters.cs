using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haskimail.Internal
{
    /// <summary>
    /// Время в ответах API: московское «YYYY-MM-DD HH:MM:SS» без смещения либо ISO 8601 со смещением.
    /// Пустая строка — null.
    /// </summary>
    internal class MoscowDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        internal static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

        private static readonly string[] LocalFormats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
        ];

        protected virtual TimeSpan AssumedOffset => MoscowOffset;

        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Ожидалась строка с датой, получено {reader.TokenType}.");

            var text = reader.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : Parse(text!, AssumedOffset);
        }

        internal static DateTimeOffset Parse(string text, TimeSpan assumedOffset)
        {
            text = text.Trim();
            if (DateTime.TryParseExact(text, LocalFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
                return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), assumedOffset);

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
                return withOffset;

            throw new JsonException($"Не удалось разобрать дату «{text}».");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is null) { writer.WriteNullValue(); return; }
            writer.WriteStringValue(value.Value.ToOffset(AssumedOffset).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Время в UTC без смещения (<c>event_time_utc</c> вебхуков).</summary>
    internal sealed class UtcDateTimeOffsetConverter : MoscowDateTimeOffsetConverter
    {
        protected override TimeSpan AssumedOffset => TimeSpan.Zero;
    }

    /// <summary>Дата «YYYY-MM-DD» (статистика по дням).</summary>
    internal sealed class DateOnlyConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            if (text != null && DateTime.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            if (text != null && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
                return dto.Date;
            throw new JsonException($"Не удалось разобрать дату «{text}».");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    /// <summary>Строка, которая приходит то строкой, то числом (например, <c>stream_id</c> в вебхуках).</summary>
    internal sealed class FlexibleStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                        return doc.RootElement.GetRawText();
                default:
                    throw new JsonException($"Ожидалась строка или число, получено {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue(); else writer.WriteStringValue(value);
        }
    }

    /// <summary>Целое число, которое может прийти строкой. Пустая строка — null.</summary>
    internal sealed class FlexibleInt64Converter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    return reader.GetInt64();
                case JsonTokenType.String:
                    var s = reader.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return null;
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
                    throw new JsonException($"Не удалось разобрать число «{s}».");
                default:
                    throw new JsonException($"Ожидалось число, получено {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue(); else writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>Логическое значение: true/false, «true»/«false», «1»/«0».</summary>
    internal sealed class FlexibleBooleanConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null: return null;
                case JsonTokenType.True: return true;
                case JsonTokenType.False: return false;
                case JsonTokenType.Number: return reader.GetInt64() != 0;
                case JsonTokenType.String:
                    var s = reader.GetString()?.Trim();
                    if (string.IsNullOrEmpty(s)) return null;
                    if (s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
                    if (s == "0" || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
                    throw new JsonException($"Не удалось разобрать логическое значение «{s}».");
                default:
                    throw new JsonException($"Ожидалось логическое значение, получено {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue(); else writer.WriteBooleanValue(value.Value);
        }
    }

    /// <summary>
    /// Словарь «строка → строка» из объекта или из JSON-строки с объектом.
    /// Нестроковые значения сохраняются как исходный JSON («5», «true»).
    /// Нужен для <c>custom_vars</c> (в разных вебхуках то строка, то объект) и SuggestedTemplateModel.
    /// </summary>
    internal sealed class StringDictionaryConverter : JsonConverter<IReadOnlyDictionary<string, string>?>
    {
        public override IReadOnlyDictionary<string, string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                    var text = reader.GetString();
                    if (string.IsNullOrWhiteSpace(text)) return null;
                    try
                    {
                        using var inner = JsonDocument.Parse(text!);
                        return inner.RootElement.ValueKind == JsonValueKind.Object ? ToDictionary(inner.RootElement) : null;
                    }
                    catch (JsonException)
                    {
                        return null;
                    }
                case JsonTokenType.StartObject:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                        return ToDictionary(doc.RootElement);
                default:
                    reader.Skip();
                    return null;
            }
        }

        private static Dictionary<string, string> ToDictionary(JsonElement obj)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var p in obj.EnumerateObject())
                result[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.GetRawText();
            return result;
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, string>? value, JsonSerializerOptions options)
        {
            if (value is null) { writer.WriteNullValue(); return; }
            writer.WriteStartObject();
            foreach (var kv in value) writer.WriteString(kv.Key, kv.Value);
            writer.WriteEndObject();
        }
    }

    /// <summary>Базовый конвертер для расширяемых enum-структур (значение — строка).</summary>
    internal abstract class StringValueConverter<T> : JsonConverter<T> where T : struct
    {
        protected abstract T Create(string value);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return default;
            if (reader.TokenType == JsonTokenType.String) return Create(reader.GetString() ?? string.Empty);
            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False || reader.TokenType == JsonTokenType.Number)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return Create(doc.RootElement.GetRawText());
            }
            throw new JsonException($"Ожидалась строка, получено {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
