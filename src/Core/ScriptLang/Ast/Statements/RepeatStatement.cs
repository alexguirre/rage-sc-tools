namespace ScTools.ScriptLang.Ast.Statements
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class RepeatStatement : BaseStatement, ILoopStatement
    {
        public IExpression Limit { get; set; }
        public IExpression Counter { get; set; }
        public IList<IStatement> Body { get; set; } = new List<IStatement>();

        public RepeatStatement(SourceRange source, IExpression limit, IExpression counter) : base(source)
            => (Limit, Counter) = (limit, counter);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
