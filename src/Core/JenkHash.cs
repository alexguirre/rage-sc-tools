namespace ScTools;

public class JenkHash
{
    public static uint Hash(string str, uint seed = 0) => Hash(str.AsSpan(), seed);
    public static uint Hash(ReadOnlyMemory<char> str, uint seed = 0) => Hash(str.Span, seed);
    public static uint Hash(ReadOnlySpan<char> str, uint seed = 0)
    {
        var max = System.Text.Encoding.UTF8.GetMaxByteCount(str.Length);
        var buff = max > 256 ? new byte[max] : stackalloc byte[max];
        var length = System.Text.Encoding.UTF8.GetBytes(str, buff);
        return Hash(buff[..length], seed);
    }
    public static uint Hash(ReadOnlyMemory<byte> data, uint seed = 0) => Hash(data.Span, seed);
    public static uint Hash(ReadOnlySpan<byte> data, uint seed = 0)
    {
        uint h = seed;
        foreach (var b in data)
        {
            h += b;
            h += (h << 10);
            h ^= (h >> 6);
        }
        h += (h << 3);
        h ^= (h >> 11);
        h += (h << 15);

        return h;
    }


    public static uint LowercaseHash(string str, uint seed = 0) => LowercaseHash(str.AsSpan(), seed);
    public static uint LowercaseHash(ReadOnlyMemory<char> str, uint seed = 0) => LowercaseHash(str.Span, seed);
    public static uint LowercaseHash(ReadOnlySpan<char> str, uint seed = 0)
    {
        var max = System.Text.Encoding.UTF8.GetMaxByteCount(str.Length);
        var buff = max > 256 ? new byte[max] : stackalloc byte[max];
        var length = System.Text.Encoding.UTF8.GetBytes(str, buff);
        return LowercaseHash(buff[..length], seed);
    }
    public static uint LowercaseHash(ReadOnlyMemory<byte> data, uint seed = 0) => LowercaseHash(data.Span, seed);
    public static uint LowercaseHash(ReadOnlySpan<byte> data, uint seed = 0)
    {
        uint h = seed;
        foreach (var b in data)
        {
            var c = b;
            if (c <= (25 + 65))
            {
                c += 32;
            }
            
            h += c;
            h += (h << 10);
            h ^= (h >> 6);
        }
        h += (h << 3);
        h ^= (h >> 11);
        h += (h << 15);

        return h;
    }
}
