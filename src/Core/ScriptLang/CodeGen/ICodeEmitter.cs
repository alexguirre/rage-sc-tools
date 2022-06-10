namespace ScTools.ScriptLang.CodeGen;

using ScTools.GameFiles;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

public interface ICodeEmitter
{
    IScript EmitScript(ScriptDeclaration script);

    void Label(string label);

    void EmitStatementBlock(ImmutableArray<IStatement> statements);
    void EmitStatement(IStatement stmt);

    void EmitEpilogue();

    int AllocateFrameSpaceForLocal(VarDeclaration varDecl);

    void EmitAssignment(IExpression destination, IExpression source);
    void EmitAssignmentToVar(VarDeclaration destination, IExpression source);

    void EmitDefaultInit(VarDeclaration declaration);

    void EmitVarAddress(VarDeclaration declaration);
    void EmitOffset(int offset);

    void EmitArrayIndexing(IndexingExpression expr);

    void EmitLoadFrom(IExpression lvalueExpr);
    void EmitStoreAt(IExpression lvalueExpr);

    void EmitValue(IExpression expr);
    void EmitValueAndDrop(IExpression expr);
    void EmitAddress(IExpression expr);
    void EmitArg(IExpression arg);

    // Control flow
    void EmitJump(string label);
    void EmitJumpIfZero(string label);
    void EmitCall(FunctionDeclaration function);
    void EmitFunctionAddress(FunctionDeclaration function);
    void EmitNativeCall(NativeFunctionDeclaration nativeFunction);
    void EmitIndirectCall();
    void EmitSwitch(IEnumerable<ValueSwitchCase> valueCases);

    // TEXT_LABEL_* support
    void EmitTextLabelAssignString(IExpression destinationTextLabel, IExpression sourceString);
    void EmitTextLabelAssignInt(IExpression destinationTextLabel, IExpression sourceInt);
    void EmitTextLabelAppendString(IExpression destinationTextLabel, IExpression sourceString);
    void EmitTextLabelAppendInt(IExpression destinationTextLabel, IExpression sourceInt);
    void EmitTextLabelCopy(IExpression destinationTextLabel, IExpression sourceTextLabel);

    // Operators
    void EmitUnaryOp(UnaryOperator unaryOp, TypeInfo operandType);
    void EmitBinaryOp(BinaryOperator binaryOp, TypeInfo operandsType);

    // Built-in casts
    void EmitCastIntToFloat();
    void EmitCastFloatToInt();
    void EmitCastFloatToVector();

    // Exception handling
    void EmitCatch();
    void EmitThrow();

    // Stack manipulation
    void EmitDup();
    void EmitDrop();

    // Literals and constants
    void EmitPushNull();
    void EmitPushBool(bool value);
    void EmitPushInt(int value);
    void EmitPushFloat(float value);
    void EmitPushString(string value);
    void EmitPushConst(ConstantValue value);
}
