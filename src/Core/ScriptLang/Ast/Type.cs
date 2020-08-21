#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;

    public sealed class Type : Node
    {
        public string Name { get; }
        public bool IsReference { get; }

        public Type(string name, bool isReference, SourceRange source) : base(source)
            => (Name, IsReference) = (name, isReference);

        public override string ToString() => $"{Name}{(IsReference ? "&" : "")}";
    }
}
