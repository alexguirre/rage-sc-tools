namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Immutable;

public interface IStatement : INode
{
    /// <summary>
    /// Gets the label associated to this statement.
    /// </summary>
    string? Label { get; set; }
}

/// <param name="ExitLabel">Name of the label used to indicate the end of this statement. This is where a BREAK statement jumps to.</param>
public record struct BreakableStatementSemantics(string? ExitLabel);

public interface IBreakableStatement : IStatement, ISemanticNode<BreakableStatementSemantics>
{
}

/// <param name="ExitLabel">Name of the label used to indicate the end of this statement. This is where a BREAK statement jumps to.</param>
/// <param name="BeginLabel">Name of the label used to indicate the beginning of the loop, where the loop condition is checked.</param>
/// <param name="ContinueLabel">Name of the label used to indicate where the CONTINUE statement needs to jump to to start a new iteration.</param>
public record struct LoopStatementSemantics(string? ExitLabel, string? BeginLabel, string? ContinueLabel);

public interface ILoopStatement : IBreakableStatement, ISemanticNode<LoopStatementSemantics>
{
}

public abstract class BaseStatement : BaseNode, IStatement
{
    public string? Label { get; set; } // TODO: make IStatement.Label read-only
                                       // TODO: make IStatement.Label a node?

    public BaseStatement(ImmutableArray<Token> tokens, ImmutableArray<INode> children) : base(tokens, children) { }
}
