namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using System;
    using System.Diagnostics;

    public sealed class ParseFieldDecl : ScAsmBaseVisitor<(string Name, string TypeName, TypeDefinition Type)>
    {
        private readonly Registry registry;

        private ParseFieldDecl(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override (string Name, string TypeName, TypeDefinition Type) VisitFieldDecl([NotNull] ScAsmParser.FieldDeclContext context)
        {
            var t = context.type();
            var arrayType = t.type();
            bool isArray = arrayType != null;
            long arrayLength = isArray ? ParseInteger.Visit(t.integer()) : 0;

            if (arrayLength < 0)
            {
                throw new InvalidOperationException("Array length is negative");
            }

            var typeName = isArray ? arrayType.identifier().GetText() : t.identifier().GetText();
            var typeDef = isArray ? registry.FindOrRegisterArray(typeName, (uint)arrayLength) : registry.FindType(typeName);
            return (context.identifier().GetText(), typeName, typeDef);
        }

        public static (string Name, string TypeName, TypeDefinition Type) Visit(ScAsmParser.FieldDeclContext fieldDecl, Registry registry)
            => fieldDecl?.Accept(new ParseFieldDecl(registry)) ?? throw new ArgumentNullException(nameof(fieldDecl));
    }
}
