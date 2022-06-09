namespace ScTools.Tests.ScriptLang;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Semantics;

internal static class AstExtensions
{
    public static ISymbol? GetNameSymbol(this IExpression expression)
    {
        var nameExpr = IsType<NameExpression>(expression);
        return nameExpr.Semantics.Symbol;
    }

    public static T FindFirstNodeOfType<T>(this INode parent) where T : class, INode
        => FindNthNodeOfType<T>(parent, n: 1);

    public static T FindNthNodeOfType<T>(this INode parent, int n) where T : class, INode
    {
        if (n < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "n must be 1 or greater");
        }

        var found = 0;
        // Children are added to the stack in reverse order so that nodes that appear first in source code are explored first
        var stack = new Stack<INode>(parent.Children.Reverse());
        while (stack.TryPop(out var node))
        {
            if (node is T t)
            {
                found++;
                if (found == n)
                {
                    return t;
                }
            }

            foreach (var child in node.Children.Reverse())
            {
                stack.Push(child);
            }
        }

        throw new InvalidOperationException($"Could not find nth={n} node of type {typeof(T).Name}");
    }
}
