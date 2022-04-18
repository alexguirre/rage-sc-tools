namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Types;

using System.Collections.Generic;

public record struct ExpressionSemantics(TypeInfo? Type, ValueKind ValueKind);

public interface IExpression : ISemanticNode<ExpressionSemantics>
{
    // Helpers for accessing semantic information
    /// <summary>
    /// Gets the semantic type of this expression.
    /// </summary>
    public sealed TypeInfo? Type => Semantics.Type;
    public sealed ValueKind ValueKind => Semantics.ValueKind;
}

public interface ILiteralExpression : IExpression
{
    object? Value { get; }
}

public interface ILiteralExpression<T> : ILiteralExpression
{
    new T Value { get; }
}

public abstract class BaseExpression : BaseNode, IExpression
{
    public ExpressionSemantics Semantics { get; set; }

    public BaseExpression(IEnumerable<Token> tokens, IEnumerable<INode> children) : base(tokens, children) { }
}
