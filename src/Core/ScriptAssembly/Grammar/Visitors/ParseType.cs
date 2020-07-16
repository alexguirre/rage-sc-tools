namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using ScTools.ScriptAssembly.Types;
    using System;

    public sealed class ParseType : ScAsmBaseVisitor<(string TypeName, TypeBase Type)>
    {
        private readonly Registry registry;

        private ParseType(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override (string TypeName, TypeBase Type) VisitType([NotNull] ScAsmParser.TypeContext context)
        {
            var arrayType = context.type();
            bool isArray = arrayType != null;
            long arrayLength = isArray ? ParseInteger.Visit(context.integer()) : 0;

            if (arrayLength < 0)
            {
                throw new InvalidOperationException("Array length is negative");
            }

            var typeName = isArray ? arrayType.identifier().GetText() : context.identifier().GetText();
            var typeDef = isArray ? registry.Types.FindOrRegisterArray(typeName, (uint)arrayLength) : registry.Types.FindType(typeName);
            return (typeName, typeDef);
        }

        public static (string TypeName, TypeBase Type) Visit(ScAsmParser.TypeContext type, Registry registry)
            => type?.Accept(new ParseType(registry)) ?? throw new ArgumentNullException(nameof(type));
    }
}
