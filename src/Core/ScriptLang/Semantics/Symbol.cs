using ScTools.ScriptLang.Types;

namespace ScTools.ScriptLang.Semantics;

public interface ISymbol
{
    public string Name { get; }
}

public interface ITypeSymbol : ISymbol
{
    public TypeInfo DeclaredType { get; }
}

public sealed class BuiltInTypeSymbol : ITypeSymbol
{
    public string Name { get; }
    public TypeInfo DeclaredType { get; }
    
    public BuiltInTypeSymbol(string name, TypeInfo type)
    {
        Name = name;
        DeclaredType = type;
    }
}
