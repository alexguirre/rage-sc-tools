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

    public sealed class Declaration : Node
    {
        public string Type { get; }
        public InitDeclaratorList Declarators { get; }

        public override IEnumerable<Node> Children { get { yield return Declarators; } }

        public Declaration(string type, InitDeclaratorList declarators, SourceRange source) : base(source)
            => (Type, Declarators) = (type, declarators);

        public override string ToString() => $"{Type} {Declarators}";

        public override void Accept(AstVisitor visitor) => visitor.VisitDeclaration(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitDeclaration(this);
    }

    public sealed class SingleDeclaration : Node
    {
        public string Type { get; }
        public InitDeclarator Declarator { get; }

        public override IEnumerable<Node> Children { get { yield return Declarator; } }

        public SingleDeclaration(string type, InitDeclarator declarator, SourceRange source) : base(source)
            => (Type, Declarator) = (type, declarator);

        public override string ToString() => $"{Type} {Declarator}";

        public override void Accept(AstVisitor visitor) => visitor.VisitSingleDeclaration(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitSingleDeclaration(this);
    }

    public sealed class InitDeclarator : Node
    {
        public Declarator Declarator { get; }
        public Expression? Initializer { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Declarator;
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }

        public InitDeclarator(Declarator declarator, Expression? initializer, SourceRange source) : base(source)
            => (Declarator, Initializer) = (declarator, initializer);

        public override string ToString() => Initializer == null ? $"{Declarator}" : $"{Declarator} = {Initializer}";

        public override void Accept(AstVisitor visitor) => visitor.VisitInitDeclarator(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitInitDeclarator(this);
    }

    public sealed class InitDeclaratorList : Node, IEnumerable<InitDeclarator>
    {
        public ImmutableArray<InitDeclarator> Declarators { get; }

        public override IEnumerable<Node> Children => Declarators;

        public InitDeclaratorList(IEnumerable<InitDeclarator> declarators, SourceRange source) : base(source)
            => Declarators = declarators.ToImmutableArray();

        public override string ToString() => string.Join(", ", Declarators);

        public override void Accept(AstVisitor visitor) => visitor.VisitInitDeclaratorList(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitInitDeclaratorList(this);

        public IEnumerator<InitDeclarator> GetEnumerator() => ((IEnumerable<InitDeclarator>)Declarators).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Declarators).GetEnumerator();
    }

    public sealed class ParameterList : Node
    {
        public ImmutableArray<SingleDeclaration> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public ParameterList(IEnumerable<SingleDeclaration> parameters, SourceRange source) : base(source)
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
        public ImmutableArray<Declaration> Fields { get; }

        public override IEnumerable<Node> Children => Fields;

        public StructFieldList(IEnumerable<Declaration> fields, SourceRange source) : base(source)
            => Fields = fields.ToImmutableArray();

        public override string ToString() => $"{string.Join('\n', Fields)}";


        public override void Accept(AstVisitor visitor) => visitor.VisitStructFieldList(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitStructFieldList(this);
    }
}
