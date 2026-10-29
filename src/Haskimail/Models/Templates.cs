using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Haskimail.Internal;

namespace Haskimail.Models
{
    /// <summary>Шаблон целиком.</summary>
    public sealed class Template
    {
        /// <summary>ID шаблона.</summary>
        public long TemplateId { get; set; }

        /// <summary>Название. Используется как TemplateAlias.</summary>
        public string? Name { get; set; }

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>HTML-версия.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>Текстовая версия.</summary>
        public string? TextBody { get; set; }

        /// <summary>ID сервера, которому принадлежит шаблон.</summary>
        public long? AssociatedServerId { get; set; }
    }

    /// <summary>Краткие сведения о шаблоне.</summary>
    public sealed class TemplateSummary
    {
        /// <summary>ID шаблона.</summary>
        public long TemplateId { get; set; }

        /// <summary>Название шаблона.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Список шаблонов.</summary>
    public sealed class TemplateList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Шаблоны.</summary>
        public IReadOnlyList<TemplateSummary> Templates { get; set; } = [];
    }

    /// <summary>Создание шаблона.</summary>
    public sealed class CreateTemplateRequest
    {
        /// <summary>Название. По нему шаблон выбирают вместо ID, поэтому лучше делать его уникальным.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>HTML-версия.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>Текстовая версия.</summary>
        public string? TextBody { get; set; }

        /// <summary>С ключом сервера не нужен (шаблон создаётся на сервере ключа); с ключом аккаунта обязателен.</summary>
        public long? AssociatedServerId { get; set; }
    }

    /// <summary>
    /// Изменение шаблона. API перезаписывает все четыре поля целиком: отсутствующее станет пустым.
    /// Чтобы поменять одно поле, используйте <see cref="Api.ITemplatesApi.UpdateAsync(string, Action{UpdateTemplateRequest}, System.Threading.CancellationToken)"/>.
    /// </summary>
    public sealed class UpdateTemplateRequest
    {
        /// <summary>Название.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Тема.</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>HTML-версия.</summary>
        public string HtmlBody { get; set; } = string.Empty;

        /// <summary>Текстовая версия.</summary>
        public string TextBody { get; set; } = string.Empty;

        /// <summary>Запрос со всеми полями текущего шаблона — удобная основа для изменения.</summary>
        public static UpdateTemplateRequest FromTemplate(Template template)
        {
            Guard.NotNull(template, nameof(template));
            return new UpdateTemplateRequest
            {
                Name = template.Name ?? string.Empty,
                Subject = template.Subject ?? string.Empty,
                HtmlBody = template.HtmlBody ?? string.Empty,
                TextBody = template.TextBody ?? string.Empty,
            };
        }
    }

    /// <summary>Перенос шаблонов. Шаблоны перемещаются, а не копируются.</summary>
    public sealed class PushTemplatesRequest
    {
        /// <summary>ID сервера, с которого переносятся шаблоны.</summary>
        [JsonPropertyName("SourceServerID")]
        public long SourceServerId { get; set; }

        /// <summary>ID сервера, на который переносятся шаблоны.</summary>
        [JsonPropertyName("DestinationServerID")]
        public long DestinationServerId { get; set; }

        /// <summary>false — только показать, какие шаблоны будут перенесены.</summary>
        public bool PerformChanges { get; set; }
    }

    /// <summary>Проверка шаблона: ничего не сохраняется и не отправляется. Нужно хотя бы одно из трёх полей.</summary>
    public sealed class ValidateTemplateRequest
    {
        /// <summary>Тема.</summary>
        public string? Subject { get; set; }

        /// <summary>HTML-версия.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>Текстовая версия.</summary>
        public string? TextBody { get; set; }

        /// <summary>Тестовые подстановки, ключи вместе с разделителями: <c>{"{{name}}": "Иван"}</c>.</summary>
        public Dictionary<string, string>? TestRenderModel { get; set; }
    }

    /// <summary>Ошибка в шаблоне.</summary>
    public sealed class TemplateValidationError
    {
        /// <summary>Описание ошибки.</summary>
        public string? Message { get; set; }

        /// <summary>Номер строки.</summary>
        public int? Line { get; set; }

        /// <summary>Позиция в строке.</summary>
        public int? CharacterPosition { get; set; }
    }

    /// <summary>Результат проверки одной части шаблона.</summary>
    public sealed class TemplateContentValidation
    {
        /// <summary>Часть шаблона без ошибок.</summary>
        public bool ContentIsValid { get; set; }

        /// <summary>Что увидит получатель после подстановки.</summary>
        public string? RenderedContent { get; set; }

        /// <summary>Найденные ошибки.</summary>
        public IReadOnlyList<TemplateValidationError> ValidationErrors { get; set; } = [];
    }

    /// <summary>Результат проверки шаблона.</summary>
    public sealed class TemplateValidationResult
    {
        /// <summary>Все переданные части без ошибок.</summary>
        public bool AllContentIsValid { get; set; }

        /// <summary>null, если часть не передавалась на проверку.</summary>
        public TemplateContentValidation? Subject { get; set; }

        /// <summary>Проверка HTML-версии; null, если не передавалась.</summary>
        public TemplateContentValidation? HtmlBody { get; set; }

        /// <summary>Проверка текстовой версии; null, если не передавалась.</summary>
        public TemplateContentValidation? TextBody { get; set; }

        /// <summary>Подстановки, найденные в шаблоне, с примерами значений.</summary>
        [JsonConverter(typeof(StringDictionaryConverter))]
        public IReadOnlyDictionary<string, string>? SuggestedTemplateModel { get; set; }
    }
}
