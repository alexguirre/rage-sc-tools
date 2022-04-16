namespace ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Represents a symbol declaration.
/// </summary>
public interface IDeclaration : INode
{
    string Name { get; }
}

/// <summary>
/// Represents a declaration of a type.
/// </summary>
public interface ITypeDeclaration : IDeclaration
{
}

/// <summary>
/// Represents a declaration of a variable, a procedure, a function or an enum member.
/// </summary>
public interface IValueDeclaration : IDeclaration
{
}

public sealed class TypeName : BaseNode
{
    public string Name => Tokens[0].Lexeme.ToString();

    public TypeName(Token nameIdentifier)
        : base(OfTokens(nameIdentifier), OfChildren())
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(TypeName)} {{ {nameof(Name)} = {Name} }}";
}

public abstract class BaseTypeDeclaration : BaseNode, ITypeDeclaration
{
    public abstract string Name { get; }

    public BaseTypeDeclaration(IEnumerable<Token> tokens, IEnumerable<INode> children) : base(tokens, children)
    {
    }
}

public abstract class BaseValueDeclaration : BaseNode, IValueDeclaration
{
    public abstract string Name { get; }

    public BaseValueDeclaration(IEnumerable<Token> tokens, IEnumerable<INode> children) : base(tokens, children)
    {
    }
}
