#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;

    public sealed class Root : Node
    {
        public ImmutableArray<TopLevelStatement> Statements { get; }

        public override IEnumerable<Node> Children => Statements;

        public Root(IEnumerable<TopLevelStatement> statements, SourceRange source) : base(source)
            => Statements = statements.ToImmutableArray();

        public override string ToString() => $"{string.Join("\n", Statements)}";

        public override void Accept(AstVisitor visitor) => visitor.VisitRoot(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitRoot(this);
    }
}
