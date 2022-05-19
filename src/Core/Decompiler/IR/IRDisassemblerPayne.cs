namespace ScTools.Decompiler.IR;

using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using ScTools.GameFiles;
using ScTools.ScriptAssembly;
using System.Collections.Immutable;

public class IRDisassemblerPayne
{
    private readonly byte[] code;

    public ScriptPayne Script { get; }

    public IRDisassemblerPayne(ScriptPayne sc)
    {
        Script = sc ?? throw new ArgumentNullException(nameof(sc));
        code = sc.Code ?? Array.Empty<byte>();
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
        return sc;
    }

    private void DisassembleInstruction(IRScript script, InstructionContext ctx, int ip, ReadOnlySpan<byte> inst)
    {
        var opcode = (OpcodePayne)inst[0];

        switch (opcode)
        {
            case OpcodePayne.NOP:
                break;
            case OpcodePayne.LEAVE:
                var leave = opcode.GetLeaveOperands(inst);
                script.AppendInstruction(new IRLeave(ip, leave.ParamCount, leave.ReturnCount));
                break;
            case OpcodePayne.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                script.AppendInstruction(new IREnter(ip, enter.ParamCount, enter.FrameSize - 2));
                break;
            case OpcodePayne.PUSH_CONST_U16:
                script.AppendInstruction(new IRPushInt(ip, opcode.GetU16Operand(inst)));
                break;
            case OpcodePayne.PUSH_CONST_U32:
                script.AppendInstruction(new IRPushInt(ip, unchecked((int)opcode.GetU32Operand(inst))));
                break;
            case OpcodePayne.PUSH_CONST_F:
                script.AppendInstruction(new IRPushFloat(ip, opcode.GetFloatOperand(inst)));
                break;
            case OpcodePayne.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                script.AppendInstruction(new IRNativeCall(ip, native.ParamCount, native.ReturnCount, native.CommandHash));
                break;
            case OpcodePayne.J:
                var jumpAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRJump(ip, jumpAddr));
                break;
            case OpcodePayne.JZ:
                var jzAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRJumpIfZero(ip, jzAddr));
                break;
            case OpcodePayne.JNZ:
                var jnzAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRINot(ip));
                script.AppendInstruction(new IRJumpIfZero(ip, jnzAddr));
                break;
            case OpcodePayne.CALL:
                var callAddr = unchecked((int)opcode.GetU32Operand(inst));
                script.AppendInstruction(new IRCall(ip, callAddr));
                break;
            case OpcodePayne.SWITCH:
                var cases = ImmutableArray.CreateBuilder<IRSwitchCase>(opcode.GetSwitchNumberOfCases(inst));
                foreach (var (caseValue, caseJumpAddr) in opcode.GetSwitchOperands(inst))
                {
                    cases.Add(new(unchecked((int)caseValue), unchecked((int)caseJumpAddr)));
                }
                script.AppendInstruction(new IRSwitch(ip, cases.MoveToImmutable()));
                break;
                
            case OpcodePayne.STRING: script.AppendInstruction(new IRPushString(ip, opcode.GetStringOperand(inst))); break;
            case OpcodePayne.IADD: script.AppendInstruction(new IRIAdd(ip)); break;
            case OpcodePayne.ISUB: script.AppendInstruction(new IRISub(ip)); break;
            case OpcodePayne.IMUL: script.AppendInstruction(new IRIMul(ip)); break;
            case OpcodePayne.IDIV: script.AppendInstruction(new IRIDiv(ip)); break;
            case OpcodePayne.IMOD: script.AppendInstruction(new IRIMod(ip)); break;
            case OpcodePayne.INOT: script.AppendInstruction(new IRINot(ip)); break;
            case OpcodePayne.INEG: script.AppendInstruction(new IRINeg(ip)); break;
            case OpcodePayne.IEQ: script.AppendInstruction(new IRIEqual(ip)); break;
            case OpcodePayne.INE: script.AppendInstruction(new IRINotEqual(ip)); break;
            case OpcodePayne.IGT: script.AppendInstruction(new IRIGreaterThan(ip)); break;
            case OpcodePayne.IGE: script.AppendInstruction(new IRIGreaterOrEqual(ip)); break;
            case OpcodePayne.ILT: script.AppendInstruction(new IRILessThan(ip)); break;
            case OpcodePayne.ILE: script.AppendInstruction(new IRILessOrEqual(ip)); break;
            case OpcodePayne.FADD: script.AppendInstruction(new IRFAdd(ip)); break;
            case OpcodePayne.FSUB: script.AppendInstruction(new IRFSub(ip)); break;
            case OpcodePayne.FMUL: script.AppendInstruction(new IRFMul(ip)); break;
            case OpcodePayne.FDIV: script.AppendInstruction(new IRFDiv(ip)); break;
            case OpcodePayne.FMOD: script.AppendInstruction(new IRFMod(ip)); break;
            case OpcodePayne.FNEG: script.AppendInstruction(new IRFNeg(ip)); break;
            case OpcodePayne.FEQ: script.AppendInstruction(new IRFEqual(ip)); break;
            case OpcodePayne.FNE: script.AppendInstruction(new IRFNotEqual(ip)); break;
            case OpcodePayne.FGT: script.AppendInstruction(new IRFGreaterThan(ip)); break;
            case OpcodePayne.FGE: script.AppendInstruction(new IRFGreaterOrEqual(ip)); break;
            case OpcodePayne.FLT: script.AppendInstruction(new IRFLessThan(ip)); break;
            case OpcodePayne.FLE: script.AppendInstruction(new IRFLessOrEqual(ip)); break;
            case OpcodePayne.VADD: script.AppendInstruction(new IRVAdd(ip)); break;
            case OpcodePayne.VSUB: script.AppendInstruction(new IRVSub(ip)); break;
            case OpcodePayne.VMUL: script.AppendInstruction(new IRVMul(ip)); break;
            case OpcodePayne.VDIV: script.AppendInstruction(new IRVDiv(ip)); break;
            case OpcodePayne.VNEG: script.AppendInstruction(new IRVNeg(ip)); break;
            case OpcodePayne.IAND: script.AppendInstruction(new IRIAnd(ip)); break;
            case OpcodePayne.IOR: script.AppendInstruction(new IRIOr(ip)); break;
            case OpcodePayne.IXOR: script.AppendInstruction(new IRIXor(ip)); break;
            case OpcodePayne.I2F: script.AppendInstruction(new IRIntToFloat(ip)); break;
            case OpcodePayne.F2I: script.AppendInstruction(new IRFloatToInt(ip)); break;
            case OpcodePayne.F2V: script.AppendInstruction(new IRFloatToVector(ip)); break;
            case OpcodePayne.DUP: script.AppendInstruction(new IRDup(ip)); break;
            case OpcodePayne.DROP: script.AppendInstruction(new IRDrop(ip)); break;
            case OpcodePayne.LOAD: script.AppendInstruction(new IRLoad(ip)); break;
            case OpcodePayne.STORE: script.AppendInstruction(new IRStore(ip)); break;
            case OpcodePayne.STORE_REV: script.AppendInstruction(new IRStoreRev(ip)); break;
            case OpcodePayne.LOAD_N: script.AppendInstruction(new IRLoadN(ip)); break;
            case OpcodePayne.STORE_N: script.AppendInstruction(new IRStoreN(ip)); break;

            case OpcodePayne.LOCAL_0:
            case OpcodePayne.LOCAL_1:
            case OpcodePayne.LOCAL_2:
            case OpcodePayne.LOCAL_3:
            case OpcodePayne.LOCAL_4:
            case OpcodePayne.LOCAL_5:
            case OpcodePayne.LOCAL_6:
            case OpcodePayne.LOCAL_7:
                script.AppendInstruction(new IRLocalRef(ip, opcode - OpcodePayne.LOCAL_0));
                break;
            case OpcodePayne.LOCAL:
                script.AppendInstruction(new IRLocalRefFromStack(ip));
                break;
            case OpcodePayne.STATIC:
                script.AppendInstruction(new IRStaticRefFromStack(ip));
                break;
            case OpcodePayne.GLOBAL:
                script.AppendInstruction(new IRGlobalRefFromStack(ip));
                break;
            case OpcodePayne.ARRAY:
                script.AppendInstruction(new IRArrayItemRef(ip));
                break;
            case OpcodePayne.NULL:
            case OpcodePayne.TEXT_LABEL_ASSIGN_STRING:
            case OpcodePayne.TEXT_LABEL_ASSIGN_INT:
            case OpcodePayne.TEXT_LABEL_APPEND_STRING:
            case OpcodePayne.TEXT_LABEL_APPEND_INT:
            case OpcodePayne.CATCH:
            case OpcodePayne.THROW:
            case OpcodePayne.TEXT_LABEL_COPY: break;
            //throw new NotImplementedException(opcode.ToString());
            case OpcodePayne.CALLINDIRECT:
                script.AppendInstruction(new IRCallIndirect(ip));
                break;
            case >= OpcodePayne.PUSH_CONST_M16 and <= OpcodePayne.PUSH_CONST_159:
                script.AppendInstruction(new IRPushInt(ip, (int)opcode - (int)OpcodePayne.PUSH_CONST_0));
                break;
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
                address += OpcodePayneExtensions.ByteSize(code.AsSpan(address));
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
                Bytes = OpcodePayneExtensions.GetInstructionSpan(code, address),
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
        public OpcodePayne Opcode => (OpcodePayne)Bytes[0];
        public CB PreviousCB { get; init; }
        public CB NextCB { get; init; }

        public InstructionContext Previous() => PreviousCB(this);
        public InstructionContext Next() => NextCB(this);
    }

    public static void Disassemble(TextWriter output, ScriptPayne sc, string scriptName, Dictionary<uint, string> nativeCommands)
    {
        var a = new DisassemblerPayne(sc, scriptName, nativeCommands);
        a.Disassemble(output);
    }
}
