#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    public class VariableSymbol : ISymbol
    {
        public string Name { get; }
        public Type Type { get; set; }
        public VariableKind Kind { get; set; }
        public int Location { get; set; } = -1;

        public bool IsAllocated => Location != -1;

        public VariableSymbol(string name, Type type, VariableKind kind)
            => (Name, Type, Kind) = (name, type, kind);
    }

    public enum VariableKind
    {
        Static,
        Local,
        LocalArgument,
    }
}
