namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Directives;

    public sealed class Program : BaseNode
    {
        public IList<IDirective> Directives { get; set; }

        public Program(SourceRange source, IEnumerable<IDirective> directives) : base(source)
            => Directives = new List<IDirective>(directives);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
