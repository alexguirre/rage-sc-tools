namespace ScTools.GameFiles;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

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
        using var f = new FileStream(cacheFile, FileMode.Create, FileAccess.Write);
        foreach (var key in keys)
        {
            f.Write(key, 0, key.Length);
        }
    }

    public static bool ReadKeysFromCache(string cacheFile, out ScTools.GameFiles.Crypto.NgContext ng)
    {
        if (File.Exists(cacheFile))
        {
            try
            {
                using var f = new ZLibStream(new FileStream(cacheFile, FileMode.Open, FileAccess.Read), CompressionMode.Decompress);
                var keySet = new byte[101][];
                for (int i = 0; i < keySet.Length; i++)
                {
                    keySet[i] = new byte[0x110];
                    f.ReadExactly(keySet[i]);
                }
                
                var decryptTables = new uint[17][,];
                for (int i = 0; i < decryptTables.Length; i++)
                {
                    decryptTables[i] = new uint[16, 0x100];
                    f.ReadExactly(MemoryMarshal.AsBytes(decryptTables[i].AsSpan()));
                }

                var encryptTables = new uint[17][,];
                for (int i = 0; i < encryptTables.Length; i++)
                {
                    encryptTables[i] = new uint[16, 0x100];
                    f.ReadExactly(MemoryMarshal.AsBytes(encryptTables[i].AsSpan()));
                }

                var encryptLuts = new ScTools.GameFiles.Crypto.NgLut[17][];
                for (int i = 0; i < encryptLuts.Length; i++)
                {
                    encryptLuts[i] = new ScTools.GameFiles.Crypto.NgLut[16];
                    for (int j = 0; j < encryptLuts[i].Length; j++)
                    {
                        var lut0 = new byte[256, 256];
                        var lut1 = new byte[256, 256];
                        var indices = new byte[65536];

                        f.ReadExactly(lut0.AsSpan());
                        f.ReadExactly(lut1.AsSpan());
                        f.ReadExactly(indices);

                        encryptLuts[i][j] = new(lut0, lut1, indices);
                    }
                }

                ng = new(keySet, decryptTables, encryptTables, encryptLuts);
                return true;
            }
            catch
            {
            }
        }

        ng = default;
        return false;
    }

    public static void CacheKeys(string cacheFile, ScTools.GameFiles.Crypto.NgContext ng)
    {
        using var f = new ZLibStream(new FileStream(cacheFile, FileMode.Create, FileAccess.Write), CompressionLevel.Fastest);

        foreach (var key in ng.KeySet)
        {
            f.Write(key);
        }

        foreach (var table in ng.DecryptTables)
        {
            f.Write(MemoryMarshal.AsBytes(table.AsSpan()));
        }

        foreach (var table in ng.EncryptTables)
        {
            f.Write(MemoryMarshal.AsBytes(table.AsSpan()));
        }

        foreach (var luts in ng.EncryptLuts)
        {
            foreach (var lut in luts)
            {
                f.Write(lut.LUT0.AsSpan());
                f.Write(lut.LUT1.AsSpan());
                f.Write(lut.Indices);
            }
        }
    }
}
