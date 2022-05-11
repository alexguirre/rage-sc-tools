namespace ScTools.GameFiles;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Represents script values in 32-bit platforms.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct ScriptValue32
{
    [FieldOffset(0)] public float AsFloat;
    [FieldOffset(0)] public int AsInt32;
    [FieldOffset(0)] public uint AsUInt32;
}

/// <summary>
/// Represents script values in 64-bit platforms.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct ScriptValue64
{
    [FieldOffset(0)] public float AsFloat;
    [FieldOffset(0)] public int AsInt32;
    [FieldOffset(0)] public uint AsUInt32;
    [FieldOffset(0)] public long AsInt64;
    [FieldOffset(0)] public ulong AsUInt64;
}

public static class ScriptValue
{
    private static unsafe T[] FromBytes<T>(Span<byte> buffer) where T : unmanaged
    {
        if (buffer.IsEmpty) return Array.Empty<T>();

        var result = new T[buffer.Length / sizeof(T)];
        buffer.CopyTo(MemoryMarshal.AsBytes(result.AsSpan()));
        return result;
    }

    private static unsafe byte[] ToBytes<T>(Span<T> values) where T : unmanaged
    {
        if (values.IsEmpty) return Array.Empty<byte>();

        var result = new byte[values.Length * sizeof(T)];
        MemoryMarshal.AsBytes(values).CopyTo(result);
        return result;
    }

    public static ScriptValue32[] FromBytes32(Span<byte> buffer)
        => FromBytes<ScriptValue32>(buffer);
    public static byte[] ToBytes32(Span<ScriptValue32> values)
        => ToBytes(values);

    public static ScriptValue64[] FromBytes64(Span<byte> buffer)
        => FromBytes<ScriptValue64>(buffer);
    public static byte[] ToBytes64(Span<ScriptValue64> values)
        => ToBytes(values);
}