using System;
using System.Globalization;

namespace Haskimail.Internal
{
    internal static class Guard
    {
        public static T NotNull<T>(T? value, string paramName) where T : class =>
            value ?? throw new ArgumentNullException(paramName);

        public static string NotEmpty(string? value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Значение не может быть пустым.", paramName);
            return value!;
        }

        /// <summary>Сегмент пути: проверяется на пустоту и экранируется (ID шаблона может быть названием с «/» и кириллицей).</summary>
        public static string PathSegment(string? value, string paramName) =>
            Uri.EscapeDataString(NotEmpty(value, paramName));

        public static string PathSegment(long value) => value.ToString(CultureInfo.InvariantCulture);

        public static void Paging(int? count, int? offset, int maxCount)
        {
            if (count.HasValue && (count.Value < 1 || count.Value > maxCount))
                throw new ArgumentOutOfRangeException(nameof(count), count, $"count должен быть от 1 до {maxCount}.");
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset не может быть отрицательным.");
        }
    }
}
