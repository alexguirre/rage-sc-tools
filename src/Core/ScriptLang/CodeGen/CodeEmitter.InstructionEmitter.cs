namespace ScTools.ScriptLang.CodeGen;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using ScTools.ScriptAssembly;

public partial class CodeEmitter
{
    private class InstructionEmitter
    {
        public interface IFlushStrategy
        {
            InstructionReference Flush(List<byte> instructionBytes);
        }

        public sealed class AppendFlushStrategy : IFlushStrategy
        {
            public CodeBuffer CodeBuffer { get; set; }

            public AppendFlushStrategy(CodeBuffer codeBuffer)
                => CodeBuffer = codeBuffer;

            public InstructionReference Flush(List<byte> instructionBytes)
                => CodeBuffer.Append(instructionBytes);
        }

        public sealed class UpdateFlushStrategy : IFlushStrategy
        {
            public CodeBuffer CodeBuffer { get; set; }
            public InstructionReference InstructionToUpdate { get; set; }

            public UpdateFlushStrategy(CodeBuffer codeBuffer, InstructionReference instructionToUpdate)
            {
                CodeBuffer = codeBuffer;
                InstructionToUpdate = instructionToUpdate;
            }

            public InstructionReference Flush(List<byte> instructionBytes)
            {
                CodeBuffer.Update(InstructionToUpdate, instructionBytes);
                return InstructionToUpdate;
            }
        }

        public sealed class InsertAfterFlushStrategy : IFlushStrategy
        {
            public CodeBuffer CodeBuffer { get; set; }
            public InstructionReference Instruction { get; set; }

            public InsertAfterFlushStrategy(CodeBuffer codeBuffer, InstructionReference instruction)
            {
                CodeBuffer = codeBuffer;
                Instruction = instruction;
            }

            public InstructionReference Flush(List<byte> instructionBytes)
                => Instruction = CodeBuffer.InsertAfter(Instruction, instructionBytes);
        }

        private readonly List<byte> instructionBuffer = new();
        public IFlushStrategy FlushStrategy { get; set; }

        public InstructionEmitter(IFlushStrategy flushStrategy)
            => FlushStrategy = flushStrategy;

        #region Byte Emitters
        private void EmitBytes(ReadOnlySpan<byte> bytes)
        {
            foreach (var b in bytes)
            {
                instructionBuffer.Add(b);
            }
        }

        private void EmitU8(byte v)
        {
            instructionBuffer.Add(v);
        }

        private void EmitU16(ushort v)
        {
            instructionBuffer.Add((byte)(v & 0xFF));
            instructionBuffer.Add((byte)(v >> 8));
        }

        private void EmitS16(short v) => EmitU16(unchecked((ushort)v));

        private void EmitU32(uint v)
        {
            instructionBuffer.Add((byte)(v & 0xFF));
            instructionBuffer.Add((byte)((v >> 8) & 0xFF));
            instructionBuffer.Add((byte)((v >> 16) & 0xFF));
            instructionBuffer.Add((byte)(v >> 24));
        }

        private void EmitU24(uint v)
        {
            Debug.Assert((v & 0xFFFFFF) == v);
            instructionBuffer.Add((byte)(v & 0xFF));
            instructionBuffer.Add((byte)((v >> 8) & 0xFF));
            instructionBuffer.Add((byte)((v >> 16) & 0xFF));
        }

        private unsafe void EmitF32(float v) => EmitU32(*(uint*)&v);

        private void EmitOpcode(Opcode v) => EmitU8((byte)v);

        /// <summary>
        /// Clears the current instruction buffer.
        /// </summary>
        private void Drop()
        {
            instructionBuffer.Clear();
        }


        /// <summary>
        /// Writes the current instruction buffer to the segment.
        /// </summary>
        private InstructionReference Flush()
        {
            var instRef = FlushStrategy.Flush(instructionBuffer);
            Drop();
            return instRef;
        }
        #endregion Byte Emitters

        #region Instruction Emitters
        /// <summary>
        /// Emits an instruction of length 0. Used as label marker.
        /// </summary>
        public InstructionReference EmitLabelMarker()
            => Flush();
        public InstructionReference EmitInst(Opcode opcode)
        {
            EmitOpcode(opcode);
            return Flush();
        }
        public InstructionReference EmitInstU8(Opcode opcode, byte operand)
        {
            EmitOpcode(opcode);
            EmitU8(operand);
            return Flush();
        }
        public InstructionReference EmitInstU8U8(Opcode opcode, byte operand1, byte operand2)
        {
            EmitOpcode(opcode);
            EmitU8(operand1);
            EmitU8(operand2);
            return Flush();
        }
        public InstructionReference EmitInstU8U8U8(Opcode opcode, byte operand1, byte operand2, byte operand3)
        {
            EmitOpcode(opcode);
            EmitU8(operand1);
            EmitU8(operand2);
            EmitU8(operand3);
            return Flush();
        }
        public InstructionReference EmitInstS16(Opcode opcode, short operand)
        {
            EmitOpcode(opcode);
            EmitS16(operand);
            return Flush();
        }
        public InstructionReference EmitInstU16(Opcode opcode, ushort operand)
        {
            EmitOpcode(opcode);
            EmitU16(operand);
            return Flush();
        }
        public InstructionReference EmitInstU24(Opcode opcode, uint operand)
        {
            EmitOpcode(opcode);
            EmitU24(operand);
            return Flush();
        }
        public InstructionReference EmitInstU32(Opcode opcode, uint operand)
        {
            EmitOpcode(opcode);
            EmitU32(operand);
            return Flush();
        }
        public InstructionReference EmitInstF32(Opcode opcode, float operand)
        {
            EmitOpcode(opcode);
            EmitF32(operand);
            return Flush();
        }

        public InstructionReference EmitNop() => EmitInst(Opcode.NOP);
        public InstructionReference EmitIAdd() => EmitInst(Opcode.IADD);
        public InstructionReference EmitIAddU8(byte value) => EmitInstU8(Opcode.IADD_U8, value);
        public InstructionReference EmitIAddS16(short value) => EmitInstS16(Opcode.IADD_S16, value);
        public InstructionReference EmitISub() => EmitInst(Opcode.ISUB);
        public InstructionReference EmitIMul() => EmitInst(Opcode.IMUL);
        public InstructionReference EmitIMulU8(byte value) => EmitInstU8(Opcode.IMUL_U8, value);
        public InstructionReference EmitIMulS16(short value) => EmitInstS16(Opcode.IMUL_S16, value);
        public InstructionReference EmitIDiv() => EmitInst(Opcode.IDIV);
        public InstructionReference EmitIMod() => EmitInst(Opcode.IMOD);
        public InstructionReference EmitINot() => EmitInst(Opcode.INOT);
        public InstructionReference EmitINeg() => EmitInst(Opcode.INEG);
        public InstructionReference EmitIEq() => EmitInst(Opcode.IEQ);
        public InstructionReference EmitINe() => EmitInst(Opcode.INE);
        public InstructionReference EmitIGt() => EmitInst(Opcode.IGT);
        public InstructionReference EmitIGe() => EmitInst(Opcode.IGE);
        public InstructionReference EmitILt() => EmitInst(Opcode.ILT);
        public InstructionReference EmitILe() => EmitInst(Opcode.ILE);
        public InstructionReference EmitFAdd() => EmitInst(Opcode.FADD);
        public InstructionReference EmitFSub() => EmitInst(Opcode.FSUB);
        public InstructionReference EmitFMul() => EmitInst(Opcode.FMUL);
        public InstructionReference EmitFDiv() => EmitInst(Opcode.FDIV);
        public InstructionReference EmitFMod() => EmitInst(Opcode.FMOD);
        public InstructionReference EmitFNeg() => EmitInst(Opcode.FNEG);
        public InstructionReference EmitFEq() => EmitInst(Opcode.FEQ);
        public InstructionReference EmitFNe() => EmitInst(Opcode.FNE);
        public InstructionReference EmitFGt() => EmitInst(Opcode.FGT);
        public InstructionReference EmitFGe() => EmitInst(Opcode.FGE);
        public InstructionReference EmitFLt() => EmitInst(Opcode.FLT);
        public InstructionReference EmitFLe() => EmitInst(Opcode.FLE);
        public InstructionReference EmitVAdd() => EmitInst(Opcode.VADD);
        public InstructionReference EmitVSub() => EmitInst(Opcode.VSUB);
        public InstructionReference EmitVMul() => EmitInst(Opcode.VMUL);
        public InstructionReference EmitVDiv() => EmitInst(Opcode.VDIV);
        public InstructionReference EmitIAnd() => EmitInst(Opcode.IAND);
        public InstructionReference EmitIOr() => EmitInst(Opcode.IOR);
        public InstructionReference EmitIXor() => EmitInst(Opcode.IXOR);
        public InstructionReference EmitI2F() => EmitInst(Opcode.I2F);
        public InstructionReference EmitF2I() => EmitInst(Opcode.F2I);
        public InstructionReference EmitF2V() => EmitInst(Opcode.F2V);
        public InstructionReference EmitDup() => EmitInst(Opcode.DUP);
        public InstructionReference EmitDrop() => EmitInst(Opcode.DROP);
        public InstructionReference EmitLoad() => EmitInst(Opcode.LOAD);
        public InstructionReference EmitLoadN() => EmitInst(Opcode.LOAD_N);
        public InstructionReference EmitStore() => EmitInst(Opcode.STORE);
        public InstructionReference EmitStoreN() => EmitInst(Opcode.STORE_N);
        public InstructionReference EmitStoreRev() => EmitInst(Opcode.STORE_REV);
        public InstructionReference EmitString() => EmitInst(Opcode.STRING);
        public InstructionReference EmitStringHash() => EmitInst(Opcode.STRINGHASH);
        public InstructionReference EmitPushConstM1() => EmitInst(Opcode.PUSH_CONST_M1);
        public InstructionReference EmitPushConst0() => EmitInst(Opcode.PUSH_CONST_0);
        public InstructionReference EmitPushConst1() => EmitInst(Opcode.PUSH_CONST_1);
        public InstructionReference EmitPushConst2() => EmitInst(Opcode.PUSH_CONST_2);
        public InstructionReference EmitPushConst3() => EmitInst(Opcode.PUSH_CONST_3);
        public InstructionReference EmitPushConst4() => EmitInst(Opcode.PUSH_CONST_4);
        public InstructionReference EmitPushConst5() => EmitInst(Opcode.PUSH_CONST_5);
        public InstructionReference EmitPushConst6() => EmitInst(Opcode.PUSH_CONST_6);
        public InstructionReference EmitPushConst7() => EmitInst(Opcode.PUSH_CONST_7);
        public InstructionReference EmitPushConstFM1() => EmitInst(Opcode.PUSH_CONST_FM1);
        public InstructionReference EmitPushConstF0() => EmitInst(Opcode.PUSH_CONST_F0);
        public InstructionReference EmitPushConstF1() => EmitInst(Opcode.PUSH_CONST_F1);
        public InstructionReference EmitPushConstF2() => EmitInst(Opcode.PUSH_CONST_F2);
        public InstructionReference EmitPushConstF3() => EmitInst(Opcode.PUSH_CONST_F3);
        public InstructionReference EmitPushConstF4() => EmitInst(Opcode.PUSH_CONST_F4);
        public InstructionReference EmitPushConstF5() => EmitInst(Opcode.PUSH_CONST_F5);
        public InstructionReference EmitPushConstF6() => EmitInst(Opcode.PUSH_CONST_F6);
        public InstructionReference EmitPushConstF7() => EmitInst(Opcode.PUSH_CONST_F7);
        public InstructionReference EmitPushConstU8(byte value) => EmitInstU8(Opcode.PUSH_CONST_U8, value);
        public InstructionReference EmitPushConstU8U8(byte value1, byte value2) => EmitInstU8U8(Opcode.PUSH_CONST_U8_U8, value1, value2);
        public InstructionReference EmitPushConstU8U8U8(byte value1, byte value2, byte value3) => EmitInstU8U8U8(Opcode.PUSH_CONST_U8_U8_U8, value1, value2, value3);
        public InstructionReference EmitPushConstS16(short value) => EmitInstS16(Opcode.PUSH_CONST_S16, value);
        public InstructionReference EmitPushConstU24(uint value) => EmitInstU24(Opcode.PUSH_CONST_U24, value);
        public InstructionReference EmitPushConstU32(uint value) => EmitInstU32(Opcode.PUSH_CONST_U32, value);
        public InstructionReference EmitPushConstF(float value) => EmitInstF32(Opcode.PUSH_CONST_F, value);
        public InstructionReference EmitArrayU8(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8, itemSize);
        public InstructionReference EmitArrayU8Load(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8_LOAD, itemSize);
        public InstructionReference EmitArrayU8Store(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8_STORE, itemSize);
        public InstructionReference EmitArrayU16(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16, itemSize);
        public InstructionReference EmitArrayU16Load(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16_LOAD, itemSize);
        public InstructionReference EmitArrayU16Store(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16_STORE, itemSize);
        public InstructionReference EmitLocalU8(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8, frameOffset);
        public InstructionReference EmitLocalU8Load(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8_LOAD, frameOffset);
        public InstructionReference EmitLocalU8Store(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8_STORE, frameOffset);
        public InstructionReference EmitLocalU16(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16, frameOffset);
        public InstructionReference EmitLocalU16Load(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16_LOAD, frameOffset);
        public InstructionReference EmitLocalU16Store(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16_STORE, frameOffset);
        public InstructionReference EmitStaticU8(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8, staticOffset);
        public InstructionReference EmitStaticU8Load(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8_LOAD, staticOffset);
        public InstructionReference EmitStaticU8Store(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8_STORE, staticOffset);
        public InstructionReference EmitStaticU16(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16, staticOffset);
        public InstructionReference EmitStaticU16Load(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16_LOAD, staticOffset);
        public InstructionReference EmitStaticU16Store(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16_STORE, staticOffset);
        public InstructionReference EmitGlobalU16(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16, globalOffset);
        public InstructionReference EmitGlobalU16Load(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16_LOAD, globalOffset);
        public InstructionReference EmitGlobalU16Store(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16_STORE, globalOffset);
        public InstructionReference EmitGlobalU24(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24, globalOffset);
        public InstructionReference EmitGlobalU24Load(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24_LOAD, globalOffset);
        public InstructionReference EmitGlobalU24Store(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24_STORE, globalOffset);
        public InstructionReference EmitIOffset() => EmitInst(Opcode.IOFFSET);
        public InstructionReference EmitIOffsetU8(byte offset) => EmitInstU8(Opcode.IOFFSET_U8, offset);
        public InstructionReference EmitIOffsetU8Load(byte offset) => EmitInstU8(Opcode.IOFFSET_U8_LOAD, offset);
        public InstructionReference EmitIOffsetU8Store(byte offset) => EmitInstU8(Opcode.IOFFSET_U8_STORE, offset);
        public InstructionReference EmitIOffsetS16(short offset) => EmitInstS16(Opcode.IOFFSET_S16, offset);
        public InstructionReference EmitIOffsetS16Load(short offset) => EmitInstS16(Opcode.IOFFSET_S16_LOAD, offset);
        public InstructionReference EmitIOffsetS16Store(short offset) => EmitInstS16(Opcode.IOFFSET_S16_STORE, offset);
        public InstructionReference EmitTextLabelAssignString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_STRING, textLabelLength);
        public InstructionReference EmitTextLabelAssignInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_INT, textLabelLength);
        public InstructionReference EmitTextLabelAppendString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_STRING, textLabelLength);
        public InstructionReference EmitTextLabelAppendInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_INT, textLabelLength);
        public InstructionReference EmitTextLabelCopy() => EmitInst(Opcode.TEXT_LABEL_COPY);
        public InstructionReference EmitNative(byte argCount, byte returnCount, ushort nativeIndex)
        {
            Debug.Assert((argCount & 0x3F) == argCount); // arg count max bits 6
            Debug.Assert((returnCount & 0x3) == returnCount); // arg count max bits 2
            return EmitInstU8U8U8(Opcode.NATIVE,
                operand1: (byte)((argCount & 0x3F) << 2 | (returnCount & 0x3)),
                operand2: (byte)((nativeIndex >> 8) & 0xFF),
                operand3: (byte)(nativeIndex & 0xFF));
        }
        public InstructionReference EmitEnter(byte argCount, ushort frameSize, string? name)
        {
            EmitOpcode(Opcode.ENTER);
            EmitU8(argCount);
            EmitU16(frameSize);
            if (IncludeFunctionNames && name is not null)
            {
                var nameBytes = Encoding.UTF8.GetBytes(name).AsSpan();
                nameBytes = nameBytes[..Math.Min(nameBytes.Length, byte.MaxValue - 1)]; // limit length to 255 bytes (including null terminators)
                EmitU8((byte)(nameBytes.Length + 1));
                EmitBytes(nameBytes);
                EmitU8(0); // null terminator
            }
            else
            {
                EmitU8(0);
            }
            return Flush();
        }
        public InstructionReference EmitLeave(byte argCount, byte returnCount) => EmitInstU8U8(Opcode.LEAVE, argCount, returnCount);
        public InstructionReference EmitJ(short relativeOffset) => EmitInstS16(Opcode.J, relativeOffset);
        public InstructionReference EmitJZ(short relativeOffset) => EmitInstS16(Opcode.JZ, relativeOffset);
        public InstructionReference EmitIEqJZ(short relativeOffset) => EmitInstS16(Opcode.IEQ_JZ, relativeOffset);
        public InstructionReference EmitINeJZ(short relativeOffset) => EmitInstS16(Opcode.INE_JZ, relativeOffset);
        public InstructionReference EmitIGtJZ(short relativeOffset) => EmitInstS16(Opcode.IGT_JZ, relativeOffset);
        public InstructionReference EmitIGeJZ(short relativeOffset) => EmitInstS16(Opcode.IGE_JZ, relativeOffset);
        public InstructionReference EmitILtJZ(short relativeOffset) => EmitInstS16(Opcode.ILT_JZ, relativeOffset);
        public InstructionReference EmitILeJZ(short relativeOffset) => EmitInstS16(Opcode.ILE_JZ, relativeOffset);
        public InstructionReference EmitCall(uint functionOffset) => EmitInstU24(Opcode.CALL, functionOffset);
        public InstructionReference EmitCallIndirect() => EmitInst(Opcode.CALLINDIRECT);
        public InstructionReference EmitSwitch(byte numCases) // cases will be backfilled later
        {
            EmitOpcode(Opcode.SWITCH);
            EmitU8(numCases);
            for (int i = 0; i < numCases; i++)
            {
                EmitU32(0); // value
                EmitS16(0); // label relative offset
            }
            return Flush();
        }
        public InstructionReference EmitCatch() => EmitInst(Opcode.CATCH);
        public InstructionReference EmitThrow() => EmitInst(Opcode.THROW);
        #endregion Instruction Emitters
    }
}
