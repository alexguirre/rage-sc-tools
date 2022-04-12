namespace ScTools.ScriptLang.Ast.Statements;

public interface IStatement : INode
{
    string? Label { get; set; }
}

public interface IBreakableStatement : IStatement
{
    /// <summary>
    /// Gets or sets the name of the label used to indicate the end of this statement. This is where a BREAK statement jumps to.
    /// </summary>
    string? ExitLabel { get; set; }
}

public interface ILoopStatement : IBreakableStatement
{
    /// <summary>
    /// Gets or sets the name of the label used to indicate the beginning of this loop, where the loop condition is checked.
    /// </summary>
    string? BeginLabel { get; set; }
    /// <summary>
    /// Gets or sets the name of the label used to indicate where the CONTINUE statement needs to jump to to start a new iteration.
    /// </summary>
    string? ContinueLabel { get; set; }
}

public abstract class BaseStatement : BaseNode, IStatement
{
    public string? Label { get; set; }

    public BaseStatement(params Token[] tokens) : base(tokens) { }
    public BaseStatement(SourceRange source) : base(source) {}
}
