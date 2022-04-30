namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

// TODO: implement intrinsics
// APPEND (TEXT_LABEL_n, STRING|INT) -> VOID
// ENUM_TO_STRING (ENUM) -> STRING
// HASH_ENUM_TO_INT_INDEX (HASH_ENUM) -> INT
// INT_INDEX_TO_HASH_ENUM (ENUNNAME, INT) -> HASH_ENUM

public interface IIntrinsicDeclaration : IDeclaration
{
    ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker expressionTypeChecker);
    /// <summary>
    /// Evaluates the invocation at compile-time. Can be assumed that <see cref="InvocationTypeCheck"/> was successful.
    /// </summary>
    ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics);
}

public static partial class Intrinsics
{
    // Casts
    public static IIntrinsicDeclaration I2F { get; } = new IntrinsicI2F();
    public static IIntrinsicDeclaration F2I { get; } = new IntrinsicF2I();
    public static IIntrinsicDeclaration F2V { get; } = new IntrinsicF2V();

    // Type Utilities
    public static IIntrinsicDeclaration SIZE_OF { get; } = new IntrinsicSIZE_OF();

    // Array Utilities
    public static IIntrinsicDeclaration COUNT_OF { get; } = new IntrinsicCOUNT_OF();

    // Enum Utilities
    public static IIntrinsicDeclaration ENUM_TO_INT { get; } = new IntrinsicENUM_TO_INT();
    public static IIntrinsicDeclaration INT_TO_ENUM { get; } = new IntrinsicINT_TO_ENUM();

    // Bit Utilities
    public static IIntrinsicDeclaration IS_BIT_SET { get; } = new IntrinsicIS_BIT_SET();


    public static ImmutableArray<IIntrinsicDeclaration> All { get; } = ImmutableArray.Create(
        I2F, F2I, F2V,
        SIZE_OF,
        COUNT_OF,
        ENUM_TO_INT, INT_TO_ENUM,
        IS_BIT_SET);

    private abstract class BaseIntrinsic : IIntrinsicDeclaration
    {
        public string Name { get; }
        public Token NameToken => Token.Identifier(Name);
        public ImmutableArray<Token> Tokens => ImmutableArray<Token>.Empty;
        public ImmutableArray<INode> Children => ImmutableArray<INode>.Empty;
        public SourceRange Location => default;
        public string DebuggerDisplay => $"<intrinsic {Name}>";

        public BaseIntrinsic(string name)
        {
            Name = name;
        }

        public TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotSupportedException($"Cannot visit intrinsics");
        public void Accept(IVisitor visitor) => throw new NotSupportedException($"Cannot visit intrinsics");

        public abstract ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker expressionTypeChecker);
        public abstract ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics);
    }

    private abstract class BaseFunctionLikeIntrinsic : BaseIntrinsic
    {
        public FunctionType Type { get; }

        public BaseFunctionLikeIntrinsic(string name, FunctionType functionType) : base(name)
        {
            Type = functionType;
        }

        public sealed override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            Debug.Assert(node.Callee is NameExpression nameExpr && ReferenceEquals(nameExpr.Semantics.Declaration, this));

            // NOTE: invocation type-checking code copied from ExpressionTypeChecker
            var argTypes = node.Arguments.Select(a => a.Accept(exprTypeChecker, semantics)).ToArray();

            // type-check arguments
            var parameters = Type.Parameters;
            var args = node.Arguments;
            if (parameters.Length != args.Length)
            {
                ExpressionTypeChecker.MismatchedArgumentCountError(semantics, parameters.Length, node);
            }

            var n = Math.Min(args.Length, parameters.Length);
            var constantFlag = ValueKind.Constant;
            for (int i = 0; i < n; i++)
            {
                var param = parameters[i];
                var arg = args[i];
                var paramType = param.Type;
                var argType = argTypes[i];

                if (param.IsReference)
                {
                    // pass by reference
                    if (!paramType.IsRefAssignableFrom(argType))
                    {
                        ExpressionTypeChecker.ArgCannotPassRefTypeError(semantics, i, arg, argType, paramType);
                    }
                    else if (!arg.Semantics.ValueKind.Is(ValueKind.Addressable))
                    {
                        ExpressionTypeChecker.ArgCannotPassNonLValueToRefParamError(semantics, i, arg);
                    }
                }
                else if (paramType is ArrayType)
                {
                    Debug.Assert(arg.Semantics.ValueKind.Is(ValueKind.Addressable)); // all expression of array type should be lvalues

                    // pass array by reference
                    if (paramType != argType)
                    {
                        ExpressionTypeChecker.ArgCannotPassTypeError(semantics, i, arg, argType, paramType);
                    }
                    // TODO: check 'incomplete' array
                }
                else
                {
                    // pass by value
                    if (!paramType.IsAssignableFrom(argType))
                    {
                        ExpressionTypeChecker.ArgCannotPassTypeError(semantics, i, arg, argType, paramType);
                    }
                }

                constantFlag &= arg.ValueKind;
            }

            return new(Type.Return, ValueKind.RValue | constantFlag);
        }
    }
}
