using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Возвраты (bounces).</summary>
    public interface IBouncesApi
    {
        /// <summary>Возвраты сервера по типам за последние 45 дней (<c>GET /deliverystats</c>).</summary>
        Task<DeliveryStats> GetDeliveryStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>Возвраты, новые первыми (<c>GET /bounces</c>).</summary>
        Task<BounceList> ListAsync(BounceSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Все найденные возвраты постранично.</summary>
        IAsyncEnumerable<Bounce> ListAllAsync(BounceSearch? search = null, CancellationToken cancellationToken = default);
    }

    internal sealed class BouncesApi : IBouncesApi
    {
        private const int DefaultPage = 500;
        private readonly ApiConnection _api;

        internal BouncesApi(ApiConnection api) { _api = api; }

        public Task<DeliveryStats> GetDeliveryStatsAsync(CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get("/deliverystats", TokenType.ServerOrAccount), HaskimailJsonContext.Api.DeliveryStats, cancellationToken);

        public Task<BounceList> ListAsync(BounceSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new BounceSearch();
            return ListCoreAsync(search, search.Count, search.Offset, cancellationToken);
        }

        public IAsyncEnumerable<Bounce> ListAllAsync(BounceSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new BounceSearch();
            Guard.Paging(search.Count, search.Offset, int.MaxValue);
            var filter = search;
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await ListCoreAsync(filter, count, offset, ct).ConfigureAwait(false); return (page.Bounces, page.TotalCount); },
                filter.Offset ?? 0, filter.Count ?? DefaultPage, null, cancellationToken);
        }

        private Task<BounceList> ListCoreAsync(BounceSearch search, int? count, int? offset, CancellationToken cancellationToken)
        {
            Guard.Paging(count, offset, int.MaxValue);
            var query = new QueryBuilder()
                .Add("count", count)
                .Add("offset", offset)
                .Add("type", search.Type?.ToString())
                .Add("emailFilter", search.EmailFilter)
                .Add("tag", search.Tag)
                .Add("messageID", search.MessageId)
                .Add("messagestream", search.MessageStream)
                .AddDate("fromdate", search.FromDate)
                .AddDate("todate", search.ToDate);
            return _api.SendAsync(ApiCall.Get("/bounces", TokenType.ServerOrAccount, query), HaskimailJsonContext.Api.BounceList, cancellationToken);
        }
    }
}
