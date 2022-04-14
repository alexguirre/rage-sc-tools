namespace ScTools.ScriptLang.Ast;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;

public interface INode
{
    ImmutableArray<Token> Tokens { get; }
    ImmutableArray<INode> Children { get; }
    SourceRange Location { get; }
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    string DebuggerDisplay { get; }

    TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);
}

/// <summary>
/// Represents a node with additional semantic information that is filled during the semantic analysis phase.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISemanticNode<T> : INode where T : notnull
{
    /// <summary>
    /// Gets or sets the semantic information of this node.
    /// </summary>
    public T Semantics { get; set; }
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class BaseNode : INode
{
    private SourceRange? location;

    public ImmutableArray<Token> Tokens { get; }
    public ImmutableArray<INode> Children { get; }
    public SourceRange Location => location ??= MergeSourceLocations(Tokens, Children);

    public BaseNode(IEnumerable<Token> tokens, IEnumerable<INode> children)
    {
        Tokens = tokens.ToImmutableArray();
        Children = children.ToImmutableArray();
    }

    [Obsolete("Nodes now require tokens and children nodes", false)]
    public BaseNode(SourceRange source) => throw new NotImplementedException("Deprecated constructor");

    public abstract TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);

    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    public virtual string DebuggerDisplay => GetType().Name;

    // Helpers to pass the BaseNode constructor parameters, making the code clearer with names and without new[] { ... }
    protected static IEnumerable<Token> OfTokens(params Token[] tokens) => tokens;
    protected static IEnumerable<Token> OfTokens(IEnumerable<Token> tokens) => tokens;
    protected static IEnumerable<INode> OfChildren(params INode[] nodes) => nodes;
    protected static IEnumerable<INode> OfChildren(IEnumerable<INode> nodes) => nodes;

    private static SourceRange MergeSourceLocations(ImmutableArray<Token> tokens, ImmutableArray<INode> nodes)
    {
        var s = tokens.Length > 0 ? tokens[0].Location :
                nodes.Length > 0 ? nodes[0].Location :
                SourceRange.Unknown;

        if (s.IsUnknown)
        {
            return s;
        }

        foreach (var t in tokens) { s = s.Merge(t.Location); }
        foreach (var n in nodes) { s = s.Merge(n.Location); }

        return s;
    }
}
