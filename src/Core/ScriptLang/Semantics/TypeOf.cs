#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;

    using ScTools.ScriptLang.Ast;

    public static class TypeOf
    {
        public static bool Expression(Expression expr, out TypeInfo type)
            => expr switch
            {
                BinaryExpression e => BinaryExpression(e, out type),
                LiteralExpression e => (type = Literal(e), result: true).result,
                _ => throw new NotImplementedException(),
            };

        public static bool BinaryExpression(BinaryExpression expr, out TypeInfo type)
        {
            type = default;
            if (!Expression(expr.Left, out var leftType))
            {
                return false;
            }
            
            if (!Expression(expr.Right, out var rightType))
            {
                return false;
            }

            if (leftType.IsArray || rightType.IsArray)
            {
                return false;
            }

            var requiredCapabilities = OpToCapabilities(expr.Op);

            if (!leftType.Capabilities.HasFlag(requiredCapabilities) ||
                !rightType.Capabilities.HasFlag(requiredCapabilities))
            {
                return false;
            }

            // TODO: move these constants somewhere else
            const string INT = "INT";
            const string FLOAT = "FLOAT";
            const string VEC3 = "VEC3";

            type = (leftType.Name, rightType.Name) switch
            {
                (INT, INT) => TypeInfo.Int,
                (INT, FLOAT) => TypeInfo.Float,
                (FLOAT, INT) => TypeInfo.Float,
                (FLOAT, FLOAT) => TypeInfo.Float,
                (VEC3, VEC3) => TypeInfo.Vec3,
                _ => default,
            };

            return type.Name != null;

            static TypeCapabilities OpToCapabilities(BinaryOperator op) => op switch
            {
                BinaryOperator.Add => TypeCapabilities.Add,
                BinaryOperator.Subtract => TypeCapabilities.Subtract,
                BinaryOperator.Multiply => TypeCapabilities.Multiply,
                BinaryOperator.Divide => TypeCapabilities.Divide,
                BinaryOperator.Modulo => TypeCapabilities.Modulo,
                BinaryOperator.Or => TypeCapabilities.BitwiseOr,
                BinaryOperator.And => TypeCapabilities.BitwiseAnd,
                BinaryOperator.Xor => TypeCapabilities.BitwiseXor,
                _ => throw new NotImplementedException()
            };
        }

        public static TypeInfo Literal(LiteralExpression expr)
            => expr.Kind switch
            {
                LiteralKind.Numeric => int.TryParse(expr.ValueText, out _) ? TypeInfo.Int : TypeInfo.Float,
                LiteralKind.Bool => TypeInfo.Bool,
                LiteralKind.String => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
    }
}
