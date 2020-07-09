namespace ScTools.ScriptAssembly.Disassembly
{
    /// <summary>
    /// Defines the interface used for decoding <see cref="Instruction"/>s.
    /// </summary>
    public interface IInstructionDecoder
    {
        /// <summary>
        /// Gets the IP of the current instruction.
        /// </summary>
        public uint IP { get; }

        /// <summary>
        /// Gets the <see cref="byte"/> at <see cref="IP"/> + <paramref name="offset"/>.
        /// </summary>
        public byte Get(uint offset);
        public T Get<T>(uint offset) where T : unmanaged;

        public void U8(byte v);
        public void U16(ushort v);
        public void U24(uint v);
        public void U32(uint v);
        public void S16(short v);
        public void F32(float v);
        public void LabelTarget(uint ip);
        public void FunctionTarget(uint ip);
        public void SwitchCase(uint value, uint ip);
    }
}
