#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Diagnostics;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.AstOld;
    using ScTools.ScriptLang.Semantics.Binding;

    /// <summary>
    /// Evaluates constant expressions.
    /// </summary>
    public class Evaluator
    {
        public Func<Symbols.FunctionSymbol, uint>? FunctionAddressResolver { get; set; }

        public ScriptValue[] DoEvaluate(BoundExpression expr)
        {
            if (!expr.IsConstant)
            {
                throw new ArgumentException("Expression is not constant", nameof(expr));
            }

            if (expr.Type == null)
            {
                throw new ArgumentException("Expression has invalid type", nameof(expr));
            }

            var result = new ScriptValue[expr.Type.SizeOf];
            Evaluate(result, expr);
            return result;
        }

        private void Evaluate(Span<ScriptValue> dest, BoundExpression expr)
        {
            Debug.Assert(dest.Length == expr.Type!.SizeOf);
            Debug.Assert(expr.IsConstant);

            switch (expr)
            {
                case BoundIntLiteralExpression x: EvaluateIntLiteral(dest, x); break;
                case BoundFloatLiteralExpression x: EvaluateFloatLiteral(dest, x); break;
                case BoundBoolLiteralExpression x: EvaluateBoolLiteral(dest, x); break;
                case BoundVectorExpression x: EvaluateVector(dest, x); break;
                case BoundUnaryExpression x: EvaluateUnary(dest, x); break;
                case BoundBinaryExpression x: EvaluateBinary(dest, x); break;
                case BoundVariableExpression x:
                    if (x.Var.Initializer == null)
                    {
                        throw new ArgumentException($"Unresolved constant '{x.Var.Name}'", nameof(expr));
                    }

                    Evaluate(dest, x.Var.Initializer);
                    break;
                case BoundFunctionExpression x:
                    if (FunctionAddressResolver == null)
                    {
                        throw new InvalidOperationException($"Cannot get address of '{x.Function.Name}'. No function address resolver provided.");
                    }

                    dest[0].AsUInt32 = FunctionAddressResolver(x.Function);
                    break;
                default: throw new NotImplementedException();
            }
        }

        private void EvaluateIntLiteral(Span<ScriptValue> dest, BoundIntLiteralExpression expr)
        {
            dest[0].AsInt32 = expr.Value;
        }

        private void EvaluateFloatLiteral(Span<ScriptValue> dest, BoundFloatLiteralExpression expr)
        {
            dest[0].AsFloat = expr.Value;
        }

        private void EvaluateBoolLiteral(Span<ScriptValue> dest, BoundBoolLiteralExpression expr)
        {
            dest[0].AsInt64 = expr.Value ? 1 : 0;
        }

        private void EvaluateVector(Span<ScriptValue> dest, BoundVectorExpression expr)
        {
            foreach(var comp in new[] { expr.X, expr.Y, expr.Z })
            {
                var size = comp.Type!.SizeOf;
                var subDest = dest[0..size];
                Evaluate(subDest, comp);
                dest = dest[size..];
            }
        }

        private void EvaluateUnary(Span<ScriptValue> dest, BoundUnaryExpression expr)
        {
            Evaluate(dest, expr.Operand);
            switch (expr.Operand.Type)
            {
                case BasicType { TypeCode: BasicTypeCode.Float }:
                    dest[0].AsFloat = expr.Op switch
                    {
                        UnaryOperator.Negate => -dest[0].AsFloat,
                        _ => throw new NotSupportedException(),
                    };
                    break;
                case BasicType { TypeCode: BasicTypeCode.Int }:
                    dest[0].AsInt32 = expr.Op switch
                    {
                        UnaryOperator.Negate => -dest[0].AsInt32,
                        _ => throw new NotSupportedException(),
                    };
                    break;
                case BasicType { TypeCode: BasicTypeCode.Bool }:
                    dest[0].AsInt64 = expr.Op switch
                    {
                        UnaryOperator.Not => dest[0].AsInt64 == 0,
                        _ => throw new NotSupportedException(),
                    } ? 1 : 0;
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void EvaluateBinary(Span<ScriptValue> dest, BoundBinaryExpression expr)
        {
            var left = Evaluate(expr.Left);
            var right = Evaluate(expr.Right);
            var operandsType = expr.Left.Type;
            var resultType = expr.Type;
            switch (operandsType)
            {
                case BasicType { TypeCode: BasicTypeCode.Float }:
                    switch (resultType)
                    {
                        case BasicType { TypeCode: BasicTypeCode.Float }:
                            dest[0].AsFloat = expr.Op switch
                            {
                                BinaryOperator.Add => left[0].AsFloat + right[0].AsFloat,
                                BinaryOperator.Subtract => left[0].AsFloat - right[0].AsFloat,
                                BinaryOperator.Multiply => left[0].AsFloat * right[0].AsFloat,
                                BinaryOperator.Divide => left[0].AsFloat / right[0].AsFloat,
                                BinaryOperator.Modulo => left[0].AsFloat % right[0].AsFloat,
                                _ => throw new NotSupportedException(),
                            };
                            break;
                        case BasicType { TypeCode: BasicTypeCode.Bool }:
                            dest[0].AsInt64 = expr.Op switch
                            {
                                BinaryOperator.Equal => left[0].AsFloat == right[0].AsFloat,
                                BinaryOperator.NotEqual => left[0].AsFloat != right[0].AsFloat,
                                BinaryOperator.Greater => left[0].AsFloat > right[0].AsFloat,
                                BinaryOperator.GreaterOrEqual => left[0].AsFloat >= right[0].AsFloat,
                                BinaryOperator.Less => left[0].AsFloat < right[0].AsFloat,
                                BinaryOperator.LessOrEqual => left[0].AsFloat <= right[0].AsFloat,
                                _ => throw new NotSupportedException(),
                            } ? 1 : 0;
                            break;
                        default: throw new NotSupportedException();
                    }
                    break;
                case BasicType { TypeCode: BasicTypeCode.Int }:
                    switch (resultType)
                    {
                        case BasicType { TypeCode: BasicTypeCode.Int }:
                            dest[0].AsInt32 = expr.Op switch
                            {
                                BinaryOperator.Add => left[0].AsInt32 + right[0].AsInt32,
                                BinaryOperator.Subtract => left[0].AsInt32 - right[0].AsInt32,
                                BinaryOperator.Multiply => left[0].AsInt32 * right[0].AsInt32,
                                BinaryOperator.Divide => left[0].AsInt32 / right[0].AsInt32,
                                BinaryOperator.Modulo => left[0].AsInt32 % right[0].AsInt32,
                                BinaryOperator.And => left[0].AsInt32 & right[0].AsInt32,
                                BinaryOperator.Or => left[0].AsInt32 | right[0].AsInt32,
                                BinaryOperator.Xor => left[0].AsInt32 ^ right[0].AsInt32,
                                _ => throw new NotSupportedException(),
                            };
                            break;
                        case BasicType { TypeCode: BasicTypeCode.Bool }:
                            dest[0].AsInt64 = expr.Op switch
                            {
                                BinaryOperator.Equal => left[0].AsInt32 == right[0].AsInt32,
                                BinaryOperator.NotEqual => left[0].AsInt32 != right[0].AsInt32,
                                BinaryOperator.Greater => left[0].AsInt32 > right[0].AsInt32,
                                BinaryOperator.GreaterOrEqual => left[0].AsInt32 >= right[0].AsInt32,
                                BinaryOperator.Less => left[0].AsInt32 < right[0].AsInt32,
                                BinaryOperator.LessOrEqual => left[0].AsInt32 <= right[0].AsInt32,
                                _ => throw new NotSupportedException(),
                            } ? 1 : 0;
                            break;
                        default: throw new NotSupportedException();
                    }
                    break;
                case BasicType { TypeCode: BasicTypeCode.Bool }:
                    dest[0].AsInt64 = expr.Op switch
                    {
                        BinaryOperator.LogicalAnd => (left[0].AsInt64 != 0) && (right[0].AsInt64 != 0),
                        BinaryOperator.LogicalOr => (left[0].AsInt64 != 0) || (right[0].AsInt64 != 0),
                        BinaryOperator.Equal => left[0].AsInt64 == right[0].AsInt64,
                        BinaryOperator.NotEqual => left[0].AsInt64 != right[0].AsInt64,
                        _ => throw new NotSupportedException(),
                    } ? 1 : 0;
                    break;
                default: throw new NotSupportedException();
            }
        }

        public static ScriptValue[] Evaluate(BoundExpression expr)
            => new Evaluator().DoEvaluate(expr);

        public static ScriptValue[] Evaluate(BoundExpression expr, Func<Symbols.FunctionSymbol, uint> functionAddressResolver)
            => new Evaluator { FunctionAddressResolver = functionAddressResolver }.DoEvaluate(expr);
    }
}
