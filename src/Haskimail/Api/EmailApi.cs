using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haskimail.Internal;
using Haskimail.Models;

namespace Haskimail.Api
{
    /// <summary>Отправка писем (ключ сервера).</summary>
    public interface IEmailApi
    {
        /// <summary>
        /// Отправить письмо (<c>POST /email</c>). Если API письмо не принял, выбрасывается
        /// <see cref="HaskimailValidationException"/> с кодом ошибки.
        /// </summary>
        Task<SendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Пакетная отправка, 1–500 писем (<c>POST /email/batch</c>). Можно смешивать
        /// <see cref="EmailMessage"/> и <see cref="TemplatedEmailMessage"/>.
        /// Письма проверяются по отдельности: результат на каждое — в том же порядке; частичный отказ ошибкой
        /// не считается (см. <see cref="SendResultExtensions.EnsureAllSucceeded"/>).
        /// </summary>
        Task<IReadOnlyList<SendResult>> SendBatchAsync(IEnumerable<EmailMessageBase> messages, CancellationToken cancellationToken = default);
    }

    internal sealed class EmailApi : IEmailApi
    {
        private readonly ApiConnection _api;

        internal EmailApi(ApiConnection api) { _api = api; }

        public Task<SendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            RequestValidator.ValidateMessage(message, _api.Options.ValidateRequests);
            return _api.SendAsync(ApiCall.Post("/email", TokenType.Server), message,
                HaskimailJsonContext.Api.EmailMessage, HaskimailJsonContext.Api.SendResult, cancellationToken);
        }

        public Task<IReadOnlyList<SendResult>> SendBatchAsync(IEnumerable<EmailMessageBase> messages, CancellationToken cancellationToken = default)
        {
            var list = RequestValidator.ValidateBatch(messages, _api.Options.ValidateRequests);
            return _api.SendAsync(ApiCall.Post("/email/batch", TokenType.Server), list,
                HaskimailJsonContext.Api.ListEmailMessageBase, HaskimailJsonContext.Api.IReadOnlyListSendResult, cancellationToken);
        }
    }
}
