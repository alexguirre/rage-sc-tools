namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Types;

using System;
using System.Diagnostics;

internal static class ConstantExpressionEvaluator
{
    /// <summary>
    /// Expression must alredy be type-checked.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="symbols"></param>
    /// <returns></returns>
    public static ConstantValue Eval(IExpression expression, SemanticsAnalyzer semantics)
        => expression.Accept(new Evaluator(), semantics);

    private sealed class Evaluator : EmptyVisitor<ConstantValue, SemanticsAnalyzer>
    {
        public override ConstantValue Visit(NullExpression node, SemanticsAnalyzer param)
            => ConstantValue.Null;
        public override ConstantValue Visit(IntLiteralExpression node, SemanticsAnalyzer param)
            => ConstantValue.Int(node.Value);
        public override ConstantValue Visit(FloatLiteralExpression node, SemanticsAnalyzer param)
            => ConstantValue.Float(node.Value);
        public override ConstantValue Visit(BoolLiteralExpression node, SemanticsAnalyzer param)
            => ConstantValue.Bool(node.Value);
        public override ConstantValue Visit(StringLiteralExpression node, SemanticsAnalyzer param)
            => ConstantValue.String(node.Value);
        public override ConstantValue Visit(VectorExpression node, SemanticsAnalyzer param)
            => ConstantValue.Vector(node.X.Accept(this, param).FloatValue, node.Y.Accept(this, param).FloatValue, node.Z.Accept(this, param).FloatValue);

        public override ConstantValue Visit(UnaryExpression node, SemanticsAnalyzer param)
        {
            var value = node.SubExpression.Accept(this, param);
            var result = value.Type switch
            {
                IntType => node.Operator switch
                {
                    UnaryOperator.Negate => ConstantValue.Int(-value.IntValue),
                    UnaryOperator.LogicalNot => ConstantValue.Bool(!value.BoolValue), // INT is implicitly convertible to BOOL
                    _ => null,
                },
                FloatType => node.Operator switch
                {
                    UnaryOperator.Negate => ConstantValue.Float(-value.FloatValue),
                    _ => null,
                },
                BoolType => node.Operator switch
                {
                    UnaryOperator.LogicalNot => ConstantValue.Bool(!value.BoolValue),
                    _ => null,
                },
                VectorType => node.Operator switch
                {
                    UnaryOperator.Negate => ConstantValue.Vector(-value.VectorValue.X, -value.VectorValue.Y, -value.VectorValue.Z),
                    _ => null,
                },
                _ => null,
            };
            return result ?? throw new InvalidOperationException($"Unary operator '{node.Operator}' is not supported on type '{value.Type.GetType().Name}'");
        }

        public override ConstantValue Visit(BinaryExpression node, SemanticsAnalyzer param)
        {
            /*
                TODO: support remaining binary operators
                Equals,
                NotEquals,
                LessThan,
                LessThanOrEqual,
                GreaterThan,
                GreaterThanOrEqual,
                LogicalAnd,
                LogicalOr,
             */

            var lhs = node.LHS.Accept(this, param);
            var rhs = node.RHS.Accept(this, param);
            var result = (lhs.Type, rhs.Type) switch
            {
                (FloatType, FloatType) or
                (FloatType, IntType) or
                (IntType, FloatType) => MakeFloat(node.Operator switch
                {
                    BinaryOperator.Add => PromoteToFloat(lhs) + PromoteToFloat(rhs),
                    BinaryOperator.Subtract => PromoteToFloat(lhs) - PromoteToFloat(rhs),
                    BinaryOperator.Multiply => PromoteToFloat(lhs) * PromoteToFloat(rhs),
                    BinaryOperator.Divide => PromoteToFloat(lhs) / PromoteToFloat(rhs),
                    BinaryOperator.Modulo => PromoteToFloat(lhs) % PromoteToFloat(rhs),
                    _ => null,
                }),
                (IntType, IntType) => MakeInt(node.Operator switch
                {
                    BinaryOperator.Add => lhs.IntValue + rhs.IntValue,
                    BinaryOperator.Subtract => lhs.IntValue - rhs.IntValue,
                    BinaryOperator.Multiply => lhs.IntValue * rhs.IntValue,
                    BinaryOperator.Divide => lhs.IntValue / rhs.IntValue,
                    BinaryOperator.Modulo => lhs.IntValue % rhs.IntValue,
                    BinaryOperator.And => lhs.IntValue & rhs.IntValue,
                    BinaryOperator.Xor => lhs.IntValue ^ rhs.IntValue,
                    BinaryOperator.Or => lhs.IntValue | rhs.IntValue,
                    _ => null,
                }),
                (VectorType, VectorType) => node.Operator switch
                {
                    BinaryOperator.Add => VecAdd(lhs, rhs),
                    BinaryOperator.Subtract => VecSub(lhs, rhs),
                    BinaryOperator.Multiply => VecMul(lhs, rhs),
                    BinaryOperator.Divide => VecDiv(lhs, rhs),
                    _ => null,
                },
                _ => null,
            };

            return result ?? throw new InvalidOperationException($"Binary operator '{node.Operator}' is not supported on types '{lhs.Type.GetType().Name}' and '{rhs.Type.GetType().Name}'");

            static ConstantValue? MakeInt(int? value) => value is null ? null : ConstantValue.Int(value.Value);
            static ConstantValue? MakeFloat(float? value) => value is null ? null : ConstantValue.Float(value.Value);
            static ConstantValue VecAdd(ConstantValue lhs, ConstantValue rhs)
            {
                var (ax, ay, az) = lhs.VectorValue;
                var (bx, by, bz) = rhs.VectorValue;
                return ConstantValue.Vector(ax + bx, ay + by, az + bz);
            }
            static ConstantValue VecSub(ConstantValue lhs, ConstantValue rhs)
            {
                var (ax, ay, az) = lhs.VectorValue;
                var (bx, by, bz) = rhs.VectorValue;
                return ConstantValue.Vector(ax - bx, ay - by, az - bz);
            }
            static ConstantValue VecMul(ConstantValue lhs, ConstantValue rhs)
            {
                var (ax, ay, az) = lhs.VectorValue;
                var (bx, by, bz) = rhs.VectorValue;
                return ConstantValue.Vector(ax * bx, ay * by, az * bz);
            }
            static ConstantValue VecDiv(ConstantValue lhs, ConstantValue rhs)
            {
                var (ax, ay, az) = lhs.VectorValue;
                var (bx, by, bz) = rhs.VectorValue;
                return ConstantValue.Vector(ax / bx, ay / by, az / bz);
            }

            static float PromoteToFloat(ConstantValue value)
                => value.Type switch
                {
                    FloatType => value.FloatValue,
                    IntType => value.IntValue,
                    _ => throw new ArgumentException($"Cannot promote '{value.Type.GetType().Name}' to FLOAT", nameof(value)),
                };
        }

        public override ConstantValue Visit(InvocationExpression node, SemanticsAnalyzer param)
        {
            // TODO: ConstantExpressionEvaluator handle intrinsics calls

            return base.Visit(node, param);
        }

        public override ConstantValue Visit(NameExpression node, SemanticsAnalyzer s)
        {
            if (!s.GetSymbolUnchecked(node.Name, out var decl))
            {
                throw new ArgumentException($"Unknown symbol '{node.Name}'", nameof(node));
            }

            if (decl is not IValueDeclaration { Semantics.ConstantValue: not null } constValueDecl)
            {
                throw new ArgumentException($"Symbol '{node.Name}' is not a constant value declaration", nameof(node));
            }

            return constValueDecl.Semantics.ConstantValue;
        }
    }
}
