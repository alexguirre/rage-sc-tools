namespace ScTools.ScriptAssembly.Disassembly
{
    using ScTools.ScriptAssembly.Types;

    public class Static
    {
        public string Name { get; set; }
        public uint Offset { get; set; }
        public TypeBase Type { get; set; }
        public ulong InitialValue { get; set; }
    }

    public class StaticArgument : Static
    {
    }
}
