namespace ScTools.LanguageServer;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Linq;

internal static class AstNodeLocator
{
    public static INode? Locate(INode where, SourceLocation targetLocation)
    {
        if (!where.Location.Contains(targetLocation))
            return null;

        foreach (var childNode in where.Children)
        {
            var res = Locate(childNode, targetLocation);
            if (res is not null)
            {
                return res;
            }
        }

        return where;
    }
}
