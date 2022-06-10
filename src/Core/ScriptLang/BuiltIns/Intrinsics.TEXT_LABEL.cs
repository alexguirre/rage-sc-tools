namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;
using System.Linq;

public static partial class Intrinsics
{
    private static void TypeCheckTextLabelOfAnyLengthReferenceParameter(int argIndex, IExpression arg, SemanticsAnalyzer s)
    {
        var argType = arg.Type;
        if (argType is null)
        {
            throw new ArgumentException($"Argument type is null, argument expression was not type-checked yet.", nameof(arg));
        }

        if (argType.IsError)
        {
            return;
        }

        // pass text label by reference
        if (argType is not TextLabelType)
        {
            ExpressionTypeChecker.ArgCannotPassRefTextLabelError(s, argIndex, arg, argType);
        }
        else if (!arg.Semantics.ValueKind.Is(ValueKind.Addressable))
        {
            ExpressionTypeChecker.ArgCannotPassNonLValueToRefParamError(s, argIndex, arg);
        }

        arg.Semantics = arg.Semantics with { ArgumentKind = ArgumentKind.ByRef };
    }

    /// <summary>
    /// Type-checks an invocation against the signature (TEXT_LABEL_n&, <paramref name="secondParamType"/>) -> VOID
    /// </summary>
    private static ExpressionSemantics TypeCheckInvocationWithTextLabelRefParameter(IIntrinsic intrinsic, InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker, TypeInfo secondParamType)
    {
        IntrinsicUsagePrecondition(intrinsic, node);

        node.Arguments.ForEach(a => a.Accept(exprTypeChecker, semantics));

        // type-check arguments
        var args = node.Arguments;
        ExpressionTypeChecker.CheckArgumentCount(parameterCount: 2, node, semantics);

        TypeInfo returnType = ErrorType.Instance;
        if (args.Length > 0)
        {
            TypeCheckTextLabelOfAnyLengthReferenceParameter(0, args[0], semantics);
        }

        if (args.Length > 1)
        {
            ExpressionTypeChecker.TypeCheckArgumentAgainstParameter(1, args[1], new(secondParamType, IsReference: false), semantics);
        }

        return new(VoidType.Instance, ValueKind.RValue, ArgumentKind.None);
    }

    /// <summary>
    /// Assigns a STRING to a TEXT_LABEL_n.
    /// <br/>
    /// Signature: TEXT_LABEL_ASSIGN_STRING (TEXT_LABEL_n&, STRING) -> VOID
    /// </summary>
    private sealed class IntrinsicTEXT_LABEL_ASSIGN_STRING : BaseIntrinsic
    {
        public new const string Name = "TEXT_LABEL_ASSIGN_STRING";

        public IntrinsicTEXT_LABEL_ASSIGN_STRING() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
            => TypeCheckInvocationWithTextLabelRefParameter(this, node, semantics, exprTypeChecker, StringType.Instance);

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
            => c.EmitTextLabelAssignString(destinationTextLabel: node.Arguments[0], sourceString: node.Arguments[1]);
    }

    /// <summary>
    /// Assings the string representation of an INT to a TEXT_LABEL_n.
    /// <br/>
    /// Signature: TEXT_LABEL_ASSIGN_INT (TEXT_LABEL_n&, INT) -> VOID
    /// </summary>
    private sealed class IntrinsicTEXT_LABEL_ASSIGN_INT : BaseIntrinsic
    {
        public new const string Name = "TEXT_LABEL_ASSIGN_INT";

        public IntrinsicTEXT_LABEL_ASSIGN_INT() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
            => TypeCheckInvocationWithTextLabelRefParameter(this, node, semantics, exprTypeChecker, IntType.Instance);

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
            => c.EmitTextLabelAssignInt(destinationTextLabel: node.Arguments[0], sourceInt: node.Arguments[1]);
    }

    /// <summary>
    /// Appends a STRING to a TEXT_LABEL_n.
    /// <br/>
    /// Signature: TEXT_LABEL_APPEND_STRING (TEXT_LABEL_n&, STRING) -> VOID
    /// </summary>
    private sealed class IntrinsicTEXT_LABEL_APPEND_STRING : BaseIntrinsic
    {
        public new const string Name = "TEXT_LABEL_APPEND_STRING";

        public IntrinsicTEXT_LABEL_APPEND_STRING() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
            => TypeCheckInvocationWithTextLabelRefParameter(this, node, semantics, exprTypeChecker, StringType.Instance);

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
            => c.EmitTextLabelAppendString(destinationTextLabel: node.Arguments[0], sourceString: node.Arguments[1]);
    }
    
    /// <summary>
    /// Appends the string representation of an INT to a TEXT_LABEL_n.
    /// <br/>
    /// Signature: TEXT_LABEL_APPEND_INT (TEXT_LABEL_n&, INT) -> VOID
    /// </summary>
    private sealed class IntrinsicTEXT_LABEL_APPEND_INT : BaseIntrinsic
    {
        public new const string Name = "TEXT_LABEL_APPEND_INT";

        public IntrinsicTEXT_LABEL_APPEND_INT() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
            => TypeCheckInvocationWithTextLabelRefParameter(this, node, semantics, exprTypeChecker, IntType.Instance);

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
            => c.EmitTextLabelAppendInt(destinationTextLabel: node.Arguments[0], sourceInt: node.Arguments[1]);
    }
}
