namespace ScTools.ScriptAssembly.Definitions
{
    using ScTools.GameFiles;

    public class StaticFieldDefinition : FieldDefinition, ISymbolDefinition
    {
        public uint Id { get; }
        public ScriptValue InitialValue { get; }

        public StaticFieldDefinition(string name, TypeDefinition type, ScriptValue initialValue) : base(name, type)
        {
            Id = Registry.NameToId(Name);
            InitialValue = initialValue;
        }
    }

    public sealed class ArgDefinition : StaticFieldDefinition
    {
        public ArgDefinition(string name, TypeDefinition type, ScriptValue initialValue) : base(name, type, initialValue)
        {
        }
    }
}
