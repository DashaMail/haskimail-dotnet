using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Haskimail.Internal
{
    /// <summary>
    /// Единая точка для API, которых нет в netstandard2.0. Остальной код от целевой платформы не зависит.
    /// </summary>
    internal static class Polyfills
    {
#if NETSTANDARD2_0
        public static readonly HttpMethod Patch = new("PATCH");
#else
        public static readonly HttpMethod Patch = HttpMethod.Patch;
#endif

        public static Task<byte[]> ReadBytesAsync(HttpContent content, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            cancellationToken.ThrowIfCancellationRequested();
            return content.ReadAsByteArrayAsync();
#else
            return content.ReadAsByteArrayAsync(cancellationToken);
#endif
        }

        public static Task<string> ReadToEndAsync(StreamReader reader, CancellationToken cancellationToken)
        {
#if NET7_0_OR_GREATER
            return reader.ReadToEndAsync(cancellationToken);
#else
            cancellationToken.ThrowIfCancellationRequested();
            return reader.ReadToEndAsync();
#endif
        }

        /// <summary>MD5 в виде hex-строки в нижнем регистре.</summary>
        public static string Md5HexLower(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
#if NETSTANDARD2_0
            byte[] hash;
#pragma warning disable CA5351 // MD5 задан протоколом подписи вебхуков Haskimail, это не наш выбор алгоритма
            using (var md5 = MD5.Create()) hash = md5.ComputeHash(bytes);
#pragma warning restore CA5351
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return sb.ToString();
#else
#pragma warning disable CA5351 // MD5 задан протоколом подписи вебхуков Haskimail, это не наш выбор алгоритма
            var hash = MD5.HashData(bytes);
#pragma warning restore CA5351
            return Convert.ToHexString(hash).ToLowerInvariant();
#endif
        }

        /// <summary>Сравнение за постоянное время (не выдаёт позицию первого расхождения).</summary>
        public static bool FixedTimeEquals(byte[] left, byte[] right)
        {
#if NETSTANDARD2_0
            if (left.Length != right.Length) return false;
            var diff = 0;
            for (var i = 0; i < left.Length; i++) diff |= left[i] ^ right[i];
            return diff == 0;
#else
            return CryptographicOperations.FixedTimeEquals(left, right);
#endif
        }

        /// <summary>Потокобезопасное случайное число в [0, 1).</summary>
        public static double NextRandomDouble()
        {
#if NET6_0_OR_GREATER
            return Random.Shared.NextDouble();
#else
            lock (SharedRandom) return SharedRandom.NextDouble();
#endif
        }

#if !NET6_0_OR_GREATER
        private static readonly Random SharedRandom = new();
#endif
    }
}
