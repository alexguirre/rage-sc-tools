namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Ast.Types;

using System.Collections.Immutable;

public record struct ExpressionSemantics(IType? Type, bool IsLValue, bool IsConstant);

public interface IExpression : ISemanticNode<ExpressionSemantics>
{
    // Helpers for accessing semantic information
    public sealed IType? Type => Semantics.Type;
    public sealed bool IsLValue => Semantics.IsLValue;
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
