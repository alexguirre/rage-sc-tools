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
internal sealed class ValueEmitter : Visitor
{
    private readonly CodeEmitter _C;

    public ValueEmitter(CodeEmitter codeEmitter) => _C = codeEmitter;

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
        if (node.Callee is NameExpression { Semantics.Declaration: IIntrinsicDeclaration intrinsic })
        {
            intrinsic.CodeGen(node, _C);
        }
        else if (node.Callee is NameExpression { Semantics.Declaration: FunctionDeclaration funcDecl })
        {
            EmitArgs(_C, node);
            _C.EmitCall(funcDecl);
        }
        else if (node.Callee is NameExpression { Semantics.Declaration: NativeFunctionDeclaration nativeDecl })
        {
            EmitArgs(_C, node);
            _C.EmitNativeCall(nativeDecl);
        }
        else
        {
            EmitArgs(_C, node);
            _C.EmitValue(node.Callee);
            _C.EmitIndirectCall();
        }

        static void EmitArgs(CodeEmitter c, InvocationExpression invocation)
        {
            var parameters = ((FunctionType)invocation.Callee.Type!).Parameters;
            var arguments = invocation.Arguments;
            foreach (var (p, a) in parameters.Zip(arguments))
            {
                EmitArg(c, p, a);
            }
        }

        static void EmitArg(CodeEmitter c, ParameterInfo param, IExpression arg)
        {
            if (arg.ArgumentKind is ArgumentKind.ByRef)
            {
                // pass by reference
                Debug.Assert(arg.ValueKind.Is(ValueKind.Addressable));
                c.EmitAddress(arg);
            }
            else if(arg.ArgumentKind is ArgumentKind.ByValue)
            {
                // pass by value
                c.EmitValue(arg);
            }
            else
            {
                Debug.Assert(false, "No semantic analysis on the arg?");
            }
        }
    }

    public override void Visit(UnaryExpression node)
    {
        if (node.Operator is UnaryOperator.LogicalNot)
        {
            _C.EmitValue(node.SubExpression);
            //_C.EmitNOT();
        }
        else
        {
            throw new NotImplementedException(nameof(UnaryExpression));
        }
    }

    public override void Visit(BinaryExpression node)
    {
        throw new NotImplementedException(nameof(BinaryExpression));
    }

    public override void Visit(NameExpression node)
    {
        if (node.Semantics.ValueKind.Is(ValueKind.Addressable))
        {
            _C.EmitLoadFrom(node);
        }
        else
        {
            switch (node.Semantics.Declaration)
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
