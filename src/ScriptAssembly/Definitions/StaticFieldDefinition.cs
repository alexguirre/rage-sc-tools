namespace ScTools.ScriptAssembly.Definitions
{
    public sealed class StaticFieldDefinition : FieldDefinition, ISymbolDefinition
    {
        public uint Id { get; }

        public StaticFieldDefinition(string name, TypeDefinition type) : base(name, type)
        {
            Id = Registry.NameToId(Name);
        }
    }
}
