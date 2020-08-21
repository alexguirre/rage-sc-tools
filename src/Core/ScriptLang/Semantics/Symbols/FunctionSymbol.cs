#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    public class FunctionSymbol : ISymbol
    {
        public string Name { get; }
        public FunctionType Type { get; set; }
        public int LocalArgsSize { get; set; } = -1;
        public int LocalsSize { get; set; } = -1;

        public bool AreLocalsAllocated => LocalArgsSize != -1 && LocalsSize != -1;

        public FunctionSymbol(string name, FunctionType type)
            => (Name, Type) = (name, type);
    }
}
