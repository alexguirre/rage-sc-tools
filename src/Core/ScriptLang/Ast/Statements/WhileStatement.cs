namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class WhileStatement : BaseStatement, ILoopStatement
{
    public IExpression Condition { get; set; }
    public List<IStatement> Body { get; set; } = new();
    public string? ExitLabel { get; set; }
    public string? BeginLabel { get; set; }
    /// <summary>
    /// Same as <see cref="BeginLabel"/>.
    /// </summary>
    public string? ContinueLabel { get => BeginLabel; set => BeginLabel = value; }

    public WhileStatement(SourceRange source, IExpression condition) : base(source)
        => Condition = condition;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}
