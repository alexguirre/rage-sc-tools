namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Ast.Types;

using System.Collections.Immutable;

public record struct ExpressionSemantics(IType? Type, bool IsLValue, bool IsConstant);

public interface IExpression : ISemanticNode<ExpressionSemantics>
{
    // Helpers for accessing semantic information
    /// <summary>
    /// Gets the semantic type of this expression.
    /// </summary>
    public sealed IType? Type => Semantics.Type;
    public sealed bool IsLValue => Semantics.IsLValue;
    /// <summary>
    /// Gets whether this expression value is known at compile time.
    /// </summary>
    public sealed bool IsConstant => Semantics.IsConstant;
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

    public BaseExpression(ImmutableArray<Token> tokens, ImmutableArray<INode> children) : base(tokens, children) { }
    public BaseExpression(SourceRange source) : base(source) {}
}
