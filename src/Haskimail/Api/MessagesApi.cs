using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>
    /// Отправленные и входящие письма, открытия и клики. Отправленные, открытия и клики доступны с ключом сервера
    /// (данные своего сервера) или только с ключом аккаунта (данные всех серверов). Входящие письма — только с ключом сервера.
    /// </summary>
    public interface IMessagesApi
    {
        /// <summary>Поиск отправленных писем, новые первыми (<c>GET /messages</c>). Письмо нескольким получателям — запись на каждого.</summary>
        Task<OutboundMessageList> SearchOutboundAsync(OutboundMessageSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Все найденные отправленные письма постранично. API отдаёт не дальше 10 000-й записи — для больших выборок сужайте период.
        /// </summary>
        IAsyncEnumerable<OutboundMessage> SearchOutboundAllAsync(OutboundMessageSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Одно отправленное письмо (<c>GET /messages/{id}/details</c>). Нет письма — <see cref="HaskimailValidationException"/> с кодом 701.</summary>
        Task<OutboundMessage> GetOutboundDetailsAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>Открытия писем, новые первыми (<c>GET /messages/opens</c>).</summary>
        Task<MessageOpenList> ListOpensAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Все открытия постранично (не дальше 10 000-й записи).</summary>
        IAsyncEnumerable<MessageOpen> ListAllOpensAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Открытия одного письма за всё время (<c>GET /messages/opens/{id}</c>).</summary>
        Task<MessageOpenList> GetOpensForMessageAsync(string messageId, MessageEventFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Клики по ссылкам, новые первыми (<c>GET /messages/clicks</c>).</summary>
        Task<MessageClickList> ListClicksAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Все клики постранично (не дальше 10 000-й записи).</summary>
        IAsyncEnumerable<MessageClick> ListAllClicksAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Клики одного письма за всё время (<c>GET /messages/clicks/{id}</c>).</summary>
        Task<MessageClickList> GetClicksForMessageAsync(string messageId, MessageEventFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>Входящие письма за последние 45 дней, новые первыми (<c>GET /messages/inbound</c>, ключ сервера).</summary>
        Task<InboundMessageList> SearchInboundAsync(InboundMessageSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Все найденные входящие письма постранично.</summary>
        IAsyncEnumerable<InboundMessageSummary> SearchInboundAllAsync(InboundMessageSearch? search = null, CancellationToken cancellationToken = default);

        /// <summary>Входящее письмо целиком, без содержимого вложений (<c>GET /messages/inbound/{id}/details</c>, ключ сервера).</summary>
        Task<InboundMessageDetails> GetInboundDetailsAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Отправить заблокированное входящее письмо на вебхук в обход правил и спам-фильтра
        /// (<c>PUT /messages/inbound/{id}/bypass</c>). Только для статуса Blocked (иначе 702). Уходит в течение минуты.
        /// </summary>
        Task<ApiResult> BypassInboundAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Заново отправить на вебхук входящее письмо в статусе Failed (<c>PUT /messages/inbound/{id}/retry</c>, иначе 703).
        /// Счётчик попыток сбрасывается.
        /// </summary>
        Task<ApiResult> RetryInboundAsync(string messageId, CancellationToken cancellationToken = default);
    }

    internal sealed class MessagesApi : IMessagesApi
    {
        private const int MaxPage = 500;
        private const int DefaultPage = 100;

        /// <summary>Для отправленных, открытий и кликов count + offset не больше 10 000.</summary>
        private const int MaxWindow = 10000;

        private readonly ApiConnection _api;

        internal MessagesApi(ApiConnection api) { _api = api; }

        public Task<OutboundMessageList> SearchOutboundAsync(OutboundMessageSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new OutboundMessageSearch();
            return SearchOutboundCoreAsync(search, search.Count, search.Offset, cancellationToken);
        }

        public IAsyncEnumerable<OutboundMessage> SearchOutboundAllAsync(OutboundMessageSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new OutboundMessageSearch();
            Guard.Paging(search.Count, search.Offset, MaxPage);
            var filter = search;
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await SearchOutboundCoreAsync(filter, count, offset, ct).ConfigureAwait(false); return (page.Messages, page.TotalCount); },
                filter.Offset ?? 0, filter.Count ?? MaxPage, MaxWindow, cancellationToken);
        }

        private Task<OutboundMessageList> SearchOutboundCoreAsync(OutboundMessageSearch search, int? count, int? offset, CancellationToken cancellationToken)
        {
            GuardWindow(count, offset);
            var query = new QueryBuilder()
                .Add("count", count)
                .Add("offset", offset)
                .Add("recipient", search.Recipient)
                .Add("fromemail", search.FromEmail)
                .Add("tag", search.Tag)
                .Add("subject", search.Subject)
                .Add("messagestream", search.MessageStream)
                .AddDateTime("fromdate", search.FromDate)
                .AddDateTime("todate", search.ToDate);
            return _api.SendAsync(ApiCall.Get("/messages", TokenType.ServerOrAccount, query), HaskimailJsonContext.Api.OutboundMessageList, cancellationToken);
        }

        public Task<OutboundMessage> GetOutboundDetailsAsync(string messageId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get($"/messages/{Guard.PathSegment(messageId, nameof(messageId))}/details", TokenType.ServerOrAccount),
                HaskimailJsonContext.Api.OutboundMessage, cancellationToken);

        public Task<MessageOpenList> ListOpensAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new MessageEventSearch();
            return ListOpensCoreAsync(search, search.Count, search.Offset, cancellationToken);
        }

        public IAsyncEnumerable<MessageOpen> ListAllOpensAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new MessageEventSearch();
            Guard.Paging(search.Count, search.Offset, MaxPage);
            var filter = search;
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await ListOpensCoreAsync(filter, count, offset, ct).ConfigureAwait(false); return (page.Opens, page.TotalCount); },
                filter.Offset ?? 0, filter.Count ?? MaxPage, MaxWindow, cancellationToken);
        }

        private Task<MessageOpenList> ListOpensCoreAsync(MessageEventSearch search, int? count, int? offset, CancellationToken cancellationToken) =>
            _api.SendAsync(ApiCall.Get("/messages/opens", TokenType.ServerOrAccount, EventQuery(search, count, offset, withDates: true)),
                HaskimailJsonContext.Api.MessageOpenList, cancellationToken);

        public Task<MessageOpenList> GetOpensForMessageAsync(string messageId, MessageEventFilter? filter = null, CancellationToken cancellationToken = default)
        {
            filter ??= new MessageEventFilter();
            return _api.SendAsync(
                ApiCall.Get("/messages/opens/" + Guard.PathSegment(messageId, nameof(messageId)), TokenType.ServerOrAccount, EventQuery(filter, filter.Count, filter.Offset, withDates: false)),
                HaskimailJsonContext.Api.MessageOpenList, cancellationToken);
        }

        public Task<MessageClickList> ListClicksAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new MessageEventSearch();
            return ListClicksCoreAsync(search, search.Count, search.Offset, cancellationToken);
        }

        public IAsyncEnumerable<MessageClick> ListAllClicksAsync(MessageEventSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new MessageEventSearch();
            Guard.Paging(search.Count, search.Offset, MaxPage);
            var filter = search;
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await ListClicksCoreAsync(filter, count, offset, ct).ConfigureAwait(false); return (page.Clicks, page.TotalCount); },
                filter.Offset ?? 0, filter.Count ?? MaxPage, MaxWindow, cancellationToken);
        }

        private Task<MessageClickList> ListClicksCoreAsync(MessageEventSearch search, int? count, int? offset, CancellationToken cancellationToken) =>
            _api.SendAsync(ApiCall.Get("/messages/clicks", TokenType.ServerOrAccount, EventQuery(search, count, offset, withDates: true)),
                HaskimailJsonContext.Api.MessageClickList, cancellationToken);

        public Task<MessageClickList> GetClicksForMessageAsync(string messageId, MessageEventFilter? filter = null, CancellationToken cancellationToken = default)
        {
            filter ??= new MessageEventFilter();
            return _api.SendAsync(
                ApiCall.Get("/messages/clicks/" + Guard.PathSegment(messageId, nameof(messageId)), TokenType.ServerOrAccount, EventQuery(filter, filter.Count, filter.Offset, withDates: false)),
                HaskimailJsonContext.Api.MessageClickList, cancellationToken);
        }

        public Task<InboundMessageList> SearchInboundAsync(InboundMessageSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new InboundMessageSearch();
            return SearchInboundCoreAsync(search, search.Count, search.Offset, cancellationToken);
        }

        public IAsyncEnumerable<InboundMessageSummary> SearchInboundAllAsync(InboundMessageSearch? search = null, CancellationToken cancellationToken = default)
        {
            search ??= new InboundMessageSearch();
            Guard.Paging(search.Count, search.Offset, MaxPage);
            var filter = search;
            return Paging.EnumerateAsync(
                async (offset, count, ct) => { var page = await SearchInboundCoreAsync(filter, count, offset, ct).ConfigureAwait(false); return (page.InboundMessages, page.TotalCount); },
                filter.Offset ?? 0, filter.Count ?? MaxPage, null, cancellationToken);
        }

        private Task<InboundMessageList> SearchInboundCoreAsync(InboundMessageSearch search, int? count, int? offset, CancellationToken cancellationToken)
        {
            Guard.Paging(count, offset, MaxPage);
            var query = new QueryBuilder()
                .Add("count", count)
                .Add("offset", offset)
                .Add("recipient", search.Recipient)
                .Add("fromemail", search.FromEmail)
                .Add("subject", search.Subject)
                .Add("mailboxhash", search.MailboxHash)
                .Add("status", search.Status?.ToString().ToLowerInvariant())
                .AddDateTime("fromdate", search.FromDate, 'T')
                .AddDateTime("todate", search.ToDate, 'T');
            return _api.SendAsync(ApiCall.Get("/messages/inbound", TokenType.Server, query), HaskimailJsonContext.Api.InboundMessageList, cancellationToken);
        }

        public Task<InboundMessageDetails> GetInboundDetailsAsync(string messageId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Get($"/messages/inbound/{Guard.PathSegment(messageId, nameof(messageId))}/details", TokenType.Server),
                HaskimailJsonContext.Api.InboundMessageDetails, cancellationToken);

        // Повтор обхода или повторной доставки может отправить письмо на вебхук дважды — запросы неидемпотентны.
        public Task<ApiResult> BypassInboundAsync(string messageId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Put($"/messages/inbound/{Guard.PathSegment(messageId, nameof(messageId))}/bypass", TokenType.Server, idempotent: false),
                HaskimailJsonContext.Api.ApiResult, cancellationToken);

        public Task<ApiResult> RetryInboundAsync(string messageId, CancellationToken cancellationToken = default) =>
            _api.SendAsync(ApiCall.Put($"/messages/inbound/{Guard.PathSegment(messageId, nameof(messageId))}/retry", TokenType.Server, idempotent: false),
                HaskimailJsonContext.Api.ApiResult, cancellationToken);

        /// <summary>count + offset ≤ 10 000 (по умолчанию count = 100) — иначе API вернёт ошибку 700.</summary>
        private static void GuardWindow(int? count, int? offset)
        {
            Guard.Paging(count, offset, MaxPage);
            if ((count ?? DefaultPage) + (offset ?? 0) > MaxWindow)
                throw new System.ArgumentOutOfRangeException(nameof(offset), offset, $"count + offset не может быть больше {MaxWindow}. Сузьте период поиска.");
        }

        private static QueryBuilder EventQuery(MessageEventFilter filter, int? count, int? offset, bool withDates)
        {
            GuardWindow(count, offset);
            var query = new QueryBuilder()
                .Add("count", count)
                .Add("offset", offset)
                .Add("recipient", filter.Recipient)
                .Add("tag", filter.Tag)
                .Add("messagestream", filter.MessageStream);
            if (withDates && filter is MessageEventSearch search)
                query.AddDateTime("fromdate", search.FromDate).AddDateTime("todate", search.ToDate);
            return query;
        }
    }
}
