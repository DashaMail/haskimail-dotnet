using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Haskimail.Internal;

namespace Haskimail.Models
{
    /// <summary>Общий фильтр статистики. Учитываются только даты (московское время).</summary>
    public sealed class StatsFilter
    {
        /// <summary>Начало периода. По умолчанию — месяц назад.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Конец периода включительно. По умолчанию — сегодня.</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>ID канала. Без него — все каналы сервера.</summary>
        public string? MessageStream { get; set; }
    }

    /// <summary>
    /// Итоги за период. Отправленные, открытия и клики — события (с повторами);
    /// возвраты, отписки и жалобы — адреса. Проценты считаются от Sent и округлены до сотых.
    /// </summary>
    public sealed class OutboundOverview
    {
        /// <summary>Отправлено писем.</summary>
        public int Sent { get; set; }

        /// <summary>Адресов с возвратом.</summary>
        public int Bounced { get; set; }

        /// <summary>Открытий, с повторными.</summary>
        public int Opened { get; set; }

        /// <summary>Кликов, с повторными.</summary>
        public int Clicked { get; set; }

        /// <summary>Отписавшихся адресов.</summary>
        public int Unsubscribed { get; set; }

        /// <summary>Адресов с жалобой на спам.</summary>
        public int SpamComplaints { get; set; }

        /// <summary>Доля возвратов, %.</summary>
        public double BounceRate { get; set; }

        /// <summary>Доля жалоб, %.</summary>
        public double SpamComplaintsRate { get; set; }

        /// <summary>Открываемость, %.</summary>
        public double OpenRate { get; set; }

        /// <summary>Кликабельность, %.</summary>
        public double ClickRate { get; set; }
    }

    /// <summary>Статистика по дням, по возрастанию даты. Дней без событий в списке нет.</summary>
    public sealed class DailyStats<TDay>
    {
        /// <summary>Дни с событиями.</summary>
        public IReadOnlyList<TDay> Days { get; set; } = [];
    }

    /// <summary>Отправленные за день.</summary>
    public sealed class SentDay
    {
        /// <summary>Дата.</summary>
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime Date { get; set; }

        /// <summary>Отправлено.</summary>
        public int Sent { get; set; }
    }

    /// <summary>Возвраты за день.</summary>
    public sealed class BounceDay
    {
        /// <summary>Дата.</summary>
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime Date { get; set; }

        /// <summary>Жёсткие возвраты.</summary>
        public int HardBounce { get; set; }

        /// <summary>Мягкие возвраты.</summary>
        public int SoftBounce { get; set; }
    }

    /// <summary>Жалобы на спам за день.</summary>
    public sealed class SpamComplaintDay
    {
        /// <summary>Дата.</summary>
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime Date { get; set; }

        /// <summary>Жалобы.</summary>
        public int SpamComplaint { get; set; }
    }

    /// <summary>Открытия за день.</summary>
    public sealed class OpenDay
    {
        /// <summary>Дата.</summary>
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime Date { get; set; }

        /// <summary>С повторными.</summary>
        public int Opens { get; set; }
    }

    /// <summary>Клики за день.</summary>
    public sealed class ClickDay
    {
        /// <summary>Дата.</summary>
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime Date { get; set; }

        /// <summary>С повторными.</summary>
        public int Clicks { get; set; }
    }

    /// <summary>Разбивка по платформам за день.</summary>
    public sealed class PlatformDay
    {
        /// <summary>Дата.</summary>
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime Date { get; set; }

        /// <summary>Компьютер.</summary>
        public int Desktop { get; set; }

        /// <summary>Телефон или планшет.</summary>
        public int Mobile { get; set; }

        /// <summary>Веб-интерфейс почты. Веб-почты грузят картинки через прокси, поэтому их открытия попадают сюда.</summary>
        public int WebMail { get; set; }

        /// <summary>В том числе боты.</summary>
        public int Unknown { get; set; }
    }

    /// <summary>Открытия или клики по платформам: по дням и итоги за период.</summary>
    public sealed class PlatformStats
    {
        /// <summary>По дням.</summary>
        public IReadOnlyList<PlatformDay> Days { get; set; } = [];

        /// <summary>Компьютер, за период.</summary>
        public int Desktop { get; set; }

        /// <summary>Телефон или планшет, за период.</summary>
        public int Mobile { get; set; }

        /// <summary>Веб-почта, за период.</summary>
        public int WebMail { get; set; }

        /// <summary>Не определено (в том числе боты), за период.</summary>
        public int Unknown { get; set; }
    }

    /// <summary>Разбивка за день.</summary>
    public sealed class BreakdownDay
    {
        /// <summary>Дата.</summary>
        public DateTime Date { get; set; }

        /// <summary>Название почтового клиента или браузера → количество за день.</summary>
        public IReadOnlyDictionary<string, int> Counts { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Разбивка с динамическими ключами (почтовые клиенты, браузеры).
    /// <see cref="Totals"/> — по убыванию, как в ответе API; «Unknown» — не удалось определить.
    /// </summary>
    public sealed class BreakdownStats
    {
        /// <summary>По дням.</summary>
        public IReadOnlyList<BreakdownDay> Days { get; set; } = [];

        /// <summary>Итоги за период по убыванию.</summary>
        public IReadOnlyList<KeyValuePair<string, int>> Totals { get; set; } = [];
    }
}
