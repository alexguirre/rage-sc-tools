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
    public byte[] SelectKey(string name, uint length)
    {
        uint hash =  JenkHash.LowercaseHash(name);
        uint keyIdx = (hash + length + (101 - 40)) % 0x65;
        return KeySet[keyIdx];
    }
}

public static class Ng
{
#region Decryption
    public static byte[] Decrypt(byte[] data, string name, uint length, NgContext ctx) => Decrypt(data, ctx.SelectKey(name, length), ctx);

    public static byte[] Decrypt(byte[] data, byte[] key, NgContext ctx)
    {
        var decryptedData = new byte[data.Length];

        var keyuints = new uint[key.Length / 4];
        Buffer.BlockCopy(key, 0, keyuints, 0, key.Length);

        for (int blockIndex = 0; blockIndex < data.Length / 16; blockIndex++)
        {
            var encryptedBlock = new byte[16];
            Array.Copy(data, 16 * blockIndex, encryptedBlock, 0, 16);
            var decryptedBlock = DecryptBlock(encryptedBlock, keyuints, ctx);
            Array.Copy(decryptedBlock, 0, decryptedData, 16 * blockIndex, 16);
        }

        if (data.Length % 16 != 0)
        {
            var left = data.Length % 16;
            Buffer.BlockCopy(data, data.Length - left, decryptedData, data.Length - left, left);
        }

        return decryptedData;
    }

    private static byte[] DecryptBlock(byte[] data, uint[] key, NgContext ctx)
    {
        var buffer = data;

        // prepare key...
        var subKeys = new uint[17][];
        for (int i = 0; i < 17; i++)
        {
            subKeys[i] = new uint[4];
            subKeys[i][0] = key[4 * i + 0];
            subKeys[i][1] = key[4 * i + 1];
            subKeys[i][2] = key[4 * i + 2];
            subKeys[i][3] = key[4 * i + 3];
        }

        buffer = DecryptRoundA(buffer, subKeys[0], ctx.DecryptTables[0]);
        buffer = DecryptRoundA(buffer, subKeys[1], ctx.DecryptTables[1]);
        for (int k = 2; k <= 15; k++)
            buffer = DecryptRoundB(buffer, subKeys[k], ctx.DecryptTables[k]);
        buffer = DecryptRoundA(buffer, subKeys[16], ctx.DecryptTables[16]);

        return buffer;
    }

    // round 1,2,16
    internal static byte[] DecryptRoundA(byte[] data, uint[] key, uint[,] table)
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

        var result = new byte[16];
        Array.Copy(BitConverter.GetBytes(x1), 0, result, 0, 4);
        Array.Copy(BitConverter.GetBytes(x2), 0, result, 4, 4);
        Array.Copy(BitConverter.GetBytes(x3), 0, result, 8, 4);
        Array.Copy(BitConverter.GetBytes(x4), 0, result, 12, 4);
        return result;
    }

    // round 3-15
    private static byte[] DecryptRoundB(byte[] data, uint[] key, uint[,] table)
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

        var result = new byte[16];
        result[0] = (byte)((x1 >> 0) & 0xFF);
        result[1] = (byte)((x1 >> 8) & 0xFF);
        result[2] = (byte)((x1 >> 16) & 0xFF);
        result[3] = (byte)((x1 >> 24) & 0xFF);
        result[4] = (byte)((x2 >> 0) & 0xFF);
        result[5] = (byte)((x2 >> 8) & 0xFF);
        result[6] = (byte)((x2 >> 16) & 0xFF);
        result[7] = (byte)((x2 >> 24) & 0xFF);
        result[8] = (byte)((x3 >> 0) & 0xFF);
        result[9] = (byte)((x3 >> 8) & 0xFF);
        result[10] = (byte)((x3 >> 16) & 0xFF);
        result[11] = (byte)((x3 >> 24) & 0xFF);
        result[12] = (byte)((x4 >> 0) & 0xFF);
        result[13] = (byte)((x4 >> 8) & 0xFF);
        result[14] = (byte)((x4 >> 16) & 0xFF);
        result[15] = (byte)((x4 >> 24) & 0xFF);
        return result;
    }
#endregion // Decryption

#region Encryption
    public static byte[] Encrypt(byte[] data, string name, uint length, NgContext ctx) => Encrypt(data, ctx.SelectKey(name, length), ctx);

    public static byte[] Encrypt(byte[] data, byte[] key, NgContext ctx)
    {
        var encryptedData = new byte[data.Length];

        var keyuints = new uint[key.Length / 4];
        Buffer.BlockCopy(key, 0, keyuints, 0, key.Length);

        for (int blockIndex = 0; blockIndex < data.Length / 16; blockIndex++)
        {
            byte[] decryptedBlock = new byte[16];
            Array.Copy(data, 16 * blockIndex, decryptedBlock, 0, 16);
            byte[] encryptedBlock = EncryptBlock(decryptedBlock, keyuints, ctx);
            Array.Copy(encryptedBlock, 0, encryptedData, 16 * blockIndex, 16);
        }

        if (data.Length % 16 != 0)
        {
            var left = data.Length % 16;
            Buffer.BlockCopy(data, data.Length - left, encryptedData, data.Length - left, left);
        }

        return encryptedData;
    }

    private static byte[] EncryptBlock(byte[] data, uint[] key, NgContext ctx)
    {
        var buffer = data;

        // prepare key...
        var subKeys = new uint[17][];
        for (int i = 0; i < 17; i++)
        {
            subKeys[i] = new uint[4];
            subKeys[i][0] = key[4 * i + 0];
            subKeys[i][1] = key[4 * i + 1];
            subKeys[i][2] = key[4 * i + 2];
            subKeys[i][3] = key[4 * i + 3];
        }

        buffer = EncryptRoundA(buffer, subKeys[16], ctx.EncryptTables[16]);
        for (int k = 15; k >= 2; k--)
            buffer = EncryptRoundB_LUT(buffer, subKeys[k], ctx.EncryptLuts[k]);
        buffer = EncryptRoundA(buffer, subKeys[1], ctx.EncryptTables[1]);
        buffer = EncryptRoundA(buffer, subKeys[0], ctx.EncryptTables[0]);

        return buffer;
    }

    private static byte[] EncryptRoundA(byte[] data, uint[] key, uint[,] table)
    {
        // apply xor to data first...
        var xorbuf = new byte[16];
        Buffer.BlockCopy(key, 0, xorbuf, 0, 16);

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

        var buf = new byte[16];
        Array.Copy(BitConverter.GetBytes(x1), 0, buf, 0, 4);
        Array.Copy(BitConverter.GetBytes(x2), 0, buf, 4, 4);
        Array.Copy(BitConverter.GetBytes(x3), 0, buf, 8, 4);
        Array.Copy(BitConverter.GetBytes(x4), 0, buf, 12, 4);
        return buf;
    }

    private static byte[] EncryptRoundB_LUT(byte[] dataOld, uint[] key, NgLut[] lut)
    {
        var data = (byte[])dataOld.Clone();

        // apply xor to data first...
        var xorbuf = new byte[16];
        Buffer.BlockCopy(key, 0, xorbuf, 0, 16);
        for (int y = 0; y < 16; y++)
        {
            data[y] ^= xorbuf[y];
        }

        return new byte[]
        {
            lut[0].LookUp(BitConverter.ToUInt32(new byte[] { data[0], data[1], data[2], data[3] }, 0)),
            lut[1].LookUp(BitConverter.ToUInt32(new byte[] { data[4], data[5], data[6], data[7] }, 0)),
            lut[2].LookUp(BitConverter.ToUInt32(new byte[] { data[8], data[9], data[10], data[11] }, 0)),
            lut[3].LookUp(BitConverter.ToUInt32(new byte[] { data[12], data[13], data[14], data[15] }, 0)),
            lut[4].LookUp(BitConverter.ToUInt32(new byte[] { data[4], data[5], data[6], data[7] }, 0)),
            lut[5].LookUp(BitConverter.ToUInt32(new byte[] { data[8], data[9], data[10], data[11] }, 0)),
            lut[6].LookUp(BitConverter.ToUInt32(new byte[] { data[12], data[13], data[14], data[15] }, 0)),
            lut[7].LookUp(BitConverter.ToUInt32(new byte[] { data[0], data[1], data[2], data[3] }, 0)),
            lut[8].LookUp(BitConverter.ToUInt32(new byte[] { data[8], data[9], data[10], data[11] }, 0)),
            lut[9].LookUp(BitConverter.ToUInt32(new byte[] { data[12], data[13], data[14], data[15] }, 0)),
            lut[10].LookUp(BitConverter.ToUInt32(new byte[] { data[0], data[1], data[2], data[3] }, 0)),
            lut[11].LookUp(BitConverter.ToUInt32(new byte[] { data[4], data[5], data[6], data[7] }, 0)),
            lut[12].LookUp(BitConverter.ToUInt32(new byte[] { data[12], data[13], data[14], data[15] }, 0)),
            lut[13].LookUp(BitConverter.ToUInt32(new byte[] { data[0], data[1], data[2], data[3] }, 0)),
            lut[14].LookUp(BitConverter.ToUInt32(new byte[] { data[4], data[5], data[6], data[7] }, 0)),
            lut[15].LookUp(BitConverter.ToUInt32(new byte[] { data[8], data[9], data[10], data[11] }, 0))
        };
    }    
#endregion
}
