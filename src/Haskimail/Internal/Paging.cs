using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Haskimail.Internal
{
    internal static class Paging
    {
        /// <summary>
        /// Обходит постраничный список. Останавливается, когда записи кончились, достигнут TotalCount
        /// или окно count + offset упёрлось в лимит API (<paramref name="maxWindow"/>).
        /// </summary>
        public static async IAsyncEnumerable<T> EnumerateAsync<T>(
            Func<int, int, CancellationToken, Task<(IReadOnlyList<T> Items, int TotalCount)>> fetchPage,
            int startOffset,
            int pageSize,
            int? maxWindow,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var offset = startOffset;
            while (true)
            {
                var count = pageSize;
                if (maxWindow.HasValue)
                {
                    count = Math.Min(count, maxWindow.Value - offset);
                    if (count <= 0) yield break;
                }

                var (items, total) = await fetchPage(offset, count, cancellationToken).ConfigureAwait(false);
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }

                offset += items.Count;
                if (items.Count == 0 || items.Count < count || offset >= total) yield break;
            }
        }
    }
}
