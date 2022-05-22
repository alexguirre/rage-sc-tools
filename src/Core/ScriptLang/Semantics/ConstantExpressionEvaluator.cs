namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.BuiltIns;
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
        => expression.Accept(Evaluator.Instance, semantics);

    private sealed class Evaluator : AstVisitor<ConstantValue, SemanticsAnalyzer>
    {
        public static readonly Evaluator Instance = new Evaluator();

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
            return result ?? throw new InvalidOperationException($"Unary operator '{node.Operator}' is not supported on type '{value.Type.ToPrettyString()}'");
        }

        public override ConstantValue Visit(BinaryExpression node, SemanticsAnalyzer param)
        {
            // TODO: handle promotions here based on Rules.IsPromotableTo()?
            var lhs = node.LHS.Accept(this, param);
            var rhs = node.RHS.Accept(this, param);
            var result = (lhs.Type, rhs.Type) switch
            {
                (FloatType, FloatType) or
                (FloatType, IntType) or
                (IntType, FloatType) => node.Operator switch
                {
                    BinaryOperator.Add => Float(lhs.FloatValue + rhs.FloatValue),
                    BinaryOperator.Subtract => Float(lhs.FloatValue - rhs.FloatValue),
                    BinaryOperator.Multiply => Float(lhs.FloatValue * rhs.FloatValue),
                    BinaryOperator.Divide => Float(lhs.FloatValue / rhs.FloatValue),
                    BinaryOperator.Modulo => Float(lhs.FloatValue % rhs.FloatValue),

                    BinaryOperator.Equals => Bool(lhs.FloatValue == rhs.FloatValue),
                    BinaryOperator.NotEquals => Bool(lhs.FloatValue != rhs.FloatValue),
                    BinaryOperator.LessThan => Bool(lhs.FloatValue < rhs.FloatValue),
                    BinaryOperator.LessThanOrEqual => Bool(lhs.FloatValue <= rhs.FloatValue),
                    BinaryOperator.GreaterThan => Bool(lhs.FloatValue > rhs.FloatValue),
                    BinaryOperator.GreaterThanOrEqual => Bool(lhs.FloatValue >= rhs.FloatValue),

                    _ => null,
                },
                (IntType, IntType) => node.Operator switch
                {
                    BinaryOperator.Add => Int(lhs.IntValue + rhs.IntValue),
                    BinaryOperator.Subtract => Int(lhs.IntValue - rhs.IntValue),
                    BinaryOperator.Multiply => Int(lhs.IntValue * rhs.IntValue),
                    BinaryOperator.Divide => Int(lhs.IntValue / rhs.IntValue),
                    BinaryOperator.Modulo => Int(lhs.IntValue % rhs.IntValue),
                    BinaryOperator.And => Int(lhs.IntValue & rhs.IntValue),
                    BinaryOperator.Xor => Int(lhs.IntValue ^ rhs.IntValue),
                    BinaryOperator.Or => Int(lhs.IntValue | rhs.IntValue),

                    BinaryOperator.Equals => Bool(lhs.IntValue == rhs.IntValue),
                    BinaryOperator.NotEquals => Bool(lhs.IntValue != rhs.IntValue),
                    BinaryOperator.LessThan => Bool(lhs.IntValue < rhs.IntValue),
                    BinaryOperator.LessThanOrEqual => Bool(lhs.IntValue <= rhs.IntValue),
                    BinaryOperator.GreaterThan => Bool(lhs.IntValue > rhs.IntValue),
                    BinaryOperator.GreaterThanOrEqual => Bool(lhs.IntValue >= rhs.IntValue),

                    _ => null,
                },
                (VectorType, VectorType) => node.Operator switch
                {
                    BinaryOperator.Add => VecAdd(lhs, rhs),
                    BinaryOperator.Subtract => VecSub(lhs, rhs),
                    BinaryOperator.Multiply => VecMul(lhs, rhs),
                    BinaryOperator.Divide => VecDiv(lhs, rhs),
                    _ => null,
                },
                (BoolType, BoolType) or
                (BoolType, IntType) or
                (IntType, BoolType) => node.Operator switch
                {
                    BinaryOperator.Equals => Bool(lhs.BoolValue == rhs.BoolValue),
                    BinaryOperator.NotEquals => Bool(lhs.BoolValue != rhs.BoolValue),
                    BinaryOperator.LogicalAnd => Bool(lhs.BoolValue && rhs.BoolValue),
                    BinaryOperator.LogicalOr => Bool(lhs.BoolValue || rhs.BoolValue),
                    _ => null,
                },
                (StringType, NullType) or
                (NullType, StringType) => node.Operator switch
                {
                    BinaryOperator.Equals => Bool(lhs.StringValue == rhs.StringValue),
                    BinaryOperator.NotEquals => Bool(lhs.StringValue != rhs.StringValue),
                    _ => null,
                },
                _ => null,
            };

            return result ?? throw new InvalidOperationException($"Binary operator '{node.Operator}' is not supported on types '{lhs.Type.ToPrettyString()}' and '{rhs.Type.ToPrettyString()}'");

            static ConstantValue Int(int value) => ConstantValue.Int(value);
            static ConstantValue Float(float value) => ConstantValue.Float(value);
            static ConstantValue Bool(bool value) => ConstantValue.Bool(value);
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
        }

        public override ConstantValue Visit(InvocationExpression node, SemanticsAnalyzer param)
        {
            // TODO: ConstantExpressionEvaluator handle intrinsics calls
            if (node.Callee is NameExpression { Semantics.Symbol: IIntrinsic intrinsic })
            {
                return intrinsic.ConstantEval(node, param);
            }
            else
            {
                throw new ArgumentException($"Callee is not an intrinsic, cannot be constant evaluated", nameof(node));
            }
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
