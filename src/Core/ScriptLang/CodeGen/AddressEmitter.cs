namespace ScTools.ScriptLang.CodeGen
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Emits code to push the address of lvalue expressions.
    /// </summary>
    internal sealed class AddressEmitter : Visitor
    {
        private readonly CodeEmitter _C;

        public AddressEmitter(CodeEmitter codeEmitter) => _C = codeEmitter;

        public override void Visit(NameExpression node)
        {
            Debug.Assert(node.Semantics.Declaration is VarDeclaration); // VarDeclaration are the only declarations that can be lvalues
            var varDecl = (VarDeclaration)node.Semantics.Declaration!;
            _C.EmitVarAddress(varDecl);
        }

        public override void Visit(FieldAccessExpression node)
        {
            var field = node.Semantics.Field;
            Debug.Assert(field is not null);
            _C.EmitAddress(node.SubExpression);
            _C.EmitOffset(field.Offset);
        }

        public override void Visit(IndexingExpression node)
        {
            _C.EmitArrayIndexing(node);
        }
    }
}
