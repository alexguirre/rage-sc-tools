namespace ScTools.ScriptLang.Ast;

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

public interface INode
{
    ImmutableArray<Token> Tokens { get; }
    ImmutableArray<INode> Children { get; }
    SourceRange Source { get; }
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    string DebuggerDisplay { get; }

    TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class BaseNode : INode
{
    private SourceRange? source;

    public ImmutableArray<Token> Tokens { get; }
    public ImmutableArray<INode> Children { get; }
    public SourceRange Source => source ??= MergeSourceLocations(Tokens, Children);

    public BaseNode(ImmutableArray<Token> tokens, ImmutableArray<INode> children)
    {
        Tokens = tokens;
        Children = children;
    }

    [Obsolete("Nodes now require tokens and children nodes", false)]
    public BaseNode(SourceRange source) => throw new NotImplementedException("Deprecated constructor");

    public abstract TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);

    public virtual string DebuggerDisplay => GetType().Name;

    protected static ImmutableArray<Token> OfTokens(params Token[] tokens) => tokens.ToImmutableArray();
    protected static ImmutableArray<INode> OfChildren(params INode[] nodes) => nodes.ToImmutableArray();

    private static SourceRange MergeSourceLocations(ImmutableArray<Token> tokens, ImmutableArray<INode> nodes)
    {
        var s = tokens.Length > 0 ? tokens[0].Location :
                nodes.Length > 0 ? nodes[0].Source :
                SourceRange.Unknown;

        if (s.IsUnknown)
        {
            return s;
        }

        foreach (var t in tokens) { s = s.Merge(t.Location); }
        foreach (var n in nodes) { s = s.Merge(n.Source); }

        return s;
    }
}
