namespace ScTools.Decompiler.IR;

using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using ScTools.GameFiles;
using ScTools.ScriptAssembly;
using System.Collections.Immutable;

public sealed class IRDisassemblerNY
{
    public static IRScript Disassemble(ScriptNY script) => new IRDisassemblerNY(script).Disassemble();

    private ScriptNY Script { get; }

    private IRDisassemblerNY(ScriptNY sc)
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
        var opcode = (OpcodeNY)inst[0];

        switch (opcode)
        {
            case OpcodeNY.LEAVE:
                var leave = opcode.GetLeaveOperands(inst);
                script.AppendInstruction(new IRLeave(ip, leave.ParamCount, leave.ReturnCount));
                break;
            case OpcodeNY.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                script.AppendInstruction(new IREnter(ip, enter.ParamCount, enter.FrameSize - 2));
                break;
            case OpcodeNY.PUSH_CONST_U16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU16Operand(inst)));
                break;
            case OpcodeNY.PUSH_CONST_U32:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU32Operand(inst))));
                break;
            case OpcodeNY.PUSH_CONST_F:
                script.AppendInstruction(new IRPushFloat(ip, opcode.GetFloatOperand(inst)));
                break;
            case OpcodeNY.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                script.AppendInstruction(new IRNativeCall(ip, native.ParamCount, native.ReturnCount, native.CommandHash));
                break;
            case OpcodeNY.J:
                var jumpAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRJump(ip, jumpAddr));
                break;
            case OpcodeNY.JZ:
                var jzAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRJumpIfZero(ip, jzAddr));
                break;
            case OpcodeNY.JNZ:
                var jnzAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRINot(ip));
                script.AppendInstruction(new IRJumpIfZero(ip, jnzAddr));
                break;
            case OpcodeNY.CALL:
                var callAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRCall(ip, callAddr));
                break;
            case OpcodeNY.SWITCH:
                var cases = ImmutableArray.CreateBuilder<IRSwitchCase>(opcode.GetSwitchNumberOfCases(inst));
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    cases.Add(new(unchecked((int)c.Value), c.JumpAddress));
                }
                script.AppendInstruction(new IRSwitch(ip, cases.MoveToImmutable()));
                break;
                
            case OpcodeNY.STRING: script.AppendInstruction(new IRPushString(ip, opcode.GetStringOperand(inst))); break;
            case OpcodeNY.IADD: script.AppendInstruction(new IRIAdd(ip)); break;
            case OpcodeNY.ISUB: script.AppendInstruction(new IRISub(ip)); break;
            case OpcodeNY.IMUL: script.AppendInstruction(new IRIMul(ip)); break;
            case OpcodeNY.IDIV: script.AppendInstruction(new IRIDiv(ip)); break;
            case OpcodeNY.IMOD: script.AppendInstruction(new IRIMod(ip)); break;
            case OpcodeNY.INOT: script.AppendInstruction(new IRINot(ip)); break;
            case OpcodeNY.INEG: script.AppendInstruction(new IRINeg(ip)); break;
            case OpcodeNY.IEQ: script.AppendInstruction(new IRIEqual(ip)); break;
            case OpcodeNY.INE: script.AppendInstruction(new IRINotEqual(ip)); break;
            case OpcodeNY.IGT: script.AppendInstruction(new IRIGreaterThan(ip)); break;
            case OpcodeNY.IGE: script.AppendInstruction(new IRIGreaterOrEqual(ip)); break;
            case OpcodeNY.ILT: script.AppendInstruction(new IRILessThan(ip)); break;
            case OpcodeNY.ILE: script.AppendInstruction(new IRILessOrEqual(ip)); break;
            case OpcodeNY.FADD: script.AppendInstruction(new IRFAdd(ip)); break;
            case OpcodeNY.FSUB: script.AppendInstruction(new IRFSub(ip)); break;
            case OpcodeNY.FMUL: script.AppendInstruction(new IRFMul(ip)); break;
            case OpcodeNY.FDIV: script.AppendInstruction(new IRFDiv(ip)); break;
            case OpcodeNY.FMOD: script.AppendInstruction(new IRFMod(ip)); break;
            case OpcodeNY.FNEG: script.AppendInstruction(new IRFNeg(ip)); break;
            case OpcodeNY.FEQ: script.AppendInstruction(new IRFEqual(ip)); break;
            case OpcodeNY.FNE: script.AppendInstruction(new IRFNotEqual(ip)); break;
            case OpcodeNY.FGT: script.AppendInstruction(new IRFGreaterThan(ip)); break;
            case OpcodeNY.FGE: script.AppendInstruction(new IRFGreaterOrEqual(ip)); break;
            case OpcodeNY.FLT: script.AppendInstruction(new IRFLessThan(ip)); break;
            case OpcodeNY.FLE: script.AppendInstruction(new IRFLessOrEqual(ip)); break;
            case OpcodeNY.VADD: script.AppendInstruction(new IRVAdd(ip)); break;
            case OpcodeNY.VSUB: script.AppendInstruction(new IRVSub(ip)); break;
            case OpcodeNY.VMUL: script.AppendInstruction(new IRVMul(ip)); break;
            case OpcodeNY.VDIV: script.AppendInstruction(new IRVDiv(ip)); break;
            case OpcodeNY.VNEG: script.AppendInstruction(new IRVNeg(ip)); break;
            case OpcodeNY.IAND: script.AppendInstruction(new IRIAnd(ip)); break;
            case OpcodeNY.IOR: script.AppendInstruction(new IRIOr(ip)); break;
            case OpcodeNY.IXOR: script.AppendInstruction(new IRIXor(ip)); break;
            case OpcodeNY.I2F: script.AppendInstruction(new IRIntToFloat(ip)); break;
            case OpcodeNY.F2I: script.AppendInstruction(new IRFloatToInt(ip)); break;
            case OpcodeNY.F2V: script.AppendInstruction(new IRFloatToVector(ip)); break;
            case OpcodeNY.DUP: script.AppendInstruction(new IRDup(ip)); break;
            case OpcodeNY.DROP: script.AppendInstruction(new IRDrop(ip)); break;
            case OpcodeNY.LOAD: script.AppendInstruction(new IRLoad(ip)); break;
            case OpcodeNY.STORE: script.AppendInstruction(new IRStore(ip)); break;
            case OpcodeNY.STORE_REV: script.AppendInstruction(new IRStoreRev(ip)); break;
            case OpcodeNY.LOAD_N: script.AppendInstruction(new IRLoadN(ip)); break;
            case OpcodeNY.STORE_N: script.AppendInstruction(new IRStoreN(ip)); break;

            case OpcodeNY._XPROTECT_LOAD: script.AppendInstruction(new IRXProtectLoad(ip)); break;
            case OpcodeNY._XPROTECT_STORE: script.AppendInstruction(new IRXProtectStore(ip)); break;
            case OpcodeNY._XPROTECT_REF: script.AppendInstruction(new IRXProtectRef(ip)); break;

            case OpcodeNY.LOCAL_0:
            case OpcodeNY.LOCAL_1:
            case OpcodeNY.LOCAL_2:
            case OpcodeNY.LOCAL_3:
            case OpcodeNY.LOCAL_4:
            case OpcodeNY.LOCAL_5:
            case OpcodeNY.LOCAL_6:
            case OpcodeNY.LOCAL_7:
                script.AppendInstruction(new IRLocalRef(ip, opcode - OpcodeNY.LOCAL_0));
                break;

            case OpcodeNY.LOCAL: script.AppendInstruction(new IRLocalRefFromStack(ip)); break;
            case OpcodeNY.STATIC: script.AppendInstruction(new IRStaticRefFromStack(ip)); break;
            case OpcodeNY.GLOBAL: script.AppendInstruction(new IRGlobalRefFromStack(ip)); break;
            case OpcodeNY.ARRAY: script.AppendInstruction(new IRArrayItemRefSizeInStack(ip)); break;
            case OpcodeNY.NULL: script.AppendInstruction(new IRNullRef(ip)); break;
            case OpcodeNY.TEXT_LABEL_ASSIGN_STRING: script.AppendInstruction(new IRTextLabelAssignString(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeNY.TEXT_LABEL_ASSIGN_INT: script.AppendInstruction(new IRTextLabelAssignInt(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeNY.TEXT_LABEL_APPEND_STRING: script.AppendInstruction(new IRTextLabelAppendString(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeNY.TEXT_LABEL_APPEND_INT: script.AppendInstruction(new IRTextLabelAppendInt(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeNY.TEXT_LABEL_COPY: script.AppendInstruction(new IRTextLabelCopy(ip)); break;
            case >= OpcodeNY.PUSH_CONST_M16 and <= OpcodeNY.PUSH_CONST_159:
                script.AppendInstruction(new IRPushInt(ip, (int)opcode - (int)OpcodeNY.PUSH_CONST_0));
                break;

            case OpcodeNY.CATCH: script.AppendInstruction(new IRCatch(ip)); break;
            case OpcodeNY.THROW: script.AppendInstruction(new IRThrow(ip)); break;

            default:
                throw new NotImplementedException(opcode.ToString());
        }
    }
}
