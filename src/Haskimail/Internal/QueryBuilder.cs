using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Haskimail.Internal
{
    /// <summary>Строка запроса. Пустые значения не добавляются, всё экранируется.</summary>
    internal sealed class QueryBuilder
    {
        private readonly List<KeyValuePair<string, string>> _items = [];

        public QueryBuilder Add(string name, string? value)
        {
            if (!string.IsNullOrEmpty(value)) _items.Add(new KeyValuePair<string, string>(name, value!));
            return this;
        }

        public QueryBuilder Add(string name, int? value) =>
            value.HasValue ? Add(name, value.Value.ToString(CultureInfo.InvariantCulture)) : this;

        /// <summary>Только дата: YYYY-MM-DD.</summary>
        public QueryBuilder AddDate(string name, DateTime? value) =>
            value.HasValue ? Add(name, value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : this;

        /// <summary>
        /// Дата или дата со временем (московское время). Полночь передаётся как дата без времени —
        /// для «по» API трактует её как конец дня.
        /// </summary>
        public QueryBuilder AddDateTime(string name, DateTime? value, char separator = ' ')
        {
            if (!value.HasValue) return this;
            var v = value.Value;
            var text = v.TimeOfDay == TimeSpan.Zero
                ? v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + separator + v.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            return Add(name, text);
        }

        public override string ToString()
        {
            if (_items.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            for (var i = 0; i < _items.Count; i++)
            {
                sb.Append(i == 0 ? '?' : '&')
                  .Append(Uri.EscapeDataString(_items[i].Key))
                  .Append('=')
                  .Append(Uri.EscapeDataString(_items[i].Value));
            }
            return sb.ToString();
        }
    }
}
