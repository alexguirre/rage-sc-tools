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

        public static float EvalFloat(IExpression expression, GlobalSymbolTable symbols)
            => expression.Accept(new FloatEvaluator(symbols), default);

        public static bool EvalBool(IExpression expression, GlobalSymbolTable symbols)
            => expression.Accept(new BoolEvaluator(symbols), default);

        public static string? EvalString(IExpression expression, GlobalSymbolTable symbols)
            => expression.Accept(new StringEvaluator(symbols), default);

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
                if (node.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
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

        private sealed class FloatEvaluator : EmptyVisitor<float, Void>
        {
            public GlobalSymbolTable Symbols { get; }

            public FloatEvaluator(GlobalSymbolTable symbols) => Symbols = symbols;

            public override float Visit(BinaryExpression node, Void param)
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
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override float Visit(FloatLiteralExpression node, Void param)
                => node.Value;

            public override float Visit(NullExpression node, Void param)
                => 0.0f;

            public override float Visit(UnaryExpression node, Void param)
            {
                var v = node.SubExpression.Accept(this, param);
                return node.Operator switch
                {
                    UnaryOperator.Negate => -v,
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override float Visit(ValueDeclRefExpression node, Void param)
            {
                if (node.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
                {
                    return varDecl.Initializer!.Accept(this, param);
                }

                throw new System.NotImplementedException();
            }
        }

        private sealed class BoolEvaluator : EmptyVisitor<bool, Void>
        {
            public GlobalSymbolTable Symbols { get; }

            public BoolEvaluator(GlobalSymbolTable symbols) => Symbols = symbols;

            public override bool Visit(BinaryExpression node, Void param)
            {
                var lhs = node.LHS.Accept(this, param);
                var rhs = node.RHS.Accept(this, param);
                return node.Operator switch
                {
                    BinaryOperator.LogicalAnd => lhs && rhs,
                    BinaryOperator.LogicalOr => lhs || rhs,
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override bool Visit(BoolLiteralExpression node, Void param)
                => node.Value;

            public override bool Visit(NullExpression node, Void param)
                => false;

            public override bool Visit(UnaryExpression node, Void param)
            {
                var v = node.SubExpression.Accept(this, param);
                return node.Operator switch
                {
                    UnaryOperator.LogicalNot => !v,
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override bool Visit(ValueDeclRefExpression node, Void param)
            {
                if (node.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
                {
                    return varDecl.Initializer!.Accept(this, param);
                }

                throw new System.NotImplementedException();
            }
        }

        private sealed class StringEvaluator : EmptyVisitor<string?, Void>
        {
            public GlobalSymbolTable Symbols { get; }

            public StringEvaluator(GlobalSymbolTable symbols) => Symbols = symbols;

            public override string? Visit(StringLiteralExpression node, Void param)
                => node.Value;

            public override string? Visit(NullExpression node, Void param)
                => null;

            public override string? Visit(ValueDeclRefExpression node, Void param)
            {
                if (node.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
                {
                    return varDecl.Initializer!.Accept(this, param);
                }

                throw new System.NotImplementedException();
            }
        }
    }
}
