using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Haskimail.Models;

namespace Haskimail.Internal
{
    /// <summary>
    /// Проверка писем на документированные ограничения до отправки: ошибка видна сразу и с понятным текстом,
    /// а не после сетевого запроса. Сервер всё равно остаётся последней инстанцией.
    /// </summary>
    internal static class RequestValidator
    {
        public const int MaxBatchSize = 500;
        public const int MaxRecipients = 50;
        public const int MaxFromLength = 255;
        public const int MaxSubjectLength = 2000;
        public const int MaxTagLength = 1000;
        public const int MaxBodyBytes = 5 * 1024 * 1024;
        /// <summary>Размер всего письма: тело плюс вложения после Base64.</summary>
        public const int MaxMessageBytes = 10 * 1024 * 1024;

        public static void ValidateMessage(EmailMessageBase message, bool deep, int? index = null)
        {
            var where = index.HasValue ? $"Письмо [{index.Value}]: " : string.Empty;
            if (message is null)
            {
                if (index.HasValue) throw new ArgumentException(where + "письмо равно null.", nameof(message));
                throw new ArgumentNullException(nameof(message));
            }

            Require(!string.IsNullOrWhiteSpace(message.From), where + "не задан отправитель (From).");
            Require(!string.IsNullOrWhiteSpace(message.To), where + "не заданы получатели (To).");

            switch (message)
            {
                case EmailMessage plain:
                    Require(!string.IsNullOrEmpty(plain.HtmlBody) || !string.IsNullOrEmpty(plain.TextBody),
                        where + "нужно задать HtmlBody, TextBody или оба.");
                    if (deep)
                    {
                        RequireBodySize(plain.HtmlBody, "HtmlBody", where);
                        RequireBodySize(plain.TextBody, "TextBody", where);
                    }
                    break;
                case TemplatedEmailMessage templated:
                    Require(templated.TemplateId.HasValue || !string.IsNullOrWhiteSpace(templated.TemplateAlias),
                        where + "нужно задать TemplateId или TemplateAlias.");
                    break;
            }

            if (!deep) return;

            // Длина считается в единицах UTF-16 — ровно так, как её считает Haskimail (и как string.Length).
            Require(message.From.Length <= MaxFromLength, where + $"From длиннее {MaxFromLength} символов.");
            Require((message.Subject?.Length ?? 0) <= MaxSubjectLength, where + $"Subject длиннее {MaxSubjectLength} символов.");
            Require((message.Tag?.Length ?? 0) <= MaxTagLength, where + $"Tag длиннее {MaxTagLength} символов.");

            var recipients = CountAddresses(message.To) + CountAddresses(message.Cc) + CountAddresses(message.Bcc);
            Require(recipients <= MaxRecipients, where + $"получателей в To, Cc и Bcc вместе — {recipients}, допускается не больше {MaxRecipients}.");

            if (message.Headers != null)
            {
                foreach (var header in message.Headers)
                    Require(header != null && !string.IsNullOrWhiteSpace(header.Name), where + "у заголовка в Headers не задано имя.");
            }

            long messageBytes = 0;
            switch (message)
            {
                case EmailMessage plain:
                    messageBytes += Encoding.UTF8.GetByteCount(plain.HtmlBody ?? string.Empty);
                    messageBytes += Encoding.UTF8.GetByteCount(plain.TextBody ?? string.Empty);
                    break;
                case TemplatedEmailMessage templated when templated.TemplateModel != null:
                    messageBytes += templated.TemplateModel.Sum(pair => Encoding.UTF8.GetByteCount(pair.Key) + Encoding.UTF8.GetByteCount(pair.Value));
                    break;
            }

            if (message.Attachments != null)
            {
                long base64Bytes = 0;
                foreach (var attachment in message.Attachments)
                {
                    Require(attachment != null, where + "вложение равно null.");
                    Require(!string.IsNullOrWhiteSpace(attachment!.Name), where + "у вложения не задано имя (Name).");
                    Require(!string.IsNullOrWhiteSpace(attachment.ContentType), where + $"у вложения «{attachment.Name}» не задан ContentType.");
                    var extension = Path.GetExtension(attachment.Name).TrimStart('.');
                    Require(extension.Length == 0 || !Attachment.IsForbiddenExtension(extension),
                        where + $"вложение «{attachment.Name}»: тип .{extension} запрещён в Haskimail.");
                    base64Bytes += attachment.Content?.Length ?? 0;
                }
                messageBytes += base64Bytes;
            }

            Require(messageBytes <= MaxMessageBytes, where + $"всё письмо (тело и вложения после Base64) занимает {messageBytes / 1024.0 / 1024.0:0.##} МБ, допускается не больше 10 МБ.");
        }

        public static List<T> ValidateBatch<T>(IEnumerable<T> messages, bool deep) where T : EmailMessageBase
        {
            Guard.NotNull(messages, nameof(messages));
            var list = new List<T>(messages);
            if (list.Count == 0 || list.Count > MaxBatchSize)
                throw new ArgumentException($"В пакете должно быть от 1 до {MaxBatchSize} писем, передано {list.Count}.", nameof(messages));
            for (var i = 0; i < list.Count; i++) ValidateMessage(list[i], deep, i);
            return list;
        }

        /// <summary>
        /// Число адресов в списке «a@b.ru, "Фамилия, Имя" &lt;c@d.ru&gt;»: запятые внутри кавычек и угловых скобок не разделяют адреса.
        /// </summary>
        internal static int CountAddresses(string? list)
        {
            if (string.IsNullOrWhiteSpace(list)) return 0;
            int count = 0, depth = 0;
            bool quoted = false, escaped = false, hasContent = false;
            foreach (var c in list!)
            {
                if (escaped) { escaped = false; hasContent = true; continue; }
                switch (c)
                {
                    case '\\' when quoted: escaped = true; break;
                    case '"': quoted = !quoted; break;
                    case '<' when !quoted: depth++; break;
                    case '>' when !quoted && depth > 0: depth--; break;
                    case ',' when !quoted && depth == 0:
                        if (hasContent) count++;
                        hasContent = false;
                        continue;
                }
                if (!char.IsWhiteSpace(c)) hasContent = true;
            }
            return hasContent ? count + 1 : count;
        }

        private static void RequireBodySize(string? body, string name, string where)
        {
            if (body is null || body.Length * 3 <= MaxBodyBytes) return; // быстрый путь без подсчёта байт
            var bytes = Encoding.UTF8.GetByteCount(body);
            Require(bytes <= MaxBodyBytes, where + $"{name} занимает {bytes / 1024.0 / 1024.0:0.##} МБ, допускается не больше 5 МБ.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new ArgumentException(message);
        }
    }
}
