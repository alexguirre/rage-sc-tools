namespace ScTools.Tests.Decompiler;

using System;
using ScTools.Decompiler.IR;

internal sealed class IRCodeBuilder
{
    private readonly List<IRInstruction> instructions = new();
    private readonly Dictionary<IRInstruction, string> labelRefs = new();
    private readonly Dictionary<string, int> labels = new();
    private int address = 0;
    private bool done = false;
    
    private IRCodeBuilder AppendInstruction(IRInstruction instruction)
    {
        if (done)
        {
            throw new InvalidOperationException($"Cannot append instructions after calling {nameof(Build)}");
        }
        
        instructions.Add(instruction);
        address++;
        return this;
    }

    private IRCodeBuilder AppendInstructionWithLabelRef(IRInstruction instruction, string label)
    {
        Debug.Assert(instruction is IRJump or IRJumpIfZero or IRCall);
        AppendInstruction(instruction);
        labelRefs.Add(instruction, label);
        return this;
    }

    private string? GetLabelAt(int address)
    {
        foreach (var (label, labelAddress) in labels)
        {
            if (labelAddress == address)
            {
                return label;
            }
        }

        return null;
    }

    public IRCodeBuilder Label(string name)
    {
        labels.Add(name, address);
        return this;
    }

    public IRCodeBuilder Nop() => AppendInstruction(new IRNop(address));
    public IRCodeBuilder Jump(string label) => AppendInstructionWithLabelRef(new IRJump(address, 0), label);
    public IRCodeBuilder JumpIfZero(string label) => AppendInstructionWithLabelRef(new IRJumpIfZero(address, 0), label);
    public IRCodeBuilder Call(string label) => AppendInstructionWithLabelRef(new IRCall(address, 0), label);
    public IRCodeBuilder CallIndirect() => AppendInstruction(new IRCallIndirect(address));
    public IRCodeBuilder NativeCall(int paramCount, int returnCount, ulong command) => AppendInstruction(new IRNativeCall(address, paramCount, returnCount, command));
    public IRCodeBuilder Enter(int paramCount, int localCount) => AppendInstruction(new IREnter(address, paramCount, localCount, GetLabelAt(address)));
    public IRCodeBuilder Leave(int paramCount, int returnCount) => AppendInstruction(new IRLeave(address, paramCount, returnCount));
    public IRCodeBuilder PushInt(int value) => AppendInstruction(new IRPushInt(address, value));
    public IRCodeBuilder PushFloat(float value) => AppendInstruction(new IRPushFloat(address, value));
    public IRCodeBuilder PushString(string value) => AppendInstruction(new IRPushString(address, value));
    public IRCodeBuilder PushStringFromStringTable() => AppendInstruction(new IRPushStringFromStringTable(address));
    public IRCodeBuilder IAdd() => AppendInstruction(new IRIAdd(address));
    public IRCodeBuilder ISub() => AppendInstruction(new IRISub(address));
    public IRCodeBuilder IMul() => AppendInstruction(new IRIMul(address));
    public IRCodeBuilder IDiv() => AppendInstruction(new IRIDiv(address));
    public IRCodeBuilder IMod() => AppendInstruction(new IRIMod(address));
    public IRCodeBuilder IAnd() => AppendInstruction(new IRIAnd(address));
    public IRCodeBuilder IOr() => AppendInstruction(new IRIOr(address));
    public IRCodeBuilder IXor() => AppendInstruction(new IRIXor(address));
    public IRCodeBuilder IBitTest() => AppendInstruction(new IRIBitTest(address));
    public IRCodeBuilder INot() => AppendInstruction(new IRINot(address));
    public IRCodeBuilder INeg() => AppendInstruction(new IRINeg(address));
    public IRCodeBuilder IEqual() => AppendInstruction(new IRIEqual(address));
    public IRCodeBuilder INotEqual() => AppendInstruction(new IRINotEqual(address));
    public IRCodeBuilder ILessThan() => AppendInstruction(new IRILessThan(address));
    public IRCodeBuilder ILessOrEqual() => AppendInstruction(new IRILessOrEqual(address));
    public IRCodeBuilder IGreaterThan() => AppendInstruction(new IRIGreaterThan(address));
    public IRCodeBuilder IGreaterOrEqual() => AppendInstruction(new IRIGreaterOrEqual(address));
    public IRCodeBuilder FAdd() => AppendInstruction(new IRFAdd(address));
    public IRCodeBuilder FSub() => AppendInstruction(new IRFSub(address));
    public IRCodeBuilder FMul() => AppendInstruction(new IRFMul(address));
    public IRCodeBuilder FDiv() => AppendInstruction(new IRFDiv(address));
    public IRCodeBuilder FMod() => AppendInstruction(new IRFMod(address));
    public IRCodeBuilder FEqual() => AppendInstruction(new IRFEqual(address));
    public IRCodeBuilder FNotEqual() => AppendInstruction(new IRFNotEqual(address));
    public IRCodeBuilder FLessThan() => AppendInstruction(new IRFLessThan(address));
    public IRCodeBuilder FLessOrEqual() => AppendInstruction(new IRFLessOrEqual(address));
    public IRCodeBuilder FGreaterThan() => AppendInstruction(new IRFGreaterThan(address));
    public IRCodeBuilder FGreaterOrEqual() => AppendInstruction(new IRFGreaterOrEqual(address));
    public IRCodeBuilder FNeg() => AppendInstruction(new IRFNeg(address));
    public IRCodeBuilder VAdd() => AppendInstruction(new IRVAdd(address));
    public IRCodeBuilder VSub() => AppendInstruction(new IRVSub(address));
    public IRCodeBuilder VMul() => AppendInstruction(new IRVMul(address));
    public IRCodeBuilder VDiv() => AppendInstruction(new IRVDiv(address));
    public IRCodeBuilder VNeg() => AppendInstruction(new IRVNeg(address));
    public IRCodeBuilder IntToFloat() => AppendInstruction(new IRIntToFloat(address));
    public IRCodeBuilder FloatToInt() => AppendInstruction(new IRFloatToInt(address));
    public IRCodeBuilder FloatToVector() => AppendInstruction(new IRFloatToVector(address));
    public IRCodeBuilder Dup() => AppendInstruction(new IRDup(address));
    public IRCodeBuilder Drop() => AppendInstruction(new IRDrop(address));
    public IRCodeBuilder Load() => AppendInstruction(new IRLoad(address));
    public IRCodeBuilder Store() => AppendInstruction(new IRStore(address));
    public IRCodeBuilder StoreRev() => AppendInstruction(new IRStoreRev(address));
    public IRCodeBuilder LoadN() => AppendInstruction(new IRLoadN(address));
    public IRCodeBuilder StoreN() => AppendInstruction(new IRStoreN(address));
    public IRCodeBuilder XProtectLoad() => AppendInstruction(new IRXProtectLoad(address));
    public IRCodeBuilder XProtectStore() => AppendInstruction(new IRXProtectStore(address));
    public IRCodeBuilder XProtectRef() => AppendInstruction(new IRXProtectRef(address));
    public IRCodeBuilder LocalRef(int varAddress) => AppendInstruction(new IRLocalRef(address, varAddress));
    public IRCodeBuilder StaticRef(int varAddress) => AppendInstruction(new IRStaticRef(address, varAddress));
    public IRCodeBuilder GlobalRef(int varAddress) => AppendInstruction(new IRGlobalRef(address, varAddress));
    public IRCodeBuilder LocalRefFromStack() => AppendInstruction(new IRLocalRefFromStack(address));
    public IRCodeBuilder StaticRefFromStack() => AppendInstruction(new IRStaticRefFromStack(address));
    public IRCodeBuilder GlobalRefFromStack() => AppendInstruction(new IRGlobalRefFromStack(address));
    public IRCodeBuilder ArrayItemRefSizeInStack() => AppendInstruction(new IRArrayItemRefSizeInStack(address));
    public IRCodeBuilder ArrayItemRef(int itemSize) => AppendInstruction(new IRArrayItemRef(address, itemSize));
    public IRCodeBuilder NullRef() => AppendInstruction(new IRNullRef(address));
    public IRCodeBuilder TextLabelAssignString(int textLabelLength) => AppendInstruction(new IRTextLabelAssignString(address, textLabelLength));
    public IRCodeBuilder TextLabelAssignInt(int textLabelLength) => AppendInstruction(new IRTextLabelAssignInt(address, textLabelLength));
    public IRCodeBuilder TextLabelAppendString(int textLabelLength) => AppendInstruction(new IRTextLabelAppendString(address, textLabelLength));
    public IRCodeBuilder TextLabelAppendInt(int textLabelLength) => AppendInstruction(new IRTextLabelAppendInt(address, textLabelLength));
    public IRCodeBuilder TextLabelCopy() => AppendInstruction(new IRTextLabelCopy(address));
    public IRCodeBuilder Catch() => AppendInstruction(new IRCatch(address));
    public IRCodeBuilder Throw() => AppendInstruction(new IRThrow(address));

    public IRCode Build()
    {
        AppendInstruction(new IREndOfScript(address));
        var code = new IRCode();
        foreach (var inst in instructions)
        {
            if (labelRefs.TryGetValue(inst, out var label))
            {
                if (!labels.TryGetValue(label, out var labelAddres))
                {
                    throw new InvalidOperationException($"Label '{label}' not found. Referenced by '{IRPrinter.PrintToString(inst)}'.");
                }

                IRInstruction newInst = inst switch
                {
                    IRJump j => j with { JumpAddress = labelAddres },
                    IRJumpIfZero jz => jz with { JumpAddress = labelAddres },
                    IRCall c => c with { CallAddress = labelAddres },
                    _ => throw new UnreachableException($"Instruction '{IRPrinter.PrintToString(inst)}' cannot reference labels"),
                };
                code.AppendInstruction(newInst);
            }
            else
            {
                code.AppendInstruction(inst);
            }
        }

        done = true;
        return code;
    }
}
