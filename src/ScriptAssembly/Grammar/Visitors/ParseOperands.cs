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
            => (op, Identifiers: op.identifier().Select(id => id.GetText()).ToImmutableArray()) switch
            {
                var x when op.K_SIZEOF() != null => OperatorSizeOf(x.Identifiers.AsSpan()),
                var x when op.K_OFFSETOF() != null => OperatorOffsetOf(x.Identifiers.AsSpan()),
                var x when op.K_ITEMSIZEOF() != null => OperatorItemSizeOf(x.Identifiers.AsSpan()),
                var x when op.K_LENGTHOF() != null => OperatorLengthOf(x.Identifiers.AsSpan()),
                _ when op.K_HASH() != null => OperatorHash(UnquoteString(op.@string().GetText())),
                _ => throw new NotImplementedException()
            };

        private Operand OperatorSizeOf(ReadOnlySpan<string> identifiers)
        {
            Debug.Assert(registry != null);

            var type = AccessTypeOrVariable(identifiers).Type;

            return new Operand(type.SizeOf);
        }

        private Operand OperatorOffsetOf(ReadOnlySpan<string> identifiers)
        {
            Debug.Assert(registry != null);
            Debug.Assert(identifiers.Length > 1);

            var offset = AccessTypeOrVariable(identifiers).Offset;

            return new Operand(offset);
        }

        private Operand OperatorItemSizeOf(ReadOnlySpan<string> identifiers)
        {
            Debug.Assert(registry != null);

            var type = AccessVariable(identifiers).Type;

            return type is ArrayType arr ?
                    new Operand(arr.ItemType.SizeOf) :
                    throw new InvalidOperationException($"'{string.Join('.', identifiers.ToArray())}' is not an array");
        }

        private Operand OperatorLengthOf(ReadOnlySpan<string> identifiers)
        {
            Debug.Assert(registry != null);

            var type = AccessVariable(identifiers).Type;

            return type is ArrayType arr ?
                    new Operand(arr.Length) :
                    throw new InvalidOperationException($"'{string.Join('.', identifiers.ToArray())}' is not an array");
        }

        private Operand OperatorHash(string str) => new Operand(str.ToLowercaseHash());

        private TypeBase FindTypeOrVariableType(string name)
            => registry.Types.FindType(name) ??
               registry.FindStaticField(name)?.Type ??
               FindLocalType(name) ??
               throw new InvalidOperationException($"Unknown type, static field, local or function argument '{name}'");

        private TypeBase FindVariableType(string name)
            => registry.FindStaticField(name)?.Type ??
               FindLocalType(name) ??
               throw new InvalidOperationException($"Unknown static field, local or function argument '{name}'");

        private (TypeBase Type, uint Offset) AccessTypeOrVariable(ReadOnlySpan<string> identifiers)
            => AccessFields(FindTypeOrVariableType(identifiers[0]), identifiers[1..]);

        private (TypeBase Type, uint Offset) AccessVariable(ReadOnlySpan<string> identifiers)
            => AccessFields(identifiers.Length == 1 ? FindVariableType(identifiers[0]) : FindTypeOrVariableType(identifiers[0]),
                            identifiers[1..]);

        private (TypeBase Type, uint Offset) AccessFields(TypeBase type, ReadOnlySpan<string> identifiers)
        {
            uint offset = 0xFFFFFFFF;
            for (int i = 0; i < identifiers.Length; i++)
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

            return (type, offset);
        }

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
