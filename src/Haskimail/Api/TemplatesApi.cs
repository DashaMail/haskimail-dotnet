using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Шаблоны писем и отправка по шаблонам.</summary>
    public interface ITemplatesApi
    {
        /// <summary>Отправить письмо по шаблону (<c>POST /email/withTemplate</c>). Шаблон должен принадлежать серверу ключа (иначе 1101).</summary>
        Task<SendResult> SendAsync(TemplatedEmailMessage message, CancellationToken cancellationToken = default);

        /// <summary>Пакетная отправка по шаблонам, 1–500 писем (<c>POST /email/batchWithTemplates</c>). Статус — у каждого письма.</summary>
        Task<IReadOnlyList<SendResult>> SendBatchAsync(IEnumerable<TemplatedEmailMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Перенести ВСЕ шаблоны с сервера на сервер (<c>PUT /templates/push</c>, ключ аккаунта). Шаблоны перемещаются,
        /// а не копируются: сначала вызовите с <see cref="PushTemplatesRequest.PerformChanges"/> = false.
        /// </summary>
        Task<TemplateList> PushAsync(PushTemplatesRequest request, CancellationToken cancellationToken = default);

        /// <summary>Шаблон по ID (<c>GET /templates/{id}</c>). Нет шаблона — <see cref="HaskimailNotFoundException"/> с кодом 1101.</summary>
        Task<Template> GetAsync(long templateId, CancellationToken cancellationToken = default);

        /// <summary>Шаблон по ID или названию (<c>GET /templates/{idOrAlias}</c>).</summary>
        Task<Template> GetAsync(string templateIdOrAlias, CancellationToken cancellationToken = default);

        /// <summary>Создать шаблон (<c>POST /templates</c>). С ключом аккаунта обязателен AssociatedServerId.</summary>
        Task<TemplateSummary> CreateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default);

        /// <summary>Изменить шаблон (<c>PUT /templates/{idOrAlias}</c>). Все четыре поля перезаписываются целиком.</summary>
        Task<TemplateSummary> UpdateAsync(string templateIdOrAlias, UpdateTemplateRequest request, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="UpdateAsync(string, UpdateTemplateRequest, CancellationToken)"/>
        Task<TemplateSummary> UpdateAsync(long templateId, UpdateTemplateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Изменить часть полей: читает шаблон, применяет <paramref name="modify"/> и сохраняет все поля.
        /// </summary>
        Task<TemplateSummary> UpdateAsync(string templateIdOrAlias, Action<UpdateTemplateRequest> modify, CancellationToken cancellationToken = default);

        /// <summary>Шаблоны сервера (<c>GET /templates</c>). count по умолчанию 500.</summary>
        Task<TemplateList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default);

        /// <summary>Все шаблоны сервера постранично.</summary>
        IAsyncEnumerable<TemplateSummary> ListAllAsync(int pageSize = 500, CancellationToken cancellationToken = default);

        /// <summary>Удалить шаблон (<c>DELETE /templates/{idOrAlias}</c>).</summary>
        Task<ApiResult> DeleteAsync(string templateIdOrAlias, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="DeleteAsync(string, CancellationToken)"/>
        Task<ApiResult> DeleteAsync(long templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Показать, что получит получатель (<c>POST /templates/validate</c>): ничего не сохраняется и не отправляется.
        /// Нужно хотя бы одно из Subject, HtmlBody, TextBody.
        /// </summary>
        Task<TemplateValidationResult> ValidateAsync(ValidateTemplateRequest request, CancellationToken cancellationToken = default);
    }

    internal sealed class TemplatesApi : ITemplatesApi
    {
        private readonly ApiConnection _api;

        internal TemplatesApi(ApiConnection api) { _api = api; }

        public Task<SendResult> SendAsync(TemplatedEmailMessage message, CancellationToken cancellationToken = default)
        {
            RequestValidator.ValidateMessage(message, _api.Options.ValidateRequests);
            return _api.SendAsync(ApiCall.Post("/email/withTemplate", TokenType.Server), message,
                HaskimailJsonContext.Api.TemplatedEmailMessage, HaskimailJsonContext.Api.SendResult, cancellationToken);
        }

        public Task<IReadOnlyList<SendResult>> SendBatchAsync(IEnumerable<TemplatedEmailMessage> messages, CancellationToken cancellationToken = default)
        {
            var body = new BatchWithTemplatesRequest { Messages = RequestValidator.ValidateBatch(messages, _api.Options.ValidateRequests) };
            return _api.SendAsync(ApiCall.Post("/email/batchWithTemplates", TokenType.Server), body,
                HaskimailJsonContext.Api.BatchWithTemplatesRequest, HaskimailJsonContext.Api.IReadOnlyListSendResult, cancellationToken);
        }

        public Task<TemplateList> PushAsync(PushTemplatesRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            return _api.SendAsync(ApiCall.Put("/templates/push", TokenType.Account, idempotent: false), request,
                HaskimailJsonContext.Api.PushTemplatesRequest, HaskimailJsonContext.Api.TemplateList, cancellationToken);
        }

        public Task<Template> GetAsync(long templateId, CancellationToken cancellationToken = default) =>
            GetCoreAsync(Guard.PathSegment(templateId), cancellationToken);

        public Task<Template> GetAsync(string templateIdOrAlias, CancellationToken cancellationToken = default) =>
            GetCoreAsync(Guard.PathSegment(templateIdOrAlias, nameof(templateIdOrAlias)), cancellationToken);

        private Task<Template> GetCoreAsync(string segment, CancellationToken cancellationToken) =>
            _api.SendAsync(ApiCall.Get("/templates/" + segment, TokenType.Server), HaskimailJsonContext.Api.Template, cancellationToken);

        public Task<TemplateSummary> CreateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            Guard.NotEmpty(request.Name, "request.Name");
            return _api.SendAsync(ApiCall.Post("/templates", TokenType.ServerOrAccount), request,
                HaskimailJsonContext.Api.CreateTemplateRequest, HaskimailJsonContext.Api.TemplateSummary, cancellationToken);
        }

        public Task<TemplateSummary> UpdateAsync(string templateIdOrAlias, UpdateTemplateRequest request, CancellationToken cancellationToken = default) =>
            UpdateCoreAsync(Guard.PathSegment(templateIdOrAlias, nameof(templateIdOrAlias)), request, cancellationToken);

        public Task<TemplateSummary> UpdateAsync(long templateId, UpdateTemplateRequest request, CancellationToken cancellationToken = default) =>
            UpdateCoreAsync(Guard.PathSegment(templateId), request, cancellationToken);

        public async Task<TemplateSummary> UpdateAsync(string templateIdOrAlias, Action<UpdateTemplateRequest> modify, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(modify, nameof(modify));
            var current = await GetAsync(templateIdOrAlias, cancellationToken).ConfigureAwait(false);
            var request = UpdateTemplateRequest.FromTemplate(current);
            modify(request);
            // После переименования шаблона его старое название уже не адрес — обновляем по ID.
            return await UpdateCoreAsync(Guard.PathSegment(current.TemplateId), request, cancellationToken).ConfigureAwait(false);
        }

        private Task<TemplateSummary> UpdateCoreAsync(string segment, UpdateTemplateRequest request, CancellationToken cancellationToken)
        {
            Guard.NotNull(request, nameof(request));
            return _api.SendAsync(ApiCall.Put("/templates/" + segment, TokenType.Server), request,
                HaskimailJsonContext.Api.UpdateTemplateRequest, HaskimailJsonContext.Api.TemplateSummary, cancellationToken);
        }

        public Task<TemplateList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default)
        {
            Guard.Paging(count, offset, int.MaxValue);
            return _api.SendAsync(ApiCall.Get("/templates", TokenType.Server, new QueryBuilder().Add("count", count).Add("offset", offset)),
                HaskimailJsonContext.Api.TemplateList, cancellationToken);
        }

        public IAsyncEnumerable<TemplateSummary> ListAllAsync(int pageSize = 500, CancellationToken cancellationToken = default)
        {
            Guard.Paging(pageSize, 0, int.MaxValue);
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await ListAsync(count, offset, ct).ConfigureAwait(false); return (page.Templates, page.TotalCount); },
                0, pageSize, null, cancellationToken);
        }

        public Task<ApiResult> DeleteAsync(string templateIdOrAlias, CancellationToken cancellationToken = default) =>
            DeleteCoreAsync(Guard.PathSegment(templateIdOrAlias, nameof(templateIdOrAlias)), cancellationToken);

        public Task<ApiResult> DeleteAsync(long templateId, CancellationToken cancellationToken = default) =>
            DeleteCoreAsync(Guard.PathSegment(templateId), cancellationToken);

        private Task<ApiResult> DeleteCoreAsync(string segment, CancellationToken cancellationToken) =>
            _api.SendAsync(ApiCall.Delete("/templates/" + segment, TokenType.Server), HaskimailJsonContext.Api.ApiResult, cancellationToken);

        public Task<TemplateValidationResult> ValidateAsync(ValidateTemplateRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            if (string.IsNullOrEmpty(request.Subject) && string.IsNullOrEmpty(request.HtmlBody) && string.IsNullOrEmpty(request.TextBody))
                throw new ArgumentException("Нужно передать хотя бы одно из Subject, HtmlBody, TextBody.", nameof(request));
            return _api.SendAsync(ApiCall.Post("/templates/validate", TokenType.ServerOrAccount, idempotent: true), request,
                HaskimailJsonContext.Api.ValidateTemplateRequest, HaskimailJsonContext.Api.TemplateValidationResult, cancellationToken);
        }
    }
}
