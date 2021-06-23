namespace ScTools.ScriptLang.CodeGen
{
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Emits code to push the address of lvalue expressions.
    /// </summary>
    public sealed class AddressEmitter : EmptyVisitor
    {
        public CodeGenerator CG { get; }

        public AddressEmitter(CodeGenerator cg) => CG = cg;

        public override Void Visit(FieldAccessExpression node, Void param) => default;
        public override Void Visit(IndexingExpression node, Void param) => default;
        public override Void Visit(ValueDeclRefExpression node, Void param) => default;
    }
}
