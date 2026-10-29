using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Настройка вебхуков. Разбор приходящих событий — <see cref="Webhooks.HaskimailWebhooks"/>.</summary>
    public interface IWebhooksApi
    {
        /// <summary>Вебхуки каналов сервера; только с ключом аккаунта — всех каналов (<c>GET /webhooks</c>).</summary>
        Task<WebhookList> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>Вебхук (<c>GET /webhooks/{id}</c>). Нет вебхука — <see cref="HaskimailNotFoundException"/> с кодом 12.</summary>
        Task<Webhook> GetAsync(long webhookId, CancellationToken cancellationToken = default);

        /// <summary>Создать вебхук (<c>POST /webhooks</c>).</summary>
        Task<Webhook> CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default);

        /// <summary>Изменить URL и события (<c>PUT /webhooks/{id}</c>); незаданные события остаются как были.</summary>
        Task<Webhook> UpdateAsync(long webhookId, UpdateWebhookRequest request, CancellationToken cancellationToken = default);

        /// <summary>Удалить вебхук (<c>DELETE /webhooks/{id}</c>).</summary>
        Task<ApiResult> DeleteAsync(long webhookId, CancellationToken cancellationToken = default);
    }

    internal sealed class WebhooksApi : IWebhooksApi
    {
        private readonly ApiConnection _api;

        internal WebhooksApi(ApiConnection api) { _api = api; }

        public Task<WebhookList> ListAsync(CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get("/webhooks", TokenType.ServerOrAccount), HaskimailJsonContext.Api.WebhookList, cancellationToken);

        public Task<Webhook> GetAsync(long webhookId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get("/webhooks/" + Guard.PathSegment(webhookId), TokenType.ServerOrAccount), HaskimailJsonContext.Api.Webhook, cancellationToken);

        public Task<Webhook> CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            Guard.NotEmpty(request.Url, "request.Url");
            return _api.SendAsync(ApiCall.Post("/webhooks", TokenType.ServerOrAccount), request,
                HaskimailJsonContext.Api.CreateWebhookRequest, HaskimailJsonContext.Api.Webhook, cancellationToken);
        }

        public Task<Webhook> UpdateAsync(long webhookId, UpdateWebhookRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            return _api.SendAsync(ApiCall.Put("/webhooks/" + Guard.PathSegment(webhookId), TokenType.ServerOrAccount), request,
                HaskimailJsonContext.Api.UpdateWebhookRequest, HaskimailJsonContext.Api.Webhook, cancellationToken);
        }

        public Task<ApiResult> DeleteAsync(long webhookId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Delete("/webhooks/" + Guard.PathSegment(webhookId), TokenType.ServerOrAccount), HaskimailJsonContext.Api.ApiResult, cancellationToken);
    }
}
