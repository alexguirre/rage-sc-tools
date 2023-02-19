namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

// TODO: implement intrinsics
// HASH_ENUM_TO_INT_INDEX (HASH_ENUM) -> INT
// INT_INDEX_TO_HASH_ENUM (ENUNNAME, INT) -> HASH_ENUM

public interface IIntrinsic : ISymbol
{
    ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker expressionTypeChecker);
    /// <summary>
    /// Evaluates the invocation at compile-time. Can be assumed that <see cref="InvocationTypeCheck"/> was successful.
    /// </summary>
    ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics);
    /// <summary>
    /// Generates the code for the invocation. Can be assumed that <see cref="InvocationTypeCheck"/> was successful.
    /// </summary>
    void CodeGen(InvocationExpression node, ICodeEmitter codeEmitter);
}

public static partial class Intrinsics
{
    // Casts
    public static IIntrinsic I2F { get; } = new IntrinsicI2F();
    public static IIntrinsic F2I { get; } = new IntrinsicF2I();
    public static IIntrinsic F2V { get; } = new IntrinsicF2V();

    // Type Utilities
    public static IIntrinsic SIZE_OF { get; } = new IntrinsicSIZE_OF();

    // Array Utilities
    public static IIntrinsic COUNT_OF { get; } = new IntrinsicCOUNT_OF();

    // Enum Utilities
    public static IIntrinsic ENUM_TO_INT { get; } = new IntrinsicENUM_TO_INT();
    public static IIntrinsic INT_TO_ENUM { get; } = new IntrinsicINT_TO_ENUM();
    public static IIntrinsic ENUM_TO_STRING { get; } = new IntrinsicENUM_TO_STRING();

    // Text Label Utilities
    public static IIntrinsic TEXT_LABEL_ASSIGN_STRING { get; } = new IntrinsicTEXT_LABEL_ASSIGN_STRING();
    public static IIntrinsic TEXT_LABEL_ASSIGN_INT { get; } = new IntrinsicTEXT_LABEL_ASSIGN_INT();
    public static IIntrinsic TEXT_LABEL_APPEND_STRING { get; } = new IntrinsicTEXT_LABEL_APPEND_STRING();
    public static IIntrinsic TEXT_LABEL_APPEND_INT { get; } = new IntrinsicTEXT_LABEL_APPEND_INT();

    // Bit Utilities
    public static IIntrinsic IS_BIT_SET { get; } = new IntrinsicIS_BIT_SET();

    // Native Type Utilities
    public static IIntrinsic NATIVE_TO_INT { get; } = new IntrinsicNATIVE_TO_INT();
    public static IIntrinsic INT_TO_NATIVE { get; } = new IntrinsicINT_TO_NATIVE();

    // Compile-time hashing
    public static IIntrinsic HASH { get; } = new IntrinsicHASH();

    // Exception Handling
    public static IIntrinsic CATCH { get; } = new IntrinsicCATCH();
    public static IIntrinsic THROW { get; } = new IntrinsicTHROW();

    public static ImmutableArray<IIntrinsic> All { get; } = ImmutableArray.Create(
        I2F, F2I, F2V,
        SIZE_OF,
        COUNT_OF,
        ENUM_TO_INT, INT_TO_ENUM, ENUM_TO_STRING,
        TEXT_LABEL_ASSIGN_STRING, TEXT_LABEL_ASSIGN_INT, TEXT_LABEL_APPEND_STRING, TEXT_LABEL_APPEND_INT,
        IS_BIT_SET,
        NATIVE_TO_INT, INT_TO_NATIVE,
        HASH,
        CATCH, THROW);

    private static void IntrinsicUsagePrecondition(IIntrinsic intrinsic, InvocationExpression node, [CallerArgumentExpression("node")] string? paramName = null)
    {
        if (node.Callee is not NameExpression nameExpr || !ReferenceEquals(nameExpr.Semantics.Symbol, intrinsic))
        {
            throw new ArgumentException("Expected a call to this intrinsic.", paramName);
        }
    }

    [DoesNotReturn]
    private static void ThrowCannotBeConstantEvaluated(IIntrinsic intrinsic)
        => throw new NotSupportedException($"Intrinsic '{intrinsic.Name}' cannot be constant-evaluated.");

    private abstract class BaseIntrinsic : IIntrinsic
    {
        public string Name { get; }

        public BaseIntrinsic(string name)
        {
            Name = name;
        }

        public abstract ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker expressionTypeChecker);
        public virtual ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            ThrowCannotBeConstantEvaluated(this);
            return null;
        }
        public abstract void CodeGen(InvocationExpression node, ICodeEmitter codeEmitter);

        protected void UsagePrecondition(InvocationExpression node, [CallerArgumentExpression("node")] string? paramName = null)
            => IntrinsicUsagePrecondition(this, node, paramName);
    }

    private abstract class BaseFunctionLikeIntrinsic : BaseIntrinsic
    {
        public FunctionType Type { get; }
        public bool CanBeConstant { get; }

        public BaseFunctionLikeIntrinsic(string name, FunctionType functionType, bool canBeConstant = true) : base(name)
        {
            Type = functionType;
            CanBeConstant = canBeConstant;
        }

        public sealed override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            UsagePrecondition(node);

            // NOTE: invocation type-checking code copied from ExpressionTypeChecker
            var argTypes = node.Arguments.Select(a => a.Accept(exprTypeChecker, semantics)).ToArray();

            // type-check arguments
            var parameters = Type.Parameters;
            var args = node.Arguments;
            ExpressionTypeChecker.CheckArgumentCount(parameters.Length, node, semantics);

            var n = Math.Min(args.Length, parameters.Length);
            var constantFlag = CanBeConstant ? ValueKind.Constant : 0;
            for (int i = 0; i < n; i++)
            {
                ExpressionTypeChecker.TypeCheckArgumentAgainstParameter(i, args[i], parameters[i], semantics);

                constantFlag &= args[i].ValueKind;
            }

            return new(Type.Return, ValueKind.RValue | constantFlag, ArgumentKind.None);
        }
    }
}
