using System;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Каналы (message streams).</summary>
    public interface IMessageStreamsApi
    {
        /// <summary>Каналы сервера, включая архивные (<c>GET /message-streams</c>).</summary>
        Task<MessageStreamList> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>Канал (<c>GET /message-streams/{id}</c>). Нет канала — <see cref="HaskimailNotFoundException"/> с кодом 1226.</summary>
        Task<MessageStream> GetAsync(string streamId, CancellationToken cancellationToken = default);

        /// <summary>Переименовать канал (<c>PATCH /message-streams/{id}</c>). Тип и сервер не меняются.</summary>
        Task<MessageStream> UpdateAsync(string streamId, string name, CancellationToken cancellationToken = default);

        /// <summary>Создать канал (<c>POST /message-streams</c>). Входящий канал создать нельзя.</summary>
        Task<MessageStream> CreateAsync(CreateMessageStreamRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Отправить канал в архив (<c>POST /message-streams/{id}/archive</c>). Через архивный канал письма не уходят (1226),
        /// через 45 дней он удаляется. Входящий канал архивировать нельзя (1229).
        /// </summary>
        Task<ArchiveMessageStreamResult> ArchiveAsync(string streamId, CancellationToken cancellationToken = default);

        /// <summary>Вернуть канал из архива (<c>POST /message-streams/{id}/unarchive</c>).</summary>
        Task<MessageStream> UnarchiveAsync(string streamId, CancellationToken cancellationToken = default);
    }

    internal sealed class MessageStreamsApi : IMessageStreamsApi
    {
        private readonly ApiConnection _api;

        internal MessageStreamsApi(ApiConnection api) { _api = api; }

        public Task<MessageStreamList> ListAsync(CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get("/message-streams", TokenType.ServerOrAccount), HaskimailJsonContext.Api.MessageStreamList, cancellationToken);

        public Task<MessageStream> GetAsync(string streamId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get("/message-streams/" + Guard.PathSegment(streamId, nameof(streamId)), TokenType.ServerOrAccount),
                HaskimailJsonContext.Api.MessageStream, cancellationToken);

        public Task<MessageStream> UpdateAsync(string streamId, string name, CancellationToken cancellationToken = default)
        {
            var body = new RenameMessageStreamRequest { Name = Guard.NotEmpty(name, nameof(name)) };
            return _api.SendAsync(ApiCall.Patch("/message-streams/" + Guard.PathSegment(streamId, nameof(streamId)), TokenType.ServerOrAccount), body,
                HaskimailJsonContext.Api.RenameMessageStreamRequest, HaskimailJsonContext.Api.MessageStream, cancellationToken);
        }

        public Task<MessageStream> CreateAsync(CreateMessageStreamRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(request, nameof(request));
            Guard.NotEmpty(request.Name, "request.Name");
            if (request.MessageStreamType == MessageStreamType.Inbound)
                throw new ArgumentException("Входящий канал у сервера один и создаётся автоматически (ошибка API 1228).", nameof(request));
            return _api.SendAsync(ApiCall.Post("/message-streams", TokenType.ServerOrAccount), request,
                HaskimailJsonContext.Api.CreateMessageStreamRequest, HaskimailJsonContext.Api.MessageStream, cancellationToken);
        }

        // Архивация и разархивация приводят канал в одно и то же состояние — повтор безопасен.
        public Task<ArchiveMessageStreamResult> ArchiveAsync(string streamId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Post($"/message-streams/{Guard.PathSegment(streamId, nameof(streamId))}/archive", TokenType.ServerOrAccount, idempotent: true),
                HaskimailJsonContext.Api.ArchiveMessageStreamResult, cancellationToken);

        public Task<MessageStream> UnarchiveAsync(string streamId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Post($"/message-streams/{Guard.PathSegment(streamId, nameof(streamId))}/unarchive", TokenType.ServerOrAccount, idempotent: true),
                HaskimailJsonContext.Api.MessageStream, cancellationToken);
    }
}
