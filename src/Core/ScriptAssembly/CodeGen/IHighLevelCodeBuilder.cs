namespace ScTools.ScriptAssembly.CodeGen
{
    using System;
    using ScTools.ScriptAssembly.Definitions;

    /// <summary>
    /// Defines the interface used for assembling <see cref="HighLevelInstruction"/>s.
    /// </summary>
    public interface IHighLevelCodeBuilder
    {
        public CodeGenOptions Options { get; }
        public NativeDB NativeDB { get; }
        public Registry Symbols { get; }

        /// <summary>
        /// Assembles a low-level instruction with the specified operands.
        /// </summary>
        /// <param name="opcode">The instruction to assemble.</param>
        /// <param name="operands">The operands of the instruction.</param>
        public void Emit(Opcode opcode, ReadOnlySpan<Operand> operands);

        public uint AddOrGetString(string str);
        public ushort AddOrGetNative(ulong hash);

        public uint GetStaticOffset(string name);
        public uint GetLocalOffset(string name);
    }
}
