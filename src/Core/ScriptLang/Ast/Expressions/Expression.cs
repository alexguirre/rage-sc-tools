namespace ScTools.ScriptLang.Ast.Expressions
{
    public interface IExpression
    {
    }

    public abstract class BaseExpression : BaseNode, IExpression
    {
        public BaseExpression(SourceRange source) : base(source) {}
    }
}
