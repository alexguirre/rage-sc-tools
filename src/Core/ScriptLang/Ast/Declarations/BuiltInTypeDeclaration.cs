namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;

/// <summary>
/// Type declaration added by the compiler.
/// </summary>
public sealed class BuiltInTypeDeclaration : ITypeDeclaration
{
    public TypeInfo BuiltInType { get; }
    public string Name { get; }
    public Token NameToken => Token.Identifier(Name);
    public ImmutableArray<Token> Tokens => ImmutableArray<Token>.Empty;
    public ImmutableArray<INode> Children => ImmutableArray<INode>.Empty;
    public SourceRange Location => default;
    public string DebuggerDisplay => $"<built-in type {Name}>";
    public TypeDeclarationSemantics Semantics
    {
        get => new(BuiltInType);
        set => throw new NotSupportedException("Cannot change declared type of built-in type declaration");
    }

    public BuiltInTypeDeclaration(string name, TypeInfo type)
    {
        BuiltInType = type;
        Name = name;
    }

    public TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => visitor.Visit(this, param);
    public void Accept(IVisitor visitor) => visitor.Visit(this);
}
