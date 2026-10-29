using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Haskimail.Models
{
    /// <summary>DNS-запись, которую нужно добавить для домена.</summary>
    public sealed class DnsRecord
    {
        /// <summary>Имя записи.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>Всегда TXT.</summary>
        [JsonPropertyName("record_type")]
        public string? RecordType { get; set; }

        /// <summary>Значение записи.</summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        /// <summary>1 — запись найдена и верна, 0 — нет.</summary>
        [JsonPropertyName("valid")]
        public int Valid { get; set; }

        /// <summary>Запись найдена и верна.</summary>
        [JsonIgnore]
        public bool IsValid => Valid == 1;
    }

    /// <summary>Домен отправителя.</summary>
    public class DomainSummary
    {
        /// <summary>ID домена.</summary>
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        /// <summary>Домен.</summary>
        public string? Name { get; set; }

        /// <summary>DKIM подтверждён.</summary>
        [JsonPropertyName("DKIMVerified")]
        public bool DkimVerified { get; set; }

        /// <summary>Всегда false.</summary>
        [JsonPropertyName("WeakDKIM")]
        public bool WeakDkim { get; set; }

        /// <summary>SPF подтверждён.</summary>
        [JsonPropertyName("SPFVerified")]
        public bool SpfVerified { get; set; }

        /// <summary>Совпадает с <see cref="SpfVerified"/>.</summary>
        public bool ReturnPathDomainVerified { get; set; }
    }

    /// <summary>Домен с DNS-записями. Состояние — по последней проверке.</summary>
    public sealed class DomainDetails : DomainSummary
    {
        /// <summary>SPF-запись.</summary>
        [JsonPropertyName("SPFTextValue")]
        public DnsRecord? SpfTextValue { get; set; }

        /// <summary>DKIM-запись.</summary>
        [JsonPropertyName("DKIMTextValue")]
        public DnsRecord? DkimTextValue { get; set; }
    }

    /// <summary>Страница доменов.</summary>
    public sealed class DomainList
    {
        /// <summary>Всего записей по фильтру, без учёта count и offset.</summary>
        public int TotalCount { get; set; }

        /// <summary>Домены.</summary>
        public IReadOnlyList<DomainSummary> Domains { get; set; } = [];
    }

    /// <summary>Результат проверки DKIM.</summary>
    public sealed class DkimVerificationResult
    {
        /// <summary>ID домена.</summary>
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        /// <summary>Домен.</summary>
        public string? Name { get; set; }

        /// <summary>DKIM подтверждён.</summary>
        [JsonPropertyName("DKIMVerified")]
        public bool DkimVerified { get; set; }

        /// <summary>Всегда false.</summary>
        [JsonPropertyName("WeakDKIM")]
        public bool WeakDkim { get; set; }

        /// <summary>Совпадает с SPFVerified.</summary>
        public bool ReturnPathDomainVerified { get; set; }

        /// <summary>Имя DKIM-записи.</summary>
        [JsonPropertyName("DKIMHost")]
        public string? DkimHost { get; set; }

        /// <summary>DKIM-запись.</summary>
        [JsonPropertyName("DKIMTextValue")]
        public DnsRecord? DkimTextValue { get; set; }
    }

    /// <summary>Результат проверки SPF.</summary>
    public sealed class SpfVerificationResult
    {
        /// <summary>ID домена.</summary>
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        /// <summary>Домен.</summary>
        public string? Name { get; set; }

        /// <summary>SPF подтверждён.</summary>
        [JsonPropertyName("SPFVerified")]
        public bool SpfVerified { get; set; }

        /// <summary>Имя SPF-записи.</summary>
        [JsonPropertyName("SPFHost")]
        public string? SpfHost { get; set; }

        /// <summary>Ваша SPF-запись с добавленным include:_spf.haskisender.ru — её и нужно опубликовать.</summary>
        [JsonPropertyName("SPFTextValue")]
        public DnsRecord? SpfTextValue { get; set; }
    }

    internal sealed class CreateDomainRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
