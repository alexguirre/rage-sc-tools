namespace ScTools.ScriptAssembly;

using System;

public interface IOpcodeTraits<TOpcode> where TOpcode : struct, Enum
{
    public static abstract int NumberOfOpcodes { get; }

    /// <returns>
    /// The byte size of a instruction with this <paramref name="opcode"/>; or, <c>0</c> if the size is variable.
    /// </returns>
    public static abstract int ConstantByteSize(TOpcode opcode);
}

public interface IOpcodeTraitsGTA5<TOpcode> : IOpcodeTraits<TOpcode> where TOpcode : struct, Enum
{
    public static abstract TOpcode ENTER { get; }
    public static abstract TOpcode SWITCH { get; }
    public static abstract string[] DumpOpcodeFormats { get; }
}
