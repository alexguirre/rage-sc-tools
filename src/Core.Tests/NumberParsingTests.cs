namespace ScTools.Tests;

using System;

using Xunit;
using static Xunit.Assert;

//public static int ParseAsInt(this ReadOnlySpan<char> str)
//    => TryParseHexInt(str, out var hexValue) ?
//        hexValue :
//        int.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
//public static int ParseAsInt(this ReadOnlyMemory<char> str) => str.Span.ParseAsInt();
//public static int ParseAsInt(this string str) => str.AsSpan().ParseAsInt();

//public static uint ParseAsUInt(this ReadOnlySpan<char> str)
//    => TryParseHexUInt(str, out var hexValue) ?
//        hexValue :
//        uint.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
//public static uint ParseAsUInt(this ReadOnlyMemory<char> str) => str.Span.ParseAsUInt();
//public static uint ParseAsUInt(this string str) => str.AsSpan().ParseAsUInt();

//public static float ParseAsFloat(this ReadOnlySpan<char> str)
//    => float.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
//public static float ParseAsFloat(this ReadOnlyMemory<char> str) => str.Span.ParseAsFloat();
//public static float ParseAsFloat(this string str) => str.AsSpan().ParseAsFloat();

//public static ulong ParseAsUInt64(this ReadOnlySpan<char> str)
//    => TryParseHexUInt64(str, out var hexValue) ?
//        hexValue :
//        ulong.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
//public static ulong ParseAsUInt64(this ReadOnlyMemory<char> str) => str.Span.ParseAsUInt64();
//public static ulong ParseAsUInt64(this string str) => str.AsSpan().ParseAsUInt64();

//public static long ParseAsInt64(this ReadOnlySpan<char> str)
//    => TryParseHexInt64(str, out var hexValue) ?
//        hexValue :
//        long.Parse(str, NumberStyles.Number, CultureInfo.InvariantCulture);
//public static long ParseAsInt64(this ReadOnlyMemory<char> str) => str.Span.ParseAsInt64();
//public static long ParseAsInt64(this string str) => str.AsSpan().ParseAsInt64();

public class NumberParsingTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("-1", -1)]
    [InlineData("+1", 1)]
    [InlineData("123", 123)]
    [InlineData("-123", -123)]
    [InlineData("+123", +123)]
    [InlineData("0x123", 0x123)]
    [InlineData("0X123", 0x123)]
    [InlineData("-0x123", -0x123)]
    [InlineData("+0x123", +0x123)]
    [InlineData("0xFFFFFFFF", -1)]
    [InlineData("-0xFFFFFFFF", 1)]
    [InlineData("+0xFFFFFFFF", -1)]
    public void ParseAsInt(string str, int expected)
    {
        Equal(expected, str.ParseAsInt());
        Equal(expected, str.AsSpan().ParseAsInt());
        Equal(expected, str.AsMemory().ParseAsInt());
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    //[InlineData("-1", unchecked((uint)-1))] // TODO: currently this throws
    [InlineData("+1", 1)]
    [InlineData("123", 123)]
    //[InlineData("-123", unchecked((uint)-123))] // TODO: currently this throws
    [InlineData("+123", +123)]
    [InlineData("0x123", 0x123)]
    [InlineData("0X123", 0x123)]
    [InlineData("-0x123", unchecked((uint)-0x123))]
    [InlineData("+0x123", +0x123)]
    [InlineData("0xFFFFFFFF", 0xFFFFFFFF)]
    [InlineData("-0xFFFFFFFF", 1)]
    [InlineData("+0xFFFFFFFF", +0xFFFFFFFF)]
    public void ParseAsUInt(string str, uint expected)
    {
        Equal(expected, str.ParseAsUInt());
        Equal(expected, str.AsSpan().ParseAsUInt());
        Equal(expected, str.AsMemory().ParseAsUInt());
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("-1", -1)]
    [InlineData("+1", 1)]
    [InlineData("123", 123)]
    [InlineData("-123", -123)]
    [InlineData("+123", +123)]
    [InlineData("0x123", 0x123)]
    [InlineData("0X123", 0x123)]
    [InlineData("-0x123", -0x123)]
    [InlineData("+0x123", +0x123)]
    [InlineData("0xFFFFFFFFFFFFFFFF", -1)]
    [InlineData("-0xFFFFFFFFFFFFFFFF", 1)]
    [InlineData("+0xFFFFFFFFFFFFFFFF", -1)]
    public void ParseAsInt64(string str, long expected)
    {
        Equal(expected, str.ParseAsInt64());
        Equal(expected, str.AsSpan().ParseAsInt64());
        Equal(expected, str.AsMemory().ParseAsInt64());
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    //[InlineData("-1", unchecked((ulong)-1))] // TODO: currently this throws
    [InlineData("+1", 1)]
    [InlineData("123", 123)]
    //[InlineData("-123", unchecked((ulong)-123))] // TODO: currently this throws
    [InlineData("+123", +123)]
    [InlineData("0x123", 0x123)]
    [InlineData("0X123", 0x123)]
    [InlineData("-0x123", unchecked((ulong)-0x123))]
    [InlineData("+0x123", +0x123)]
    [InlineData("0xFFFFFFFFFFFFFFFF", 0xFFFFFFFFFFFFFFFF)]
    [InlineData("-0xFFFFFFFFFFFFFFFF", 1)]
    [InlineData("+0xFFFFFFFFFFFFFFFF", +0xFFFFFFFFFFFFFFFF)]
    public void ParseAsUInt64(string str, ulong expected)
    {
        Equal(expected, str.ParseAsUInt64());
        Equal(expected, str.AsSpan().ParseAsUInt64());
        Equal(expected, str.AsMemory().ParseAsUInt64());
    }
}
