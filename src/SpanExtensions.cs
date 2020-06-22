namespace ScTools
{
    using System;

    internal static class SpanExtensions
    {
        public static uint ToHash(this ReadOnlySpan<char> s)
        {
            uint h = 0;
            for (int i = 0; i < s.Length; i++)
            {
                h += (byte)s[i];
                h += (h << 10);
                h ^= (h >> 6);
            }
            h += (h << 3);
            h ^= (h >> 11);
            h += (h << 15);

            return h;
        }

        public static uint ToHash(this string s) => s.AsSpan().ToHash(); 
    }
}
