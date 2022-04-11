﻿namespace ScTools.ScriptLang.Ast;

using System;
using System.Collections.Immutable;
using System.Linq;

public interface INode
{
    ImmutableArray<Token> Tokens { get; }
    SourceRange Source { get; }

    TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);
}

public abstract class BaseNode : INode
{
    public ImmutableArray<Token> Tokens { get; }
    public SourceRange Source => Tokens.Length == 0 ? SourceRange.Unknown : Tokens.Skip(1).Aggregate(Tokens.First().Location, (acc, t) => acc.Merge(t.Location));


    public BaseNode(params Token[] tokens) => Tokens = tokens.ToImmutableArray();

    [Obsolete("Nodes now require tokens", false)]
    public BaseNode(SourceRange source) => throw new NotImplementedException("Deprecated constructor");

    public abstract TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);
}
