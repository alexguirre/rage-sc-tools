namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using Antlr4.Runtime.Misc;
    using System;
    using System.Diagnostics;

    public sealed class ParseFloat : ScAsmBaseVisitor<float>
    {
        public static float Visit(ScAsmParser.FloatContext floatContext)
            => floatContext?.Accept(Instance) ?? throw new ArgumentNullException(nameof(floatContext));

        private static ParseFloat Instance { get; } = new ParseFloat();

        private ParseFloat() { }

        public override float VisitFloat([NotNull] ScAsmParser.FloatContext context)
            => float.Parse(context.FLOAT().GetText());
    }
}
