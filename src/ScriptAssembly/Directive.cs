namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Immutable;

    internal delegate void DirectiveCallback(in Directive directive, AssemblerContext context, ReadOnlySpan<Operand> operands);

    internal readonly struct Directive
    {
        public string Name { get; }
        public uint NameHash { get; }
        public DirectiveCallback Callback { get; }

        public Directive(string name, DirectiveCallback callback)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NameHash = name.ToHash();
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }
    }

    internal static class Directives
    {
        public static int Find(uint nameHash)
        {
            int left = 0;
            int right = Set.Length - 1;

            while (left <= right)
            {
                int middle = (left + right) / 2;
                uint middleKey = Set[middle].NameHash;
                int cmp = middleKey.CompareTo(nameHash);
                if (cmp == 0)
                {
                    return middle;
                }
                else if (cmp < 0)
                {
                    left = middle + 1;
                }
                else
                {
                    right = middle - 1;
                }
            }
            return -1;
        }

        private static ImmutableArray<Directive> Sort(Directive[] directives)
        {
            // sort the directive based on NameHash so we can do binary search later
            Array.Sort(directives, (a, b) => a.NameHash.CompareTo(b.NameHash));
            return directives.ToImmutableArray();
        }

        private static Exception IncorrectOperands => new ArgumentException("Incorrect operands");

        public static readonly ImmutableArray<Directive> Set = Sort(new[]
        {
            new Directive("NAME",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 1 || o[0].Type != OperandType.Identifier)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetName(o[0].Identifier);
                }),
            new Directive("ARGS",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 1 || o[0].Type != OperandType.U32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetArgsCount(o[0].U32);
                }),
            new Directive("STATICS_COUNT",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 1 || o[0].Type != OperandType.U32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetStaticsCount(o[0].U32);
                }),
            new Directive("STATIC_INT_INIT",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 2 || o[0].Type != OperandType.U32 || o[1].Type != OperandType.U32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetStaticValue(o[0].U32, (int)o[1].U32);
                }),
            new Directive("STATIC_FLOAT_INIT",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 2 || o[0].Type != OperandType.U32 || o[1].Type != OperandType.F32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetStaticValue(o[0].U32, o[1].F32);
                }),
            new Directive("HASH", // tmp directive until we figure out how to calculate this hash automatically
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 1 || o[0].Type != OperandType.U32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetHash(o[0].U32);
                }),
            new Directive("GLOBALS",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 2 || o[0].Type != OperandType.U32 || o[1].Type != OperandType.U32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetGlobals((byte)o[0].U32, o[1].U32);
                }),
            new Directive("GLOBAL_INT_INIT",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 2 || o[0].Type != OperandType.U32 || o[1].Type != OperandType.U32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetGlobalValue(o[0].U32, o[1].U32);
                }),
            new Directive("GLOBAL_FLOAT_INIT",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 2 || o[0].Type != OperandType.U32 || o[1].Type != OperandType.F32)
                    {
                        throw IncorrectOperands;
                    }

                    c.SetGlobalValue(o[0].U32, o[1].F32);
                }),
            new Directive("STRING",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 1 || o[0].Type != OperandType.String)
                    {
                        throw IncorrectOperands;
                    }

                    c.AddString(o[0].String);
                }),
            new Directive("NATIVE_DEF",
                (in Directive d, AssemblerContext c, ReadOnlySpan<Operand> o) =>
                {
                    if (o.Length != 1 || o[0].Type != OperandType.U64)
                    {
                        throw IncorrectOperands;
                    }
                    
                    c.AddNative(o[0].U64);
                }),
        });
    }
}
