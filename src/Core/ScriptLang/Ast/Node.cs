#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public abstract class Node
    {
        public SourceRange Source { get; }
        public virtual IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public Node(SourceRange source) => Source = source;

        public abstract void Accept(AstVisitor visitor);
        [return: MaybeNull] public abstract T Accept<T>(AstVisitor<T> visitor);
    }

    public sealed class Declaration : Node
    {
        public string Type { get; }
        public Declarator Declarator { get; }
        public Expression? Initializer { get; }

        public override IEnumerable<Node> Children { get { yield return Declarator; if (Initializer != null) { yield return Initializer; } } }

        public Declaration(string type, Declarator declarator, Expression? initializer, SourceRange source) : base(source)
            => (Type, Declarator, Initializer) = (type, declarator, initializer);

        public override string ToString() => Initializer == null ? $"{Type} {Declarator}" :
                                                                   $"{Type} {Declarator} = {Initializer}";

        public override void Accept(AstVisitor visitor) => visitor.VisitDeclaration(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitDeclaration(this);
    }
}
