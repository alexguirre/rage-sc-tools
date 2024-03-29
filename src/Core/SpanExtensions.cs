﻿namespace ScTools;

using System;
using System.Text;
using System.Globalization;

internal static class SpanExtensions
{
    public static uint ToLowercaseHash(this ReadOnlySpan<char> str, uint seed = 0) => JenkHash.LowercaseHash(str, seed);
    public static uint ToLowercaseHash(this ReadOnlyMemory<char> str, uint seed = 0) => JenkHash.LowercaseHash(str, seed);
    public static uint ToLowercaseHash(this string str, uint seed = 0) => JenkHash.LowercaseHash(str, seed);

    public static uint ToHash(this ReadOnlySpan<byte> data, uint seed = 0) => JenkHash.Hash(data, seed); 
    public static uint ToHash(this ReadOnlyMemory<byte> data, uint seed = 0) => JenkHash.Hash(data, seed);
    public static uint ToHash(this ReadOnlySpan<char> str, uint seed = 0) => JenkHash.Hash(str, seed); 
    public static uint ToHash(this ReadOnlyMemory<char> str, uint seed = 0) => JenkHash.Hash(str, seed);
    public static uint ToHash(this string str, uint seed = 0) => JenkHash.Hash(str, seed);

    private static bool NeedsEscaping(char c) => c is '\n' or '\r' or '\t' or '\0' or '\\' or '\"' or '\'';
    private static string EscapeSequence(char c) => c switch
    {
        '\n' => "\\n",
        '\r' => "\\r",
        '\t' => "\\t",
        '\0' => "\\0",
        '\\' => "\\\\",
        '\"' => "\\\"",
        '\'' => "\\\'",
        _ => throw new ArgumentException($"Char '{c}' does not need escaping", nameof(c)),
    };

    private static bool NeedsUnescaping(ReadOnlySpan<char> s) => s.Length >= 2 && s[0] == '\\' && (s[1] is 'n' or 'r' or 't' or '0' or '\\' or '\"' or '\'');
    private static char UnescapeSequence(ReadOnlySpan<char> s) => s switch
    {
        _ when s[1] == 'n' => '\n',
        _ when s[1] == 'r' => '\r',
        _ when s[1] == 't' => '\t',
        _ when s[1] == '0' => '\0',
        _ when s[1] == '\\' => '\\',
        _ when s[1] == '\"' => '\"',
        _ when s[1] == '\'' => '\'',
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
        => TryParseHexInt(str, out var hexValue) ?
            hexValue :
            int.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
    public static int ParseAsInt(this ReadOnlyMemory<char> str) => str.Span.ParseAsInt();
    public static int ParseAsInt(this string str) => str.AsSpan().ParseAsInt();

    public static uint ParseAsUInt(this ReadOnlySpan<char> str)
        => TryParseHexUInt(str, out var hexValue) ?
            hexValue :
            uint.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
    public static uint ParseAsUInt(this ReadOnlyMemory<char> str) => str.Span.ParseAsUInt();
    public static uint ParseAsUInt(this string str) => str.AsSpan().ParseAsUInt();

    public static float ParseAsFloat(this ReadOnlySpan<char> str)
        => float.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
    public static float ParseAsFloat(this ReadOnlyMemory<char> str) => str.Span.ParseAsFloat();
    public static float ParseAsFloat(this string str) => str.AsSpan().ParseAsFloat();

    public static ulong ParseAsUInt64(this ReadOnlySpan<char> str)
        => TryParseHexUInt64(str, out var hexValue) ?
            hexValue :
            ulong.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
    public static ulong ParseAsUInt64(this ReadOnlyMemory<char> str) => str.Span.ParseAsUInt64();
    public static ulong ParseAsUInt64(this string str) => str.AsSpan().ParseAsUInt64();

    public static long ParseAsInt64(this ReadOnlySpan<char> str)
        => TryParseHexInt64(str, out var hexValue) ?
            hexValue :
            long.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
    public static long ParseAsInt64(this ReadOnlyMemory<char> str) => str.Span.ParseAsInt64();
    public static long ParseAsInt64(this string str) => str.AsSpan().ParseAsInt64();

    public static bool ParseAsBool(this ReadOnlySpan<char> str)
        => bool.Parse(str);
    public static bool ParseAsBool(this ReadOnlyMemory<char> str) => str.Span.ParseAsBool();
    public static bool ParseAsBool(this string str) => str.AsSpan().ParseAsBool();


    private static bool TryParseHexUInt(ReadOnlySpan<char> str, out uint value)
    {
        var s = TryParseHexInt(str, out var valueSigned);
        value = unchecked((uint)valueSigned);
        return s;
    }
    private static bool TryParseHexInt(ReadOnlySpan<char> str, out int value)
    {
        if (str.Length < 3)
        {
            value = 0;
            return false;
        }

        int sign = 1;
        if (str[0] == '-')
        {
            sign = -1;
            str = str[1..];
        }
        else if (str[0] == '+')
        {
            str = str[1..];
        }

        if (str[0] != '0' || (str[1] != 'x' && str[1] != 'X'))
        {
            value = 0;
            return false;
        }

        if (uint.TryParse(str[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var valueUnsigned))
        {
            value = unchecked((int)valueUnsigned) * sign;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryParseHexUInt64(ReadOnlySpan<char> str, out ulong value)
    {
        var s = TryParseHexInt64(str, out var valueSigned);
        value = unchecked((ulong)valueSigned);
        return s;
    }
    private static bool TryParseHexInt64(ReadOnlySpan<char> str, out long value)
    {
        if (str.Length < 3)
        {
            value = 0;
            return false;
        }

        int sign = 1;
        if (str[0] == '-')
        {
            sign = -1;
            str = str[1..];
        }
        else if (str[0] == '+')
        {
            str = str[1..];
        }

        if (str[0] != '0' || (str[1] != 'x' && str[1] != 'X'))
        {
            value = 0;
            return false;
        }

        if (ulong.TryParse(str[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var valueUnsigned))
        {
            value = unchecked((long)valueUnsigned) * sign;
            return true;
        }

        value = 0;
        return false;
    }

    public static Span<T> AsSpan<T>(this T[,] array2D)
    {
        var len = array2D.GetLength(0) * array2D.GetLength(1);
        return System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref array2D[0, 0], len);
    }
}
