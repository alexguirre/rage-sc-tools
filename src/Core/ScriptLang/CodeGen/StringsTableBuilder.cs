namespace ScTools.ScriptLang.CodeGen
{
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class StringsTableBuilder : DFSVisitor
    {
        public StringsTable Strings { get; } = new();

        public override Void Visit(Program node, Void param)
        {
            // TODO: should static STRING vars be allowed to be initialized?
            //   would require to insert some code to initialize them in MAIN() as the strings are dynamically allocated

            // only collect strings inside function bodies
            node.Declarations.OfType<FuncDeclaration>().ForEach(decl => decl.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(FuncDeclaration node, Void param)
        {
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(StringLiteralExpression node, Void param)
        {
            if (node.Value is not null)
            {
                Strings.Add(node.Value);
            }
            return DefaultReturn;
        }

        public static StringsTable Build(Program root)
        {
            var visitor = new StringsTableBuilder();
            root.Accept(visitor, default);
            return visitor.Strings;
        }
    }
}
