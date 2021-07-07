namespace ScTools.ScriptLang.Ast.Statements
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class IfStatement : BaseStatement
    {
        public IExpression Condition { get; set; }
        public IList<IStatement> Then { get; set; } = new List<IStatement>();
        public IList<IStatement> Else { get; set; } = new List<IStatement>();
        public string? ElseLabel { get; set; }
        public string? EndLabel { get; set; }

        public IfStatement(SourceRange source, IExpression condition) : base(source)
            => Condition = condition;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
