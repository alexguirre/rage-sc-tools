namespace ScTools.ScriptLang.Ast.Expressions
{
    using ScTools.ScriptLang.Ast.Types;

    public interface IExpression : INode
    {
        IType? Type { get; set; }
        bool LValue { get; set; }
    }

    public abstract class BaseExpression : BaseNode, IExpression
    {
        public IType? Type { get; set; }
        public bool LValue { get; set; }

        public BaseExpression(SourceRange source) : base(source) {}
    }
}
