namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using Antlr4.Runtime.Misc;
    using System;
    using System.Diagnostics;

    public sealed class ParseInteger : ScAsmBaseVisitor<long>
    {
        public static long Visit(ScAsmParser.IntegerContext integer)
            => integer?.Accept(Instance) ?? throw new ArgumentNullException(nameof(integer));

        private static ParseInteger Instance { get; } = new ParseInteger();

        private ParseInteger() { }

        public override long VisitInteger([NotNull] ScAsmParser.IntegerContext context)
        {
            if (context.DECIMAL_INTEGER() != null)
            {
                string str = context.DECIMAL_INTEGER().GetText();

                return long.Parse(str);
            }
            else
            {
                Debug.Assert(context.HEX_INTEGER() != null);

                string str = context.HEX_INTEGER().GetText();

                return long.Parse(str.AsSpan()[2..], System.Globalization.NumberStyles.HexNumber); // skip '0x' and parse it as hex
            }
        }
    }

    public sealed class ParseUnsignedInteger : ScAsmBaseVisitor<ulong>
    {
        public static ulong Visit(ScAsmParser.IntegerContext integer)
            => integer?.Accept(Instance) ?? throw new ArgumentNullException(nameof(integer));

        private static ParseUnsignedInteger Instance { get; } = new ParseUnsignedInteger();

        private ParseUnsignedInteger() { }

        public override ulong VisitInteger([NotNull] ScAsmParser.IntegerContext context)
        {
            if (context.DECIMAL_INTEGER() != null)
            {
                string str = context.DECIMAL_INTEGER().GetText();

                return str[0] == '-' ?
                        unchecked((ulong)long.Parse(str)) : // has '-', try to parse it as signed int and bit-cast it to unsigned
                        ulong.Parse(str);
            }
            else
            {
                Debug.Assert(context.HEX_INTEGER() != null);

                string str = context.HEX_INTEGER().GetText();

                return ulong.Parse(str.AsSpan()[2..], System.Globalization.NumberStyles.HexNumber); // skip '0x' and parse it as hex
            }
        }
    }
}
