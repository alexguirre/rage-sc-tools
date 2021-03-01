namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public sealed class GlobalBlock : ISymbol
    {
        public const int MaxBlockCount = 64; // limit hardcoded in the game .exe (and max value that fits in GLOBAL_U24* instructions)

        private readonly Dictionary<VariableSymbol, int> offsets = new();
        private int sizeOf = -1;

        public string Name { get; }
        public SourceRange Source { get; }
        public int Block { get; }
        public string Owner { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public int SizeOf
        {
            get
            {
                if (sizeOf == -1)
                {
                    AllocateVariables();
                }

                return sizeOf;
            }
        }

        public GlobalBlock(int block, string owner, IEnumerable<VariableSymbol> variables, SourceRange source)
        {
            if (block < 0 || block >= MaxBlockCount)
            {
                throw new ArgumentOutOfRangeException(nameof(block), "Block is negative or greater than or equal to 64");
            }

            Block = block;
            Owner = owner;
            Name = $"@global_block_{block}@"; // use special symbols not allowed in identifiers so this symbol won't be accessible
            Source = source;
            Variables = variables.ToImmutableArray();
        }

        public bool Contains(VariableSymbol variable) => Variables.Contains(variable);

        public int? GetLocation(VariableSymbol variable)
        {
            if (offsets.Count == 0)
            {
                AllocateVariables();
            }

            if (!offsets.TryGetValue(variable, out int offset))
            {
                return null;
            }

            return Block << 18 | offset;
        }

        private void AllocateVariables()
        {
            offsets.Clear();

            int location = 0;
            foreach (var v in Variables)
            {
                offsets.Add(v, location);
                location += v.Type.SizeOf;
            }
            sizeOf = location;
        }
    }
}
