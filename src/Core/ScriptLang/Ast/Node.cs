#nullable enable
namespace ScTools.ScriptLang.Ast
{
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

    public sealed class ArrayIndexer : Node
    {
        public Expression Expression { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public ArrayIndexer(Expression expression, SourceRange source) : base(source)
            => Expression = expression;

        public override string ToString() => $"[{Expression}]";

        public override void Accept(AstVisitor visitor) => visitor.VisitArrayIndexer(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitArrayIndexer(this);
    }

    public sealed class VariableDeclaration : Node
    {
        public string Type { get; }
        public Declarator Decl { get; }

        public override IEnumerable<Node> Children { get { yield return Decl; } }

        public VariableDeclaration(string type, Declarator decl, SourceRange source) : base(source)
            => (Type, Decl) = (type, decl);

        public override string ToString() => $"{Type} {Decl}";

        public override void Accept(AstVisitor visitor) => visitor.VisitVariableDeclaration(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitVariableDeclaration(this);
    }

    public sealed class VariableDeclarationWithInitializer : Node
    {
        public VariableDeclaration Declaration { get; }
        public Expression? Initializer { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Declaration;
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }

        public VariableDeclarationWithInitializer(VariableDeclaration declaration, Expression? initializer, SourceRange source) : base(source)
            => (Declaration, Initializer) = (declaration, initializer);

        public override string ToString() => Declaration.ToString() + (Initializer != null ? $" = {Initializer}" : "");

        public override void Accept(AstVisitor visitor) => visitor.VisitVariableDeclarationWithInitializer(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitVariableDeclarationWithInitializer(this);
    }

    public sealed class ParameterList : Node
    {
        public ImmutableArray<VariableDeclaration> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public ParameterList(IEnumerable<VariableDeclaration> parameters, SourceRange source) : base(source)
            => Parameters = parameters.ToImmutableArray();

        public override string ToString() => $"({string.Join(", ", Parameters)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitParameterList(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitParameterList(this);
    }

    public sealed class ArgumentList : Node
    {
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments;

        public ArgumentList(IEnumerable<Expression> arguments, SourceRange source) : base(source)
            => Arguments = arguments.ToImmutableArray();

        public override string ToString() => $"({string.Join(", ", Arguments)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitArgumentList(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitArgumentList(this);
    }

    public sealed class StructFieldList : Node
    {
        public ImmutableArray<VariableDeclarationWithInitializer> Fields { get; }

        public override IEnumerable<Node> Children => Fields;

        public StructFieldList(IEnumerable<VariableDeclarationWithInitializer> fields, SourceRange source) : base(source)
            => Fields = fields.ToImmutableArray();

        public override string ToString() => $"{string.Join('\n', Fields)}";


        public override void Accept(AstVisitor visitor) => visitor.VisitStructFieldList(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitStructFieldList(this);
    }
}
