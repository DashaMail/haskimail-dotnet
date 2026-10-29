using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Haskimail.Models
{
    /// <summary>SMTP-сервер Haskimail.</summary>
    public sealed class Server
    {
        /// <summary>ID сервера.</summary>
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        /// <summary>Название.</summary>
        public string? Name { get; set; }

        /// <summary>Цвет в списке серверов.</summary>
        public ServerColor Color { get; set; }

        /// <summary>Разрешена ли отправка через SMTP.</summary>
        public bool SmtpApiActivated { get; set; }

        /// <summary>Отслеживать открытия.</summary>
        public bool TrackOpens { get; set; }

        /// <summary>Отслеживание ссылок.</summary>
        public LinkTrackingMode TrackLinks { get; set; }

        /// <summary>Live или Sandbox.</summary>
        public DeliveryType DeliveryType { get; set; }

        /// <summary>Уникальная часть входящего адреса.</summary>
        public string? InboundHash { get; set; }

        /// <summary>URL вебхука входящих.</summary>
        public string? InboundHookUrl { get; set; }

        /// <summary>Домен для входящих.</summary>
        public string? InboundDomain { get; set; }

        /// <summary>Порог спам-оценки входящих (0 — фильтр выключен).</summary>
        public int InboundSpamThreshold { get; set; }

        /// <summary>Добавлять исходник письма в вебхук входящих.</summary>
        public bool RawEmailEnabled { get; set; }

        /// <summary>API-ключи сервера.</summary>
        public IReadOnlyList<string> ApiTokens { get; set; } = [];

        /// <summary>Адрес для входящих писем.</summary>
        public string? InboundAddress { get; set; }
    }

    /// <summary>Страница серверов.</summary>
    public sealed class ServerList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Серверы.</summary>
        public IReadOnlyList<Server> Servers { get; set; } = [];
    }

    /// <summary>Настройки, общие для создания и изменения сервера.</summary>
    public abstract class ServerSettingsBase
    {
        /// <summary>Разрешить отправку через SMTP.</summary>
        public bool? SmtpApiActivated { get; set; }

        /// <summary>Отслеживать открытия писем сервера.</summary>
        public bool? TrackOpens { get; set; }

        /// <summary>Отслеживание ссылок по умолчанию для писем сервера.</summary>
        public LinkTrackingMode? TrackLinks { get; set; }

        /// <summary>URL, на который придёт POST с JSON каждого входящего письма. Пустая строка отключает.</summary>
        public string? InboundHookUrl { get; set; }

        /// <summary>
        /// Домен для входящих на любые адреса. Сначала добавьте MX-запись на inbound.haskimail.ru (иначе ошибка 610).
        /// Пустая строка отключает.
        /// </summary>
        public string? InboundDomain { get; set; }

        /// <summary>Порог спам-оценки входящих: 0–30 с шагом 5; 0 — фильтр выключен.</summary>
        public int? InboundSpamThreshold { get; set; }

        /// <summary>Добавлять исходник письма в поле RawEmail вебхука входящих.</summary>
        public bool? RawEmailEnabled { get; set; }
    }

    /// <summary>Создание сервера.</summary>
    public sealed class CreateServerRequest : ServerSettingsBase
    {
        /// <summary>Название сервера.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Обязателен. По умолчанию purple.</summary>
        public ServerColor Color { get; set; } = ServerColor.Purple;

        /// <summary>Live (по умолчанию) или Sandbox. После создания не меняется.</summary>
        public DeliveryType? DeliveryType { get; set; }
    }

    /// <summary>Изменение сервера: передавайте только поля, которые нужно поменять.</summary>
    public sealed class UpdateServerRequest : ServerSettingsBase
    {
        /// <summary>Новое название.</summary>
        public string? Name { get; set; }

        /// <summary>Новый цвет.</summary>
        public ServerColor? Color { get; set; }

        /// <summary>
        /// Уведомлять об ошибках SMTP API — попытках отправить через SMTP на адрес из стоп-листа.
        /// </summary>
        public bool? EnableSmtpApiErrorHooks { get; set; }
    }
}
