using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Домены отправителей (ключ аккаунта).</summary>
    public interface IDomainsApi
    {
        /// <summary>Домены аккаунта и статус DKIM/SPF (<c>GET /domains</c>). count по умолчанию 500.</summary>
        Task<DomainList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default);

        /// <summary>Все домены постранично.</summary>
        IAsyncEnumerable<DomainSummary> ListAllAsync(int pageSize = 500, CancellationToken cancellationToken = default);

        /// <summary>
        /// Домен и DNS-записи, которые нужно добавить (<c>GET /domains/{id}</c>). Состояние — по последней проверке;
        /// перепроверить — <see cref="VerifyDkimAsync"/> и <see cref="VerifySpfAsync"/>.
        /// </summary>
        Task<DomainDetails> GetAsync(long domainId, CancellationToken cancellationToken = default);

        /// <summary>Добавить домен и сразу проверить DNS (<c>POST /domains</c>). Домен из другого аккаунта добавить нельзя (512).</summary>
        Task<DomainDetails> CreateAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>Удалить домен вместе с подтверждёнными адресами на нём (<c>DELETE /domains/{id}</c>).</summary>
        Task<ApiResult> DeleteAsync(long domainId, CancellationToken cancellationToken = default);

        /// <summary>Заново проверить DKIM (<c>PUT /domains/{id}/verifyDkim</c>).</summary>
        Task<DkimVerificationResult> VerifyDkimAsync(long domainId, CancellationToken cancellationToken = default);

        /// <summary>Заново проверить SPF (<c>PUT /domains/{id}/verifyspf</c>).</summary>
        Task<SpfVerificationResult> VerifySpfAsync(long domainId, CancellationToken cancellationToken = default);
    }

    internal sealed class DomainsApi : IDomainsApi
    {
        private readonly ApiConnection _api;

        internal DomainsApi(ApiConnection api) { _api = api; }

        public Task<DomainList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default)
        {
            Guard.Paging(count, offset, int.MaxValue);
            return _api.SendAsync(ApiCall.Get("/domains", TokenType.Account, new QueryBuilder().Add("count", count).Add("offset", offset)),
                HaskimailJsonContext.Api.DomainList, cancellationToken);
        }

        public IAsyncEnumerable<DomainSummary> ListAllAsync(int pageSize = 500, CancellationToken cancellationToken = default)
        {
            Guard.Paging(pageSize, 0, int.MaxValue);
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await ListAsync(count, offset, ct).ConfigureAwait(false); return (page.Domains, page.TotalCount); },
                0, pageSize, null, cancellationToken);
        }

        public Task<DomainDetails> GetAsync(long domainId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get("/domains/" + Guard.PathSegment(domainId), TokenType.Account), HaskimailJsonContext.Api.DomainDetails, cancellationToken);

        public Task<DomainDetails> CreateAsync(string name, CancellationToken cancellationToken = default)
        {
            var body = new CreateDomainRequest { Name = Guard.NotEmpty(name, nameof(name)).Trim() };
            return _api.SendAsync(ApiCall.Post("/domains", TokenType.Account), body,
                HaskimailJsonContext.Api.CreateDomainRequest, HaskimailJsonContext.Api.DomainDetails, cancellationToken);
        }

        public Task<ApiResult> DeleteAsync(long domainId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Delete("/domains/" + Guard.PathSegment(domainId), TokenType.Account), HaskimailJsonContext.Api.ApiResult, cancellationToken);

        public Task<DkimVerificationResult> VerifyDkimAsync(long domainId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Put($"/domains/{Guard.PathSegment(domainId)}/verifyDkim", TokenType.Account),
                HaskimailJsonContext.Api.DkimVerificationResult, cancellationToken);

        public Task<SpfVerificationResult> VerifySpfAsync(long domainId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Put($"/domains/{Guard.PathSegment(domainId)}/verifyspf", TokenType.Account),
                HaskimailJsonContext.Api.SpfVerificationResult, cancellationToken);
    }
}
