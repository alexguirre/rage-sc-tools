namespace ScTools
{
    using System;
    using System.Text;

    public static class SpanExtensions
    {
        public static uint ToLowercaseHash(this ReadOnlySpan<char> s)
        {
            uint h = 0;
            for (int i = 0; i < s.Length; i++)
            {
                h += (byte)char.ToLowerInvariant(s[i]);
                h += (h << 10);
                h ^= (h >> 6);
            }
            h += (h << 3);
            h ^= (h >> 11);
            h += (h << 15);

            return h;
        }
        public static uint ToLowercaseHash(this ReadOnlyMemory<char> str) => str.Span.ToLowercaseHash();
        public static uint ToLowercaseHash(this string s) => s.AsSpan().ToLowercaseHash();

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
        public static uint ToHash(this ReadOnlyMemory<char> str) => str.Span.ToHash();
        public static uint ToHash(this string str) => str.AsSpan().ToHash();

        private static bool NeedsEscaping(char c) => c is '\n' or '\r' or '\t' or '\0' or '\\' or '\"' or '\'' or '`';
        private static string EscapeSequence(char c) => c switch
        {
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\0' => "\\0",
            '\\' => "\\\\",
            '\"' => "\\\"",
            '\'' => "\\\'",
            '`' => "\\`",
            _ => throw new ArgumentException($"Char '{c}' does not need escaping", nameof(c)),
        };

        private static bool NeedsUnescaping(ReadOnlySpan<char> s) => s.Length >= 2 && s[0] == '\\' && (s[1] is 'n' or 'r' or 't' or '0' or '\\' or '\"' or '\'' or '`');
        private static char UnescapeSequence(ReadOnlySpan<char> s) => s switch
        {
            _ when s[1] == 'n' => '\n',
            _ when s[1] == 'r' => '\r',
            _ when s[1] == 't' => '\t',
            _ when s[1] == '0' => '\0',
            _ when s[1] == '\\' => '\\',
            _ when s[1] == '\"' => '\"',
            _ when s[1] == '\'' => '\'',
            _ when s[1] == '`' => '`',
            _ => throw new ArgumentException("Not a valid escape sequence", nameof(s)),
        };

        public static string Escape(this ReadOnlySpan<char> str)
        {
            StringBuilder? sb = null;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (sb == null)
                {
                    if (NeedsEscaping(c))
                    {
                        sb = new StringBuilder(str.Length + 16); // start with capacity of the string length + some space for the escape sequences
                        sb.Append(str[0..i]);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (NeedsEscaping(c))
                {
                    sb.Append(EscapeSequence(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb?.ToString() ?? str.ToString();
        }
        public static string Escape(this ReadOnlyMemory<char> str) => str.Span.Escape();
        public static string Escape(this string str) => str.AsSpan().Escape();

        public static string Unescape(this ReadOnlySpan<char> str)
        {
            StringBuilder? sb = null;

            for (int i = 0; i < str.Length; i++)
            {
                var slice = str[i..];

                if (sb == null)
                {
                    if (NeedsUnescaping(slice))
                    {
                        sb = new StringBuilder(str.Length);
                        sb.Append(str[0..i]);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (NeedsUnescaping(slice))
                {
                    sb.Append(UnescapeSequence(slice));
                    i++; // skip the backslash
                }
                else
                {
                    sb.Append(slice[0]);
                }
            }

            return sb?.ToString() ?? str.ToString();
        }
        public static string Unescape(this ReadOnlyMemory<char> str) => str.Span.Unescape();
        public static string Unescape(this string str) => str.AsSpan().Unescape();

        public static int ParseAsInt(this ReadOnlySpan<char> str)
            => str.StartsWith("0x") ?
                int.Parse(str[2..], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture) :
                int.Parse(str, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
        public static int ParseAsInt(this ReadOnlyMemory<char> str) => str.Span.ParseAsInt();
        public static int ParseAsInt(this string str) => str.AsSpan().ParseAsInt();

        public static float ParseAsFloat(this ReadOnlySpan<char> str)
            => float.Parse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        public static float ParseAsFloat(this ReadOnlyMemory<char> str) => str.Span.ParseAsFloat();
        public static float ParseAsFloat(this string str) => str.AsSpan().ParseAsFloat();

        public static ulong ParseAsUInt64(this ReadOnlySpan<char> str)
            => str.StartsWith("0x") ?
                ulong.Parse(str[2..], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture) :
                ulong.Parse(str, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
        public static ulong ParseAsUInt64(this ReadOnlyMemory<char> str) => str.Span.ParseAsUInt64();
        public static ulong ParseAsUInt64(this string str) => str.AsSpan().ParseAsUInt64();

        public static long ParseAsInt64(this ReadOnlySpan<char> str)
            => str.StartsWith("0x") ?
                long.Parse(str[2..], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture) :
                long.Parse(str, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
        public static long ParseAsInt64(this ReadOnlyMemory<char> str) => str.Span.ParseAsInt64();
        public static long ParseAsInt64(this string str) => str.AsSpan().ParseAsInt64();

        public static bool ParseAsBool(this ReadOnlySpan<char> str)
            => bool.Parse(str);
        public static bool ParseAsBool(this ReadOnlyMemory<char> str) => str.Span.ParseAsBool();
        public static bool ParseAsBool(this string str) => str.AsSpan().ParseAsBool();
    }
}
