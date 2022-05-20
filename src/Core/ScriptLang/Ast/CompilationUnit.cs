namespace ScTools.ScriptLang.Ast;

using ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public sealed partial class CompilationUnit : BaseNode
{
    public ImmutableArray<UsingDirective> Usings { get; }
    public ImmutableArray<IDeclaration> Declarations { get; }

    public CompilationUnit(IEnumerable<UsingDirective> usings, IEnumerable<IDeclaration> declarations)
           : base(OfTokens(), OfChildren().Concat(usings).Concat(declarations))
    {
        Usings = usings.ToImmutableArray();
        Declarations = declarations.ToImmutableArray();
    }
}
