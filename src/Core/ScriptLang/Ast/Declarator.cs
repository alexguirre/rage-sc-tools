#nullable enable
using System;
using System.Collections.Generic;

namespace ScTools.ScriptLang.Ast
{
    public abstract class Declarator : Node
    {
        public abstract string Identifier { get; }

        public Declarator(SourceRange source) : base(source)
        { }
    }

    public sealed class RefDeclarator : Declarator
    {
        public override string Identifier => Inner.Identifier;
        public Declarator Inner { get; }

        public override IEnumerable<Node> Children { get { yield return Inner; } }

        public RefDeclarator(Declarator inner, SourceRange source) : base(source)
            => Inner = inner is not RefDeclarator ? inner : throw new ArgumentException("Ref to ref is not valid");

        public override string ToString() => $"&{Inner}";
    }

    public sealed class SimpleDeclarator : Declarator
    {
        public override string Identifier { get; }

        public SimpleDeclarator(string identifier, SourceRange source) : base(source)
            => Identifier = identifier;

        public override string ToString() => Identifier;
    }

    public sealed class ArrayDeclarator : Declarator
    {
        public override string Identifier => Inner.Identifier;
        public Declarator Inner { get; }
        public Expression Length { get; }

        public override IEnumerable<Node> Children { get { yield return Inner; yield return Length; } }

        public ArrayDeclarator(Declarator inner, Expression length, SourceRange source) : base(source)
            => (Inner, Length) = (inner, length);

        public override string ToString() => Inner is RefDeclarator ? $"({Inner})[{Length}]" : $"{Inner}[{Length}]";
    }
}
