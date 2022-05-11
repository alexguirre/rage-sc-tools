namespace ScTools.GameFiles.Payne;

using System;
using System.IO;

public static class Keys
{
    private const string CacheFile = "keys_payne.dat";

    private const int AesKeyLength = 32;
    public static byte[] AesKeyPC { get; private set; } = Array.Empty<byte>();

    public static void Load(string exeFilePath)
    {
        if (KeyCache.ReadKeysFromCache(CacheFile, out var keys, AesKeyLength))
        {
            AesKeyPC = keys[0];
            return;
        }

        var exeFile = File.ReadAllBytes(exeFilePath).AsSpan();
        // Max Payne 3 uses the same AES key as GTA IV for the encrypted scripts
        AesKeyPC = NY.Keys.SearchAesKeyPC(exeFile, out var aesKey) ? aesKey.ToArray() : throw new InvalidDataException("AES key not found");
        KeyCache.CacheKeys(CacheFile, AesKeyPC);
    }
}
