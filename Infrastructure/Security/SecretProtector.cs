using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security
{
    public sealed class EncryptionOptions
    {
        public const string Section = "Encryption";

        /// <summary>Ключ AES-256 в Base64 (32 байта). Задаётся в секретах/переменной окружения Encryption__Key.</summary>
        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// Шифрует секреты (пароли аккаунтов и прокси) перед записью в БД алгоритмом AES-256-GCM.
    /// Формат значения в БД: "enc:v1:" + Base64(nonce | tag | ciphertext).
    /// Значения без префикса считаются старыми незашифрованными и возвращаются как есть.
    /// </summary>
    public sealed class SecretProtector
    {
        public const string Prefix = "enc:v1:";
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly byte[] _key;

        public SecretProtector(IOptions<EncryptionOptions> options)
        {
            var raw = options.Value.Key;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "Не задан ключ шифрования Encryption:Key (переменная окружения Encryption__Key). " +
                    "Сгенерируйте его: openssl rand -base64 32");

            try { _key = Convert.FromBase64String(raw); }
            catch (FormatException)
            {
                throw new InvalidOperationException("Encryption:Key должен быть строкой Base64.");
            }

            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption:Key должен содержать ровно 32 байта (AES-256).");
        }

        public static bool IsProtected(string? value)
            => value is not null && value.StartsWith(Prefix, StringComparison.Ordinal);

        public string Protect(string? plain)
        {
            if (string.IsNullOrEmpty(plain) || IsProtected(plain))
                return plain ?? string.Empty;

            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var buffer = new byte[NonceSize + TagSize + plainBytes.Length];
            var nonce = buffer.AsSpan(0, NonceSize);
            var tag = buffer.AsSpan(NonceSize, TagSize);
            var cipher = buffer.AsSpan(NonceSize + TagSize);

            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipher, tag);

            return Prefix + Convert.ToBase64String(buffer);
        }

        public string Unprotect(string? stored)
        {
            if (string.IsNullOrEmpty(stored) || !IsProtected(stored))
                return stored ?? string.Empty; // старое незашифрованное значение

            var buffer = Convert.FromBase64String(stored[Prefix.Length..]);
            if (buffer.Length < NonceSize + TagSize)
                throw new CryptographicException("Повреждённое зашифрованное значение.");

            var nonce = buffer.AsSpan(0, NonceSize);
            var tag = buffer.AsSpan(NonceSize, TagSize);
            var cipher = buffer.AsSpan(NonceSize + TagSize);
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
