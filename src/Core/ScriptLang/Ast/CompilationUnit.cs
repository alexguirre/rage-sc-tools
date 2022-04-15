namespace ScTools.ScriptLang.Ast;

using ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public sealed class CompilationUnit : BaseNode
{
    public ImmutableArray<UsingDirective> Usings { get; }
    public ImmutableArray<IDeclaration_New> Declarations { get; }

    public CompilationUnit(IEnumerable<UsingDirective> usings, IEnumerable<IDeclaration_New> declarations)
           : base(OfTokens(), OfChildren().Concat(usings).Concat(declarations))
    {
        Usings = usings.ToImmutableArray();
        Declarations = declarations.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}
