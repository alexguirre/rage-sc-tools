namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Declarations;

    public sealed class Program : BaseNode
    {
        public int ScriptHash { get; set; }
        public IList<IDeclaration> Declarations { get; set; } = new List<IDeclaration>();
        public FuncDeclaration? Script { get; set; }
        public int StaticsSize { get; set; }
        public int ScriptParametersSize => Script!.Prototype.ParametersSize;
        public IDictionary<int, VarDeclaration> Statics { get; set; } = new Dictionary<int, VarDeclaration>();
        public VarDeclaration? ArgVar { get; set; }
        public GlobalBlockDeclaration? GlobalBlock { get; set; }

        public Program(SourceRange source) : base(source) {}

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
