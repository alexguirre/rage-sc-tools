namespace ScTools.ScriptAssembly.Targets;

using System;

/// <summary>
/// Provides methods to inspect the bytecode of a script from a specific game target.
/// </summary>
/// <typeparam name="TOpcode">The opcodes enumeration type.</typeparam>
public interface IOpcodeTraits<TOpcode> where TOpcode : struct, Enum
{
    public static abstract int NumberOfOpcodes { get; }

    /// <returns>
    /// The byte size of a instruction with this <paramref name="opcode"/>; or, <c>0</c> if the size is variable.
    /// </returns>
    public static abstract int ConstantByteSize(TOpcode opcode);

    /// <returns>
    /// The length in bytes of the instruction at the beginning of <paramref name="bytecode"/>.
    /// </returns>
    public static abstract int ByteSize(ReadOnlySpan<byte> bytecode);

    /// <summary>
    /// Returns the span of bytes that make up the instruction at <paramref name="address"/> in <paramref name="code"/>.
    /// </summary>
    /// <param name="code">The bytecode buffer.</param>
    /// <param name="address">Index of the first byte of the instruction.</param>
    /// <returns>The slice of <paramref name="code"/> that contains the instruction.</returns>
    public static abstract Span<byte> GetInstructionSpan(Span<byte> code, int address);

    /// <inheritdoc cref="GetInstructionSpan(System.Span{byte},int)"/>
    public static abstract ReadOnlySpan<byte> GetInstructionSpan(ReadOnlySpan<byte> code, int address);
    

    protected static void ThrowIfOpcodeDoesNotMatch(TOpcode opcode, ReadOnlySpan<byte> bytecode)
    {
        if (opcode.AsInteger<TOpcode, byte>() != bytecode[0])
        {
            throw new ArgumentException($"The opcode {opcode} does not match the bytecode {bytecode[0]:X2}.", nameof(bytecode));
        }
    }
}
