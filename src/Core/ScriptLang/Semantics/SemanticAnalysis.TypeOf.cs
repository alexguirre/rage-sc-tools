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
            private readonly string filePath;
            private readonly SymbolTable symbols;

            public TypeOf(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                => (this.diagnostics, this.filePath, this.symbols) = (diagnostics, filePath, symbols);

            public override Type? VisitIdentifierExpression(IdentifierExpression node)
            {
                var symbol = symbols.Lookup(node.Identifier);
                if (symbol == null)
                {
                    diagnostics.AddError(filePath, $"Unknown symbol '{node.Identifier}'", node.Source);
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

                diagnostics.AddError(filePath, $"Identifier '{node.Identifier}' must refer to a variable, procedure or function", node.Source);
                return null;
            }

            public override Type? VisitLiteralExpression(LiteralExpression node)
                => node.Kind switch
                {
                    LiteralKind.Int => (symbols.Lookup("INT") as TypeSymbol)!.Type,
                    LiteralKind.Float => (symbols.Lookup("FLOAT") as TypeSymbol)!.Type,
                    LiteralKind.Bool => (symbols.Lookup("BOOL") as TypeSymbol)!.Type,
                    LiteralKind.String => (symbols.Lookup("STRING") as TypeSymbol)!.Type,
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
                    diagnostics.AddError(filePath, $"Mismatched type in binary operation '{BinaryExpression.OpToString(node.Op)}'", node.Source);
                    return null;
                }

                if (BinaryExpression.OpIsComparison(node.Op))
                {
                    return (symbols.Lookup("BOOL") as TypeSymbol)!.Type;
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
                        diagnostics.AddError(filePath, $"Cannot call '{node.Expression}', it is not a function", node.Expression.Source);
                    }
                    return null;
                }

                if (f.ReturnType == null)
                {
                    diagnostics.AddError(filePath, $"Cannot call '{node.Expression}'. It is a procedure, it has no return value", node.Source);
                    return null;
                }

                int expected = f.Parameters.Count;
                int found = node.ArgumentList.Arguments.Length;
                if (found != expected)
                {
                    diagnostics.AddError(filePath, $"Mismatched number of arguments. Expected {expected}, found {found}", node.ArgumentList.Source);
                }

                int argCount = Math.Min(expected, found);
                for (int i = 0; i < argCount; i++)
                {
                    var expectedType = f.Parameters[i].Type;
                    var foundType = node.ArgumentList.Arguments[i].Accept(this);

                    if (foundType == null || !expectedType.IsAssignableFrom(foundType, considerReferences: true))
                    {
                        diagnostics.AddError(filePath, $"Mismatched type of argument #{i}", node.ArgumentList.Arguments[i].Source);
                    }
                }

                return f.ReturnType;
            }

            public override Type? VisitMemberAccessExpression(MemberAccessExpression node)
            {
                var type = node.Expression.Accept(this);
                if (!(type?.UnderlyingType is StructType struc))
                {
                    if (type != null)
                    {
                        diagnostics.AddError(filePath, "Only structs have members", node.Expression.Source);
                    }
                    return null;
                }

                var field = struc.Fields.SingleOrDefault(f => f.Name == node.Member);
                if (field == default)
                {
                    diagnostics.AddError(filePath, $"Unknown field '{struc.Name}.{node.Member}'", node.Source);
                    return null;
                }

                return field.Type;
            }

            public override Type? VisitAggregateExpression(AggregateExpression node)
            {
                var fieldTypes = node.Expressions.Select(expr => expr.Accept(this)!);

                if (fieldTypes.All(t => t != null))
                {
                    return StructType.NewAggregate(fieldTypes);
                }
                else
                {
                    return null;
                }
            }

            // TODO: VisitArrayAccessExpression

            public override Type? VisitErrorExpression(ErrorExpression node) => null;

            public override Type? DefaultVisit(Node node) => throw new InvalidOperationException($"Unsupported AST node {node.GetType().Name}");
        }
    }
}
