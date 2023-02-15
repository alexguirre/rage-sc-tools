namespace ScTools.ScriptLang.Semantics;

using System;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.BuiltIns;
using ScTools.ScriptLang.Types;

/// <summary>
/// Handles type-checking of <see cref="IExpression"/>s.
/// Only visit methods for expression nodes are implemented, the other methods throw <see cref="System.NotImplementedException"/>.
/// </summary>
public sealed class ExpressionTypeChecker : AstVisitor<TypeInfo, SemanticsAnalyzer>
{
    private static readonly ErrorType ErrorType = ErrorType.Instance;

    private static TypeInfo Literal(IExpression node, TypeInfo type)
    {
        node.Semantics = new(Type: type, ValueKind: ValueKind.Constant | ValueKind.RValue, ArgumentKind.None);
        return type;
    }

    public override TypeInfo Visit(NullExpression node, SemanticsAnalyzer s) => Literal(node, NullType.Instance);
    public override TypeInfo Visit(IntLiteralExpression node, SemanticsAnalyzer s) => Literal(node, IntType.Instance);
    public override TypeInfo Visit(FloatLiteralExpression node, SemanticsAnalyzer s) => Literal(node, FloatType.Instance);
    public override TypeInfo Visit(BoolLiteralExpression node, SemanticsAnalyzer s) => Literal(node, BoolType.Instance);
    public override TypeInfo Visit(StringLiteralExpression node, SemanticsAnalyzer s) => Literal(node, StringType.Instance);

    public override TypeInfo Visit(UnaryExpression node, SemanticsAnalyzer s)
    {
        var type = node.SubExpression.Accept(this, s);
        if (type.IsError)
        {
            return ErrorType;
        }

        return type switch
        {
            IntType => CheckUnaryOp(s, node, type, UnaryOperator.Negate, UnaryOperator.LogicalNot),
            FloatType or VectorType => CheckUnaryOp(s, node, type, UnaryOperator.Negate),
            BoolType => CheckUnaryOp(s, node, type, UnaryOperator.LogicalNot),
            _ => UnaryOperatorNotSupportedError(s, node, type),
        };

        static TypeInfo CheckUnaryOp(SemanticsAnalyzer s, UnaryExpression node, TypeInfo type, params UnaryOperator[] supportedOperators)
        {
            if (!supportedOperators.Contains(node.Operator))
            {
                return UnaryOperatorNotSupportedError(s, node, type);
            }

            var result = node.Operator switch
            {
                UnaryOperator.Negate => type, // negation always return the same type
                UnaryOperator.LogicalNot => BoolType.Instance, // NOT always returns a bool
                _ => throw new ArgumentOutOfRangeException(nameof(node), $"Unknown unary operator '{node.Operator}'")
            };

            if (!result.IsError)
            {
                node.Semantics = new(result, ValueKind.RValue | (node.SubExpression.ValueKind & ValueKind.Constant), ArgumentKind.None);
            }
            return result;
        }
    }

    public override TypeInfo Visit(BinaryExpression node, SemanticsAnalyzer s)
    {
        var lhs = node.LHS.Accept(this, s);
        var rhs = node.RHS.Accept(this, s);

        if (lhs.IsError || rhs.IsError)
        {
            return ErrorType;
        }

        TypeInfo INT = IntType.Instance,
                 FLOAT = FloatType.Instance,
                 BOOL = BoolType.Instance,
                 VECTOR = VectorType.Instance;

        if (node.Operator is BinaryOperator.Add or BinaryOperator.Subtract or
                             BinaryOperator.Multiply or BinaryOperator.Divide)
        {
            return CheckBinaryOp(s, node, (lhs, rhs),
            //   LHS  op RHS  -> Result
                (INT,    INT,    INT),
                (FLOAT,  FLOAT,  FLOAT),
                (VECTOR, VECTOR, VECTOR));
        }
        else if (node.Operator is BinaryOperator.Modulo)
        {
            return CheckBinaryOp(s, node, (lhs, rhs),
            //   LHS  op RHS  -> Result
                (INT,    INT,    INT),
                (FLOAT,  FLOAT,  FLOAT));
        }
        else if (node.Operator is BinaryOperator.And or BinaryOperator.Xor or BinaryOperator.Or)
        {
            if (lhs is EnumType && lhs == rhs)
            {
                return SetBinaryOpResultType(node, lhs);
            }

            return CheckBinaryOp(s, node, (lhs, rhs),
            //   LHS  op RHS  -> Result
                (INT,    INT,    INT));
        }
        else if (node.Operator is BinaryOperator.Equals or BinaryOperator.NotEquals)
        {
            if (lhs is EnumType && lhs == rhs)
            {
                return SetBinaryOpResultType(node, BOOL);
            }
            else if (lhs is NativeType || rhs is NativeType)
            {
                bool isComparableNativeTy = (lhs == rhs) || lhs.IsPromotableTo(rhs) || rhs.IsPromotableTo(lhs);
                if (isComparableNativeTy)
                {
                    return SetBinaryOpResultType(node, BOOL);
                }
            }

            return CheckBinaryOp(s, node, (lhs, rhs),
            //   LHS  op RHS  -> Result
                (INT,    INT,    BOOL),
                (FLOAT,  FLOAT,  BOOL),
                (BOOL,   BOOL,   BOOL));
        }
        else if (node.Operator is BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
                                    BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual)
        {
            if (lhs is EnumType && lhs == rhs)
            {
                return SetBinaryOpResultType(node, BOOL);
            }

            return CheckBinaryOp(s, node, (lhs, rhs),
            //   LHS  op RHS  -> Result
                (INT,    INT,    BOOL),
                (FLOAT,  FLOAT,  BOOL));
        }
        else if (node.Operator is BinaryOperator.LogicalAnd or BinaryOperator.LogicalOr)
        {
            return CheckBinaryOp(s, node, (lhs, rhs),
            //   LHS  op RHS  -> Result
                (INT,    INT,    BOOL),
                (BOOL,   BOOL,   BOOL));
        }
        else
        {
            throw new ArgumentException($"Unknown binary operator '{node.Operator}'", nameof(node));
        }

        static TypeInfo CheckBinaryOp(SemanticsAnalyzer s, BinaryExpression node, (TypeInfo LHS, TypeInfo RHS) typePair, params (TypeInfo LHS, TypeInfo RHS, TypeInfo Result)[] typePatterns)
        {
            var (lhs, rhs) = typePair;

            var match = typePatterns.FirstOrDefault(p => (lhs == p.LHS && rhs == p.RHS) ||
                                                         (lhs == p.LHS && rhs.IsPromotableTo(p.RHS)) ||
                                                         (lhs.IsPromotableTo(p.LHS) && rhs == p.RHS));
            if (match == default)
            {
                return BinaryOperatorNotSupportedError(s, node, typePair);
            }

            return SetBinaryOpResultType(node, match.Result);
        }

        static TypeInfo SetBinaryOpResultType(BinaryExpression node, TypeInfo type)
        {
            if (!type.IsError)
            {
                node.Semantics = new(type, ValueKind.RValue | (node.LHS.ValueKind & node.RHS.ValueKind & ValueKind.Constant), ArgumentKind.None);
            }
            return type;
        }
    }

    public override TypeInfo Visit(FieldAccessExpression node, SemanticsAnalyzer s)
    {
        var type = node.SubExpression.Accept(this, s);
        if (type.IsError)
        {
            return ErrorType;
        }

        var field = type.Fields.SingleOrDefault(f => Parser.CaseInsensitiveComparer.Equals(f.Name, node.FieldName));
        if (field is not null)
        {
            node.Semantics = new(field.Type, node.SubExpression.ValueKind, ArgumentKind.None, field);
            return field.Type;
        }

        return UnknownFieldError(s, node, type);
    }

    public override TypeInfo Visit(IndexingExpression node, SemanticsAnalyzer s)
    {
        throw new NotImplementedException();
        //node.Array.Accept(this, param);
        //node.Index.Accept(this, param);

        //node.Semantics = new(Type: node.Array.Type!.Indexing(node.Index.Type!, node.Location, Diagnostics),
        //                     IsLValue: true,
        //                     IsConstant: false);
        //return default;
    }

    public override TypeInfo Visit(InvocationExpression node, SemanticsAnalyzer s)
    {
        var calleeType = node.Callee.Accept(this, s); // callee type-checked here so Semantics gets filled before accessing it in the 'is intrinsic' check

        ExpressionSemantics result;
        if (node.Callee is NameExpression { Semantics.Symbol: IIntrinsic intrinsic })
        {
            result = intrinsic.InvocationTypeCheck(node, s, this);
            Debug.Assert(result.Type is not null);
        }
        else
        {
            node.Arguments.ForEach(a => a.Accept(this, s));
            if (calleeType.IsError)
            {
                return ErrorType;
            }

            if (calleeType is not FunctionType funcType)
            {
                TypeNotCallableError(s, node.Callee, calleeType);
                return ErrorType;
            }

            // type-check arguments
            var parameters = funcType.Parameters;
            var args = node.Arguments;
            CheckArgumentCount(parameters.Length, node, s);

            var n = Math.Min(args.Length, parameters.Length);
            for (int i = 0; i < n; i++)
            {
                TypeCheckArgumentAgainstParameter(i, args[i], parameters[i], s);
            }

            result = new(funcType.Return, ValueKind.RValue, ArgumentKind.None);
        }

        node.Semantics = result;
        return result.Type!;
    }

    internal static void CheckArgumentCount(int parameterCount, InvocationExpression invocation, SemanticsAnalyzer s)
    {
        var args = invocation.Arguments;
        if (parameterCount != args.Length)
        {
            MismatchedArgumentCountError(s, parameterCount, invocation);
        }
    }
    internal static void TypeCheckArgumentAgainstParameter(int argIndex, IExpression arg, ParameterInfo param, SemanticsAnalyzer s)
    {
        var paramType = param.Type;
        var argType = arg.Type;
        if (argType is null)
        {
            throw new ArgumentException($"Argument type is null, argument expression was not type-checked yet.", nameof(arg));
        }

        if (argType.IsError)
        {
            return;
        }

        if (param.IsReference)
        {
            // pass by reference
            if (!paramType.IsRefAssignableFrom(argType))
            {
                ArgCannotPassRefTypeError(s, argIndex, arg, argType, paramType);
            }
            else if (!arg.Semantics.ValueKind.Is(ValueKind.Addressable))
            {
                ArgCannotPassNonLValueToRefParamError(s, argIndex, arg);
            }

            arg.Semantics = arg.Semantics with { ArgumentKind = ArgumentKind.ByRef };
        }
        else if (paramType is ArrayType)
        {
            Debug.Assert(arg.Semantics.ValueKind.Is(ValueKind.Addressable)); // all expression of array type should be lvalues

            // pass array by reference
            if (paramType != argType)
            {
                ArgCannotPassTypeError(s, argIndex, arg, argType, paramType);
            }
            // TODO: check 'incomplete' array

            arg.Semantics = arg.Semantics with { ArgumentKind = ArgumentKind.ByRef };
        }
        else if (paramType is StringType && argType is TextLabelType)
        {
            if (!arg.Semantics.ValueKind.Is(ValueKind.Addressable))
            {
                ArgCannotPassNonLValueTextLabelToStringParamError(s, argIndex, arg);
            }

            arg.Semantics = arg.Semantics with { ArgumentKind = ArgumentKind.ByRef };
        }
        else
        {
            // pass by value
            if (!paramType.IsAssignableFrom(argType))
            {
                ArgCannotPassTypeError(s, argIndex, arg, argType, paramType);
            }

            arg.Semantics = arg.Semantics with { ArgumentKind = ArgumentKind.ByValue };
        }
    }

    public override TypeInfo Visit(NameExpression node, SemanticsAnalyzer s)
    {
        if (!s.GetSymbol(node, out var symbol))
        {
            return ErrorType;
        }

        if (symbol is ScriptDeclaration)
        {
            node.Semantics = new(null, 0, ArgumentKind.None, symbol);
            return ScriptNameNotAllowedInExpressionError(s, node);
        }

        (TypeInfo? type, ValueKind valueKind) = symbol switch
        {
            IValueDeclaration valueDecl => (valueDecl.Semantics.ValueType, ValueKindOfDeclaration(valueDecl)),
            ITypeSymbol typeSymbol => (new TypeNameType(typeSymbol), ValueKind.Constant),
            IIntrinsic => (null, ValueKind.Constant),
            _ => throw new ArgumentException($"Unknown declaration with name '{node.Name}'", nameof(node)),
        };

        node.Semantics = new(type, valueKind, ArgumentKind.None, symbol);
        return type ?? ErrorType;

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
    }

    public override TypeInfo Visit(VectorExpression node, SemanticsAnalyzer s)
    {
        var x = node.X.Accept(this, s);
        var y = node.Y.Accept(this, s);
        var z = node.Z.Accept(this, s);

        CheckComponent(s, node.X, x);
        CheckComponent(s, node.Y, y);
        CheckComponent(s, node.Z, z);

        if (x.IsError || y.IsError || z.IsError)
        {
            return ErrorType;
        }

        node.Semantics = new(
            VectorType.Instance,
            ValueKind.RValue | (node.X.ValueKind & node.Y.ValueKind & node.Z.ValueKind & ValueKind.Constant),
            ArgumentKind.None);

        return VectorType.Instance;

        static void CheckComponent(SemanticsAnalyzer s, IExpression componentExpr, TypeInfo componentType)
        {
            if (!componentType.IsError && !FloatType.Instance.IsAssignableFrom(componentType))
            {
                s.CannotConvertTypeError(componentType, FloatType.Instance, componentExpr.Location);
            }
        }
    }

    public override TypeInfo Visit(ErrorExpression node, SemanticsAnalyzer s) => ErrorType;

    #region Errors
    private static void Error(SemanticsAnalyzer s, ErrorCode code, string message, SourceRange location)
        => s.Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);

    private static TypeInfo UnaryOperatorNotSupportedError(SemanticsAnalyzer s, UnaryExpression node, TypeInfo type)
    {
        Error(s, ErrorCode.SemanticBadUnaryOp, $"Unary operator '{node.Operator}' not supported on type '{type.ToPrettyString()}'", node.Location);
        return ErrorType;
    }

    private static TypeInfo BinaryOperatorNotSupportedError(SemanticsAnalyzer s, BinaryExpression node, (TypeInfo LHS, TypeInfo RHS) typePair)
    {
        Error(s, ErrorCode.SemanticBadBinaryOp, $"Binary operator '{node.Operator}' not supported on types '{typePair.LHS.ToPrettyString()}' and '{typePair.RHS.ToPrettyString()}'", node.Location);
        return ErrorType;
    }

    private static TypeInfo UnknownFieldError(SemanticsAnalyzer s, FieldAccessExpression node, TypeInfo type)
    {
        Error(s, ErrorCode.SemanticUnknownField, $"'{type.ToPrettyString()}' does not contain a field named '{node.FieldName}'", node.FieldNameToken.Location);
        return ErrorType;
    }

    private static TypeInfo ScriptNameNotAllowedInExpressionError(SemanticsAnalyzer s, NameExpression name)
    {
        Error(s, ErrorCode.SemanticScriptNameNotAllowedInExpression, $"Script names are not allowed in expressions", name.Location);
        return ErrorType;
    }

    internal static void TypeNotCallableError(SemanticsAnalyzer s, IExpression expr, TypeInfo type)
        => Error(s, ErrorCode.SemanticTypeNotCallable, $"Type '{type.ToPrettyString()}' is not callable", expr.Location);
    internal static void MismatchedArgumentCountError(SemanticsAnalyzer s, int paramCount, InvocationExpression expr)
        => Error(s, ErrorCode.SemanticMismatchedArgumentCount, $"Expected {paramCount} arguments, found {expr.Arguments.Length}", expr.OpenParen.Location.Merge(expr.CloseParen.Location));
    internal static void ArgCannotPassTypeError(SemanticsAnalyzer s, int argIndex, IExpression arg, TypeInfo argType, TypeInfo paramType)
        => Error(s, ErrorCode.SemanticArgCannotPassType, $"Argument {argIndex + 1}: cannot pass '{argType.ToPrettyString()}' to parameter type '{paramType.ToPrettyString()}'", arg.Location);
    internal static void ArgCannotPassRefTypeError(SemanticsAnalyzer s, int argIndex, IExpression arg, TypeInfo argType, TypeInfo paramType)
        => Error(s, ErrorCode.SemanticArgCannotPassRefType, $"Argument {argIndex + 1}: cannot pass '{argType.ToPrettyString()}' to reference parameter type '{paramType.ToPrettyString()}'", arg.Location);
    internal static void ArgCannotPassRefTextLabelError(SemanticsAnalyzer s, int argIndex, IExpression arg, TypeInfo argType)
        => Error(s, ErrorCode.SemanticArgCannotPassRefType, $"Argument {argIndex + 1}: cannot pass '{argType.ToPrettyString()}' to TEXT_LABEL_* reference", arg.Location);
    internal static void ArgCannotPassNonLValueToRefParamError(SemanticsAnalyzer s, int argIndex, IExpression arg)
        => Error(s, ErrorCode.SemanticArgCannotPassNonLValueToRefParam, $"Argument {argIndex + 1}: cannot pass non-lvalue to reference parameter", arg.Location);
    internal static void ArgCannotPassNonLValueTextLabelToStringParamError(SemanticsAnalyzer s, int argIndex, IExpression arg)
        => Error(s, ErrorCode.SemanticArgCannotPassNonLValueToRefParam, $"Argument {argIndex + 1}: cannot pass non-lvalue TEXT_LABEL_* to STRING parameter", arg.Location);
    internal static void ArgNotAnEnumError(SemanticsAnalyzer s, int argIndex, IExpression arg, TypeInfo argType)
        => Error(s, ErrorCode.SemanticArgNotAnEnum, $"Argument {argIndex + 1}: type '{argType.ToPrettyString()}' is not an ENUM value", arg.Location);
    internal static void ArgNotAnEnumTypeError(SemanticsAnalyzer s, int argIndex, IExpression arg)
        => Error(s, ErrorCode.SemanticArgNotAnEnumType, $"Argument {argIndex + 1}: expected ENUM type name", arg.Location);
    internal static void ArgNotAnArrayError(SemanticsAnalyzer s, int argIndex, IExpression arg, TypeInfo argType)
        => Error(s, ErrorCode.SemanticArgNotAnArray, $"Argument {argIndex + 1}: type '{argType.ToPrettyString()}' is not an array", arg.Location);
    internal static void ArgNotANativeTypeValueError(SemanticsAnalyzer s, int argIndex, IExpression arg, TypeInfo argType)
        => Error(s, ErrorCode.SemanticArgNotANativeTypeValue, $"Argument {argIndex + 1}: type '{argType.ToPrettyString()}' is not a NATIVE type value", arg.Location);
    internal static void ArgNotAnNativeTypeError(SemanticsAnalyzer s, int argIndex, IExpression arg)
        => Error(s, ErrorCode.SemanticArgNotANativeType, $"Argument {argIndex + 1}: expected NATIVE type name", arg.Location);
    #endregion Errors
}
