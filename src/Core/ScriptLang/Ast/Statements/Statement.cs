namespace ScTools.ScriptLang.Ast.Statements
{
    public interface IStatement : INode
    {
    }

    public interface IBreakableStatement : IStatement
    {
        string? ExitLabel { get; set; }
    }

    public interface ILoopStatement : IBreakableStatement
    {
        string? BeginLabel { get; set; }
    }

    public abstract class BaseStatement : BaseNode, IStatement
    {
        public BaseStatement(SourceRange source) : base(source) {}
    }
}
