namespace ScTools.ScriptLang.Ast.Statements
{
    public interface IStatement : INode
    {
    }

    public interface IBreakableStatement : IStatement
    {
    }

    public interface ILoopStatement : IBreakableStatement
    {
    }

    public abstract class BaseStatement : BaseNode, IStatement
    {
        public BaseStatement(SourceRange source) : base(source) {}
    }
}
