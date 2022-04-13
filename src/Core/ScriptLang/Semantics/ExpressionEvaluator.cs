namespace ScTools.ScriptLang.Semantics
{
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.BuiltIns;
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

        public static (float X, float Y, float Z) EvalVector(IExpression expression, GlobalSymbolTable symbols)
            => expression.Accept(new VectorEvaluator(symbols), default);

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

            public override int Visit(InvocationExpression node, Void param)
            {
                if (node.Callee is NameExpression { Semantics.Declaration: IIntrinsicDeclaration intrin })
                {
                    return intrin.EvalInt(node, Symbols);
                }

                throw new System.NotImplementedException();
            }

            public override int Visit(NameExpression node, Void param)
            {
                var decl = node.Semantics.Declaration;
                if (decl is VarDeclaration { Kind: VarKind.Constant } varDecl)
                {
                    return varDecl.Initializer!.Accept(this, param);
                }
                else if (decl is EnumMemberDeclaration enumMember)
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

            public override float Visit(InvocationExpression node, Void param)
            {
                if (node.Callee is NameExpression { Semantics.Declaration: IIntrinsicDeclaration intrin })
                {
                    return intrin.EvalFloat(node, Symbols);
                }

                throw new System.NotImplementedException();
            }

            public override float Visit(NameExpression node, Void param)
            {
                if (node.Semantics.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
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

            public override bool Visit(InvocationExpression node, Void param)
            {
                if (node.Callee is NameExpression { Semantics.Declaration: IIntrinsicDeclaration intrin })
                {
                    return intrin.EvalBool(node, Symbols);
                }

                throw new System.NotImplementedException();
            }

            public override bool Visit(NameExpression node, Void param)
            {
                if (node.Semantics.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
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

            public override string Visit(InvocationExpression node, Void param)
            {
                if (node.Callee is NameExpression { Semantics.Declaration: IIntrinsicDeclaration intrin })
                {
                    return intrin.EvalString(node, Symbols);
                }

                throw new System.NotImplementedException();
            }

            public override string? Visit(NameExpression node, Void param)
            {
                if (node.Semantics.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
                {
                    return varDecl.Initializer!.Accept(this, param);
                }

                throw new System.NotImplementedException();
            }
        }

        private sealed class VectorEvaluator : EmptyVisitor<(float X, float Y, float Z), Void>
        {
            private readonly FloatEvaluator floatEval;

            public GlobalSymbolTable Symbols { get; }

            public VectorEvaluator(GlobalSymbolTable symbols) => (floatEval, Symbols) = (new(symbols), symbols);

            public override (float X, float Y, float Z) Visit(BinaryExpression node, Void param)
            {
                var (x1, y1, z1) = node.LHS.Accept(this, param);
                var (x2, y2, z2) = node.RHS.Accept(this, param);
                return node.Operator switch
                {
                    BinaryOperator.Add => (x1 + x2, y1 + y2, z1 + z2),
                    BinaryOperator.Subtract => (x1 - x2, y1 - y2, z1 - z2),
                    BinaryOperator.Multiply => (x1 * x2, y1 * y2, z1 * z2),
                    BinaryOperator.Divide => (x1 / x2, y1 / y2, z1 / z2),
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override (float X, float Y, float Z) Visit(VectorExpression node, Void param)
                => (node.X.Accept(floatEval, param), node.Y.Accept(floatEval, param), node.Z.Accept(floatEval, param));

            public override (float X, float Y, float Z) Visit(UnaryExpression node, Void param)
            {
                var (x, y, z) = node.SubExpression.Accept(this, param);
                return node.Operator switch
                {
                    UnaryOperator.Negate => (-x, -y, -z),
                    _ => throw new System.NotImplementedException(),
                };
            }

            public override (float X, float Y, float Z) Visit(InvocationExpression node, Void param)
            {
                if (node.Callee is NameExpression { Semantics.Declaration: IIntrinsicDeclaration intrin })
                {
                    return intrin.EvalVector(node, Symbols);
                }

                throw new System.NotImplementedException();
            }

            public override (float X, float Y, float Z) Visit(NameExpression node, Void param)
            {
                if (node.Semantics.Declaration is VarDeclaration { Kind: VarKind.Constant } varDecl)
                {
                    return varDecl.Initializer!.Accept(this, param);
                }

                throw new System.NotImplementedException();
            }
        }
    }
}
