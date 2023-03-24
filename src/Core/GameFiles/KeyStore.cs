namespace ScTools.GameFiles;

using System;
using System.IO;
using System.Security.Cryptography;

public sealed class KeyStore
{
    public record struct GTA5Keys;
    public record struct GTA4Keys(byte[]? AesKeyPC);
    public record struct MC4Keys(byte[]? AesKeyXenon);
    public record struct RDR2Keys(byte[]? AesKeyXenon);
    public record struct MP3Keys(byte[]? AesKeyPC);

    public GTA5Keys GTA5 { get; init; }
    public GTA4Keys GTA4 { get; init; }
    public MC4Keys MC4 { get; init; }
    public RDR2Keys RDR2 { get; init; }
    public MP3Keys MP3 { get; init; }

    public static KeyStore LoadAll(string cacheDirectory, string? gta5ExePath, string? gta4ExePath, string? mc4XexPath, string? rdr2XexPath, string? mp3ExePath)
    {
        if (!Directory.Exists(cacheDirectory))
        {
            throw new DirectoryNotFoundException($"Cache directory '{cacheDirectory}' does not exist");
        }
        
        //CodeWalker.GameFiles.GTA5Keys.LoadFromPath("D:\\programs\\Rockstar Games\\Grand Theft Auto V"); // a bit slow without cache
        return new()
        {
            GTA5 = GTA5Loader.Load(gta5ExePath, cacheDirectory),
            GTA4 = GTA4Loader.Load(gta4ExePath, cacheDirectory),
            MC4 = MC4Loader.Load(mc4XexPath, cacheDirectory),
            RDR2 = RDR2Loader.Load(rdr2XexPath, cacheDirectory),
            MP3 = MP3Loader.Load(mp3ExePath, cacheDirectory),
        };
    }

    private const int SHA1HashLength = 20;

    private static class GTA5Loader
    {
        private const string CacheFile = "keys_gta5.dat";

        public static GTA5Keys Load(string? exeFilePath, string cacheDirectory)
        {
            // TODO: implement GTA5 key loader once we no longer depend on CodeWalker for writing .ysc files
            return default;
        }
    }

    private static class GTA4Loader
    {
        private const string CacheFile = "keys_gta4.dat";

        public static GTA4Keys Load(string? exeFilePath, string cacheDirectory)
        {
            var cacheFile = Path.Combine(cacheDirectory, CacheFile);
            if (KeyCache.ReadKeysFromCache(cacheFile, out var keys, Aes.KeyLength))
            {
                return new(keys[0]);
            }
    
            if (!File.Exists(exeFilePath))
            {
                return default;
            }
    
            var exeFile = File.ReadAllBytes(exeFilePath).AsSpan();
            var aesKeyPC = SearchAesKeyPC(exeFile, out var aesKey) ?
                aesKey.ToArray() :
                throw new ArgumentException($"AES key not found in '{exeFilePath}'");
            KeyCache.CacheKeys(cacheFile, aesKeyPC);
            return new(aesKeyPC);
        }
    
        private static bool SearchAesKeyPC(ReadOnlySpan<byte> exeFile, out ReadOnlySpan<byte> aesKey)
        {
            ReadOnlySpan<byte> aesKeyHash = stackalloc byte[SHA1HashLength]
            {
                0xDE, 0xA3, 0x75, 0xEF, 0x1E, 0x6E, 0xF2, 0x22, 0x3A, 0x12, 0x21, 0xC2, 0xC5, 0x75, 0xC4, 0x7B, 0xF1, 0x7E, 0xFA, 0x5E
            };
    
            return SearchKey(exeFile, aesKeyHash, Aes.KeyLength, out aesKey);
        }
    }

    private static class MC4Loader
    {
        private const string CacheFile = "keys_mc4.dat";

        public static MC4Keys Load(string? xexFilePath, string cacheDirectory)
        {
            var cacheFile = Path.Combine(cacheDirectory, CacheFile);
            if (KeyCache.ReadKeysFromCache(cacheFile, out var keys, Aes.KeyLength))
            {
                return new(keys[0]);
            }
    
            if (!File.Exists(xexFilePath))
            {
                return default;
            }
            
            var xexFile = File.ReadAllBytes(xexFilePath).AsSpan();
            var aesKeyXenon = SearchAesKeyXenon(xexFile, out var aesKey) ?
                aesKey.ToArray() :
                throw new ArgumentException($"AES key not found in '{xexFilePath}'. Make sure the XEX file is uncompressed and unencrypted.");
            KeyCache.CacheKeys(cacheFile, aesKeyXenon);
            return new(aesKeyXenon);
        }
    
        private static bool SearchAesKeyXenon(ReadOnlySpan<byte> xexFile, out ReadOnlySpan<byte> aesKey)
        {
            ReadOnlySpan<byte> aesKeyHash = stackalloc byte[SHA1HashLength]
            {
                0x59, 0x9F, 0xA7, 0x13, 0xE0, 0x50, 0x85, 0xD9, 0xB8, 0x84, 0x94, 0x91, 0x39, 0xBA, 0x1F, 0x95, 0xBA, 0x20, 0x71, 0xA7
            };
    
            return SearchKey(xexFile, aesKeyHash, Aes.KeyLength, out aesKey);
        }
    }
    
    private static class RDR2Loader
    {
        private const string CacheFile = "keys_rdr2.dat";

        public static RDR2Keys Load(string? xexFilePath, string cacheDirectory)
        {
            var cacheFile = Path.Combine(cacheDirectory, CacheFile);
            if (KeyCache.ReadKeysFromCache(cacheFile, out var keys, Aes.KeyLength))
            {
                return new(keys[0]);
            }
    
            if (!File.Exists(xexFilePath))
            {
                return default;
            }
    
            var xexFile = File.ReadAllBytes(xexFilePath).AsSpan();
            var aesKeyXenon = SearchAesKeyXenon(xexFile, out var aesKey) ?
                aesKey.ToArray() :
                throw new ArgumentException($"AES key not found in '{xexFilePath}'. Make sure the XEX file is uncompressed and unencrypted.");
            KeyCache.CacheKeys(cacheFile, aesKeyXenon);
            return new(aesKeyXenon);
        }
    
        private static bool SearchAesKeyXenon(ReadOnlySpan<byte> xexFile, out ReadOnlySpan<byte> aesKey)
        {
            ReadOnlySpan<byte> aesKeyHash = stackalloc byte[SHA1HashLength]
            {
                0x87, 0x86, 0x24, 0x97, 0xEE, 0x46, 0x85, 0x53, 0x72, 0xB5, 0x1C, 0x7A, 0x32, 0x4A, 0x2B, 0xB5, 0xCD, 0x66, 0xF4, 0xAF
            };
    
            return SearchKey(xexFile, aesKeyHash, Aes.KeyLength, out aesKey);
        }
    }
    
    private static class MP3Loader
    {
        private const string CacheFile = "keys_mp3.dat";

        public static MP3Keys Load(string? exeFilePath, string cacheDirectory)
        {
            var cacheFile = Path.Combine(cacheDirectory, CacheFile);
            if (KeyCache.ReadKeysFromCache(cacheFile, out var keys, Aes.KeyLength))
            {
                return new(keys[0]);
            }
    
            if (!File.Exists(exeFilePath))
            {
                return default;
            }
    
            var exeFile = File.ReadAllBytes(exeFilePath).AsSpan();
            var aesKeyPC = SearchAesKeyPC(exeFile, out var aesKey) ?
                aesKey.ToArray() :
                throw new ArgumentException($"AES key not found in '{exeFilePath}'");
            KeyCache.CacheKeys(cacheFile, aesKeyPC);
            return new(aesKeyPC);
        }
    
        private static bool SearchAesKeyPC(ReadOnlySpan<byte> exeFile, out ReadOnlySpan<byte> aesKey)
        {
            ReadOnlySpan<byte> aesKeyHash = stackalloc byte[SHA1HashLength]
            {
                0xDE, 0xA3, 0x75, 0xEF, 0x1E, 0x6E, 0xF2, 0x22, 0x3A, 0x12, 0x21, 0xC2, 0xC5, 0x75, 0xC4, 0x7B, 0xF1, 0x7E, 0xFA, 0x5E
            };
    
            return SearchKey(exeFile, aesKeyHash, Aes.KeyLength, out aesKey);
        }
    }

    private static bool SearchKey(ReadOnlySpan<byte> data, scoped ReadOnlySpan<byte> sha1KeyHash, int keyLength, out ReadOnlySpan<byte> key)
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
