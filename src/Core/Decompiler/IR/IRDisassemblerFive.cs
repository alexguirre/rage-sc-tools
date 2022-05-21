namespace ScTools.Decompiler.IR;

using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using ScTools.GameFiles;
using ScTools.ScriptAssembly;
using System.Collections.Immutable;
using ScTools.GameFiles.Five;

public class IRDisassemblerFive
{
    private readonly byte[] code;

    public Script Script { get; }

    public IRDisassemblerFive(Script sc)
    {
        Script = sc ?? throw new ArgumentNullException(nameof(sc));
        code = MergeCodePages(sc);
    }

    public IRScript Disassemble()
    {
        return ToIRScript();
    }

    private IRScript ToIRScript()
    {
        var sc = new IRScript();
        if (code.Length == 0)
        {
            return sc;
        }

        IterateCode(inst =>
        {
            DisassembleInstruction(sc, inst, inst.Address, inst.Bytes);
        });
        sc.AppendInstruction(new IREndOfScript(code.Length));
        return sc;
    }

    private void DisassembleInstruction(IRScript script, InstructionContext ctx, int ip, ReadOnlySpan<byte> inst)
    {
        var opcode = (Opcode)inst[0];

        switch (opcode)
        {
            case Opcode.NOP:
                break;
            case Opcode.LEAVE:
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
                var commandHash = Script.NativeHash(native.CommandIndex);
                script.AppendInstruction(new IRNativeCall(ip, native.ParamCount, native.ReturnCount, commandHash));
                break;
            case Opcode.J:
                var jumpOffset = unchecked((int)opcode.GetS16Operand(inst));
                var jumpAddress = ip + 3 + jumpOffset;
                script.AppendInstruction(new IRJump(ip, jumpAddress));
                break;
            case Opcode.JZ:
                var jzOffset = unchecked((int)opcode.GetS16Operand(inst));
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

            case Opcode.CALL:
                var callAddr = unchecked((int)opcode.GetU24Operand(inst));
                script.AppendInstruction(new IRCall(ip, callAddr));
                break;
            case Opcode.SWITCH:
                var cases = ImmutableArray.CreateBuilder<IRSwitchCase>(opcode.GetSwitchNumberOfCases(inst));
                foreach (var (caseValue, caseJumpOffset, offsetWithinInstruction) in opcode.GetSwitchOperands(inst))
                {
                    var caseJumpAddress = ip + offsetWithinInstruction + 6 + caseJumpOffset;
                    cases.Add(new(unchecked((int)caseValue), unchecked((int)caseJumpAddress)));
                }
                script.AppendInstruction(new IRSwitch(ip, cases.MoveToImmutable()));
                break;

            case Opcode.STRING: script.AppendInstruction(new IRPushStringFromStringTable(ip)); break;
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
            case Opcode.IBITTEST: script.AppendInstruction(new IRIBitTest(ip)); break;
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
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                break;
            case Opcode.IADD_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                break;
            case Opcode.IMUL_U8:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;
            case Opcode.IMUL_S16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIMul(ip));
                break;

            case Opcode.IOFFSET:
                script.AppendInstruction(new IRIAdd(ip));
                break;
            case Opcode.IOFFSET_U8:
            case Opcode.IOFFSET_U8_LOAD:
            case Opcode.IOFFSET_U8_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU8Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is Opcode.IOFFSET_U8_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.IOFFSET_U8_STORE) goto case Opcode.STORE;
                break;
            case Opcode.IOFFSET_S16:
            case Opcode.IOFFSET_S16_LOAD:
            case Opcode.IOFFSET_S16_STORE:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetS16Operand(inst)));
                script.AppendInstruction(new IRIAdd(ip));
                if (opcode is Opcode.IOFFSET_S16_LOAD) goto case Opcode.LOAD;
                if (opcode is Opcode.IOFFSET_S16_STORE) goto case Opcode.STORE;
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

    private delegate void IterateCodeCallback(InstructionContext instruction);
    private void IterateCode(IterateCodeCallback callback)
    {
        InstructionContext.CB previousCB = currInst =>
        {
            int prevAddress = 0;
            int address = 0;
            while (address < currInst.Address)
            {
                prevAddress = address;
                address += OpcodeExtensions.ByteSize(code.AsSpan(address));
            }
            return GetInstructionContext(code, prevAddress, currInst.PreviousCB, currInst.NextCB);
        };
        InstructionContext.CB nextCB = currInst =>
        {
            var nextAddress = currInst.Address + currInst.Bytes.Length;
            return GetInstructionContext(code, nextAddress, currInst.PreviousCB, currInst.NextCB);
        };

        int ip = 0;
        while (ip < code.Length)
        {
            var inst = GetInstructionContext(code, ip, previousCB, nextCB);
            callback(inst);
            ip += inst.Bytes.Length;
        }

        static InstructionContext GetInstructionContext(byte[] code, int address, InstructionContext.CB previousCB, InstructionContext.CB nextCB)
            => address >= code.Length ? default : new()
            {
                Address = address,
                Bytes = OpcodeExtensions.GetInstructionSpan(code, address),
                PreviousCB = previousCB,
                NextCB = nextCB,
            };
    }

    private readonly ref struct InstructionContext
    {
        public delegate InstructionContext CB(InstructionContext curr);

        public bool IsValid => Bytes.Length > 0;
        public int Address { get; init; }
        public ReadOnlySpan<byte> Bytes { get; init; }
        public Opcode Opcode => (Opcode)Bytes[0];
        public CB PreviousCB { get; init; }
        public CB NextCB { get; init; }

        public InstructionContext Previous() => PreviousCB(this);
        public InstructionContext Next() => NextCB(this);
    }

    private static byte[] MergeCodePages(Script sc)
    {
        if (sc.CodePages == null)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[sc.CodeLength];
        var offset = 0;
        foreach (var page in sc.CodePages)
        {
            page.Data.CopyTo(buffer.AsSpan(offset));
            offset += page.Data.Length;
        }
        return buffer;
    }

    public static void Disassemble(TextWriter output, ScriptPayne sc, string scriptName, Dictionary<uint, string> nativeCommands)
    {
        var a = new DisassemblerPayne(sc, scriptName, nativeCommands);
        a.Disassemble(output);
    }
}
