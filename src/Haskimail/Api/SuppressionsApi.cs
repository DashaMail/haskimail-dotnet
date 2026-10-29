using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Стоп-листы каналов (ключ сервера).</summary>
    public interface ISuppressionsApi
    {
        /// <summary>
        /// Адреса, на которые письма через канал не уходят (<c>GET /message-streams/{id}/suppressions/dump</c>):
        /// добавленные вручную, отписавшиеся, с жёстким возвратом или жалобой на спам.
        /// </summary>
        Task<SuppressionList> ListAsync(string streamId, CancellationToken cancellationToken = default);

        /// <summary>Добавить адреса в стоп-лист (<c>POST /message-streams/{id}/suppressions</c>). Результат — по каждому адресу.</summary>
        Task<SuppressionChangeResult> AddAsync(string streamId, IEnumerable<string> emailAddresses, CancellationToken cancellationToken = default);

        /// <summary>
        /// Убрать адреса из стоп-листа (<c>POST /message-streams/{id}/suppressions/delete</c>). Удаляются только добавленные вручную
        /// и с жёстким возвратом; отписавшиеся и пожаловавшиеся остаются.
        /// </summary>
        Task<SuppressionChangeResult> DeleteAsync(string streamId, IEnumerable<string> emailAddresses, CancellationToken cancellationToken = default);
    }

    internal sealed class SuppressionsApi : ISuppressionsApi
    {
        private readonly ApiConnection _api;

        internal SuppressionsApi(ApiConnection api) { _api = api; }

        public Task<SuppressionList> ListAsync(string streamId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get($"/message-streams/{Guard.PathSegment(streamId, nameof(streamId))}/suppressions/dump", TokenType.Server),
                HaskimailJsonContext.Api.SuppressionList, cancellationToken);

        // Добавление и удаление приводят адрес в одно и то же состояние — повтор безопасен.
        public Task<SuppressionChangeResult> AddAsync(string streamId, IEnumerable<string> emailAddresses, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Post($"/message-streams/{Guard.PathSegment(streamId, nameof(streamId))}/suppressions", TokenType.Server, idempotent: true),
                Body(emailAddresses), HaskimailJsonContext.Api.SuppressionRequest, HaskimailJsonContext.Api.SuppressionChangeResult, cancellationToken);

        public Task<SuppressionChangeResult> DeleteAsync(string streamId, IEnumerable<string> emailAddresses, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Post($"/message-streams/{Guard.PathSegment(streamId, nameof(streamId))}/suppressions/delete", TokenType.Server, idempotent: true),
                Body(emailAddresses), HaskimailJsonContext.Api.SuppressionRequest, HaskimailJsonContext.Api.SuppressionChangeResult, cancellationToken);

        private static SuppressionRequest Body(IEnumerable<string> emailAddresses)
        {
            Guard.NotNull(emailAddresses, nameof(emailAddresses));
            var entries = emailAddresses.Select(e => new SuppressionEntry { EmailAddress = Guard.NotEmpty(e, nameof(emailAddresses)).Trim() }).ToList();
            if (entries.Count == 0) throw new ArgumentException("Список адресов пуст.", nameof(emailAddresses));
            return new SuppressionRequest { Suppressions = entries };
        }
    }
}
