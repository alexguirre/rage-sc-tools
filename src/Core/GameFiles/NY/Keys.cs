namespace ScTools.GameFiles.NY;

using System;
using System.IO;
using System.Security.Cryptography;

public static class Keys
{
    private const string CacheFile = "keys_ny.dat";

    private const int AesKeyLength = 32;
    public static byte[] AesKey { get; private set; } = Array.Empty<byte>();

    public static void Load(string exeFilePath)
    {
        if (KeyCache.ReadKeysFromCache(CacheFile, out var keys, AesKeyLength))
        {
            AesKey = keys[0];
            return;
        }

        var exeFile = File.ReadAllBytes(exeFilePath).AsSpan();
        AesKey = SearchAesKey(exeFile, out var aesKey) ? aesKey.ToArray() : throw new InvalidDataException("AES key not found");
        KeyCache.CacheKeys(CacheFile, AesKey);
    }

    internal static bool SearchAesKey(ReadOnlySpan<byte> exeFile, out ReadOnlySpan<byte> aesKey)
    {
        const int HashLength = 20;
        ReadOnlySpan<byte> aesKeyHash = stackalloc byte[HashLength]
        {
            0xDE, 0xA3, 0x75, 0xEF, 0x1E, 0x6E, 0xF2, 0x22, 0x3A, 0x12, 0x21, 0xC2, 0xC5, 0x75, 0xC4, 0x7B, 0xF1, 0x7E, 0xFA, 0x5E
        };
        Span<byte> computedHash = stackalloc byte[HashLength];

        for (int i = 0; i < exeFile.Length - AesKeyLength; i++)
        {
            aesKey = exeFile.Slice(i, AesKeyLength);
            SHA1.HashData(aesKey, computedHash);

            if (computedHash.SequenceEqual(aesKeyHash))
            {
                return true;
            }
        }

        aesKey = default;
        return false;
    }
}
