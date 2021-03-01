#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    public class VariableSymbol : ISymbol
    {
        public string Name { get; }
        public SourceRange Source { get; }
        public Type Type { get; set; }
        public VariableKind Kind { get; }
        public Binding.BoundExpression? Initializer { get; set; } = null;

        public bool IsLocal => Kind == VariableKind.Local || Kind == VariableKind.LocalArgument;
        public bool IsStatic => Kind == VariableKind.Static;
        public bool IsGlobal => Kind == VariableKind.Global;
        public bool IsConstant => Kind == VariableKind.Constant;

        public VariableSymbol(string name, SourceRange source, Type type, VariableKind kind)
            => (Name, Source, Type, Kind) = (name, source, type, kind);
    }

    public enum VariableKind
    {
        Constant,
        Global,
        Static,
        Local,
        LocalArgument,
    }
}
