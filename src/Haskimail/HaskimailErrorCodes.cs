namespace Haskimail
{
    /// <summary>
    /// Коды ошибок API Haskimail (поле <c>ErrorCode</c>). Источник: раздел «Обзор» справочника API.
    /// Некоторые коды встречаются в нескольких разделах с разным смыслом — значение зависит от метода.
    /// </summary>
    public static class HaskimailErrorCodes
    {
        /// <summary>Успех.</summary>
        public const int Success = 0;

        // Общие
        /// <summary>10 — ключ API не передан, неверен или не подходит для метода.</summary>
        public const int InvalidApiKey = 10;

        /// <summary>12 — объект не найден или запрос неполный.</summary>
        public const int NotFoundOrIncompleteRequest = 12;

        /// <summary>100 — внутренняя ошибка; повторите позже.</summary>
        public const int InternalError = 100;

        /// <summary>404 — неизвестный метод API.</summary>
        public const int UnknownApiMethod = 404;

        // Отправка писем
        /// <summary>3 — нет обязательных параметров (например, ни HtmlBody, ни TextBody).</summary>
        public const int MissingRequiredParameters = 3;

        /// <summary>4 — аккаунт не может отправлять: заблокирован, удалён или на модерации.</summary>
        public const int AccountCannotSend = 4;

        /// <summary>6 — неверный адрес в From, To, Cc или Bcc.</summary>
        public const int InvalidEmailAddress = 6;

        /// <summary>15 — действие запрещено.</summary>
        public const int ActionForbidden = 15;

        /// <summary>26 — вложение или все вложения вместе слишком большие.</summary>
        public const int AttachmentTooLarge = 26;

        /// <summary>33 — недопустимый тип вложения.</summary>
        public const int InvalidAttachmentType = 33;

        /// <summary>35 — отправка недоступна: нет средств, блокировка за спам или отправка выключена.</summary>
        public const int SendingUnavailable = 35;

        /// <summary>36 — временная ошибка отправки; повторите позже.</summary>
        public const int TemporarySendingError = 36;

        /// <summary>40 — неверные заголовки в Headers.</summary>
        public const int InvalidHeaders = 40;

        /// <summary>52 — адрес отправителя не подтверждён.</summary>
        public const int SenderAddressNotConfirmed = 52;

        /// <summary>Неверное поле в тестовом режиме или некорректный пакет (формат, пустой, больше 500 писем).</summary>
        public const int InvalidBatchOrTestField = 300;

        /// <summary>Отправитель не подтверждён (аккаунт на модерации).</summary>
        public const int SenderNotConfirmedModeration = 400;

        /// <summary>Неверное поле запроса (только тестовый режим).</summary>
        public const int InvalidRequestFieldTestMode = 403;

        /// <summary>411 — запрещённый тип вложения (тестовый режим).</summary>
        public const int ForbiddenAttachmentTypeTestMode = 411;

        /// <summary>Получатель не подтверждён (аккаунт на модерации).</summary>
        public const int RecipientNotConfirmedModeration = 412;

        /// <summary>1101 — шаблон не найден.</summary>
        public const int TemplateNotFound = 1101;

        /// <summary>Канал не найден, принадлежит другому серверу или архивирован.</summary>
        public const int MessageStreamNotFound = 1226;

        /// <summary>1236 — отправка через входящий канал запрещена.</summary>
        public const int CannotSendFromInboundStream = 1236;

        // Сообщения и правила для входящих
        /// <summary>700 — неверные параметры запроса (count, offset, status или дата).</summary>
        public const int InvalidQueryParameters = 700;

        /// <summary>701 — письмо не найдено.</summary>
        public const int MessageNotFound = 701;

        /// <summary>702 — в обход правил можно отправить только заблокированное входящее письмо.</summary>
        public const int OnlyBlockedInboundCanBeBypassed = 702;

        /// <summary>703 — повторить можно только входящее письмо со статусом Failed.</summary>
        public const int OnlyFailedInboundCanBeRetried = 703;

        /// <summary>800 — неверные параметры правила или правило не сохранилось.</summary>
        public const int InvalidInboundRule = 800;

        /// <summary>810 — такое правило уже есть.</summary>
        public const int InboundRuleAlreadyExists = 810;

        /// <summary>812 — правило не найдено.</summary>
        public const int InboundRuleNotFound = 812;

        /// <summary>813 — правило должно быть email-адресом или доменом.</summary>
        public const int InboundRuleMustBeEmailOrDomain = 813;

        // Домены и шаблоны
        /// <summary>510 — домен или сервер не найден.</summary>
        public const int DomainOrServerNotFound = 510;

        /// <summary>512 — домен уже добавлен в этот или другой аккаунт.</summary>
        public const int DomainAlreadyExists = 512;

        /// <summary>516 — неверный формат домена.</summary>
        public const int InvalidDomainFormat = 516;

        /// <summary>1120 — нечего проверять или неверная модель при проверке шаблона.</summary>
        public const int InvalidTemplateValidationRequest = 1120;

        // Серверы
        /// <summary>600 — неверный домен для входящих.</summary>
        public const int InvalidInboundDomain = 600;

        /// <summary>602 — домен для входящих уже занят или зарезервирован.</summary>
        public const int InboundDomainInUse = 602;

        /// <summary>606 — неверный InboundHookUrl: нужен полный http(s)-адрес.</summary>
        public const int InvalidInboundHookUrl = 606;

        /// <summary>610 — у домена для входящих нет MX-записи на inbound.haskimail.ru.</summary>
        public const int InboundDomainMissingMx = 610;

        /// <summary>611 — неверный InboundSpamThreshold: число от 0 до 30 с шагом 5.</summary>
        public const int InvalidInboundSpamThreshold = 611;

        // Каналы
        /// <summary>1221 — неверный MessageStreamType: создать можно только Transactional или Broadcasts.</summary>
        public const int InvalidMessageStreamType = 1221;

        /// <summary>1228 — входящий канал создать нельзя: он один на сервер и создаётся автоматически.</summary>
        public const int CannotCreateInboundStream = 1228;

        /// <summary>1229 — входящий канал нельзя архивировать.</summary>
        public const int CannotArchiveInboundStream = 1229;
    }
}
