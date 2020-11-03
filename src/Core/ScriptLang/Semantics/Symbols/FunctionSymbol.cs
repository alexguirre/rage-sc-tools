#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    public class FunctionSymbol : ISymbol
    {
        public const string MainName = "MAIN";

        public string Name { get; }
        public SourceRange Source { get; }
        public FunctionType Type { get; set; }
        public int LocalArgsSize { get; set; } = -1;
        public int LocalsSize { get; set; } = -1;
        public bool IsNative { get; set; }

        public bool AreLocalsAllocated => LocalArgsSize != -1 && LocalsSize != -1;
        public bool IsProcedure => Type.ReturnType == null;
        public bool IsMain => IsProcedure && Type.Parameters.Count == 0 && Name == MainName;

        public FunctionSymbol(string name, SourceRange source, FunctionType type, bool isNative)
            => (Name, Source, Type, IsNative) = (name, source, type, isNative);
    }
}
