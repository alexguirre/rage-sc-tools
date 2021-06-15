namespace ScTools.ScriptLang.Ast.Declarations
{
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;

    public enum VarKind
    {
        Constant,
        Global,
        Static,
        StaticArg,
        Local,
        Parameter,
    }

    public sealed class VarDeclaration : BaseValueDeclaration, IStatement
    {
        public VarKind Kind { get; set; }
        public IExpression? Initializer { get; set; }

        public VarDeclaration(SourceRange source, string name, VarKind kind) : base(source, name)
            => Kind = kind;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
