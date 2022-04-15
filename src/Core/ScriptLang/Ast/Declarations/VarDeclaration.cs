namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Ast.Types;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public enum VarKind
{
    /// <summary>
    /// A static variable with the CONST modifier. Where used, it is replaced by its value at compile-time. Requires a non-null <see cref="VarDeclaration.Initializer"/>.
    /// </summary>
    Constant,
    /// <summary>
    /// A variable defined in a GLOBAL block, shared between script threads.
    /// </summary>
    Global,
    /// <summary>
    /// A variable defined outside any functions/procedures.
    /// </summary>
    Static,
    /// <summary>
    /// A static variable that represents a parameter of a SCRIPT declaration. Initialized by other script thread when starting
    /// this script with `START_NEW_SCRIPT_WITH_ARGS` or `START_NEW_SCRIPT_WITH_NAME_HASH_AND_ARGS`.
    /// </summary>
    ScriptParameter,
    /// <summary>
    /// A variable defined inside a function/procedure.
    /// </summary>
    Local,
    /// <summary>
    /// A local variable that represents a parameter of a function/procedure. <see cref="VarDeclaration.Initializer"/> must be null.
    /// </summary>
    Parameter,
}

public sealed class VarDeclaration : BaseValueDeclaration, IStatement
{
    public Label? Label { get; }
    public VarKind Kind { get; set; }
    /// <summary>
    /// Gets or sets whether this variable is a reference. Only valid with kind <see cref="VarKind.Parameter"/>.
    /// </summary>
    public bool IsReference { get; set; }
    public IExpression? Initializer { get; set; }
    public int Address { get; set; }

    public VarDeclaration(Token nameIdentifier, IType type, VarKind kind, bool isReference, Label? label = null) : base(nameIdentifier.Lexeme.ToString(), type, nameIdentifier) // TODO: pass children to BaseValueDeclaration
        => (Kind, IsReference, Label) = (kind, isReference, label);
    public VarDeclaration(SourceRange source, string name, IType type, VarKind kind, bool isReference) : base(source, name, type)
        => (Kind, IsReference) = (kind, isReference);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    internal VarDeclaration WithLabel(Label? label)
        => new(Tokens[0], Type, Kind, IsReference, label);
}

public sealed class VarDeclaration_New : BaseValueDeclaration_New, IStatement
{
    public override string Name => Declarator.Name;
    public Label? Label { get; }
    public TypeName Type => (TypeName)Children[0];
    public IVarDeclarator Declarator => (IVarDeclarator)Children[1];
    public VarKind Kind { get; }
    public IExpression? Initializer { get; }
    // TODO: move to semantic information
    //public int Address { get; set; }

    public VarDeclaration_New(TypeName type, IVarDeclarator declarator, VarKind kind, IExpression? initializer = null, Label? label = null)
        : base(OfTokens(), OfChildren(type, declarator).AppendIfNotNull(initializer).AppendIfNotNull(label))
    {
        Kind = kind;
        Initializer = initializer;
        Label = label;
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    internal VarDeclaration_New WithLabel(Label? label)
        => new(Type, Declarator, Kind, Initializer, label);

    public override string DebuggerDisplay =>
        $@"{nameof(VarDeclaration_New)} {{ {nameof(Type)} = {Type.DebuggerDisplay}, {nameof(Declarator)} = {Declarator.DebuggerDisplay}, {nameof(Initializer)} = {Initializer?.DebuggerDisplay} }}";
}

public interface IVarDeclarator : INode
{
    string Name { get; }
}

public sealed class VarDeclarator : BaseNode, IVarDeclarator
{
    public string Name => Tokens[0].Lexeme.ToString();

    public VarDeclarator(Token nameIdentifier)
        : base(OfTokens(nameIdentifier), OfChildren())
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(VarDeclarator)} {{ {nameof(Name)} = {Name} }}";
}

public sealed class VarRefDeclarator : BaseNode, IVarDeclarator
{
    public string Name => Tokens[1].Lexeme.ToString();

    public VarRefDeclarator(Token ampersandToken, Token nameIdentifier)
        : base(OfTokens(ampersandToken, nameIdentifier), OfChildren())
    {
        Debug.Assert(ampersandToken.Kind is TokenKind.Ampersand);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(VarRefDeclarator)} {{ {nameof(Name)} = {Name} }}";
}

public sealed class VarArrayDeclarator : BaseNode, IVarDeclarator
{
    public string Name => Tokens[0].Lexeme.ToString();

    /// <summary>
    /// Gets the dimensions sizes.
    /// </summary>
    /// <remarks>
    /// Expected order is as they appear in the source code, from left to right.
    /// </remarks>
    public ImmutableArray<IExpression?> Lengths { get; }
    /// <summary>
    /// Gets the number of dimensions.
    /// </summary>
    public int Rank => Lengths.Length;

    public VarArrayDeclarator(Token nameIdentifier, Token openBracket, Token closeBracket, IEnumerable<IExpression?> lengths)
        : base(OfTokens(nameIdentifier, openBracket, closeBracket), OfChildren(lengths.Where(l => l is not null)!))
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(openBracket.Kind is TokenKind.OpenBracket);
        Debug.Assert(closeBracket.Kind is TokenKind.CloseBracket);
        Lengths = lengths.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(VarArrayDeclarator)} {{ {nameof(Name)} = {Name}, {nameof(Lengths)} = [{string.Join(", ", Lengths.Select(l => l?.DebuggerDisplay ?? "null"))}] }}";
}
