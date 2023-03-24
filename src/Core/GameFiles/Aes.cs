namespace ScTools.GameFiles;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

public static class Aes
{
    public const int KeyLength = 32;

    public static void Decrypt(byte[] data, byte[] key) => Transform(data, key, encrypt: false);
    public static void Encrypt(byte[] data, byte[] key) => Transform(data, key, encrypt: true);

    private static void Transform(byte[] data, byte[] key, bool encrypt)
    {
        ThrowIfInvalidKey(key, nameof(key));

        var aes = System.Security.Cryptography.Aes.Create();
        aes.BlockSize = 128;
        aes.KeySize = 256;
        aes.Mode = CipherMode.ECB;
        aes.Key = key;
        aes.IV = new byte[16];
        aes.Padding = PaddingMode.None;

        ICryptoTransform transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();

        int dataLen = data.Length & ~0x0F;

        if (dataLen > 0)
        {
            for (int i = 0; i < 16; i++)
            {
                transform.TransformBlock(data, 0, dataLen, data, 0);
            }
        }
    }

    /// <exception cref="ArgumentNullException">If <paramref name="aesKey"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If length of <paramref name="aesKey"/> is not <see cref="KeyLength"/>.</exception>
    internal static void ThrowIfInvalidKey([NotNull] byte[]? aesKey, [CallerArgumentExpression("aesKey")] string? paramName = null)
    {
        if (aesKey is null) { throw new ArgumentNullException(paramName); }
        if (aesKey.Length != KeyLength) { throw new ArgumentException($"{paramName} must be {KeyLength} bytes long"); }
    }

    public static bool IsValidKey([NotNullWhen(true)] byte[]? aesKey)
    {
        return aesKey is not null && aesKey.Length == KeyLength;
    }
}
