namespace ScTools.ScriptLang.Semantics
{
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.SymbolTables;

    public static class ExpressionEvaluator
    {
        public static int EvalInt(IExpression expression, GlobalSymbolTable symbols)
            => expression.Accept(new IntEvaluator(symbols), default);

        private sealed class IntEvaluator : EmptyVisitor<int, Void>
        {
            public GlobalSymbolTable Symbols { get; }

            public IntEvaluator(GlobalSymbolTable symbols) => Symbols = symbols;

            public override int Visit(BinaryExpression node, Void param)
            {
                var lhs = node.LHS.Accept(this, param);
                var rhs = node.RHS.Accept(this, param);
                return node.Operator switch
                {
                    BinaryOperator.Add => lhs + rhs,
                    BinaryOperator.Subtract => lhs - rhs,
                    BinaryOperator.Multiply => lhs * rhs,
                    BinaryOperator.Divide => lhs / rhs,
                    BinaryOperator.Modulo => lhs % rhs,
                    BinaryOperator.And => lhs & rhs,
                    BinaryOperator.Xor => lhs ^ rhs,
                    BinaryOperator.Or => lhs | rhs,
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override int Visit(IntLiteralExpression node, Void param)
                => node.Value;

            public override int Visit(NullExpression node, Void param)
                => 0;

            public override int Visit(UnaryExpression node, Void param)
            {
                var v = node.SubExpression.Accept(this, param);
                return node.Operator switch
                {
                    UnaryOperator.Negate => -v,
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override int Visit(ValueDeclRefExpression node, Void param)
            {
                if (node.Declaration is VarDeclaration { Kind: not VarKind.Constant } varDecl)
                {
                    return varDecl.Initializer!.Accept(this, param);
                }
                else if (node.Declaration is EnumMemberDeclaration enumMember)
                {
                    return enumMember.Value;
                }

                throw new System.NotImplementedException();
            }
        }
    }
}
