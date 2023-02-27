namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public enum VarKind
{
    /// <summary>
    /// A CONST_INT or CONST_FLOAT declaration. Where used, it is replaced by its value at compile-time. Requires a non-null <see cref="VarDeclaration.Initializer"/>.
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
    /// <summary>
    /// A struct field.
    /// </summary>
    Field,
}

public sealed partial class VarDeclaration : BaseValueDeclaration, IStatement
{
    public override Token NameToken => Declarator.NameToken;
    public Label? Label { get; }
    public TypeName Type => (TypeName)Children[0];
    public VarDeclarator Declarator => (VarDeclarator)Children[1];
    public VarKind Kind { get; }
    public bool IsReference => Declarator.IsReference;
    public IExpression? Initializer { get; }

    public VarDeclaration(TypeName type, VarDeclarator declarator, VarKind kind, IExpression? initializer = null, Label? label = null)
        : base(OfTokens(), OfChildren(type, declarator).AppendIfNotNull(initializer).AppendIfNotNull(label))
    {
        Kind = kind;
        Initializer = initializer;
        Label = label;
    }

    internal VarDeclaration WithLabel(Label? label)
        => new(Type, Declarator, Kind, Initializer, label);

    public override string DebuggerDisplay =>
        $@"{nameof(VarDeclaration)} {{ {nameof(Type)} = {Type.DebuggerDisplay}, {nameof(Declarator)} = {Declarator.DebuggerDisplay}, {nameof(Initializer)} = {Initializer?.DebuggerDisplay} }}";
}

public sealed partial class VarDeclarator : BaseNode
{
    public Token NameToken => Tokens[0];
    public string Name => NameToken.Lexeme.ToString();
    /// <summary>
    /// Gets whether this is a reference.
    /// </summary>
    public bool IsReference { get; }
    public Token? RefAmpersandToken => IsReference ? Tokens.Last() : null;
    /// <summary>
    /// Gets whether this is an array. If true, <see cref="Lengths"/> is not empty and <see cref="Rank"/> is 1 or greater.
    /// </summary>
    public bool IsArray => Rank != 0;
    /// <summary>
    /// Gets the dimensions sizes. Empty is not an array.
    /// </summary>
    /// <remarks>
    /// Expected order is as they appear in the source code, from left to right.
    /// </remarks>
    public ImmutableArray<IExpression?> Lengths { get; }
    /// <summary>
    /// Gets the number of dimensions. Returns 0 if not an array.
    /// </summary>
    public int Rank => Lengths.IsDefaultOrEmpty ? 0 : Lengths.Length;

    public VarDeclarator(Token nameIdentifier, Token? refAmpersandToken = null)
        : base(OfTokens(nameIdentifier).AppendIfNotNull(refAmpersandToken), OfChildren())
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(!refAmpersandToken.HasValue || refAmpersandToken.Value.Kind is TokenKind.Ampersand);
        IsReference = refAmpersandToken.HasValue;
    }

    public VarDeclarator(Token nameIdentifier, Token openBracket, Token closeBracket, IEnumerable<IExpression?> lengths, Token? refAmpersandToken = null)
        : base(OfTokens(nameIdentifier, openBracket, closeBracket).AppendIfNotNull(refAmpersandToken), OfChildren(lengths.Where(l => l is not null)!))
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(openBracket.Kind is TokenKind.OpenBracket);
        Debug.Assert(closeBracket.Kind is TokenKind.CloseBracket);
        Debug.Assert(!refAmpersandToken.HasValue || refAmpersandToken.Value.Kind is TokenKind.Ampersand);
        Lengths = lengths.ToImmutableArray();
        IsReference = refAmpersandToken.HasValue;
    }

    public override string DebuggerDisplay =>
        $@"{nameof(VarDeclarator)} {{ {nameof(Name)} = {Name}, {nameof(IsReference)} = {IsReference}, {nameof(IsArray)} = {IsArray}, {nameof(Lengths)} = [{string.Join(", ", Lengths.Select(l => l?.DebuggerDisplay ?? "null"))}] }}";
}
