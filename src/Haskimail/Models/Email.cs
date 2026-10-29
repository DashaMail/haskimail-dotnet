using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Haskimail.Internal;

namespace Haskimail.Models
{
    /// <summary>Пользовательский заголовок письма, например свой Message-ID.</summary>
    public sealed class MessageHeader
    {
        /// <summary>Создаёт заголовок.</summary>
        public MessageHeader() { }

        /// <summary>Создаёт заголовок.</summary>
        public MessageHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>Имя заголовка, например «Message-ID».</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Значение заголовка.</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Вложение. Письмо вместе с вложениями после Base64 — не больше 10 МБ.
    /// Для картинки внутри HTML задайте <see cref="ContentId"/> и сошлитесь на неё как <c>&lt;img src="cid:..."&gt;</c>.
    /// </summary>
    public sealed class Attachment
    {
        /// <summary>Имя файла, которое увидит получатель.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Содержимое в Base64.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>MIME-тип, например application/pdf.</summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>CID встроенной картинки.</summary>
        [JsonPropertyName("ContentID")]
        public string? ContentId { get; set; }

        /// <summary>Вложение из массива байт (кодируется в Base64).</summary>
        public static Attachment FromBytes(string name, byte[] content, string contentType, string? contentId = null)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(content, nameof(content));
            Guard.NotEmpty(contentType, nameof(contentType));
            return new Attachment
            {
                Name = name,
                Content = Convert.ToBase64String(content),
                ContentType = contentType,
                ContentId = contentId,
            };
        }

        /// <summary>Вложение из потока (читается целиком и кодируется в Base64).</summary>
        public static Attachment FromStream(string name, Stream content, string contentType, string? contentId = null)
        {
            Guard.NotNull(content, nameof(content));
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            return FromBytes(name, buffer.ToArray(), contentType, contentId);
        }

        /// <summary>Вложение из файла; имя берётся из пути.</summary>
        public static Attachment FromFile(string path, string contentType, string? contentId = null)
        {
            Guard.NotEmpty(path, nameof(path));
            return FromBytes(Path.GetFileName(path), File.ReadAllBytes(path), contentType, contentId);
        }

        private static readonly HashSet<string> ForbiddenExtensionSet = new(StringComparer.OrdinalIgnoreCase)
        {
            "vbs", "exe", "bin", "bat", "chm", "com", "cpl", "crt", "hlp", "hta", "inf", "ins", "isp", "jse", "lnk", "mdb",
            "pcd", "pif", "reg", "scr", "sct", "shs", "vbe", "vba", "wsf", "wsh", "wsl", "msc", "msi", "msp", "mst",
        };

        /// <summary>
        /// Расширения, с которыми Haskimail отклоняет письмо целиком (исполняемые и опасные файлы).
        /// </summary>
        public static IReadOnlyCollection<string> ForbiddenExtensions { get; } = new System.Collections.ObjectModel.ReadOnlyCollection<string>([.. ForbiddenExtensionSet]);

        internal static bool IsForbiddenExtension(string extension) => ForbiddenExtensionSet.Contains(extension);


    }

    /// <summary>Общие поля письма для обычной отправки и отправки по шаблону.</summary>
    [JsonDerivedType(typeof(EmailMessage))]
    [JsonDerivedType(typeof(TemplatedEmailMessage))]
    public abstract class EmailMessageBase
    {
        /// <summary>
        /// Отправитель на подтверждённом домене, до 255 символов. Можно с именем: <c>Магазин &lt;shop@example.com&gt;</c>;
        /// имя со знаками препинания берите в кавычки.
        /// </summary>
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// Получатели через запятую. Вместе с Cc и Bcc — не больше 50. Каждый получатель получает отдельное письмо со своим MessageID.
        /// </summary>
        public string To { get; set; } = string.Empty;

        /// <summary>Получатели копии через запятую.</summary>
        public string? Cc { get; set; }

        /// <summary>Получатели скрытой копии через запятую.</summary>
        public string? Bcc { get; set; }

        /// <summary>Тема, до 2000 символов. У письма по шаблону без темы берётся тема шаблона.</summary>
        public string? Subject { get; set; }

        /// <summary>Один тег, до 1000 символов. По нему фильтруют письма, открытия, клики и возвраты.</summary>
        public string? Tag { get; set; }

        /// <summary>Адрес для ответа (заголовок Reply-To).</summary>
        public string? ReplyTo { get; set; }

        /// <summary>Дополнительные заголовки письма.</summary>
        public List<MessageHeader>? Headers { get; set; }

        /// <summary>Отслеживать открытия. null — действует настройка сервера.</summary>
        public bool? TrackOpens { get; set; }

        /// <summary>Отслеживать клики по ссылкам. null — действует настройка сервера.</summary>
        public bool? TrackLinks { get; set; }

        /// <summary>Произвольные данные «ключ — значение». Возвращаются в вебхуках как <c>custom_vars</c>.</summary>
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>Вложения и встроенные картинки.</summary>
        public List<Attachment>? Attachments { get; set; }

        /// <summary>
        /// ID канала (см. <see cref="Api.IMessageStreamsApi.ListAsync"/>). null — транзакционный канал сервера по умолчанию.
        /// </summary>
        public string? MessageStream { get; set; }
    }

    /// <summary>Письмо с собственным содержимым.</summary>
    public sealed class EmailMessage : EmailMessageBase
    {
        /// <summary>HTML-версия, до 5 МБ. Нужен HtmlBody, TextBody или оба.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>Текстовая версия, до 5 МБ.</summary>
        public string? TextBody { get; set; }
    }

    /// <summary>Письмо по шаблону. Нужен <see cref="TemplateId"/> или <see cref="TemplateAlias"/>.</summary>
    public sealed class TemplatedEmailMessage : EmailMessageBase
    {
        /// <summary>ID шаблона.</summary>
        public long? TemplateId { get; set; }

        /// <summary>Название шаблона — вместо ID.</summary>
        public string? TemplateAlias { get; set; }

        /// <summary>
        /// Подстановки. Замена буквальная: ключ — фрагмент текста шаблона вместе с разделителями,
        /// <c>{"{{name}}": "Иван"}</c>. Удобно собирать через <see cref="TemplatePlaceholders"/>.
        /// </summary>
        public Dictionary<string, string>? TemplateModel { get; set; }
    }

    /// <summary>
    /// Помощник для <see cref="TemplatedEmailMessage.TemplateModel"/>: оборачивает имена в разделители.
    /// <c>TemplatePlaceholders.From(("name", "Иван"))</c> → <c>{"{{name}}": "Иван"}</c>.
    /// </summary>
    public static class TemplatePlaceholders
    {
        /// <summary>Открывающий разделитель по умолчанию.</summary>
        public const string DefaultOpen = "{{";

        /// <summary>Закрывающий разделитель по умолчанию.</summary>
        public const string DefaultClose = "}}";

        /// <summary>Собрать подстановки, обернув имена без разделителей в {{ }}.</summary>
        public static Dictionary<string, string> From(params (string Name, string Value)[] values) =>
            From(Guard.NotNull(values, nameof(values)).Select(v => new KeyValuePair<string, string>(v.Name, v.Value)));

        /// <summary>Собрать подстановки, обернув имена без разделителей в {{ }}.</summary>
        public static Dictionary<string, string> From(IEnumerable<KeyValuePair<string, string>> values, string open = DefaultOpen, string close = DefaultClose)
        {
            Guard.NotNull(values, nameof(values));
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in values)
            {
                var key = Guard.NotEmpty(kv.Key, nameof(values));
                if (!key.StartsWith(open, StringComparison.Ordinal)) key = open + key.Trim() + close;
                result[key] = kv.Value ?? string.Empty;
            }
            return result;
        }
    }

    /// <summary>Результат приёма письма (одиночная отправка или элемент пакета).</summary>
    public sealed class SendResult
    {
        /// <summary>Получатели письма.</summary>
        public string? To { get; set; }

        /// <summary>Время приёма (московское).</summary>
        [JsonConverter(typeof(MoscowDateTimeOffsetConverter))]
        public DateTimeOffset? SubmittedAt { get; set; }

        /// <summary>
        /// ID письма. Для нескольких получателей — через запятую в том же порядке (см. <see cref="MessageIds"/>).
        /// null, если письмо отклонено.
        /// </summary>
        [JsonPropertyName("MessageID")]
        public string? MessageId { get; set; }

        /// <summary>0 — письмо принято; иначе код ошибки (<see cref="HaskimailErrorCodes"/>).</summary>
        public int ErrorCode { get; set; }

        /// <summary>Описание результата.</summary>
        public string? Message { get; set; }

        /// <summary>Письмо принято.</summary>
        [JsonIgnore]
        public bool IsSuccess => ErrorCode == 0;

        private static readonly char[] IdSeparators = [','];

        /// <summary>ID писем по получателям.</summary>
        [JsonIgnore]
        public IReadOnlyList<string> MessageIds =>
            string.IsNullOrEmpty(MessageId)
                ? []
                : MessageId!.Split(IdSeparators, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
    }

    /// <summary>Помощники для результатов пакетной отправки.</summary>
    public static class SendResultExtensions
    {
        /// <summary>Отклонённые письма вместе с их номерами в пакете.</summary>
        public static IReadOnlyList<(int Index, SendResult Result)> GetFailures(this IReadOnlyList<SendResult> results)
        {
            Guard.NotNull(results, nameof(results));
            return results.Select((r, i) => (Index: i, Result: r)).Where(x => !x.Result.IsSuccess).ToArray();
        }

        /// <summary>
        /// Выбросить <see cref="HaskimailBatchException"/>, если хоть одно письмо пакета отклонено.
        /// </summary>
        public static IReadOnlyList<SendResult> EnsureAllSucceeded(this IReadOnlyList<SendResult> results)
        {
            var failures = results.GetFailures();
            if (failures.Count > 0) throw new HaskimailBatchException(results, failures);
            return results;
        }
    }
}
