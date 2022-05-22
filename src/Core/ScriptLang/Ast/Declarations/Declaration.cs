namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Represents a symbol declaration.
/// </summary>
public interface IDeclaration : INode, ISymbol
{
    Token NameToken { get; }
}

public record struct TypeDeclarationSemantics(TypeInfo? DeclaredType);

/// <summary>
/// Represents a declaration of a type.
/// </summary>
public interface ITypeDeclaration : IDeclaration, ISemanticNode<TypeDeclarationSemantics>, ITypeSymbol
{
}

public record struct ValueDeclarationSemantics(TypeInfo? ValueType, ConstantValue? ConstantValue);

/// <summary>
/// Represents a declaration of a variable, a procedure, a function or an enum member.
/// </summary>
public interface IValueDeclaration : IDeclaration, ISemanticNode<ValueDeclarationSemantics>
{
}

public sealed partial class TypeName : BaseNode
{
    public Token NameToken => Tokens[0];
    public string Name => NameToken.Lexeme.ToString();

    public TypeName(Token nameIdentifier)
        : base(OfTokens(nameIdentifier), OfChildren())
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(TypeName)} {{ {nameof(Name)} = {Name} }}";
}

public abstract class BaseTypeDeclaration : BaseNode, ITypeDeclaration
{
    public abstract Token NameToken { get; }
    public string Name => NameToken.Lexeme.ToString();
    public TypeDeclarationSemantics Semantics { get; set; }
    public TypeInfo DeclaredType => Semantics.DeclaredType ?? throw new System.InvalidOperationException("Cannot access declared type before semantic analysis.");

    public BaseTypeDeclaration(IEnumerable<Token> tokens, IEnumerable<INode> children) : base(tokens, children)
    {
    }
}

public abstract class BaseValueDeclaration : BaseNode, IValueDeclaration
{
    public abstract Token NameToken { get; }
    public string Name => NameToken.Lexeme.ToString();
    public ValueDeclarationSemantics Semantics { get; set; }

    public BaseValueDeclaration(IEnumerable<Token> tokens, IEnumerable<INode> children) : base(tokens, children)
    {
    }
}
