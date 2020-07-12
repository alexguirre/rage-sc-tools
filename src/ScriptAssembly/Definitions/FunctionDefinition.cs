namespace ScTools.ScriptAssembly.Definitions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using ScTools.ScriptAssembly.Types;

    public sealed class FunctionDefinition : ISymbolDefinition
    {
        public const string EntrypointName = "main";
        public static readonly uint EntrypointId = Registry.NameToId(EntrypointName);

        public readonly struct Statement
        {
            public string Label { get; }
            public string Mnemonic { get; }
            public ImmutableArray<Operand> Operands { get; }

            public Statement(string label, string mnemonic, IEnumerable<Operand> operands)
            {
                Label = label;
                Mnemonic = mnemonic;
                Operands = operands?.ToImmutableArray() ?? ImmutableArray<Operand>.Empty;
            }
        }

        public uint Id { get; }
        public string Name { get; }
        public bool IsEntrypoint { get; }
        public bool Naked { get; }
        public ImmutableArray<FieldDefinition> Args { get; }
        public ImmutableArray<FieldDefinition> Locals { get; }
        public TypeBase ReturnType { get; } // null if void
        public ImmutableArray<Statement> Statements { get; }

        public FunctionDefinition(string name, bool naked, IEnumerable<FieldDefinition> args, IEnumerable<FieldDefinition> locals, TypeBase returnType, IEnumerable<Statement> statements)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("null or empty string", nameof(name));
            Id = Registry.NameToId(name);
            IsEntrypoint = Id == EntrypointId;
            Naked = naked;
            Args  = args?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(args));
            Locals = locals?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(locals));
            ReturnType = returnType;
            Statements = statements?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(statements));

            if (Naked && (Args.Length > 0 || Locals.Length > 0 || ReturnType != null))
            {
                // TODO: can we specify these requirements in the grammar?
                throw new ArgumentException("A naked function may not have any declared arguments or locals");
            }
        }
    }
}
