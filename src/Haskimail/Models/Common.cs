using System.Text.Json.Serialization;

namespace Haskimail.Models
{
    /// <summary>
    /// Ответ вида <c>{"ErrorCode": 0, "Message": "..."}</c> — удаление, повтор, обход правил.
    /// </summary>
    public sealed class ApiResult
    {
        /// <summary>0 — успех.</summary>
        public int ErrorCode { get; set; }

        /// <summary>Описание результата.</summary>
        public string? Message { get; set; }

        /// <summary>Операция выполнена.</summary>
        [JsonIgnore]
        public bool IsSuccess => ErrorCode == 0;
    }
}
