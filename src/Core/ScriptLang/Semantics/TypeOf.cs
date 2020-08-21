//#nullable enable
//namespace ScTools.ScriptLang.Semantics
//{
//    using System;

//    using ScTools.ScriptLang.Ast;
//    using ScTools.ScriptLang.Semantics.Symbols;

//    public static class TypeOf
//    {
//        private const string DummyFile = "TODO-REPLACEME.sc";

//        public static bool Variable(VariableDeclaration declaration, out TypeInfo type, DiagnosticsReport? diagnostics = null)
//        {
//            type = default;

//            int arraySize = 0;
//            if (declaration.ArrayRank != null)
//            {
//                var size = Evaluator.Evaluate(declaration.ArrayRank.Expression);

//                if (!size.HasValue)
//                {
//                    diagnostics?.AddError(DummyFile, "The array size is not a valid constant int expression.", declaration.ArrayRank.Expression.Source);
//                    return false;
//                }

//                if (size.Value.IsFloat)
//                {
//                    diagnostics?.AddError(DummyFile, "The array size expression is a float, must be an int expression.", declaration.ArrayRank.Expression.Source);
//                    return false;
//                }

//                arraySize = size.Value.IntValue;
//            }

//            type = new TypeInfo(declaration.Type.Name.Name, declaration.Type.IsReference, arraySize);
//            return true;
//        }

//        public static bool Expression(Expression expr, Scope scope, out TypeInfo type, DiagnosticsReport? diagnostics = null)
//            => expr switch
//            {
//                UnaryExpression e => ExpressionUnary(e, scope, out type, diagnostics),
//                BinaryExpression e => ExpressionBinary(e, scope, out type, diagnostics),
//                LiteralExpression e => (type = ExpressionLiteral(e), result: true).result,
//                IdentifierExpression e => ExpressionIdentifier(e, scope, out type, diagnostics),
//                ParenthesizedExpression e => Expression(e.Inner, scope, out type, diagnostics),
//                AggregateExpression e => throw new NotImplementedException(nameof(AggregateExpression)),
//                MemberAccessExpression e => throw new NotImplementedException(nameof(MemberAccessExpression)),
//                ArrayAccessExpression e => throw new NotImplementedException(nameof(ArrayAccessExpression)),
//                InvocationExpression e => throw new NotImplementedException(nameof(InvocationExpression)),
//                _ => throw new NotImplementedException(expr.GetType().FullName),
//            };

//        public static bool ExpressionIdentifier(IdentifierExpression expr, Scope scope, out TypeInfo type, DiagnosticsReport? diagnostics = null)
//        {
//            type = default;
//            if (!scope.TryFind(expr.Identifier.Name, out var symbol))
//            {
//                diagnostics?.AddError(DummyFile, $"Unknown symbol `{expr.Identifier.Name}`", expr.Source);
//                return false;
//            }

//            var decl = symbol switch
//            {
//                LocalSymbol s => s.AstNode.Declaration,
//                ParameterSymbol s => s.AstNode,
//                StaticVariableSymbol s => s.AstNode.Declaration,
//                _ => throw new NotImplementedException()
//            };

//            return Variable(decl, out type, diagnostics);
//        }

//        public static bool ExpressionUnary(UnaryExpression expr, Scope scope, out TypeInfo type, DiagnosticsReport? diagnostics = null)
//        {
//            type = default;
//            if (!Expression(expr.Operand, scope, out var operandType, diagnostics))
//            {
//                return false;
//            }

//            if (operandType.IsArray)
//            {
//                diagnostics?.AddError(DummyFile,
//                                      $"Cannot use operator `{UnaryExpression.OpToString(expr.Op)}` with an array",
//                                      expr.Operand.Source);
//                return false;
//            }

//            var requiredCapabilities = OpToCapabilities(expr.Op);

//            var capable = operandType.Capabilities.HasFlag(requiredCapabilities);
//            if (!capable)
//            {
//                diagnostics?.AddError(DummyFile,
//                                      $"Cannot use operator `{UnaryExpression.OpToString(expr.Op)}` with type `{operandType.Name}`",
//                                      expr.Operand.Source);
//                return false;
//            }

//            type = operandType;
//            return true;

//            static TypeCapabilities OpToCapabilities(UnaryOperator op) => op switch
//            {
//                UnaryOperator.Not => TypeCapabilities.Not,
//                UnaryOperator.Negate => TypeCapabilities.Negate,
//                _ => throw new NotImplementedException()
//            };
//        }

//        public static bool ExpressionBinary(BinaryExpression expr, Scope scope, out TypeInfo type, DiagnosticsReport? diagnostics = null)
//        {
//            type = default;
//            if (!Expression(expr.Left, scope, out var leftType, diagnostics))
//            {
//                return false;
//            }
            
//            if (!Expression(expr.Right, scope, out var rightType, diagnostics))
//            {
//                return false;
//            }

//            if (leftType.IsArray || rightType.IsArray)
//            {
//                if (diagnostics != null)
//                {
//                    var msg = $"Cannot use operator `{BinaryExpression.OpToString(expr.Op)}` with an array";

//                    if (leftType.IsArray)
//                    {
//                        diagnostics.AddError(DummyFile, msg, expr.Left.Source);
//                    }

//                    if (rightType.IsArray)
//                    {
//                        diagnostics.AddError(DummyFile, msg, expr.Right.Source);
//                    }
//                }
//                return false;
//            }

//            var requiredCapabilities = OpToCapabilities(expr.Op);

//            var leftCapable = leftType.Capabilities.HasFlag(requiredCapabilities);
//            var rightCapable = rightType.Capabilities.HasFlag(requiredCapabilities);
//            if (!leftCapable || !rightCapable)
//            {
//                if (diagnostics != null)
//                {
//                    var msg = $"Cannot use operator `{BinaryExpression.OpToString(expr.Op)}` with type";

//                    if (!leftCapable)
//                    {
//                        diagnostics.AddError(DummyFile, $"{msg} `{leftType.Name}`", expr.Left.Source);
//                    }

//                    if (!rightCapable)
//                    {
//                        diagnostics.AddError(DummyFile, $"{msg} `{rightType.Name}`", expr.Right.Source);
//                    }
//                }
//                return false;
//            }

//            // TODO: move these constants somewhere else
//            const string INT = "INT";
//            const string FLOAT = "FLOAT";
//            const string VEC3 = "VEC3";

//            type = (leftType.Name, rightType.Name) switch
//            {
//                (INT, INT) => TypeInfo.Int,
//                (INT, FLOAT) => TypeInfo.Float,
//                (FLOAT, INT) => TypeInfo.Float,
//                (FLOAT, FLOAT) => TypeInfo.Float,
//                (VEC3, VEC3) => TypeInfo.Vec3,
//                _ => default,
//            };

//            var success = type.Name != null;

//            if (!success)
//            {
//                diagnostics?.AddError(DummyFile,
//                                      $"Cannot use operator `{BinaryExpression.OpToString(expr.Op)}` between types `{leftType.Name}` and `{rightType.Name}`",
//                                      expr.Source);
//            }

//            return success;

//            static TypeCapabilities OpToCapabilities(BinaryOperator op) => op switch
//            {
//                BinaryOperator.Add => TypeCapabilities.Add,
//                BinaryOperator.Subtract => TypeCapabilities.Subtract,
//                BinaryOperator.Multiply => TypeCapabilities.Multiply,
//                BinaryOperator.Divide => TypeCapabilities.Divide,
//                BinaryOperator.Modulo => TypeCapabilities.Modulo,
//                BinaryOperator.Or => TypeCapabilities.BitwiseOr,
//                BinaryOperator.And => TypeCapabilities.BitwiseAnd,
//                BinaryOperator.Xor => TypeCapabilities.BitwiseXor,
//                _ => throw new NotImplementedException()
//            };
//        }

//        public static TypeInfo ExpressionLiteral(LiteralExpression expr)
//            => expr.Kind switch
//            {
//                LiteralKind.Numeric => int.TryParse(expr.ValueText, out _) ? TypeInfo.Int : TypeInfo.Float,
//                LiteralKind.Bool => TypeInfo.Bool,
//                LiteralKind.String => throw new NotImplementedException(),
//                _ => throw new NotImplementedException(),
//            };
//    }
//}
