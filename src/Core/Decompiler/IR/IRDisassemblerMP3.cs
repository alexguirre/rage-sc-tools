namespace ScTools.Decompiler.IR;

using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using ScTools.GameFiles.MP3;
using ScTools.ScriptAssembly;
using ScTools.ScriptAssembly.Targets.MP3;
using System.Collections.Immutable;

public sealed class IRDisassemblerMP3
{
    public static IRScript Disassemble(Script script) => new IRDisassemblerMP3(script).Disassemble();

    private Script Script { get; }

    private IRDisassemblerMP3(Script sc)
    {
        Script = sc ?? throw new ArgumentNullException(nameof(sc));
    }

    private IRScript Disassemble()
    {
        var sc = new IRScript();
        if (Script.CodeLength == 0)
        {
            return sc;
        }

        foreach (var inst in Script.EnumerateInstructions())
        {
            DisassembleInstruction(sc, inst.Address, inst.Bytes);
        }
        sc.AppendInstruction(new IREndOfScript((int)Script.CodeLength));
        return sc;
    }

    private void DisassembleInstruction(IRScript script, int ip, ReadOnlySpan<byte> inst)
    {
        var opcode = (Opcode)inst[0];

        switch (opcode)
        {
            case Opcode.NOP:
                script.AppendInstruction(new IRNop(ip));
                break;
            case Opcode.LEAVE:
                var leave = opcode.GetLeaveOperands(inst);
                script.AppendInstruction(new IRLeave(ip, leave.ParamCount, leave.ReturnCount));
                break;
            case Opcode.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                script.AppendInstruction(new IREnter(ip, enter.ParamCount, enter.FrameSize - 2, opcode.GetEnterFunctionName(inst)));
                break;
            case Opcode.PUSH_CONST_U16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU16Operand(inst)));
                break;
            case Opcode.PUSH_CONST_U32:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU32Operand(inst))));
                break;
            case Opcode.PUSH_CONST_F:
                script.AppendInstruction(new IRPushFloat(ip, opcode.GetFloatOperand(inst)));
                break;
            case Opcode.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                script.AppendInstruction(new IRNativeCall(ip, native.ParamCount, native.ReturnCount, native.CommandHash));
                break;
            case Opcode.J:
                var jumpAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRJump(ip, jumpAddr));
                break;
            case Opcode.JZ:
                var jzAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRJumpIfZero(ip, jzAddr));
                break;
            case Opcode.JNZ:
                var jnzAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRINot(ip));
                script.AppendInstruction(new IRJumpIfZero(ip, jnzAddr));
                break;
            case Opcode.CALL:
                var callAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRCall(ip, callAddr));
                break;
            case Opcode.SWITCH:
                var cases = ImmutableArray.CreateBuilder<IRSwitchCase>(opcode.GetSwitchNumberOfCases(inst));
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    cases.Add(new(unchecked((int)c.Value), c.JumpAddress));
                }
                script.AppendInstruction(new IRSwitch(ip, cases.MoveToImmutable()));
                break;
                
            case Opcode.STRING: script.AppendInstruction(new IRPushString(ip, opcode.GetStringOperand(inst))); break;
            case Opcode.IADD: script.AppendInstruction(new IRIAdd(ip)); break;
            case Opcode.ISUB: script.AppendInstruction(new IRISub(ip)); break;
            case Opcode.IMUL: script.AppendInstruction(new IRIMul(ip)); break;
            case Opcode.IDIV: script.AppendInstruction(new IRIDiv(ip)); break;
            case Opcode.IMOD: script.AppendInstruction(new IRIMod(ip)); break;
            case Opcode.INOT: script.AppendInstruction(new IRINot(ip)); break;
            case Opcode.INEG: script.AppendInstruction(new IRINeg(ip)); break;
            case Opcode.IEQ: script.AppendInstruction(new IRIEqual(ip)); break;
            case Opcode.INE: script.AppendInstruction(new IRINotEqual(ip)); break;
            case Opcode.IGT: script.AppendInstruction(new IRIGreaterThan(ip)); break;
            case Opcode.IGE: script.AppendInstruction(new IRIGreaterOrEqual(ip)); break;
            case Opcode.ILT: script.AppendInstruction(new IRILessThan(ip)); break;
            case Opcode.ILE: script.AppendInstruction(new IRILessOrEqual(ip)); break;
            case Opcode.FADD: script.AppendInstruction(new IRFAdd(ip)); break;
            case Opcode.FSUB: script.AppendInstruction(new IRFSub(ip)); break;
            case Opcode.FMUL: script.AppendInstruction(new IRFMul(ip)); break;
            case Opcode.FDIV: script.AppendInstruction(new IRFDiv(ip)); break;
            case Opcode.FMOD: script.AppendInstruction(new IRFMod(ip)); break;
            case Opcode.FNEG: script.AppendInstruction(new IRFNeg(ip)); break;
            case Opcode.FEQ: script.AppendInstruction(new IRFEqual(ip)); break;
            case Opcode.FNE: script.AppendInstruction(new IRFNotEqual(ip)); break;
            case Opcode.FGT: script.AppendInstruction(new IRFGreaterThan(ip)); break;
            case Opcode.FGE: script.AppendInstruction(new IRFGreaterOrEqual(ip)); break;
            case Opcode.FLT: script.AppendInstruction(new IRFLessThan(ip)); break;
            case Opcode.FLE: script.AppendInstruction(new IRFLessOrEqual(ip)); break;
            case Opcode.VADD: script.AppendInstruction(new IRVAdd(ip)); break;
            case Opcode.VSUB: script.AppendInstruction(new IRVSub(ip)); break;
            case Opcode.VMUL: script.AppendInstruction(new IRVMul(ip)); break;
            case Opcode.VDIV: script.AppendInstruction(new IRVDiv(ip)); break;
            case Opcode.VNEG: script.AppendInstruction(new IRVNeg(ip)); break;
            case Opcode.IAND: script.AppendInstruction(new IRIAnd(ip)); break;
            case Opcode.IOR: script.AppendInstruction(new IRIOr(ip)); break;
            case Opcode.IXOR: script.AppendInstruction(new IRIXor(ip)); break;
            case Opcode.I2F: script.AppendInstruction(new IRIntToFloat(ip)); break;
            case Opcode.F2I: script.AppendInstruction(new IRFloatToInt(ip)); break;
            case Opcode.F2V: script.AppendInstruction(new IRFloatToVector(ip)); break;
            case Opcode.DUP: script.AppendInstruction(new IRDup(ip)); break;
            case Opcode.DROP: script.AppendInstruction(new IRDrop(ip)); break;
            case Opcode.LOAD: script.AppendInstruction(new IRLoad(ip)); break;
            case Opcode.STORE: script.AppendInstruction(new IRStore(ip)); break;
            case Opcode.STORE_REV: script.AppendInstruction(new IRStoreRev(ip)); break;
            case Opcode.LOAD_N: script.AppendInstruction(new IRLoadN(ip)); break;
            case Opcode.STORE_N: script.AppendInstruction(new IRStoreN(ip)); break;

            case Opcode.LOCAL_0:
            case Opcode.LOCAL_1:
            case Opcode.LOCAL_2:
            case Opcode.LOCAL_3:
            case Opcode.LOCAL_4:
            case Opcode.LOCAL_5:
            case Opcode.LOCAL_6:
            case Opcode.LOCAL_7:
                script.AppendInstruction(new IRLocalRef(ip, opcode - Opcode.LOCAL_0));
                break;

            case Opcode.LOCAL: script.AppendInstruction(new IRLocalRefFromStack(ip)); break;
            case Opcode.STATIC: script.AppendInstruction(new IRStaticRefFromStack(ip)); break;
            case Opcode.GLOBAL: script.AppendInstruction(new IRGlobalRefFromStack(ip)); break;
            case Opcode.ARRAY: script.AppendInstruction(new IRArrayItemRefSizeInStack(ip)); break;
            case Opcode.NULL: script.AppendInstruction(new IRNullRef(ip)); break;
            case Opcode.TEXT_LABEL_ASSIGN_STRING: script.AppendInstruction(new IRTextLabelAssignString(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_ASSIGN_INT: script.AppendInstruction(new IRTextLabelAssignInt(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_APPEND_STRING: script.AppendInstruction(new IRTextLabelAppendString(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_APPEND_INT: script.AppendInstruction(new IRTextLabelAppendInt(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_COPY: script.AppendInstruction(new IRTextLabelCopy(ip)); break;
            case Opcode.CALLINDIRECT: script.AppendInstruction(new IRCallIndirect(ip)); break;
            case >= Opcode.PUSH_CONST_M16 and <= Opcode.PUSH_CONST_159:
                script.AppendInstruction(new IRPushInt(ip, (int)opcode - (int)Opcode.PUSH_CONST_0));
                break;

            case Opcode.CATCH: script.AppendInstruction(new IRCatch(ip)); break;
            case Opcode.THROW: script.AppendInstruction(new IRThrow(ip)); break;

            default:
                throw new NotImplementedException(opcode.ToString());
        }
    }
}
