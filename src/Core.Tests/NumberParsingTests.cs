namespace ScTools.Tests;

using System;

using Xunit;
using static Xunit.Assert;

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
