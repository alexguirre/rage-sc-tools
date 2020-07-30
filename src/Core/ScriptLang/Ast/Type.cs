namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;

    public abstract class Type : Node
    {
        public Type(SourceRange source) : base(source) { }
    }

    public sealed class BasicType : Type
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public BasicType(Identifier name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"{Name}";
    }

    public sealed class RefType : Type
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public RefType(Identifier name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"{Name}&";
    }
}
