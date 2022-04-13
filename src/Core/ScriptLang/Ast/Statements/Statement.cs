namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public interface IStatement : INode
{
    /// <summary>
    /// Gets the label associated to this statement.
    /// </summary>
    Label? Label { get; }
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
    public Label? Label { get; }

    public BaseStatement(IEnumerable<Token> tokens, IEnumerable<INode> children, Label? label)
        : base(tokens, label is null ? children : children.Append(label))
    {
        if (label is not null)
        {
            // we are appending the label as the last children, verify that it wasn't already included in the childrens by accident
            Debug.Assert(!children.Contains(label));
        }

        Label = label;
    }
}

public sealed class Label : BaseNode
{
    public string Name => Tokens[0].Lexeme.ToString();

    public Label(Token identifierToken, Token colon)
        : base(OfTokens(identifierToken, colon), OfChildren())
    {
        Debug.Assert(identifierToken.Kind is TokenKind.Identifier);
        Debug.Assert(colon.Kind is TokenKind.Colon);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(Label)} {{ {nameof(Name)} = {Name} }}";
}
