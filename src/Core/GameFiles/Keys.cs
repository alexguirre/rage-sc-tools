namespace ScTools.GameFiles;

using System;
using System.IO;
using System.Security.Cryptography;

public static class Keys
{
    private const int AesKeyLength = 32;
    private const int SHA1HashLength = 20;

    public static class NY
    {
        private const string CacheFile = "keys_ny.dat";
        public static byte[] AesKeyPC { get; private set; } = Array.Empty<byte>();

        public static void Load(string exeFilePath)
        {
            if (KeyCache.ReadKeysFromCache(CacheFile, out var keys, AesKeyLength))
            {
                AesKeyPC = keys[0];
                return;
            }

            var exeFile = File.ReadAllBytes(exeFilePath).AsSpan();
            AesKeyPC = SearchAesKeyPC(exeFile, out var aesKey) ? aesKey.ToArray() : throw new InvalidDataException("AES key not found");
            KeyCache.CacheKeys(CacheFile, AesKeyPC);
        }

        private static bool SearchAesKeyPC(ReadOnlySpan<byte> exeFile, out ReadOnlySpan<byte> aesKey)
        {
            var aesKeyHash = new byte[SHA1HashLength]
            {
                0xDE, 0xA3, 0x75, 0xEF, 0x1E, 0x6E, 0xF2, 0x22, 0x3A, 0x12, 0x21, 0xC2, 0xC5, 0x75, 0xC4, 0x7B, 0xF1, 0x7E, 0xFA, 0x5E
            };

            return SearchKey(exeFile, aesKeyHash, AesKeyLength, out aesKey);
        }
    }

    public static class MC4
    {
        private const string CacheFile = "keys_mc4.dat";
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
            var aesKeyHash = new byte[SHA1HashLength]
            {
                0x59, 0x9F, 0xA7, 0x13, 0xE0, 0x50, 0x85, 0xD9, 0xB8, 0x84, 0x94, 0x91, 0x39, 0xBA, 0x1F, 0x95, 0xBA, 0x20, 0x71, 0xA7
            };

            return SearchKey(xexFile, aesKeyHash, AesKeyLength, out aesKey);
        }
    }

    public static class Payne
    {
        private const string CacheFile = "keys_payne.dat";
        public static byte[] AesKeyPC { get; private set; } = Array.Empty<byte>();

        public static void Load(string exeFilePath)
        {
            if (KeyCache.ReadKeysFromCache(CacheFile, out var keys, AesKeyLength))
            {
                AesKeyPC = keys[0];
                return;
            }

            var exeFile = File.ReadAllBytes(exeFilePath).AsSpan();
            AesKeyPC = SearchAesKeyPC(exeFile, out var aesKey) ? aesKey.ToArray() : throw new InvalidDataException("AES key not found");
            KeyCache.CacheKeys(CacheFile, AesKeyPC);
        }

        private static bool SearchAesKeyPC(ReadOnlySpan<byte> exeFile, out ReadOnlySpan<byte> aesKey)
        {
            var aesKeyHash = new byte[SHA1HashLength]
            {
                0xDE, 0xA3, 0x75, 0xEF, 0x1E, 0x6E, 0xF2, 0x22, 0x3A, 0x12, 0x21, 0xC2, 0xC5, 0x75, 0xC4, 0x7B, 0xF1, 0x7E, 0xFA, 0x5E
            };

            return SearchKey(exeFile, aesKeyHash, AesKeyLength, out aesKey);
        }
    }


    private static bool SearchKey(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sha1KeyHash, int keyLength, out ReadOnlySpan<byte> key)
    {
        if (sha1KeyHash.Length != SHA1HashLength)
        {
            throw new ArgumentException($"{nameof(sha1KeyHash)} must be {SHA1HashLength} bytes long");
        }

        Span<byte> computedHash = stackalloc byte[SHA1HashLength];

        for (int i = 0; i < data.Length - keyLength; i++)
        {
            key = data.Slice(i, keyLength);
            SHA1.HashData(key, computedHash);

            if (computedHash.SequenceEqual(sha1KeyHash))
            {
                return true;
            }
        }

        key = default;
        return false;
    }
}
