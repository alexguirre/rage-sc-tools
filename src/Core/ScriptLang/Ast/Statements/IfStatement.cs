namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class IfStatement : BaseStatement
{
    public IExpression Condition { get; set; }
    public List<IStatement> Then { get; set; } = new(); // TODO: remove these initializers when old code is refactored
    public List<IStatement> Else { get; set; } = new();
    public string? ElseLabel { get; set; }
    public string? EndLabel { get; set; }

    public IfStatement(Token ifKeyword, Token endifKeyword, IExpression condition, IEnumerable<IStatement> thenBody) : base(ifKeyword, endifKeyword)
    {
        Debug.Assert(ifKeyword.Kind is TokenKind.IF);
        Debug.Assert(endifKeyword.Kind is TokenKind.ENDIF);
        Condition = condition;
        Then = thenBody.ToList();
        Else = new();
    }
    public IfStatement(SourceRange source, IExpression condition) : base(source)
        => Condition = condition;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(IfStatement)} {{ {nameof(Condition)} = {Condition.DebuggerDisplay}, {nameof(Then)} = [{string.Join(", ", Then.Select(a => a.DebuggerDisplay))}], {nameof(Else)} = [{string.Join(", ", Else.Select(a => a.DebuggerDisplay))}] }}";
}
