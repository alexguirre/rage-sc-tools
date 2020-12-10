#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    public class TypeSymbol : ISymbol
    {
        public string Name { get; }
        public SourceRange Source { get; }
        public Type Type { get; }

        public TypeSymbol(string name, SourceRange source, Type type)
        {
            if (type is UnresolvedType)
            {
                throw new System.ArgumentException($"Type is {nameof(UnresolvedType)}", nameof(type));
            }

            Name = name;
            Source = source;
            Type = type;
        }
    }
}
