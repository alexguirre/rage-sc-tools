#nullable enable
namespace ScTools.ScriptLang.Ast
{
    public sealed class Identifier : Node
    {
        public string Name { get; }

        public Identifier(string name, SourceLocation location) : base(location)
            => Name = name;
    }
}
