#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public sealed class Root : Node
    {
        public ImmutableArray<TopLevelStatement> Statements { get; }

        public override IEnumerable<Node> Children => Statements;

        public Root(IEnumerable<TopLevelStatement> statements, SourceRange source) : base(source)
            => Statements = statements.ToImmutableArray();

        public override string ToString() => $"{string.Join("\n", Statements)}";
    }
}
