#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static partial class SemanticAnalysis
    {
        private sealed class TypeOf : AstVisitor<Type?>
        {
            private readonly DiagnosticsReport diagnostics;
            private readonly SymbolTable symbols;

            public TypeOf(DiagnosticsReport diagnostics, SymbolTable symbols)
                => (this.diagnostics, this.symbols) = (diagnostics, symbols);

            public override Type? VisitIdentifierExpression(IdentifierExpression node)
            {
                var symbol = symbols.Lookup(node.Identifier);
                if (symbol == null)
                {
                    diagnostics.AddError($"Unknown symbol '{node.Identifier}'", node.Source);
                    return null;
                }

                if (symbol is VariableSymbol v)
                {
                    return v.Type;
                }
                else if (symbol is FunctionSymbol f)
                {
                    return f.Type;
                }

                diagnostics.AddError($"Identifier '{node.Identifier}' must refer to a variable, procedure or function", node.Source);
                return null;
            }

            public override Type? VisitLiteralExpression(LiteralExpression node)
                => node.Kind switch
                {
                    LiteralKind.Int => BuiltInTypes.INT,
                    LiteralKind.Float => BuiltInTypes.FLOAT,
                    LiteralKind.Bool => BuiltInTypes.BOOL,
                    LiteralKind.String => BuiltInTypes.STRING,
                    _ => null,
                };

            public override Type? VisitUnaryExpression(UnaryExpression node)
                => node.Operand.Accept(this);

            public override Type? VisitBinaryExpression(BinaryExpression node)
            {
                var left = node.Left.Accept(this);
                var right = node.Right.Accept(this);

                if (left == null)
                {
                    return null;
                }

                if (right == null)
                {
                    return null;
                }

                // TODO: allow some conversions (e.g. INT -> FLOAT, aggregate -> struct)
                if (left.UnderlyingType != right.UnderlyingType)
                {
                    diagnostics.AddError($"Mismatched type in binary operation '{BinaryExpression.OpToString(node.Op)}'", node.Source);
                    return null;
                }

                if (BinaryExpression.OpIsComparison(node.Op))
                {
                    return BuiltInTypes.BOOL;
                }

                return left;
            }

            public override Type? VisitInvocationExpression(InvocationExpression node)
            {
                var callableType = node.Expression.Accept(this);
                if (!(callableType is FunctionType f))
                {
                    if (callableType != null)
                    {
                        diagnostics.AddError($"Cannot call '{node.Expression}', it is not a function", node.Expression.Source);
                    }
                    return null;
                }

                if (f.IsProcedure)
                {
                    diagnostics.AddError($"Cannot call '{node.Expression}' in an expression. It is a procedure, it has no return value", node.Source);
                    return null;
                }

                int expected = f.ParameterCount;
                int found = node.Arguments.Length;
                if (found != expected)
                {
                    diagnostics.AddError($"Mismatched number of arguments. Expected {expected}, found {found}", node.Source);
                }

                int argCount = Math.Min(expected, found);
                for (int i = 0; i < argCount; i++)
                {
                    var foundType = node.Arguments[i].Accept(this);
                    if (!f.DoesParameterTypeMatch(i, foundType))
                    {
                        diagnostics.AddError($"Mismatched type of argument #{i}", node.Arguments[i].Source);
                    }
                }

                return f.ReturnType;
            }

            public override Type? VisitMemberAccessExpression(MemberAccessExpression node)
            {
                var type = node.Expression.Accept(this);
                var underlyingTy = type?.UnderlyingType;
                
                if (underlyingTy is ArrayType arrTy)
                {
                    if (node.Member != ArrayType.LengthFieldName)
                    {
                        diagnostics.AddError($"Unknown field '{node.Member}', arrays only have a 'length' field", node.Source);
                        return null;
                    }

                    return BuiltInTypes.INT;
                }
                
                if (underlyingTy is not StructType struc)
                {
                    if (type != null)
                    {
                        diagnostics.AddError("Only structs have members", node.Expression.Source);
                    }
                    return null;
                }

                var field = struc.Fields.SingleOrDefault(f => f.Name == node.Member);
                if (field == default)
                {
                    diagnostics.AddError($"Unknown field '{struc.Name}.{node.Member}'", node.Source);
                    return null;
                }

                return field.Type;
            }

            public override Type? VisitVectorExpression(VectorExpression node)
            {
                var xTy = CheckComponentType(node.X, "X");
                var yTy = CheckComponentType(node.Y, "Y");
                var zTy = CheckComponentType(node.Z, "Z");


                if (xTy != null && yTy != null && zTy != null)
                {
                    return BuiltInTypes.VECTOR;
                }
                else
                {
                    return null;
                }

                Type? CheckComponentType(Node node, string name)
                {
                    var ty = node.Accept(this);
                    if (ty?.UnderlyingType is not BasicType { TypeCode: BasicTypeCode.Float })
                    {
                        diagnostics.AddError($"Mismatched type of component {name}, expected FLOAT", node.Source);
                        ty = null;
                    }

                    return ty;
                }
            }

            public override Type? VisitArrayAccessExpression(ArrayAccessExpression node)
            {
                var type = node.Expression.Accept(this);
                if (type?.UnderlyingType is not ArrayType arr)
                {
                    if (type != null)
                    {
                        diagnostics.AddError($"Cannot index '{node.Expression}' of type '{type}'", node.Expression.Source);
                    }
                    return null;
                }

                return arr.ItemType;
            }

            public override Type? VisitErrorExpression(ErrorExpression node) => null;

            public override Type? DefaultVisit(Node node) => throw new InvalidOperationException($"Unsupported AST node {node.GetType().Name}");
        }
    }
}
