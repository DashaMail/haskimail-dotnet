namespace Haskimail
{
    /// <summary>
    /// Адреса для тестирования возвратов. Письмо не уходит в интернет, а событие приходит на вебхук сразу,
    /// как при настоящем возврате. Работают на любом сервере (в том числе в песочнице и на модерации)
    /// и не расходуют квоту. Регистр и разделители в имени не важны.
    /// </summary>
    public static class HaskimailTestAddresses
    {
        /// <summary>Домен тестовых адресов.</summary>
        public const string Domain = "bounce-testing.haskimail.ru";

        /// <summary>Жёсткий возврат: событие <c>bounced</c>, категория <c>hrd</c>.</summary>
        public const string HardBounce = "hardbounce@" + Domain;

        /// <summary>Мягкий возврат: событие <c>bounced</c>, категория <c>sft</c>.</summary>
        public const string SoftBounce = "soft@" + Domain;

        /// <summary>Блокировка провайдером: событие <c>bounced</c>, категория <c>blk</c>.</summary>
        public const string Blocked = "block@" + Domain;

        /// <summary>Жалоба на спам (FBL): событие <c>complained</c>, категория <c>spm</c>. Приходит асинхронно.</summary>
        public const string SpamComplaint = "spamnotification@" + Domain;
    }
}
