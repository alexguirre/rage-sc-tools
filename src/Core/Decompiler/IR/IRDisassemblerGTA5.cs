namespace ScTools.Decompiler.IR;

using System;
using System.Collections.Immutable;
using ScTools.ScriptAssembly.Targets;
using ScTools.ScriptAssembly.Targets.GTA5;

public sealed class IRDisassemblerGTA5
{
    public static IRCode Disassemble(ScTools.GameFiles.GTA5.Script script) => new IRDisassemblerGTA5(script).Disassemble();

    private ScTools.GameFiles.GTA5.Script Script { get; }

    private IRDisassemblerGTA5(ScTools.GameFiles.GTA5.Script sc)
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
        var opcode = (OpcodeV12)inst[0];

        switch (opcode)
        {
            case OpcodeV12.NOP:
                script.AppendInstruction(new IRNop(ip));
                break;
            case OpcodeV12.LEAVE:
                var leave = opcode.GetLeaveOperands(inst);
                script.AppendInstruction(new IRLeave(ip, leave.ParamCount, leave.ReturnCount));
                break;
            case OpcodeV12.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                script.AppendInstruction(new IREnter(ip, enter.ParamCount, enter.FrameSize - 2, opcode.GetEnterFunctionName(inst)));
                break;
            case OpcodeV12.PUSH_CONST_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                break;
            case OpcodeV12.PUSH_CONST_U8_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 0)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 1)));
                break;
            case OpcodeV12.PUSH_CONST_U8_U8_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 0)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 1)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 2)));
                break;
            case OpcodeV12.PUSH_CONST_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                break;
            case OpcodeV12.PUSH_CONST_U24:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU24Operand(inst))));
                break;
            case OpcodeV12.PUSH_CONST_U32:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU32Operand(inst))));
                break;
            case OpcodeV12.PUSH_CONST_F:
                script.AppendInstruction(new IRPushFloat(ip, opcode.GetFloatOperand(inst)));
                break;
            case OpcodeV12.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                var commandHash = Script.NativeHash(native.CommandIndex);
                script.AppendInstruction(new IRNativeCall(ip, native.ParamCount, native.ReturnCount, commandHash));
                break;
            case OpcodeV12.J:
                var jumpOffset = unchecked((int)opcode.GetS16Operand(inst));
                var jumpAddress = ip + 3 + jumpOffset;
                script.AppendInstruction(new IRJump(ip, jumpAddress));
                break;
            case OpcodeV12.JZ:
                var jzOffset = unchecked((int)opcode.GetS16Operand(inst));
                var jzAddress = ip + 3 + jzOffset;
                script.AppendInstruction(new IRJumpIfZero(ip, jzAddress));
                break;
            case OpcodeV12.IEQ_JZ:
                script.AppendInstruction(new IRIEqual(ip));
                goto case OpcodeV12.JZ;
            case OpcodeV12.INE_JZ:
                script.AppendInstruction(new IRINotEqual(ip));
                goto case OpcodeV12.JZ;
            case OpcodeV12.IGT_JZ:
                script.AppendInstruction(new IRIGreaterThan(ip));
                goto case OpcodeV12.JZ;
            case OpcodeV12.IGE_JZ:
                script.AppendInstruction(new IRIGreaterOrEqual(ip));
                goto case OpcodeV12.JZ;
            case OpcodeV12.ILT_JZ:
                script.AppendInstruction(new IRILessThan(ip));
                goto case OpcodeV12.JZ;
            case OpcodeV12.ILE_JZ:
                script.AppendInstruction(new IRILessOrEqual(ip));
                goto case OpcodeV12.JZ;

            case OpcodeV12.CALL:
                var callAddr = unchecked((int)opcode.GetU24Operand(inst));
                script.AppendInstruction(new IRCall(ip, callAddr));
                break;
            case OpcodeV12.SWITCH:
                var cases = ImmutableArray.CreateBuilder<IRSwitchCase>(opcode.GetSwitchNumberOfCases(inst));
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    cases.Add(new(unchecked((int)c.Value), c.GetJumpTargetAddress(ip)));
                }
                script.AppendInstruction(new IRSwitch(ip, cases.MoveToImmutable()));
                break;

            case OpcodeV12.STRING: script.AppendInstruction(new IRPushStringFromStringTable(ip)); break;
            case OpcodeV12.IADD: script.AppendInstruction(new IRIAdd(ip)); break;
            case OpcodeV12.ISUB: script.AppendInstruction(new IRISub(ip)); break;
            case OpcodeV12.IMUL: script.AppendInstruction(new IRIMul(ip)); break;
            case OpcodeV12.IDIV: script.AppendInstruction(new IRIDiv(ip)); break;
            case OpcodeV12.IMOD: script.AppendInstruction(new IRIMod(ip)); break;
            case OpcodeV12.INOT: script.AppendInstruction(new IRINot(ip)); break;
            case OpcodeV12.INEG: script.AppendInstruction(new IRINeg(ip)); break;
            case OpcodeV12.IEQ: script.AppendInstruction(new IRIEqual(ip)); break;
            case OpcodeV12.INE: script.AppendInstruction(new IRINotEqual(ip)); break;
            case OpcodeV12.IGT: script.AppendInstruction(new IRIGreaterThan(ip)); break;
            case OpcodeV12.IGE: script.AppendInstruction(new IRIGreaterOrEqual(ip)); break;
            case OpcodeV12.ILT: script.AppendInstruction(new IRILessThan(ip)); break;
            case OpcodeV12.ILE: script.AppendInstruction(new IRILessOrEqual(ip)); break;
            case OpcodeV12.FADD: script.AppendInstruction(new IRFAdd(ip)); break;
            case OpcodeV12.FSUB: script.AppendInstruction(new IRFSub(ip)); break;
            case OpcodeV12.FMUL: script.AppendInstruction(new IRFMul(ip)); break;
            case OpcodeV12.FDIV: script.AppendInstruction(new IRFDiv(ip)); break;
            case OpcodeV12.FMOD: script.AppendInstruction(new IRFMod(ip)); break;
            case OpcodeV12.FNEG: script.AppendInstruction(new IRFNeg(ip)); break;
            case OpcodeV12.FEQ: script.AppendInstruction(new IRFEqual(ip)); break;
            case OpcodeV12.FNE: script.AppendInstruction(new IRFNotEqual(ip)); break;
            case OpcodeV12.FGT: script.AppendInstruction(new IRFGreaterThan(ip)); break;
            case OpcodeV12.FGE: script.AppendInstruction(new IRFGreaterOrEqual(ip)); break;
            case OpcodeV12.FLT: script.AppendInstruction(new IRFLessThan(ip)); break;
            case OpcodeV12.FLE: script.AppendInstruction(new IRFLessOrEqual(ip)); break;
            case OpcodeV12.VADD: script.AppendInstruction(new IRVAdd(ip)); break;
            case OpcodeV12.VSUB: script.AppendInstruction(new IRVSub(ip)); break;
            case OpcodeV12.VMUL: script.AppendInstruction(new IRVMul(ip)); break;
            case OpcodeV12.VDIV: script.AppendInstruction(new IRVDiv(ip)); break;
            case OpcodeV12.VNEG: script.AppendInstruction(new IRVNeg(ip)); break;
            case OpcodeV12.IAND: script.AppendInstruction(new IRIAnd(ip)); break;
            case OpcodeV12.IOR: script.AppendInstruction(new IRIOr(ip)); break;
            case OpcodeV12.IXOR: script.AppendInstruction(new IRIXor(ip)); break;
            case OpcodeV12.IBITTEST: script.AppendInstruction(new IRIBitTest(ip)); break;
            case OpcodeV12.I2F: script.AppendInstruction(new IRIntToFloat(ip)); break;
            case OpcodeV12.F2I: script.AppendInstruction(new IRFloatToInt(ip)); break;
            case OpcodeV12.F2V: script.AppendInstruction(new IRFloatToVector(ip)); break;
            case OpcodeV12.DUP: script.AppendInstruction(new IRDup(ip)); break;
            case OpcodeV12.DROP: script.AppendInstruction(new IRDrop(ip)); break;
            case OpcodeV12.LOAD: script.AppendInstruction(new IRLoad(ip)); break;
            case OpcodeV12.STORE: script.AppendInstruction(new IRStore(ip)); break;
            case OpcodeV12.STORE_REV: script.AppendInstruction(new IRStoreRev(ip)); break;
            case OpcodeV12.LOAD_N: script.AppendInstruction(new IRLoadN(ip)); break;
            case OpcodeV12.STORE_N: script.AppendInstruction(new IRStoreN(ip)); break;

            case OpcodeV12.IADD_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                break;
            case OpcodeV12.IADD_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                break;
            case OpcodeV12.IMUL_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;
            case OpcodeV12.IMUL_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;

            case OpcodeV12.IOFFSET:
                script.AppendInstruction(new IRIAdd(ip));
                break;
            case OpcodeV12.IOFFSET_U8:
            case OpcodeV12.IOFFSET_U8_LOAD:
            case OpcodeV12.IOFFSET_U8_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is OpcodeV12.IOFFSET_U8_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.IOFFSET_U8_STORE) goto case OpcodeV12.STORE;
                break;
            case OpcodeV12.IOFFSET_S16:
            case OpcodeV12.IOFFSET_S16_LOAD:
            case OpcodeV12.IOFFSET_S16_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is OpcodeV12.IOFFSET_S16_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.IOFFSET_S16_STORE) goto case OpcodeV12.STORE;
                break;

            case OpcodeV12.LOCAL_U8:
            case OpcodeV12.LOCAL_U8_LOAD:
            case OpcodeV12.LOCAL_U8_STORE:
                script.AppendInstruction(new IRLocalRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is OpcodeV12.LOCAL_U8_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.LOCAL_U8_STORE) goto case OpcodeV12.STORE;
                break;
            case OpcodeV12.STATIC_U8:
            case OpcodeV12.STATIC_U8_LOAD:
            case OpcodeV12.STATIC_U8_STORE:
                script.AppendInstruction(new IRStaticRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is OpcodeV12.STATIC_U8_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.STATIC_U8_STORE) goto case OpcodeV12.STORE;
                break;


            case OpcodeV12.LOCAL_U16:
            case OpcodeV12.LOCAL_U16_LOAD:
            case OpcodeV12.LOCAL_U16_STORE:
                script.AppendInstruction(new IRLocalRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeV12.LOCAL_U16_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.LOCAL_U16_STORE) goto case OpcodeV12.STORE;
                break;
            case OpcodeV12.STATIC_U16:
            case OpcodeV12.STATIC_U16_LOAD:
            case OpcodeV12.STATIC_U16_STORE:
                script.AppendInstruction(new IRStaticRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeV12.STATIC_U16_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.STATIC_U16_STORE) goto case OpcodeV12.STORE;
                break;
            case OpcodeV12.GLOBAL_U16:
            case OpcodeV12.GLOBAL_U16_LOAD:
            case OpcodeV12.GLOBAL_U16_STORE:
                script.AppendInstruction(new IRGlobalRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeV12.GLOBAL_U16_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.GLOBAL_U16_STORE) goto case OpcodeV12.STORE;
                break;

            case OpcodeV12.STATIC_U24:
            case OpcodeV12.STATIC_U24_LOAD:
            case OpcodeV12.STATIC_U24_STORE:
                script.AppendInstruction(new IRStaticRef(ip, (int)opcode.GetU24Operand(inst)));
                if (opcode is OpcodeV12.STATIC_U24_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.STATIC_U24_STORE) goto case OpcodeV12.STORE;
                break;
            case OpcodeV12.GLOBAL_U24:
            case OpcodeV12.GLOBAL_U24_LOAD:
            case OpcodeV12.GLOBAL_U24_STORE:
                script.AppendInstruction(new IRGlobalRef(ip, (int)opcode.GetU24Operand(inst)));
                if (opcode is OpcodeV12.GLOBAL_U24_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.GLOBAL_U24_STORE) goto case OpcodeV12.STORE;
                break;

            case OpcodeV12.ARRAY_U8:
            case OpcodeV12.ARRAY_U8_LOAD:
            case OpcodeV12.ARRAY_U8_STORE:
                script.AppendInstruction(new IRArrayItemRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is OpcodeV12.ARRAY_U8_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.ARRAY_U8_STORE) goto case OpcodeV12.STORE;
                break;
            case OpcodeV12.ARRAY_U16:
            case OpcodeV12.ARRAY_U16_LOAD:
            case OpcodeV12.ARRAY_U16_STORE:
                script.AppendInstruction(new IRArrayItemRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeV12.ARRAY_U16_LOAD) goto case OpcodeV12.LOAD;
                if (opcode is OpcodeV12.ARRAY_U16_STORE) goto case OpcodeV12.STORE;
                break;

            case OpcodeV12.TEXT_LABEL_ASSIGN_STRING: script.AppendInstruction(new IRTextLabelAssignString(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeV12.TEXT_LABEL_ASSIGN_INT: script.AppendInstruction(new IRTextLabelAssignInt(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeV12.TEXT_LABEL_APPEND_STRING: script.AppendInstruction(new IRTextLabelAppendString(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeV12.TEXT_LABEL_APPEND_INT: script.AppendInstruction(new IRTextLabelAppendInt(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeV12.TEXT_LABEL_COPY: script.AppendInstruction(new IRTextLabelCopy(ip)); break;
            case OpcodeV12.CALLINDIRECT: script.AppendInstruction(new IRCallIndirect(ip)); break;
            case >= OpcodeV12.PUSH_CONST_M1 and <= OpcodeV12.PUSH_CONST_7:
                script.AppendInstruction(new IRPushInt(ip, (int)opcode - (int)OpcodeV12.PUSH_CONST_0));
                break;
            case >= OpcodeV12.PUSH_CONST_FM1 and <= OpcodeV12.PUSH_CONST_F7:
                script.AppendInstruction(new IRPushFloat(ip, (int)opcode - (int)OpcodeV12.PUSH_CONST_F0));
                break;

            case OpcodeV12.CATCH: script.AppendInstruction(new IRCatch(ip)); break;
            case OpcodeV12.THROW: script.AppendInstruction(new IRThrow(ip)); break;

            default:
                throw new NotImplementedException(opcode.ToString());
        }
    }
}
