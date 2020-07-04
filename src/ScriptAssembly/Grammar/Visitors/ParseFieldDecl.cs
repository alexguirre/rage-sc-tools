namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using System;

    public sealed class ParseFieldDecl : ScAsmBaseVisitor<(string Name, string TypeName, TypeDefinition Type)>
    {
        private readonly Registry registry;

        private ParseFieldDecl(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override (string Name, string TypeName, TypeDefinition Type) VisitFieldDecl([NotNull] ScAsmParser.FieldDeclContext context)
        {
            var (typeName, typeDef) = ParseType.Visit(context.type(), registry);
            return (context.identifier().GetText(), typeName, typeDef);
        }

        public static (string Name, string TypeName, TypeDefinition Type) Visit(ScAsmParser.FieldDeclContext fieldDecl, Registry registry)
            => fieldDecl?.Accept(new ParseFieldDecl(registry)) ?? throw new ArgumentNullException(nameof(fieldDecl));
    }
}
