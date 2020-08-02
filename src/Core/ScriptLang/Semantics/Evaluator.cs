#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;

    using ScTools.ScriptLang.Ast;

    /// <summary>
    /// Evaluates int or float expressions.
    /// </summary>
    public static class Evaluator
    {
        public readonly struct Result
        {
            public bool IsInt => !IsFloat;
            public bool IsFloat { get; }
            public int IntValue { get; }
            public float FloatValue { get; }

            public Result(int value) => (IntValue, IsFloat, FloatValue) = (value, false, value);
            public Result(float value) => (IntValue, IsFloat, FloatValue) = ((int)value, true, value);

            public override string ToString()
                => IsInt ? $"{{ Int: {IntValue} }}" : $"{{ Float: {FloatValue} }}";
        }

        public static Result? Evaluate(Expression expr)
            => expr switch
            {
                LiteralExpression e => EvaluateLiteral(e),
                UnaryExpression e => EvaluateUnary(e),
                BinaryExpression e => EvaluateBinary(e),
                ParenthesizedExpression e => Evaluate(e.Inner),
                _ => null,
            };

        private static Result? EvaluateUnary(UnaryExpression expr)
        {
            var innerResult = Evaluate(expr.Operand);

            if (!innerResult.HasValue)
            {
                return null;
            }

            var inner = innerResult.Value;

            return inner.IsInt ?
                    new Result(CalculateInt(expr.Op, inner.IntValue)) :
                    new Result(CalculateFloat(expr.Op, inner.FloatValue));
        }

        private static Result? EvaluateBinary(BinaryExpression expr)
        {
            var leftResult = Evaluate(expr.Left);
            var rightResult = Evaluate(expr.Right);

            if (!leftResult.HasValue || !rightResult.HasValue)
            {
                return null;
            }

            var left = leftResult.Value;
            var right = rightResult.Value;

            return left.IsInt && right.IsInt ?
                    new Result(CalculateInt(expr.Op, left.IntValue, right.IntValue)) :
                    new Result(CalculateFloat(expr.Op, left.FloatValue, right.FloatValue));
        }

        private static float CalculateFloat(UnaryOperator op, float value)
            => op switch
            {
                UnaryOperator.Negate => -value,
                _ => throw new NotImplementedException(),
            };

        private static float CalculateFloat(BinaryOperator op, float left, float right)
            => op switch
            {
                BinaryOperator.Add => left + right,
                BinaryOperator.Subtract => left - right,
                BinaryOperator.Multiply => left * right,
                BinaryOperator.Divide => left / right,
                BinaryOperator.Modulo => left % right,
                _ => throw new NotImplementedException(),
            };

        private static int CalculateInt(UnaryOperator op, int value)
            => op switch
            {
                UnaryOperator.Not => value == 0 ? 1 : 0,
                UnaryOperator.Negate => -value,
                _ => throw new NotImplementedException(),
            };

        private static int CalculateInt(BinaryOperator op, int left, int right)
            => op switch
            {
                BinaryOperator.Add => left + right,
                BinaryOperator.Subtract => left - right,
                BinaryOperator.Multiply => left * right,
                BinaryOperator.Divide => left / right,
                BinaryOperator.Modulo => left % right,
                BinaryOperator.Or => left | right,
                BinaryOperator.And => left & right,
                BinaryOperator.Xor => left ^ right,
                _ => throw new NotImplementedException(),
            };

        private static Result? EvaluateLiteral(LiteralExpression expr)
            => expr.Kind switch
            {
                LiteralKind.Numeric => int.TryParse(expr.ValueText, out int intVal) ?
                                            new Result(intVal) :
                                            float.TryParse(expr.ValueText, out float floatVal) ?
                                                new Result(floatVal) :
                                                (Result?)null,
                _ => null,
            };
    }
}
