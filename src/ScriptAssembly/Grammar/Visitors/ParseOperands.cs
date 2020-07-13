namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using ScTools.ScriptAssembly.Types;
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    public sealed class ParseOperands : ScAsmBaseVisitor<Operand[]>
    {
        private readonly Registry registry;
        private readonly (ImmutableArray<FieldDefinition> Locals, ImmutableArray<FieldDefinition> Args)? scope;

        public ParseOperands(Registry registry, (ImmutableArray<FieldDefinition> Locals, ImmutableArray<FieldDefinition> Args)? scope = null)
        {
            this.registry = registry;
            this.scope = scope;
        }

        public override Operand[] VisitOperandList([NotNull] ScAsmParser.OperandListContext context)
            => context.operand().Select(o => o switch
            {
                _ when o.integer() != null => IntegerToOperand(ParseUnsignedInteger.Visit(o.integer())),
                _ when o.@float() != null => new Operand(float.Parse(o.@float().GetText())),
                _ when o.@string() != null => new Operand(UnquoteString(o.GetText()), OperandType.String),
                _ when o.identifier() != null => new Operand(o.GetText(), OperandType.Identifier),
                _ when o.operandSwitchCase() != null => new Operand(((uint)ParseUnsignedInteger.Visit(o.operandSwitchCase().integer()),
                                                                     o.operandSwitchCase().identifier().GetText())),
                _ when o.@operator() != null => ParseOperator(o.@operator()),
                _ => throw new NotImplementedException()
            }).ToArray();

        private Operand ParseOperator(ScAsmParser.OperatorContext op)
            => op switch
            {
                _ when op.K_SIZEOF() != null => OperatorSizeOf(op),
                _ when op.K_OFFSETOF() != null => OperatorOffsetOf(op),
                _ when op.K_HASH() != null => OperatorHash(op),
                _ => throw new NotImplementedException()
            };

        private Operand OperatorSizeOf(ScAsmParser.OperatorContext op)
        {
            Debug.Assert(registry != null);
            Debug.Assert(op.K_SIZEOF() != null);

            var identifiers = op.identifier().Select(id => id.GetText()).ToImmutableArray();

            var type = FindType(identifiers[0]);

            for (int i = 1; i < identifiers.Length; i++)
            {
                if (type is StructType struc)
                {
                    int field = struc.IndexOfField(identifiers[i]);
                    if (field == -1)
                    {
                        throw new InvalidOperationException($"Unknown field '{identifiers[i]}'");
                    }

                    type = struc.Fields[field].Type;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid field '{identifiers[i]}': only structs can have fields");
                }
            }

            return new Operand(type.SizeOf);
        }

        private Operand OperatorOffsetOf(ScAsmParser.OperatorContext op)
        {
            Debug.Assert(registry != null);
            Debug.Assert(op.K_OFFSETOF() != null);

            var identifiers = op.identifier().Select(id => id.GetText()).ToImmutableArray();

            var type = FindType(identifiers[0]);

            uint offset = 0xFFFFFFFF;
            for (int i = 1; i < identifiers.Length; i++)
            {
                if (type is StructType struc)
                {
                    int field = struc.IndexOfField(identifiers[i]);
                    if (field == -1)
                    {
                        throw new InvalidOperationException($"Unknown field '{identifiers[i]}'");
                    }

                    offset = struc.Offsets[field];
                    type = struc.Fields[field].Type;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid field '{identifiers[i]}': only structs can have fields");
                }
            }

            return new Operand(offset);
        }

        private Operand OperatorHash(ScAsmParser.OperatorContext op)
        {
            Debug.Assert(op.K_HASH() != null);

            var str = UnquoteString(op.@string().GetText());

            return new Operand(str.ToLowercaseHash());
        }

        private TypeBase FindType(string name)
            => registry.Types.FindType(name) ??
               registry.FindStaticField(name)?.Type ??
                FindLocalType(name) ??
           throw new InvalidOperationException($"Unknown type, static field, local or function argument '{name}'");

        private TypeBase FindLocalType(string name)
        {
            if (!scope.HasValue)
            {
                return null;
            }

            var s = scope.Value;
            foreach (var f in s.Args)
            {
                if (f.Name == name)
                {
                    return f.Type;
                }
            }

            foreach (var f in s.Locals)
            {
                if (f.Name == name)
                {
                    return f.Type;
                }
            }

            return null;
        }

        private static Operand IntegerToOperand(ulong v)
            => v <= uint.MaxValue ?
                    new Operand(unchecked((uint)v)) :
                    new Operand(v);

        private static string UnquoteString(string str) => str.AsSpan()[1..^1].Unescape();

        public static Operand[] Visit(ScAsmParser.OperandListContext operandList, Registry registry, (ImmutableArray<FieldDefinition> Locals, ImmutableArray<FieldDefinition> Args)? scope = null)
            => operandList?.Accept(new ParseOperands(registry, scope)) ?? throw new ArgumentNullException(nameof(operandList));
    }
}
