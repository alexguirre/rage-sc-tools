namespace ScTools.ScriptLang.Semantics
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Types;

    public sealed class TypeResolvingVisitor : DFSVisitor<Void, Void>
    {
        public override Void DefaultReturn => default;

        public DiagnosticsReport Diagnostics { get; }
        public SymbolTable Symbols { get; }

        public TypeResolvingVisitor(DiagnosticsReport diagnostics, SymbolTable symbols)
            => (Diagnostics, Symbols) = (diagnostics, symbols);

        public override Void Visit(NamedType node, Void param)
        {
            Debug.Assert(node.ResolvedType is null); // verify we are not visiting the same node multiple times

            var typeDecl = Symbols.FindTypeDecl(node.Name);
            if (typeDecl is null)
            {
                Diagnostics.AddError($"Unknown type '{node.Name}'", node.Source);
            }
            else
            {
                node.ResolvedType = typeDecl.CreateType(node.Source);
            }

            return DefaultReturn;
        }
    }
}
