namespace ScTools.ScriptLang.Semantics
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Types;

    public sealed class TypeResolvingVisitor : DFSVisitor<Void, Void>
    {
        public override Void DefaultReturn => default;

        public override Void Visit(NamedType node, Void param)
        {
            Debug.Assert(node.ResolvedType is null); // verify we are not visiting the same node multiple times

            System.Console.WriteLine(new Diagnostic(DiagnosticTag.Warning, node.Name, node.Source));
            // TODO: proper type resolving
            if (node.Name.ToUpperInvariant() == "INT")
            {
                node.ResolvedType = new IntType(node.Source);
            }

            return DefaultReturn;
        }
    }
}
