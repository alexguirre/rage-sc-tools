﻿namespace ScTools.Decompiler.IR;

using System;
using System.Collections.Immutable;
using ScTools.GameFiles.RDR2;
using ScTools.ScriptAssembly.Targets;
using ScTools.ScriptAssembly.Targets.RDR2;

public sealed class IRDisassemblerRDR2
{
    public static IRCode Disassemble(Script script) => new IRDisassemblerRDR2(script).Disassemble();

    private Script Script { get; }

    private IRDisassemblerRDR2(Script sc)
    {
        Script = sc ?? throw new ArgumentNullException(nameof(sc));
    }

    private IRCode Disassemble()
    {
        var sc = new IRCode();
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

    private void DisassembleInstruction(IRCode script, int ip, ReadOnlySpan<byte> inst)
    {
        var opcode = (Opcode)inst[0];

        switch (opcode)
        {
            case Opcode.NOP:
                script.AppendInstruction(new IRNop(ip));
                break;
            case Opcode.LEAVE:
            case >= Opcode.LEAVE_0_0 and <= Opcode.LEAVE_3_3:
                var leave = opcode.GetLeaveOperands(inst);
                script.AppendInstruction(new IRLeave(ip, leave.ParamCount, leave.ReturnCount));
                break;
            case Opcode.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                script.AppendInstruction(new IREnter(ip, enter.ParamCount, enter.FrameSize - 2, opcode.GetEnterFunctionName(inst)));
                break;
            case Opcode.PUSH_CONST_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                break;
            case Opcode.PUSH_CONST_U8_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 0)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 1)));
                break;
            case Opcode.PUSH_CONST_U8_U8_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 0)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 1)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 2)));
                break;
            case Opcode.PUSH_CONST_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                break;
            case Opcode.PUSH_CONST_U24:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU24Operand(inst))));
                break;
            case Opcode.PUSH_CONST_U32:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU32Operand(inst))));
                break;
            case Opcode.PUSH_CONST_F:
                script.AppendInstruction(new IRPushFloat(ip, opcode.GetFloatOperand(inst)));
                break;
            case Opcode.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                var commandHash = Script.Natives[native.CommandIndex];
                script.AppendInstruction(new IRNativeCall(ip, native.ParamCount, native.ReturnCount, commandHash));
                break;
            case Opcode.J:
                var jumpOffset = (int)opcode.GetS16Operand(inst);
                var jumpAddress = ip + 3 + jumpOffset;
                script.AppendInstruction(new IRJump(ip, jumpAddress));
                break;
            case Opcode.JZ:
                var jzOffset = (int)opcode.GetS16Operand(inst);
                var jzAddress = ip + 3 + jzOffset;
                script.AppendInstruction(new IRJumpIfZero(ip, jzAddress));
                break;
            case Opcode.IEQ_JZ:
                script.AppendInstruction(new IRIEqual(ip));
                goto case Opcode.JZ;
            case Opcode.INE_JZ:
                script.AppendInstruction(new IRINotEqual(ip));
                goto case Opcode.JZ;
            case Opcode.IGT_JZ:
                script.AppendInstruction(new IRIGreaterThan(ip));
                goto case Opcode.JZ;
            case Opcode.IGE_JZ:
                script.AppendInstruction(new IRIGreaterOrEqual(ip));
                goto case Opcode.JZ;
            case Opcode.ILT_JZ:
                script.AppendInstruction(new IRILessThan(ip));
                goto case Opcode.JZ;
            case Opcode.ILE_JZ:
                script.AppendInstruction(new IRILessOrEqual(ip));
                goto case Opcode.JZ;

            case >= Opcode.CALL_0 and <= Opcode.CALL_F:
                var call = opcode.GetCallTarget(inst);
                script.AppendInstruction(new IRCall(ip, call.Address));
                break;
            case Opcode.SWITCH:
                var cases = ImmutableArray.CreateBuilder<IRSwitchCase>(opcode.GetSwitchNumberOfCases(inst));
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    cases.Add(new(unchecked((int)c.Value), c.GetJumpTargetAddress(ip)));
                }
                script.AppendInstruction(new IRSwitch(ip, cases.MoveToImmutable()));
                break;

            case Opcode.STRING: script.AppendInstruction(new IRPushString(ip, opcode.GetStringOperand(inst))); break;
            // STRING_U32 pushes the address of the length prefix instead of pushing the address of the first char, so it's not compatible with STRING.
            // Unknown purpose
            case Opcode.STRING_U32: throw new NotImplementedException("STRING_U32 is not supported");
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

            case Opcode.IADD_U8:
            case Opcode.IOFFSET_U8_LOAD:
            case Opcode.IOFFSET_U8_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is Opcode.IOFFSET_U8_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.IOFFSET_U8_STORE) goto case Opcode.STORE;
                break;
            case Opcode.IADD_S16:
            case Opcode.IOFFSET_S16_LOAD:
            case Opcode.IOFFSET_S16_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is Opcode.IOFFSET_S16_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.IOFFSET_S16_STORE) goto case Opcode.STORE;
                break;
            case Opcode.IMUL_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;
            case Opcode.IMUL_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;

            case Opcode.LOCAL_U8:
            case Opcode.LOCAL_U8_LOAD:
            case Opcode.LOCAL_U8_STORE:
                script.AppendInstruction(new IRLocalRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is Opcode.LOCAL_U8_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.LOCAL_U8_STORE) goto case Opcode.STORE;
                break;
            case Opcode.STATIC_U8:
            case Opcode.STATIC_U8_LOAD:
            case Opcode.STATIC_U8_STORE:
                script.AppendInstruction(new IRStaticRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is Opcode.STATIC_U8_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.STATIC_U8_STORE) goto case Opcode.STORE;
                break;


            case Opcode.LOCAL_U16:
            case Opcode.LOCAL_U16_LOAD:
            case Opcode.LOCAL_U16_STORE:
                script.AppendInstruction(new IRLocalRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is Opcode.LOCAL_U16_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.LOCAL_U16_STORE) goto case Opcode.STORE;
                break;
            case Opcode.STATIC_U16:
            case Opcode.STATIC_U16_LOAD:
            case Opcode.STATIC_U16_STORE:
                script.AppendInstruction(new IRStaticRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is Opcode.STATIC_U16_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.STATIC_U16_STORE) goto case Opcode.STORE;
                break;
            case Opcode.GLOBAL_U16:
            case Opcode.GLOBAL_U16_LOAD:
            case Opcode.GLOBAL_U16_STORE:
                script.AppendInstruction(new IRGlobalRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is Opcode.GLOBAL_U16_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.GLOBAL_U16_STORE) goto case Opcode.STORE;
                break;

            case Opcode.GLOBAL_U24:
            case Opcode.GLOBAL_U24_LOAD:
            case Opcode.GLOBAL_U24_STORE:
                script.AppendInstruction(new IRGlobalRef(ip, (int)opcode.GetU24Operand(inst)));
                if (opcode is Opcode.GLOBAL_U24_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.GLOBAL_U24_STORE) goto case Opcode.STORE;
                break;

            case Opcode.ARRAY_U8:
            case Opcode.ARRAY_U8_LOAD:
            case Opcode.ARRAY_U8_STORE:
                script.AppendInstruction(new IRArrayItemRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is Opcode.ARRAY_U8_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.ARRAY_U8_STORE) goto case Opcode.STORE;
                break;
            case Opcode.ARRAY_U16:
            case Opcode.ARRAY_U16_LOAD:
            case Opcode.ARRAY_U16_STORE:
                script.AppendInstruction(new IRArrayItemRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is Opcode.ARRAY_U16_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.ARRAY_U16_STORE) goto case Opcode.STORE;
                break;

            case Opcode.NULL: script.AppendInstruction(new IRNullRef(ip)); break;
            case Opcode.TEXT_LABEL_ASSIGN_STRING: script.AppendInstruction(new IRTextLabelAssignString(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_ASSIGN_INT: script.AppendInstruction(new IRTextLabelAssignInt(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_APPEND_STRING: script.AppendInstruction(new IRTextLabelAppendString(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_APPEND_INT: script.AppendInstruction(new IRTextLabelAppendInt(ip, opcode.GetTextLabelLength(inst))); break;
            case Opcode.TEXT_LABEL_COPY: script.AppendInstruction(new IRTextLabelCopy(ip)); break;
            case Opcode.CALLINDIRECT: script.AppendInstruction(new IRCallIndirect(ip)); break;
            case >= Opcode.PUSH_CONST_M1 and <= Opcode.PUSH_CONST_7:
                script.AppendInstruction(new IRPushInt(ip, (int)opcode - (int)Opcode.PUSH_CONST_0));
                break;
            case >= Opcode.PUSH_CONST_FM1 and <= Opcode.PUSH_CONST_F7:
                script.AppendInstruction(new IRPushFloat(ip, (int)opcode - (int)Opcode.PUSH_CONST_F0));
                break;

            case Opcode.CATCH: script.AppendInstruction(new IRCatch(ip)); break;
            case Opcode.THROW: script.AppendInstruction(new IRThrow(ip)); break;

            default:
                throw new NotImplementedException(opcode.ToString());
        }
    }
}
