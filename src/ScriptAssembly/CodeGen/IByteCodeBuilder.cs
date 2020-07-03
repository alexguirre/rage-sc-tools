namespace ScTools.ScriptAssembly.CodeGen
{
    /// <summary>
    /// Defines the interface used for assembling <see cref="Instruction"/>s.
    /// </summary>
    public interface IByteCodeBuilder
    {
        /// <summary>
        /// The label associated to the current instruction.
        /// </summary>
        public string Label { get; }
        public CodeGenOptions Options { get; }

        public void U8(byte v);
        public void U16(ushort v);
        public void U24(uint v);
        public void U32(uint v);
        public void S16(short v);
        public void F32(float v);
        public void LabelTarget(string label);
        public void FunctionTarget(string function);
    }
}
