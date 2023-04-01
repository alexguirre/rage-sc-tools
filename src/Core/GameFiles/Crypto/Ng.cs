/*
 * Based on https://github.com/Neodymium146/gta-toolkit/tree/master/RageLib.GTA5/Cryptography
 *   Copyright(c) 2015 Neodymium
 *
 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:
 *
 *   The above copyright notice and this permission notice shall be included in
 *   all copies or substantial portions of the Software.
 *
 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *   THE SOFTWARE.
 */

namespace ScTools.GameFiles.Crypto;

using System.Runtime.InteropServices;

public record struct NgLut(byte[,] LUT0, byte[,] LUT1, byte[] Indices)
{
    public byte LookUp(uint value)
    {
        uint h16 = (value & 0xFFFF0000) >> 16;
        uint l8 = (value & 0x0000FF00) >> 8;
        uint l0 = (value & 0x000000FF) >> 0;
        return LUT0[LUT1[Indices[h16], l8], l0];
    }
}

public record struct NgContext(byte[][] KeySet, uint[][,] DecryptTables, uint[][,] EncryptTables, NgLut[][] EncryptLuts)
{
    public ReadOnlySpan<byte> SelectKey(string name, uint length)
    {
        uint hash =  JenkHash.LowercaseHash(name);
        uint keyIdx = (hash + length + (101 - 40)) % 0x65;
        return KeySet[keyIdx];
    }
}

public static class Ng
{
#region Decryption
    public static byte[] Decrypt(ReadOnlySpan<byte> data, string name, uint length, NgContext ctx) => Decrypt(data, ctx.SelectKey(name, length), ctx);

    public static byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key, NgContext ctx)
    {
        var decryptedData = new byte[data.Length];

        var keyuints = MemoryMarshal.Cast<byte, uint>(key);

        Span<byte> blockBuffer = stackalloc byte[16];
        for (int blockIndex = 0; blockIndex < data.Length / 16; blockIndex++)
        {
            data.Slice(16 * blockIndex, 16).CopyTo(blockBuffer);
            DecryptBlock(blockBuffer, keyuints, ctx);
            blockBuffer.CopyTo(decryptedData.AsSpan(16 * blockIndex, 16));
        }

        if (data.Length % 16 != 0)
        {
            var left = data.Length % 16;
            data[^left..].CopyTo(decryptedData.AsSpan(^left..));
        }

        return decryptedData;
    }

    private static void DecryptBlock(Span<byte> data, ReadOnlySpan<uint> key, NgContext ctx)
    {
        DecryptRoundA(data, SubKey(key, 0), ctx.DecryptTables[0]);
        DecryptRoundA(data, SubKey(key, 1), ctx.DecryptTables[1]);
        for (int k = 2; k <= 15; k++)
            DecryptRoundB(data, SubKey(key, k), ctx.DecryptTables[k]);
        DecryptRoundA(data, SubKey(key, 16), ctx.DecryptTables[16]);
    }

    // round 1,2,16
    internal static void DecryptRoundA(Span<byte> data, ReadOnlySpan<uint> key, uint[,] table)
    {
        var x1 =
            table[0, data[0]] ^
            table[1, data[1]] ^
            table[2, data[2]] ^
            table[3, data[3]] ^
            key[0];
        var x2 =
            table[4, data[4]] ^
            table[5, data[5]] ^
            table[6, data[6]] ^
            table[7, data[7]] ^
            key[1];
        var x3 =
            table[8, data[8]] ^
            table[9, data[9]] ^
            table[10, data[10]] ^
            table[11, data[11]] ^
            key[2];
        var x4 =
            table[12, data[12]] ^
            table[13, data[13]] ^
            table[14, data[14]] ^
            table[15, data[15]] ^
            key[3];

        MemoryMarshal.Write(data, ref x1);
        MemoryMarshal.Write(data[4..], ref x2);
        MemoryMarshal.Write(data[8..], ref x3);
        MemoryMarshal.Write(data[12..], ref x4);
    }

    // round 3-15
    private static void DecryptRoundB(Span<byte> data, ReadOnlySpan<uint> key, uint[,] table)
    {
        var x1 =
            table[0, data[0]] ^
            table[7, data[7]] ^
            table[10, data[10]] ^
            table[13, data[13]] ^
            key[0];
        var x2 =
            table[1, data[1]] ^
            table[4, data[4]] ^
            table[11, data[11]] ^
            table[14, data[14]] ^
            key[1];
        var x3 =
            table[2, data[2]] ^
            table[5, data[5]] ^
            table[8, data[8]] ^
            table[15, data[15]] ^
            key[2];
        var x4 =
            table[3, data[3]] ^
            table[6, data[6]] ^
            table[9, data[9]] ^
            table[12, data[12]] ^
            key[3];

        //var result = new byte[16];
        //Array.Copy(BitConverter.GetBytes(x1), 0, result, 0, 4);
        //Array.Copy(BitConverter.GetBytes(x2), 0, result, 4, 4);
        //Array.Copy(BitConverter.GetBytes(x3), 0, result, 8, 4);
        //Array.Copy(BitConverter.GetBytes(x4), 0, result, 12, 4);
        //return result;

        MemoryMarshal.Write(data, ref x1);
        MemoryMarshal.Write(data[4..], ref x2);
        MemoryMarshal.Write(data[8..], ref x3);
        MemoryMarshal.Write(data[12..], ref x4);

        /*data[0] = (byte)((x1 >> 0) & 0xFF);
        data[1] = (byte)((x1 >> 8) & 0xFF);
        data[2] = (byte)((x1 >> 16) & 0xFF);
        data[3] = (byte)((x1 >> 24) & 0xFF);
        data[4] = (byte)((x2 >> 0) & 0xFF);
        data[5] = (byte)((x2 >> 8) & 0xFF);
        data[6] = (byte)((x2 >> 16) & 0xFF);
        data[7] = (byte)((x2 >> 24) & 0xFF);
        data[8] = (byte)((x3 >> 0) & 0xFF);
        data[9] = (byte)((x3 >> 8) & 0xFF);
        data[10] = (byte)((x3 >> 16) & 0xFF);
        data[11] = (byte)((x3 >> 24) & 0xFF);
        data[12] = (byte)((x4 >> 0) & 0xFF);
        data[13] = (byte)((x4 >> 8) & 0xFF);
        data[14] = (byte)((x4 >> 16) & 0xFF);
        data[15] = (byte)((x4 >> 24) & 0xFF);*/
    }
#endregion // Decryption

#region Encryption
    public static byte[] Encrypt(ReadOnlySpan<byte> data, string name, uint length, NgContext ctx) => Encrypt(data, ctx.SelectKey(name, length), ctx);

    public static byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key, NgContext ctx)
    {
        var encryptedData = new byte[data.Length];

        var keyuints = MemoryMarshal.Cast<byte, uint>(key);

        Span<byte> blockBuffer = stackalloc byte[16];
        for (int blockIndex = 0; blockIndex < data.Length / 16; blockIndex++)
        {
            data.Slice(16 * blockIndex, 16).CopyTo(blockBuffer);
            EncryptBlock(blockBuffer, keyuints, ctx);
            blockBuffer.CopyTo(encryptedData.AsSpan(16 * blockIndex, 16));
        }

        if (data.Length % 16 != 0)
        {
            var left = data.Length % 16;
            data[^left..].CopyTo(encryptedData.AsSpan(^left..));
        }

        return encryptedData;
    }

    private static void EncryptBlock(Span<byte> data, ReadOnlySpan<uint> key, NgContext ctx)
    {
        EncryptRoundA(data, SubKey(key, 16), ctx.EncryptTables[16]);
        for (int k = 15; k >= 2; k--)
            EncryptRoundB_LUT(data, SubKey(key, k), ctx.EncryptLuts[k]);
        EncryptRoundA(data, SubKey(key, 1), ctx.EncryptTables[1]);
        EncryptRoundA(data, SubKey(key, 0), ctx.EncryptTables[0]);
    }

    private static void EncryptRoundA(Span<byte> data, ReadOnlySpan<uint> key, uint[,] table)
    {
        // apply xor to data first...
        var xorbuf = MemoryMarshal.Cast<uint, byte>(key);

        var x1 =
            table[0, data[0] ^ xorbuf[0]] ^
            table[1, data[1] ^ xorbuf[1]] ^
            table[2, data[2] ^ xorbuf[2]] ^
            table[3, data[3] ^ xorbuf[3]];
        var x2 =
            table[4, data[4] ^ xorbuf[4]] ^
            table[5, data[5] ^ xorbuf[5]] ^
            table[6, data[6] ^ xorbuf[6]] ^
            table[7, data[7] ^ xorbuf[7]];
        var x3 =
            table[8, data[8] ^ xorbuf[8]] ^
            table[9, data[9] ^ xorbuf[9]] ^
            table[10, data[10] ^ xorbuf[10]] ^
            table[11, data[11] ^ xorbuf[11]];
        var x4 =      
            table[12, data[12] ^ xorbuf[12]] ^
            table[13, data[13] ^ xorbuf[13]] ^
            table[14, data[14] ^ xorbuf[14]] ^
            table[15, data[15] ^ xorbuf[15]];

        MemoryMarshal.Write(data, ref x1);
        MemoryMarshal.Write(data[4..], ref x2);
        MemoryMarshal.Write(data[8..], ref x3);
        MemoryMarshal.Write(data[12..], ref x4);
    }

    private static void EncryptRoundB_LUT(Span<byte> data, ReadOnlySpan<uint> key, NgLut[] lut)
    {
        // apply xor to data first...
        var xorbuf = MemoryMarshal.Cast<uint, byte>(key);
        for (int y = 0; y < 16; y++)
        {
            data[y] ^= xorbuf[y];
        }

        var x1 = MemoryMarshal.Read<uint>(data);
        var x2 = MemoryMarshal.Read<uint>(data[4..]);
        var x3 = MemoryMarshal.Read<uint>(data[8..]);
        var x4 = MemoryMarshal.Read<uint>(data[12..]);
        data[0] = lut[0].LookUp(x1);
        data[1] = lut[1].LookUp(x2);
        data[2] = lut[2].LookUp(x3);
        data[3] = lut[3].LookUp(x4);
        data[4] = lut[4].LookUp(x2);
        data[5] = lut[5].LookUp(x3);
        data[6] = lut[6].LookUp(x4);
        data[7] = lut[7].LookUp(x1);
        data[8] = lut[8].LookUp(x3);
        data[9] = lut[9].LookUp(x4);
        data[10] = lut[10].LookUp(x1);
        data[11] = lut[11].LookUp(x2);
        data[12] = lut[12].LookUp(x4);
        data[13] = lut[13].LookUp(x1);
        data[14] = lut[14].LookUp(x2);
        data[15] = lut[15].LookUp(x3);
    }    
#endregion

    private static ReadOnlySpan<uint> SubKey(ReadOnlySpan<uint> key, int i) => key.Slice(4 * i, 4);
}
