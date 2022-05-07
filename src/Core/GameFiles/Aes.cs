namespace ScTools.GameFiles;

using System.Security.Cryptography;

using CryptoAes = System.Security.Cryptography.Aes;

public static class Aes
{
    public static void Decrypt(byte[] data, byte[] key) => Transform(data, key, encrypt: false);
    public static void Encrypt(byte[] data, byte[] key) => Transform(data, key, encrypt: true);

    private static void Transform(byte[] data, byte[] key, bool encrypt)
    {
        var aes = CryptoAes.Create();
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
}
