using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Статистика отправленных писем.</summary>
    public interface IStatsApi
    {
        /// <summary>Итоги за период (<c>GET /stats</c>).</summary>
        Task<OutboundOverview> GetOverviewAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Отправленные по дням (<c>GET /stats/sends</c>).</summary>
        Task<DailyStats<SentDay>> GetSentCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Жёсткие и мягкие возвраты по дням (<c>GET /stats/bounces</c>).</summary>
        Task<DailyStats<BounceDay>> GetBounceCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Жалобы на спам по дням (<c>GET /stats/spam</c>).</summary>
        Task<DailyStats<SpamComplaintDay>> GetSpamComplaintsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Открытия по дням (<c>GET /stats/opens</c>).</summary>
        Task<DailyStats<OpenDay>> GetOpenCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Клики по дням (<c>GET /stats/clicks</c>).</summary>
        Task<DailyStats<ClickDay>> GetClickCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Открытия по платформам (<c>GET /stats/opens/platforms</c>).</summary>
        Task<PlatformStats> GetOpenPlatformsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Открытия по почтовым клиентам (<c>GET /stats/opens/emailclients</c>).</summary>
        Task<BreakdownStats> GetOpenEmailClientsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Клики по браузерам (<c>GET /stats/clicks/browserfamilies</c>).</summary>
        Task<BreakdownStats> GetClickBrowsersAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Клики по платформам (<c>GET /stats/clicks/platforms</c>).</summary>
        Task<PlatformStats> GetClickPlatformsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default);
    }

    internal sealed class StatsApi : IStatsApi
    {
        private readonly ApiConnection _api;

        internal StatsApi(ApiConnection api) { _api = api; }

        public Task<OutboundOverview> GetOverviewAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats", filter, HaskimailJsonContext.Api.OutboundOverview, cancellationToken);

        public Task<DailyStats<SentDay>> GetSentCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats/sends", filter, HaskimailJsonContext.Api.DailyStatsSentDay, cancellationToken);

        public Task<DailyStats<BounceDay>> GetBounceCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats/bounces", filter, HaskimailJsonContext.Api.DailyStatsBounceDay, cancellationToken);

        public Task<DailyStats<SpamComplaintDay>> GetSpamComplaintsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats/spam", filter, HaskimailJsonContext.Api.DailyStatsSpamComplaintDay, cancellationToken);

        public Task<DailyStats<OpenDay>> GetOpenCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats/opens", filter, HaskimailJsonContext.Api.DailyStatsOpenDay, cancellationToken);

        public Task<DailyStats<ClickDay>> GetClickCountsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats/clicks", filter, HaskimailJsonContext.Api.DailyStatsClickDay, cancellationToken);

        public Task<PlatformStats> GetOpenPlatformsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats/opens/platforms", filter, HaskimailJsonContext.Api.PlatformStats, cancellationToken);

        public async Task<BreakdownStats> GetOpenEmailClientsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            ParseBreakdown(await GetAsync("/stats/opens/emailclients", filter, HaskimailJsonContext.Api.JsonElement, cancellationToken).ConfigureAwait(false));

        public async Task<BreakdownStats> GetClickBrowsersAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            ParseBreakdown(await GetAsync("/stats/clicks/browserfamilies", filter, HaskimailJsonContext.Api.JsonElement, cancellationToken).ConfigureAwait(false));

        public Task<PlatformStats> GetClickPlatformsAsync(StatsFilter? filter = null, CancellationToken cancellationToken = default) =>
            GetAsync("/stats/clicks/platforms", filter, HaskimailJsonContext.Api.PlatformStats, cancellationToken);

        private Task<T> GetAsync<T>(string path, StatsFilter? filter, JsonTypeInfo<T> type, CancellationToken cancellationToken)
        {
            filter ??= new StatsFilter();
            var query = new QueryBuilder()
                .AddDate("fromdate", filter.FromDate)
                .AddDate("todate", filter.ToDate)
                .Add("messagestream", filter.MessageStream);
            return _api.SendAsync(ApiCall.Get(path, TokenType.ServerOrAccount, query), type, cancellationToken);
        }

        /// <summary>Ответ с динамическими ключами: «Days» — по дням, остальные числовые поля — итоги за период.</summary>
        internal static BreakdownStats ParseBreakdown(JsonElement root)
        {
            var days = new List<BreakdownDay>();
            var totals = new List<KeyValuePair<string, int>>();
            if (root.ValueKind != JsonValueKind.Object) return new BreakdownStats();

            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("Days") && property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var dayElement in property.Value.EnumerateArray())
                    {
                        if (dayElement.ValueKind != JsonValueKind.Object) continue;
                        var date = default(DateTime);
                        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
                        foreach (var field in dayElement.EnumerateObject())
                        {
                            if (field.NameEquals("Date"))
                                DateTime.TryParseExact(field.Value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                            else if (TryGetInt(field.Value, out var n))
                                counts[field.Name] = n;
                        }
                        days.Add(new BreakdownDay { Date = date, Counts = counts });
                    }
                }
                else if (TryGetInt(property.Value, out var total))
                {
                    totals.Add(new KeyValuePair<string, int>(property.Name, total));
                }
            }
            return new BreakdownStats { Days = days, Totals = totals };
        }

        private static bool TryGetInt(JsonElement element, out int value)
        {
            value = 0;
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.TryGetInt32(out value),
                JsonValueKind.String => int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
                _ => false,
            };
        }
    }
}
