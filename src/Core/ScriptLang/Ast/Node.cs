#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public abstract class Node
    {
        public SourceRange Source { get; }
        public virtual IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public Node(SourceRange source) => Source = source;
    }

    public sealed class ArrayIndexer : Node
    {
        public Expression Expression { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public ArrayIndexer(Expression expression, SourceRange source) : base(source)
            => Expression = expression;

        public override string ToString() => $"[{Expression}]";
    }

    public sealed class VariableDeclaration : Node
    {
        public Type Type { get; }
        public string Name { get; }
        public ArrayIndexer? ArrayRank { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Type;
                if (ArrayRank != null)
                {
                    yield return ArrayRank;
                }
            }
        }

        public VariableDeclaration(Type type, string name, ArrayIndexer? arrayRank, SourceRange source) : base(source)
            => (Type, Name, ArrayRank) = (type, name, arrayRank);

        public override string ToString() => $"{Type} {Name}{ArrayRank?.ToString() ?? ""}";
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
    }

    public sealed class ParameterList : Node
    {
        public ImmutableArray<VariableDeclaration> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public ParameterList(IEnumerable<VariableDeclaration> parameters, SourceRange source) : base(source)
            => Parameters = parameters.ToImmutableArray();

        public override string ToString() => $"({string.Join(", ", Parameters)})";
    }

    public sealed class ArgumentList : Node
    {
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments;

        public ArgumentList(IEnumerable<Expression> arguments, SourceRange source) : base(source)
            => Arguments = arguments.ToImmutableArray();

        public override string ToString() => $"({string.Join(", ", Arguments)})";
    }

    public sealed class StructFieldList : Node
    {
        public ImmutableArray<VariableDeclarationWithInitializer> Fields { get; }

        public override IEnumerable<Node> Children => Fields;

        public StructFieldList(IEnumerable<VariableDeclarationWithInitializer> fields, SourceRange source) : base(source)
            => Fields = fields.ToImmutableArray();

        public override string ToString() => $"{string.Join('\n', Fields)}";
    }
}
