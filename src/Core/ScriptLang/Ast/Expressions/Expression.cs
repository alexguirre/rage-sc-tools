namespace ScTools.ScriptLang.Ast.Expressions
{
    public interface IExpression : INode
    {
    }

    public abstract class BaseExpression : BaseNode, IExpression
    {
        public BaseExpression(SourceRange source) : base(source) {}
    }
}
