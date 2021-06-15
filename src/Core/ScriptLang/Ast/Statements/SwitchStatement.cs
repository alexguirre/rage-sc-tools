﻿namespace ScTools.ScriptLang.Ast.Statements
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class SwitchStatement : BaseStatement
    {
        public IExpression Expression { get; set; }
        public IList<SwitchCase> Cases { get; set; } = new List<SwitchCase>();

        public SwitchStatement(SourceRange source, IExpression expression) : base(source)
            => Expression = expression;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }

    public abstract class SwitchCase : BaseNode
    {
        public IList<IStatement> Body { get; set; } = new List<IStatement>();

        public SwitchCase(SourceRange source) : base(source) { }
    }

    public sealed class ValueSwitchCase : SwitchCase
    {
        public IExpression Value { get; }

        public ValueSwitchCase(SourceRange source, IExpression value) : base(source)
            => Value = value;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }

    public sealed class DefaultSwitchCase : SwitchCase
    {
        public DefaultSwitchCase(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}