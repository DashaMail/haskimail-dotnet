using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>SMTP-серверы (ключ аккаунта).</summary>
    public interface IServersApi
    {
        /// <summary>Настройки сервера и его входящий адрес (<c>GET /servers/{id}</c>).</summary>
        Task<Server> GetAsync(long serverId, CancellationToken cancellationToken = default);

        /// <summary>Создать сервер и ключ к нему (<c>POST /servers</c>); ключ — в <see cref="Server.ApiTokens"/>.</summary>
        Task<Server> CreateAsync(CreateServerRequest request, CancellationToken cancellationToken = default);

        /// <summary>Изменить сервер (<c>PUT /servers/{id}</c>): только заданные поля. DeliveryType не меняется.</summary>
        Task<Server> UpdateAsync(long serverId, UpdateServerRequest request, CancellationToken cancellationToken = default);

        /// <summary>Серверы аккаунта (<c>GET /servers</c>). count по умолчанию 50.</summary>
        Task<ServerList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default);

        /// <summary>Все серверы аккаунта постранично.</summary>
        IAsyncEnumerable<Server> ListAllAsync(int pageSize = 50, CancellationToken cancellationToken = default);

        /// <summary>Удалить сервер вместе с правилами для входящих (<c>DELETE /servers/{id}</c>).</summary>
        Task<ApiResult> DeleteAsync(long serverId, CancellationToken cancellationToken = default);

        /// <summary>Выпустить дополнительный серверный ключ (<c>POST /servers/{id}/tokens/new</c>). Значение ключа показывается один раз.</summary>
        Task<ServerToken> IssueTokenAsync(long serverId, IssueServerTokenRequest? request = null, CancellationToken cancellationToken = default);
    }

    internal sealed class ServersApi : IServersApi
    {
        private readonly ApiConnection _api;

        internal ServersApi(ApiConnection api) { _api = api; }

        public Task<Server> GetAsync(long serverId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get("/servers/" + Guard.PathSegment(serverId), TokenType.Account), HaskimailJsonContext.Api.Server, cancellationToken);

        public Task<Server> CreateAsync(CreateServerRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            Guard.NotEmpty(request.Name, "request.Name");
            if (string.IsNullOrEmpty(request.Color.ToString())) throw new ArgumentException("Не задан цвет сервера (Color).", nameof(request));
            ValidateThreshold(request.InboundSpamThreshold, nameof(request));
            return _api.SendAsync(ApiCall.Post("/servers", TokenType.Account), request,
                HaskimailJsonContext.Api.CreateServerRequest, HaskimailJsonContext.Api.Server, cancellationToken);
        }

        public Task<Server> UpdateAsync(long serverId, UpdateServerRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            ValidateThreshold(request.InboundSpamThreshold, nameof(request));
            return _api.SendAsync(ApiCall.Put("/servers/" + Guard.PathSegment(serverId), TokenType.Account), request,
                HaskimailJsonContext.Api.UpdateServerRequest, HaskimailJsonContext.Api.Server, cancellationToken);
        }

        public Task<ServerList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default)
        {
            Guard.Paging(count, offset, int.MaxValue);
            return _api.SendAsync(ApiCall.Get("/servers", TokenType.Account, new QueryBuilder().Add("count", count).Add("offset", offset)),
                HaskimailJsonContext.Api.ServerList, cancellationToken);
        }

        public IAsyncEnumerable<Server> ListAllAsync(int pageSize = 50, CancellationToken cancellationToken = default)
        {
            Guard.Paging(pageSize, 0, int.MaxValue);
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await ListAsync(count, offset, ct).ConfigureAwait(false); return (page.Servers, page.TotalCount); },
                0, pageSize, null, cancellationToken);
        }

        public Task<ApiResult> DeleteAsync(long serverId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Delete("/servers/" + Guard.PathSegment(serverId), TokenType.Account), HaskimailJsonContext.Api.ApiResult, cancellationToken);

        public Task<ServerToken> IssueTokenAsync(long serverId, IssueServerTokenRequest? request = null, CancellationToken cancellationToken = default)
        {
            // Выпуск ключа не идемпотентен: повтор после сетевого сбоя дал бы ещё один ключ.
            var call = ApiCall.Post("/servers/" + Guard.PathSegment(serverId) + "/tokens/new", TokenType.Account);
            return request is null
                ? _api.SendAsync(call, HaskimailJsonContext.Api.ServerToken, cancellationToken)
                : _api.SendAsync(call, request, HaskimailJsonContext.Api.IssueServerTokenRequest, HaskimailJsonContext.Api.ServerToken, cancellationToken);
        }

        private static void ValidateThreshold(int? threshold, string paramName)
        {
            if (threshold is int t && (t < 0 || t > 30 || t % 5 != 0))
                throw new ArgumentOutOfRangeException(paramName, t, "Порог спама должен быть от 0 до 30 с шагом 5 (ошибка API 611).");
        }
    }
}
