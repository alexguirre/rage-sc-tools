namespace ScTools.ScriptLang.Ast.Statements
{
    public interface IStatement : INode
    {
    }

    public abstract class BaseStatement : BaseNode, IStatement
    {
        public BaseStatement(SourceRange source) : base(source) {}
    }
}
