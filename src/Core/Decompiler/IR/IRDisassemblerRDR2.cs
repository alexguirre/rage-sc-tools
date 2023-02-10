namespace ScTools.Decompiler.IR;

using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using ScTools.GameFiles;
using ScTools.ScriptAssembly;
using System.Collections.Immutable;
using ScTools.ScriptAssembly.Targets.Five;

public sealed class IRDisassemblerRDR2
{
    public static IRScript Disassemble(ScriptRDR2 script) => new IRDisassemblerRDR2(script).Disassemble();

    private ScriptRDR2 Script { get; }

    private IRDisassemblerRDR2(ScriptRDR2 sc)
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
        var opcode = (OpcodeRDR2)inst[0];

        switch (opcode)
        {
            case OpcodeRDR2.NOP:
                script.AppendInstruction(new IRNop(ip));
                break;
            case OpcodeRDR2.LEAVE:
            case >= OpcodeRDR2.LEAVE_0_0 and <= OpcodeRDR2.LEAVE_3_3:
                var leave = opcode.GetLeaveOperands(inst);
                script.AppendInstruction(new IRLeave(ip, leave.ParamCount, leave.ReturnCount));
                break;
            case OpcodeRDR2.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                script.AppendInstruction(new IREnter(ip, enter.ParamCount, enter.FrameSize - 2, opcode.GetEnterFunctionName(inst)));
                break;
            case OpcodeRDR2.PUSH_CONST_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                break;
            case OpcodeRDR2.PUSH_CONST_U8_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 0)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 1)));
                break;
            case OpcodeRDR2.PUSH_CONST_U8_U8_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 0)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 1)));
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst, 2)));
                break;
            case OpcodeRDR2.PUSH_CONST_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                break;
            case OpcodeRDR2.PUSH_CONST_U24:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU24Operand(inst))));
                break;
            case OpcodeRDR2.PUSH_CONST_U32:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU32Operand(inst))));
                break;
            case OpcodeRDR2.PUSH_CONST_F:
                script.AppendInstruction(new IRPushFloat(ip, opcode.GetFloatOperand(inst)));
                break;
            case OpcodeRDR2.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                var commandHash = Script.Natives[native.CommandIndex];
                script.AppendInstruction(new IRNativeCall(ip, native.ParamCount, native.ReturnCount, commandHash));
                break;
            case OpcodeRDR2.J:
                var jumpOffset = (int)opcode.GetS16Operand(inst);
                var jumpAddress = ip + 3 + jumpOffset;
                script.AppendInstruction(new IRJump(ip, jumpAddress));
                break;
            case OpcodeRDR2.JZ:
                var jzOffset = (int)opcode.GetS16Operand(inst);
                var jzAddress = ip + 3 + jzOffset;
                script.AppendInstruction(new IRJumpIfZero(ip, jzAddress));
                break;
            case OpcodeRDR2.IEQ_JZ:
                script.AppendInstruction(new IRIEqual(ip));
                goto case OpcodeRDR2.JZ;
            case OpcodeRDR2.INE_JZ:
                script.AppendInstruction(new IRINotEqual(ip));
                goto case OpcodeRDR2.JZ;
            case OpcodeRDR2.IGT_JZ:
                script.AppendInstruction(new IRIGreaterThan(ip));
                goto case OpcodeRDR2.JZ;
            case OpcodeRDR2.IGE_JZ:
                script.AppendInstruction(new IRIGreaterOrEqual(ip));
                goto case OpcodeRDR2.JZ;
            case OpcodeRDR2.ILT_JZ:
                script.AppendInstruction(new IRILessThan(ip));
                goto case OpcodeRDR2.JZ;
            case OpcodeRDR2.ILE_JZ:
                script.AppendInstruction(new IRILessOrEqual(ip));
                goto case OpcodeRDR2.JZ;

            case >= OpcodeRDR2.CALL_0 and <= OpcodeRDR2.CALL_F:
                var call = opcode.GetCallTarget(inst);
                script.AppendInstruction(new IRCall(ip, call.Address));
                break;
            case OpcodeRDR2.SWITCH:
                var cases = ImmutableArray.CreateBuilder<IRSwitchCase>(opcode.GetSwitchNumberOfCases(inst));
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    cases.Add(new(unchecked((int)c.Value), c.GetJumpTargetAddress(ip)));
                }
                script.AppendInstruction(new IRSwitch(ip, cases.MoveToImmutable()));
                break;

            case OpcodeRDR2.STRING: script.AppendInstruction(new IRPushString(ip, opcode.GetStringOperand(inst))); break;
            // STRING_U32 pushes the address of the length prefix instead of pushing the address of the first char, so it's not compatible with STRING.
            // Unknown purpose
            case OpcodeRDR2.STRING_U32: throw new NotImplementedException("STRING_U32 is not supported");
            case OpcodeRDR2.IADD: script.AppendInstruction(new IRIAdd(ip)); break;
            case OpcodeRDR2.ISUB: script.AppendInstruction(new IRISub(ip)); break;
            case OpcodeRDR2.IMUL: script.AppendInstruction(new IRIMul(ip)); break;
            case OpcodeRDR2.IDIV: script.AppendInstruction(new IRIDiv(ip)); break;
            case OpcodeRDR2.IMOD: script.AppendInstruction(new IRIMod(ip)); break;
            case OpcodeRDR2.INOT: script.AppendInstruction(new IRINot(ip)); break;
            case OpcodeRDR2.INEG: script.AppendInstruction(new IRINeg(ip)); break;
            case OpcodeRDR2.IEQ: script.AppendInstruction(new IRIEqual(ip)); break;
            case OpcodeRDR2.INE: script.AppendInstruction(new IRINotEqual(ip)); break;
            case OpcodeRDR2.IGT: script.AppendInstruction(new IRIGreaterThan(ip)); break;
            case OpcodeRDR2.IGE: script.AppendInstruction(new IRIGreaterOrEqual(ip)); break;
            case OpcodeRDR2.ILT: script.AppendInstruction(new IRILessThan(ip)); break;
            case OpcodeRDR2.ILE: script.AppendInstruction(new IRILessOrEqual(ip)); break;
            case OpcodeRDR2.FADD: script.AppendInstruction(new IRFAdd(ip)); break;
            case OpcodeRDR2.FSUB: script.AppendInstruction(new IRFSub(ip)); break;
            case OpcodeRDR2.FMUL: script.AppendInstruction(new IRFMul(ip)); break;
            case OpcodeRDR2.FDIV: script.AppendInstruction(new IRFDiv(ip)); break;
            case OpcodeRDR2.FMOD: script.AppendInstruction(new IRFMod(ip)); break;
            case OpcodeRDR2.FNEG: script.AppendInstruction(new IRFNeg(ip)); break;
            case OpcodeRDR2.FEQ: script.AppendInstruction(new IRFEqual(ip)); break;
            case OpcodeRDR2.FNE: script.AppendInstruction(new IRFNotEqual(ip)); break;
            case OpcodeRDR2.FGT: script.AppendInstruction(new IRFGreaterThan(ip)); break;
            case OpcodeRDR2.FGE: script.AppendInstruction(new IRFGreaterOrEqual(ip)); break;
            case OpcodeRDR2.FLT: script.AppendInstruction(new IRFLessThan(ip)); break;
            case OpcodeRDR2.FLE: script.AppendInstruction(new IRFLessOrEqual(ip)); break;
            case OpcodeRDR2.VADD: script.AppendInstruction(new IRVAdd(ip)); break;
            case OpcodeRDR2.VSUB: script.AppendInstruction(new IRVSub(ip)); break;
            case OpcodeRDR2.VMUL: script.AppendInstruction(new IRVMul(ip)); break;
            case OpcodeRDR2.VDIV: script.AppendInstruction(new IRVDiv(ip)); break;
            case OpcodeRDR2.VNEG: script.AppendInstruction(new IRVNeg(ip)); break;
            case OpcodeRDR2.IAND: script.AppendInstruction(new IRIAnd(ip)); break;
            case OpcodeRDR2.IOR: script.AppendInstruction(new IRIOr(ip)); break;
            case OpcodeRDR2.IXOR: script.AppendInstruction(new IRIXor(ip)); break;
            case OpcodeRDR2.I2F: script.AppendInstruction(new IRIntToFloat(ip)); break;
            case OpcodeRDR2.F2I: script.AppendInstruction(new IRFloatToInt(ip)); break;
            case OpcodeRDR2.F2V: script.AppendInstruction(new IRFloatToVector(ip)); break;
            case OpcodeRDR2.DUP: script.AppendInstruction(new IRDup(ip)); break;
            case OpcodeRDR2.DROP: script.AppendInstruction(new IRDrop(ip)); break;
            case OpcodeRDR2.LOAD: script.AppendInstruction(new IRLoad(ip)); break;
            case OpcodeRDR2.STORE: script.AppendInstruction(new IRStore(ip)); break;
            case OpcodeRDR2.STORE_REV: script.AppendInstruction(new IRStoreRev(ip)); break;
            case OpcodeRDR2.LOAD_N: script.AppendInstruction(new IRLoadN(ip)); break;
            case OpcodeRDR2.STORE_N: script.AppendInstruction(new IRStoreN(ip)); break;

            case OpcodeRDR2.IADD_U8:
            case OpcodeRDR2.IOFFSET_U8_LOAD:
            case OpcodeRDR2.IOFFSET_U8_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is OpcodeRDR2.IOFFSET_U8_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.IOFFSET_U8_STORE) goto case OpcodeRDR2.STORE;
                break;
            case OpcodeRDR2.IADD_S16:
            case OpcodeRDR2.IOFFSET_S16_LOAD:
            case OpcodeRDR2.IOFFSET_S16_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is OpcodeRDR2.IOFFSET_S16_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.IOFFSET_S16_STORE) goto case OpcodeRDR2.STORE;
                break;
            case OpcodeRDR2.IMUL_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;
            case OpcodeRDR2.IMUL_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;

            case OpcodeRDR2.LOCAL_U8:
            case OpcodeRDR2.LOCAL_U8_LOAD:
            case OpcodeRDR2.LOCAL_U8_STORE:
                script.AppendInstruction(new IRLocalRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is OpcodeRDR2.LOCAL_U8_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.LOCAL_U8_STORE) goto case OpcodeRDR2.STORE;
                break;
            case OpcodeRDR2.STATIC_U8:
            case OpcodeRDR2.STATIC_U8_LOAD:
            case OpcodeRDR2.STATIC_U8_STORE:
                script.AppendInstruction(new IRStaticRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is OpcodeRDR2.STATIC_U8_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.STATIC_U8_STORE) goto case OpcodeRDR2.STORE;
                break;


            case OpcodeRDR2.LOCAL_U16:
            case OpcodeRDR2.LOCAL_U16_LOAD:
            case OpcodeRDR2.LOCAL_U16_STORE:
                script.AppendInstruction(new IRLocalRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeRDR2.LOCAL_U16_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.LOCAL_U16_STORE) goto case OpcodeRDR2.STORE;
                break;
            case OpcodeRDR2.STATIC_U16:
            case OpcodeRDR2.STATIC_U16_LOAD:
            case OpcodeRDR2.STATIC_U16_STORE:
                script.AppendInstruction(new IRStaticRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeRDR2.STATIC_U16_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.STATIC_U16_STORE) goto case OpcodeRDR2.STORE;
                break;
            case OpcodeRDR2.GLOBAL_U16:
            case OpcodeRDR2.GLOBAL_U16_LOAD:
            case OpcodeRDR2.GLOBAL_U16_STORE:
                script.AppendInstruction(new IRGlobalRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeRDR2.GLOBAL_U16_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.GLOBAL_U16_STORE) goto case OpcodeRDR2.STORE;
                break;

            case OpcodeRDR2.GLOBAL_U24:
            case OpcodeRDR2.GLOBAL_U24_LOAD:
            case OpcodeRDR2.GLOBAL_U24_STORE:
                script.AppendInstruction(new IRGlobalRef(ip, (int)opcode.GetU24Operand(inst)));
                if (opcode is OpcodeRDR2.GLOBAL_U24_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.GLOBAL_U24_STORE) goto case OpcodeRDR2.STORE;
                break;

            case OpcodeRDR2.ARRAY_U8:
            case OpcodeRDR2.ARRAY_U8_LOAD:
            case OpcodeRDR2.ARRAY_U8_STORE:
                script.AppendInstruction(new IRArrayItemRef(ip, opcode.GetU8Operand(inst)));
                if (opcode is OpcodeRDR2.ARRAY_U8_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.ARRAY_U8_STORE) goto case OpcodeRDR2.STORE;
                break;
            case OpcodeRDR2.ARRAY_U16:
            case OpcodeRDR2.ARRAY_U16_LOAD:
            case OpcodeRDR2.ARRAY_U16_STORE:
                script.AppendInstruction(new IRArrayItemRef(ip, opcode.GetU16Operand(inst)));
                if (opcode is OpcodeRDR2.ARRAY_U16_LOAD) goto case OpcodeRDR2.LOAD;
                if (opcode is OpcodeRDR2.ARRAY_U16_STORE) goto case OpcodeRDR2.STORE;
                break;

            case OpcodeRDR2.NULL: script.AppendInstruction(new IRNullRef(ip)); break;
            case OpcodeRDR2.TEXT_LABEL_ASSIGN_STRING: script.AppendInstruction(new IRTextLabelAssignString(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeRDR2.TEXT_LABEL_ASSIGN_INT: script.AppendInstruction(new IRTextLabelAssignInt(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeRDR2.TEXT_LABEL_APPEND_STRING: script.AppendInstruction(new IRTextLabelAppendString(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeRDR2.TEXT_LABEL_APPEND_INT: script.AppendInstruction(new IRTextLabelAppendInt(ip, opcode.GetTextLabelLength(inst))); break;
            case OpcodeRDR2.TEXT_LABEL_COPY: script.AppendInstruction(new IRTextLabelCopy(ip)); break;
            case OpcodeRDR2.CALLINDIRECT: script.AppendInstruction(new IRCallIndirect(ip)); break;
            case >= OpcodeRDR2.PUSH_CONST_M1 and <= OpcodeRDR2.PUSH_CONST_7:
                script.AppendInstruction(new IRPushInt(ip, (int)opcode - (int)OpcodeRDR2.PUSH_CONST_0));
                break;
            case >= OpcodeRDR2.PUSH_CONST_FM1 and <= OpcodeRDR2.PUSH_CONST_F7:
                script.AppendInstruction(new IRPushFloat(ip, (int)opcode - (int)OpcodeRDR2.PUSH_CONST_F0));
                break;

            case OpcodeRDR2.CATCH: script.AppendInstruction(new IRCatch(ip)); break;
            case OpcodeRDR2.THROW: script.AppendInstruction(new IRThrow(ip)); break;

            default:
                throw new NotImplementedException(opcode.ToString());
        }
    }
}
