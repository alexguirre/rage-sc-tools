namespace ScTools.GameFiles.MC4;

using System;
using System.IO;
using System.Security.Cryptography;

public static class Keys
{
    private const string CacheFile = "keys_mc4.dat";

    private const int AesKeyLength = 32;
    public static byte[] AesKeyXenon { get; private set; } = Array.Empty<byte>();

    public static void Load(string xexFilePath)
    {
        if (KeyCache.ReadKeysFromCache(CacheFile, out var keys, AesKeyLength))
        {
            AesKeyXenon = keys[0];
            return;
        }

        var xexFile = File.ReadAllBytes(xexFilePath).AsSpan();
        // TODO: .xex files are not readable as-is so SearchAesKeyXenon doesn't find the key
        //AesKeyXenon = SearchAesKeyXenon(xexFile, out var aesKey) ? aesKey.ToArray() : throw new InvalidDataException("AES key not found");
        KeyCache.CacheKeys(CacheFile, AesKeyXenon);
    }

    private static bool SearchAesKeyXenon(ReadOnlySpan<byte> xexFile, out ReadOnlySpan<byte> aesKey)
    {
        var aesKeyHash = new byte[20]
        {
            0x59, 0x9F, 0xA7, 0x13, 0xE0, 0x50, 0x85, 0xD9, 0xB8, 0x84, 0x94, 0x91, 0x39, 0xBA, 0x1F, 0x95, 0xBA, 0x20, 0x71, 0xA7
        };

        return KeyCache.SearchKey(xexFile, aesKeyHash, AesKeyLength, out aesKey);
    }
}
