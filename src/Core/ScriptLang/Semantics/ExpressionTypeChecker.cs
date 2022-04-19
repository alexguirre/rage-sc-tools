namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Types;

    internal record struct TypeInfoOrError(TypeInfo? type)
    {
        private readonly TypeInfo? type = type;
        public TypeInfo Type => type ?? throw new InvalidOperationException("Type is not available");
        public bool IsError => type is null;
    }

    /// <summary>
    /// Handles type-checking of <see cref="IExpression"/>s.
    /// Only visit methods for expression nodes are implemented, the other methods throw <see cref="System.NotImplementedException"/>.
    /// </summary>
    internal sealed class ExpressionTypeChecker : EmptyVisitor<TypeInfoOrError, SemanticsAnalyzer>
    {
        public static readonly ExpressionTypeChecker Instance = new();

        public override TypeInfoOrError Visit(NullExpression node, SemanticsAnalyzer s)
        {
            node.Semantics = new(Type: NullType.Instance, ValueKind: ValueKind.Constant | ValueKind.RValue);
            return new(node.Semantics.Type);
        }

        public override TypeInfoOrError Visit(IntLiteralExpression node, SemanticsAnalyzer s)
        {
            node.Semantics = new(Type: IntType.Instance, ValueKind: ValueKind.Constant | ValueKind.RValue);
            return new(node.Semantics.Type);
        }

        public override TypeInfoOrError Visit(FloatLiteralExpression node, SemanticsAnalyzer s)
        {
            node.Semantics = new(Type: FloatType.Instance, ValueKind: ValueKind.Constant | ValueKind.RValue);
            return new(node.Semantics.Type);
        }

        public override TypeInfoOrError Visit(BoolLiteralExpression node, SemanticsAnalyzer s)
        {
            node.Semantics = new(Type: BoolType.Instance, ValueKind: ValueKind.Constant | ValueKind.RValue);
            return new(node.Semantics.Type);
        }

        public override TypeInfoOrError Visit(StringLiteralExpression node, SemanticsAnalyzer s)
        {
            node.Semantics = new(Type: StringType.Instance, ValueKind: ValueKind.Constant | ValueKind.RValue);
            return new(node.Semantics.Type);
        }

        public override TypeInfoOrError Visit(UnaryExpression node, SemanticsAnalyzer s)
        {
            var innerResult = node.SubExpression.Accept(this, s);
            if (innerResult.IsError)
            {
                return new(null);
            }

            var type = innerResult.Type;
            var result = type switch
            {
                IntType => CheckUnaryOp(s, node, type, UnaryOperator.Negate, UnaryOperator.LogicalNot),
                FloatType or VectorType => CheckUnaryOp(s, node, type, UnaryOperator.Negate),
                BoolType => CheckUnaryOp(s, node, type, UnaryOperator.LogicalNot),
                _ => UnaryOperatorNotSupportedError(s, node, type),
            };

            return result;

            static TypeInfoOrError CheckUnaryOp(SemanticsAnalyzer s, UnaryExpression node, TypeInfo type, params UnaryOperator[] supportedOperators)
            {
                if (!supportedOperators.Contains(node.Operator))
                {
                    return UnaryOperatorNotSupportedError(s, node, type);
                }

                TypeInfoOrError result = node.Operator switch
                {
                    UnaryOperator.Negate => new(type), // negation always return the same type
                    UnaryOperator.LogicalNot => new(BoolType.Instance), // NOT always returns a bool
                    _ => throw new ArgumentOutOfRangeException($"Unknown unary operator '{node.Operator}'", nameof(node))
                };

                if (!result.IsError)
                {
                    node.Semantics = new(result.Type, ValueKind.RValue | (node.SubExpression.ValueKind & ValueKind.Constant));
                }
                return result;
            }

            static TypeInfoOrError UnaryOperatorNotSupportedError(SemanticsAnalyzer s, UnaryExpression node, TypeInfo type)
            {
                Error(s, ErrorCode.SemanticBadUnaryOp, $"Unary operator '{node.Operator}' not supported on type '{type}'", node.Location);
                return new(null);
            }
        }

        public override TypeInfoOrError Visit(BinaryExpression node, SemanticsAnalyzer s)
        {
            var lhsResult = node.LHS.Accept(this, s);
            var rhsResult = node.RHS.Accept(this, s);

            if (lhsResult.IsError || rhsResult.IsError)
            {
                return new(null);
            }

            TypeInfo INT = IntType.Instance,
                     FLOAT = FloatType.Instance,
                     BOOL = BoolType.Instance,
                     VECTOR = VectorType.Instance,
                     NULL = NullType.Instance;

            var (lhs, rhs) = (lhsResult.Type, rhsResult.Type);

            if (node.Operator is BinaryOperator.Add or BinaryOperator.Subtract or
                                 BinaryOperator.Multiply or BinaryOperator.Divide)
            {
                return CheckBinaryOp(s, node, (lhs, rhs),
                //   LHS  op RHS  -> Result
                    (INT,    INT,    INT),
                    (FLOAT,  FLOAT,  FLOAT),
                    (INT,    FLOAT,  FLOAT),
                    (FLOAT,  INT,    FLOAT),
                    (VECTOR, VECTOR, VECTOR),
                    // handle NULL promotion to INT
                    (INT,    NULL,   INT),
                    (NULL,   INT,    INT),
                    (FLOAT,  NULL,   FLOAT),
                    (NULL,   FLOAT,  FLOAT));
            }
            else if (node.Operator is BinaryOperator.Modulo)
            {
                return CheckBinaryOp(s, node, (lhs, rhs),
                //   LHS  op RHS  -> Result
                    (INT,    INT,    INT),
                    (FLOAT,  FLOAT,  FLOAT),
                    (INT,    FLOAT,  FLOAT),
                    (FLOAT,  INT,    FLOAT),
                    // handle NULL promotion to INT
                    (INT,    NULL,   INT),
                    (NULL,   INT,    INT),
                    (FLOAT,  NULL,   FLOAT),
                    (NULL,   FLOAT,  FLOAT));
            }
            else if (node.Operator is BinaryOperator.And or BinaryOperator.Xor or BinaryOperator.Or)
            {
                return CheckBinaryOp(s, node, (lhs, rhs),
                //   LHS  op RHS  -> Result
                    (INT,    INT,    INT),
                    // handle NULL promotion to INT
                    (INT,    NULL,   INT),
                    (NULL,   INT,    INT));
            }
            else if (node.Operator is BinaryOperator.Equals or BinaryOperator.NotEquals or
                                      BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
                                      BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual)
            {
                return CheckBinaryOp(s, node, (lhs, rhs),
                //   LHS  op RHS  -> Result
                    (INT,    INT,    BOOL),
                    (FLOAT,  FLOAT,  BOOL),
                    (INT,    FLOAT,  BOOL),
                    (FLOAT,  INT,    BOOL),
                    (BOOL,   BOOL,   BOOL),
                    (INT,    BOOL,   BOOL),
                    (BOOL,   INT,    BOOL));
                // handle NULL promotion to INT?
            }
            else if (node.Operator is BinaryOperator.LogicalAnd or BinaryOperator.LogicalOr)
            {
                return CheckBinaryOp(s, node, (lhs, rhs),
                //   LHS  op RHS  -> Result
                    (INT,    INT,    BOOL),
                    (BOOL,   BOOL,   BOOL),
                    (INT,    BOOL,   BOOL),
                    (BOOL,   INT,    BOOL));
                // handle NULL promotion to INT?
            }
            else
            {
                throw new ArgumentException($"Unknown binary operator '{node.Operator}'", nameof(node));
            }

            static TypeInfoOrError CheckBinaryOp(SemanticsAnalyzer s, BinaryExpression node, (TypeInfo LHS, TypeInfo RHS) typePair, params (TypeInfo LHS, TypeInfo RHS, TypeInfo Result)[] typePatterns)
            {
                var match = typePatterns.FirstOrDefault(p => (p.LHS, p.RHS) == typePair);
                if (match == default)
                {
                    return BinaryOperatorNotSupportedError(s, node, typePair);
                }

                TypeInfoOrError result = new(match.Result);
                if (!result.IsError)
                {
                    node.Semantics = new(result.Type, ValueKind.RValue | (node.LHS.ValueKind & node.RHS.ValueKind & ValueKind.Constant));
                }
                return result;
            }

            static TypeInfoOrError BinaryOperatorNotSupportedError(SemanticsAnalyzer s, BinaryExpression node, (TypeInfo LHS, TypeInfo RHS) typePair)
            {
                Error(s, ErrorCode.SemanticBadBinaryOp, $"Binary operator '{node.Operator}' not supported on types '{typePair.LHS}' and '{typePair.RHS}'", node.Location);
                return new(null);
            }
        }

        public override TypeInfoOrError Visit(FieldAccessExpression node, SemanticsAnalyzer s)
        {
            var innerResult = node.SubExpression.Accept(this, s);
            if (innerResult.IsError)
            {
                return new(null);
            }

            var type = innerResult.Type;
            var field = type.Fields.SingleOrDefault(f => ParserNew.CaseInsensitiveComparer.Equals(f.Name, node.FieldName));
            if (field is not null)
            {
                node.Semantics = new(field.Type, node.SubExpression.ValueKind);
                return new(field.Type);
            }

            return UnknownFieldError(s, node, type);

            static TypeInfoOrError UnknownFieldError(SemanticsAnalyzer s, FieldAccessExpression node, TypeInfo type)
            {
                Error(s, ErrorCode.SemanticUnknownField, $"'{type}' does not contain a field named '{node.FieldName}'", node.FieldNameToken.Location);
                return new(null);
            }
        }

        public override TypeInfoOrError Visit(IndexingExpression node, SemanticsAnalyzer s)
        {
            throw new NotImplementedException();
            //node.Array.Accept(this, param);
            //node.Index.Accept(this, param);

            //node.Semantics = new(Type: node.Array.Type!.Indexing(node.Index.Type!, node.Location, Diagnostics),
            //                     IsLValue: true,
            //                     IsConstant: false);
            //return default;
        }

        public override TypeInfoOrError Visit(InvocationExpression node, SemanticsAnalyzer s)
        {
            throw new NotImplementedException();
            //node.Callee.Accept(this, param);
            //node.Arguments.ForEach(arg => arg.Accept(this, param));

            //var (ty, isConstant) = node.Callee.Type!.Invocation(node.Arguments.ToArray(), node.Location, Diagnostics);
            //node.Semantics = new(Type: ty,
            //                     IsLValue: false,
            //                     IsConstant: isConstant);
            //return default;
        }

        public override TypeInfoOrError Visit(NameExpression node, SemanticsAnalyzer s)
        {
            if (!s.GetSymbol(node, out var decl))
            {
                return new(null);
            }

            if (decl is ScriptDeclaration)
            {
                node.Semantics = new(null, 0, decl);
                return ScriptNameNotAllowedInExpressionError(s, node);
            }

            (TypeInfo? type, ValueKind valueKind) = decl switch
            {
                IValueDeclaration valueDecl => (valueDecl.Semantics.ValueType, ValueKindOfDeclaration(valueDecl)),
                ITypeDeclaration typeDecl => (new TypeNameType(typeDecl), ValueKind.Constant),
                _ => throw new ArgumentException($"Unknown declaration with name '{node.Name}'", nameof(node)),
            };

            node.Semantics = new(type, valueKind, decl);
            return new(type);

            static ValueKind ValueKindOfDeclaration(IValueDeclaration valueDecl)
                => valueDecl switch
                {
                    EnumMemberDeclaration => ValueKind.RValue | ValueKind.Constant,
                    FunctionDeclaration => ValueKind.RValue | ValueKind.Constant,
                    NativeFunctionDeclaration => ValueKind.Constant,
                    VarDeclaration { Kind: not VarKind.Constant } => ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable,
                    VarDeclaration { Kind: VarKind.Constant } => ValueKind.RValue | ValueKind.Constant,
                    _ => throw new ArgumentException("Unknown value declaration", nameof(valueDecl)),
                };

            static TypeInfoOrError ScriptNameNotAllowedInExpressionError(SemanticsAnalyzer s, NameExpression name)
            {
                Error(s, ErrorCode.SemanticScriptNameNotAllowedInExpression, $"Script names are not allowed in expressions", name.Location);
                return new(null);
            }
        }

        public override TypeInfoOrError Visit(VectorExpression node, SemanticsAnalyzer s)
        {
            throw new NotImplementedException();
            //node.X.Accept(this, param);
            //node.Y.Accept(this, param);
            //node.Z.Accept(this, param);

            //var vectorTy = BuiltInTypes.Vector.CreateType(node.Location);
            //node.Semantics = new(Type: vectorTy,
            //                     IsLValue: false,
            //                     IsConstant: node.X.IsConstant && node.Y.IsConstant && node.Z.IsConstant);

            //var floatTy = BuiltInTypes.Float.CreateType(node.Location);
            //for (int i = 0; i < 3; i++)
            //{
            //    var src = i switch { 0 => node.X, 1 => node.Y, 2 => node.Z, _ => throw new System.NotImplementedException() };
            //    if (!floatTy.CanAssign(src.Type!, src.IsLValue))
            //    {
            //        var comp = i switch { 0 => "X", 1 => "Y", 2 => "Z", _ => throw new System.NotImplementedException() };
            //        Diagnostics.AddError($"Vector component {comp} requires type '{floatTy}', found '{src.Type}'", src.Location);
            //    }
            //}

            //return default;
        }

        public override TypeInfoOrError Visit(ErrorExpression node, SemanticsAnalyzer s)
            => new(null);

        private static void Error(SemanticsAnalyzer s, ErrorCode code, string message, SourceRange location)
            => s.Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);
    }
}
