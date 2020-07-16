namespace ScTools.ScriptAssembly.Disassembly
{
    using ScTools.ScriptAssembly.Types;

    public class Local
    {
        public string Name { get; set; }
        public uint Offset { get; set; }
        public TypeBase Type { get; set; }
    }

    public class Argument : Local
    {
    }
}
