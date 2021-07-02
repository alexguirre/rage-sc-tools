namespace ScTools.ScriptLang.CodeGen
{
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Emits code to push the value of expressions.
    /// </summary>
    public sealed class ValueEmitter : EmptyVisitor
    {
        public CodeGenerator CG { get; }

        public ValueEmitter(CodeGenerator cg) => CG = cg;

        public override Void Visit(BinaryExpression node, Void param)
        {
            node.LHS.Type!.CGBinaryOperation(CG, node);
            return default;
        }

        public override Void Visit(BoolLiteralExpression node, Void param)
        {
            CG.EmitPushConstInt(node.Value ? 1 : 0);
            return default;
        }

        public override Void Visit(FieldAccessExpression node, Void param)
        {
            CG.EmitLoadFrom(node);
            return default;
        }

        public override Void Visit(FloatLiteralExpression node, Void param)
        {
            CG.EmitPushConstFloat(node.Value);
            return default;
        }

        public override Void Visit(IndexingExpression node, Void param)
        {
            CG.EmitLoadFrom(node);
            return default;
        }

        public override Void Visit(IntLiteralExpression node, Void param)
        {
            CG.EmitPushConstInt(node.Value);
            return default;
        }

        public override Void Visit(InvocationExpression node, Void param)
        {
            node.Callee.Type!.CGInvocation(CG, node);
            return default;
        }

        public override Void Visit(NullExpression node, Void param)
        {
            CG.EmitPushConstInt(0);
            return default;
        }

        public override Void Visit(SizeOfExpression node, Void param)
        {
            CG.EmitPushConstInt(node.SubExpression.Type!.SizeOf);
            return default;
        }

        public override Void Visit(StringLiteralExpression node, Void param)
        {
            if (node.Value is null)
            {
                CG.EmitPushConstInt(0);
            }
            else
            {
                CG.EmitPushConstInt(CG.Strings[node.Value]);
                CG.Emit(Opcode.STRING);
            }
            return default;
        }

        public override Void Visit(UnaryExpression node, Void param)
        {
            node.SubExpression.Type!.CGUnaryOperation(CG, node);
            return default;
        }

        public override Void Visit(ValueDeclRefExpression node, Void param)
        {
            if (node.IsLValue)
            {
                CG.EmitLoadFrom(node);
            }
            else
            {
                switch (node.Declaration)
                {
                    case FuncDeclaration { Prototype: { Kind: FuncKind.UserDefined } } func:
                        CG.Emit(Opcode.PUSH_CONST_U24, func.Name);
                        break;
                    case VarDeclaration { Kind: VarKind.Constant } var:
                        CG.EmitValue(var.Initializer!);
                        break;
                    case EnumMemberDeclaration enumMember:
                        CG.EmitPushConstInt(enumMember.Value);
                        break;
                    default: throw new System.NotImplementedException();
                }
            }

            return default;
        }

        public override Void Visit(VectorExpression node, Void param)
        {
            node.X.Accept(this, param);
            node.Y.Accept(this, param);
            node.Z.Accept(this, param);
            return default;
        }
    }
}
