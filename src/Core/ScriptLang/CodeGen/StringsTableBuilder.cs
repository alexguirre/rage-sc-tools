namespace ScTools.ScriptLang.CodeGen
{
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class StringsTableBuilder : DFSVisitor
    {
        public StringsTable Strings { get; } = new();

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
