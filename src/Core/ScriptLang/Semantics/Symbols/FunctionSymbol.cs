#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Collections.Generic;

    public abstract class FunctionSymbol : ISymbol
    {
        public string Name { get; }
        public SourceRange Source { get; }
        public abstract FunctionType Type { get; }

        public FunctionSymbol(string name, SourceRange source)
            => (Name, Source) = (name, source);
    }
}
