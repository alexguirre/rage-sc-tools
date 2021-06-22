namespace ScTools.ScriptLang.Ast.Statements
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class WhileStatement : BaseStatement, ILoopStatement
    {
        public IExpression Condition { get; set; }
        public IList<IStatement> Body { get; set; } = new List<IStatement>();

        public WhileStatement(SourceRange source, IExpression condition) : base(source)
            => Condition = condition;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
