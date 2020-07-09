namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using Antlr4.Runtime.Misc;
    using System;
    using System.Linq;

    public sealed class ParseOperands : ScAsmBaseVisitor<Operand[]>
    {
        public static Operand[] Visit(ScAsmParser.OperandListContext operandList)
            => operandList?.Accept(Instance) ?? throw new ArgumentNullException(nameof(operandList));

        public static ParseOperands Instance { get; } = new ParseOperands();

        public override Operand[] VisitOperandList([NotNull] ScAsmParser.OperandListContext context)
            => context.operand().Select(o => o switch
            {
                _ when o.integer() != null => IntegerToOperand(ParseUnsignedInteger.Visit(o.integer())),
                _ when o.@float() != null => new Operand(float.Parse(o.@float().GetText())),
                _ when o.@string() != null => new Operand(o.GetText().AsSpan()[1..^1].Unescape(), OperandType.String),
                _ when o.identifier() != null => new Operand(o.GetText(), OperandType.Identifier),
                _ when o.operandSwitchCase() != null => new Operand(((uint)ParseUnsignedInteger.Visit(o.operandSwitchCase().integer()),
                                                                     o.operandSwitchCase().identifier().GetText())),
                _ => throw new InvalidOperationException()
            }).ToArray();

        private static Operand IntegerToOperand(ulong v)
            => v < uint.MaxValue ?
                    new Operand(unchecked((uint)v)) :
                    new Operand(v);
    }
}
