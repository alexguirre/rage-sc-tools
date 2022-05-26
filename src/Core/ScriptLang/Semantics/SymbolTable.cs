namespace ScTools.ScriptLang.Semantics;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

internal sealed class SymbolTable<T> where T : ISymbol
{
    private readonly Stack<Dictionary<string, T>> scopes = new();

    private Dictionary<string, T> CurrentScope => scopes.Peek();

    public SymbolTable()
    {
        PushScope(); // push root scope
    }

    public void PushScope() => scopes.Push(new(Parser.CaseInsensitiveComparer));
    public void PopScope()
    {
        if (scopes.Count > 1)
        {
            scopes.Pop();
        }
        else
        {
            throw new InvalidOperationException("Cannot pop root scope");
        }
    }

    public bool Add(T symbol) => CurrentScope.TryAdd(symbol.Name, symbol);

    public bool Find(string name, [MaybeNullWhen(false)] out T symbol)
    {
        foreach (var scope in scopes)
        {
            if (scope.TryGetValue(name, out symbol))
            {
                return true;
            }
        }

        symbol = default;
        return false;
    }
}
