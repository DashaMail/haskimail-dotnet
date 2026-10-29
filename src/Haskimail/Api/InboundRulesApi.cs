using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Правила блокировки входящих писем (ключ сервера).</summary>
    public interface IInboundRulesApi
    {
        /// <summary>Правила сервера (<c>GET /triggers/inboundrules</c>). count 1–500, по умолчанию 100.</summary>
        Task<InboundRuleList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default);

        /// <summary>Все правила постранично.</summary>
        IAsyncEnumerable<InboundRule> ListAllAsync(int pageSize = 500, CancellationToken cancellationToken = default);

        /// <summary>
        /// Блокировать письма от адреса (<c>someone@example.com</c>) или домена (<c>example.com</c>)
        /// (<c>POST /triggers/inboundrules</c>). Такие письма получают статус Blocked и на вебхук не уходят.
        /// </summary>
        Task<InboundRule> CreateAsync(string rule, CancellationToken cancellationToken = default);

        /// <summary>Удалить правило (<c>DELETE /triggers/inboundrules/{id}</c>).</summary>
        Task<ApiResult> DeleteAsync(long ruleId, CancellationToken cancellationToken = default);
    }

    internal sealed class InboundRulesApi : IInboundRulesApi
    {
        private const int MaxPage = 500;
        private readonly ApiConnection _api;

        internal InboundRulesApi(ApiConnection api) { _api = api; }

        public Task<InboundRuleList> ListAsync(int? count = null, int? offset = null, CancellationToken cancellationToken = default)
        {
            Guard.Paging(count, offset, MaxPage);
            return _api.SendAsync(ApiCall.Get("/triggers/inboundrules", TokenType.Server, new QueryBuilder().Add("count", count).Add("offset", offset)),
                HaskimailJsonContext.Api.InboundRuleList, cancellationToken);
        }

        public IAsyncEnumerable<InboundRule> ListAllAsync(int pageSize = 500, CancellationToken cancellationToken = default)
        {
            Guard.Paging(pageSize, 0, MaxPage);
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await ListAsync(count, offset, ct).ConfigureAwait(false); return (page.InboundRules, page.TotalCount); },
                0, pageSize, null, cancellationToken);
        }

        public Task<InboundRule> CreateAsync(string rule, CancellationToken cancellationToken = default)
        {
            var body = new CreateInboundRuleRequest { Rule = Guard.NotEmpty(rule, nameof(rule)).Trim() };
            return _api.SendAsync(ApiCall.Post("/triggers/inboundrules", TokenType.Server), body,
                HaskimailJsonContext.Api.CreateInboundRuleRequest, HaskimailJsonContext.Api.InboundRule, cancellationToken);
        }

        public Task<ApiResult> DeleteAsync(long ruleId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Delete("/triggers/inboundrules/" + Guard.PathSegment(ruleId), TokenType.Server), HaskimailJsonContext.Api.ApiResult, cancellationToken);
    }
}
