namespace ScTools.ScriptLang.CodeGen;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.BuiltIns;
using ScTools.ScriptLang.Types;

/// <summary>
/// Emits code to push the value of expressions.
/// </summary>
internal sealed class ValueEmitter : AstVisitor
{
    private readonly ICodeEmitter _C;
    private readonly Semantics.LabelGenerator shortCircuitLabelGen = new() { Prefix = "vsc_lbl" };

    public ValueEmitter(ICodeEmitter codeEmitter) => _C = codeEmitter;

    public override void Visit(IntLiteralExpression node) => _C.EmitPushInt(node.Value);
    public override void Visit(FloatLiteralExpression node) => _C.EmitPushFloat(node.Value);
    public override void Visit(BoolLiteralExpression node) => _C.EmitPushBool(node.Value);
    public override void Visit(NullExpression node) => _C.EmitPushNull();
    public override void Visit(StringLiteralExpression node) => _C.EmitPushString(node.Value);

    public override void Visit(VectorExpression node)
    {
        node.X.Accept(this);
        node.Y.Accept(this);
        node.Z.Accept(this);
    }

    public override void Visit(FieldAccessExpression node)
    {
        _C.EmitLoadFrom(node);
    }

    public override void Visit(IndexingExpression node)
    {
        _C.EmitLoadFrom(node);
    }

    public override void Visit(InvocationExpression node)
    {
        if (node.Callee is NameExpression { Semantics.Symbol: IIntrinsic intrinsic })
        {
            intrinsic.CodeGen(node, _C);
        }
        else if (node.Callee is NameExpression { Semantics.Symbol: FunctionDeclaration funcDecl })
        {
            EmitArgs(_C, node, (FunctionType)funcDecl.Semantics.ValueType!);
            _C.EmitCall(funcDecl);
        }
        else if (node.Callee is NameExpression { Semantics.Symbol: NativeFunctionDeclaration nativeDecl })
        {
            EmitArgs(_C, node, (FunctionType)nativeDecl.Semantics.ValueType!);
            _C.EmitNativeCall(nativeDecl);
        }
        else
        {
            EmitArgs(_C, node, (FunctionType)node.Callee.Type!);
            _C.EmitValue(node.Callee);
            _C.EmitIndirectCall();
        }

        static void EmitArgs(ICodeEmitter c, InvocationExpression invocation, FunctionType funcTy)
        {
            for (int i = 0; i < funcTy.Parameters.Length; i++)
            {
                if (i < invocation.Arguments.Length)
                {
                    c.EmitArg(invocation.Arguments[i]);
                }
                else
                {
                    Debug.Assert(funcTy.Parameters[i].IsOptional);
                    c.EmitValue(funcTy.Parameters[i].OptionalInitializer!);
                }
            }
        }
    }

    public override void Visit(UnaryExpression node)
    {
        _C.EmitValue(node.SubExpression);
        _C.EmitUnaryOp(node.Operator, node.SubExpression.Type!);
    }

    public override void Visit(BinaryExpression node)
    {
        var lhsType = node.LHS.Semantics.Type;
        var rhsType = node.LHS.Semantics.Type;
        TypeInfo operandsType;
        if (lhsType is EnumType or NativeType || rhsType is EnumType or NativeType)
        {
            operandsType = IntType.Instance;
        }
        else if (lhsType is FloatType || rhsType is FloatType)
        {
            operandsType = FloatType.Instance;
        }
        else if (lhsType is IntType || rhsType is IntType)
        {
            operandsType = IntType.Instance;
        }
        else if (lhsType is BoolType || rhsType is BoolType)
        {
            operandsType = BoolType.Instance;
        }
        else if (lhsType is VectorType || rhsType is VectorType)
        {
            operandsType = VectorType.Instance;
        }
        else
        {
            throw new InvalidOperationException("No common operand type found for binary expression");
        }

        if (node.Operator is BinaryOperator.LogicalAnd)
        {
            // short-circuit AND
            var skipLabel = shortCircuitLabelGen.NextLabel();
            _C.EmitValue(node.LHS);
            _C.EmitDup();
            _C.EmitJumpIfZero(skipLabel);
            _C.EmitValue(node.RHS);
            _C.EmitBinaryOp(BinaryOperator.LogicalAnd, BoolType.Instance);
            _C.Label(skipLabel);
        }
        else if (node.Operator is BinaryOperator.LogicalOr)
        {
            // short-circuit OR
            var skipLabel = shortCircuitLabelGen.NextLabel();
            _C.EmitValue(node.LHS);
            _C.EmitDup();
            _C.EmitUnaryOp(UnaryOperator.LogicalNot, BoolType.Instance);
            _C.EmitJumpIfZero(skipLabel);
            _C.EmitValue(node.RHS);
            _C.EmitBinaryOp(BinaryOperator.LogicalOr, BoolType.Instance);
            _C.Label(skipLabel);
        }
        else
        {
            EmitValueAndPromote(node.LHS, operandsType);
            EmitValueAndPromote(node.RHS, operandsType);
            _C.EmitBinaryOp(node.Operator, operandsType);
        }

        void EmitValueAndPromote(IExpression expr, TypeInfo targetType)
        {
            _C.EmitValue(expr);
            if (expr.Semantics.Type is IntType && targetType is FloatType) // only valid promotion, INT -> FLOAT
            {
                _C.EmitCastIntToFloat();
            }
        }
    }

    public override void Visit(NameExpression node)
    {
        if (node.Semantics.ValueKind.Is(ValueKind.Addressable))
        {
            _C.EmitLoadFrom(node);
        }
        else
        {
            switch (node.Semantics.Symbol)
            {
                case FunctionDeclaration func:
                    _C.EmitFunctionAddress(func);
                    break;
                case IValueDeclaration { Semantics.ConstantValue: not null } val:
                    _C.EmitPushConst(val.Semantics.ConstantValue);
                    break;
                default:
                    throw new NotImplementedException($"Unsupported declaration");
            }
        }
    }
}
