namespace ScTools.GameFiles;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

internal static class KeyCache
{
    public static bool ReadKeysFromCache(string cacheFile, [NotNullWhen(true)] out byte[][]? keys, params int[] keysLengths)
    {
        if (File.Exists(cacheFile))
        {
            try
            {
                using var f = new FileStream(cacheFile, FileMode.Open, FileAccess.Read);
                var read = 0;
                keys = new byte[keysLengths.Length][];
                for (int i = 0; i < keysLengths.Length; i++)
                {
                    keys[i] = new byte[keysLengths[i]];
                    read += f.Read(keys[i], 0, keysLengths[i]);
                }

                return read == keysLengths.Sum();
            }
            catch
            {
            }
        }

        keys = null;
        return false;
    }

    public static void CacheKeys(string cacheFile, params byte[][] keys)
    {
        using var f = new FileStream(cacheFile, FileMode.OpenOrCreate, FileAccess.Write);
        foreach (var key in keys)
        {
            f.Write(key, 0, key.Length);
        }
    }
}
