﻿namespace ScTools.ScriptAssembly
{
    using System;
    using System.Buffers.Binary;
    using System.Text;

    public enum Opcode : byte
    {
        NOP = 0x00,
        IADD = 0x01,
        ISUB = 0x02,
        IMUL = 0x03,
        IDIV = 0x04,
        IMOD = 0x05,
        INOT = 0x06,
        INEG = 0x07,
        IEQ = 0x08,
        INE = 0x09,
        IGT = 0x0A,
        IGE = 0x0B,
        ILT = 0x0C,
        ILE = 0x0D,
        FADD = 0x0E,
        FSUB = 0x0F,
        FMUL = 0x10,
        FDIV = 0x11,
        FMOD = 0x12,
        FNEG = 0x13,
        FEQ = 0x14,
        FNE = 0x15,
        FGT = 0x16,
        FGE = 0x17,
        FLT = 0x18,
        FLE = 0x19,
        VADD = 0x1A,
        VSUB = 0x1B,
        VMUL = 0x1C,
        VDIV = 0x1D,
        VNEG = 0x1E,
        IAND = 0x1F,
        IOR = 0x20,
        IXOR = 0x21,
        I2F = 0x22,
        F2I = 0x23,
        F2V = 0x24,
        PUSH_CONST_U8 = 0x25,
        PUSH_CONST_U8_U8 = 0x26,
        PUSH_CONST_U8_U8_U8 = 0x27,
        PUSH_CONST_U32 = 0x28,
        PUSH_CONST_F = 0x29,
        DUP = 0x2A,
        DROP = 0x2B,
        NATIVE = 0x2C,
        ENTER = 0x2D,
        LEAVE = 0x2E,
        LOAD = 0x2F,
        STORE = 0x30,
        STORE_REV = 0x31,
        LOAD_N = 0x32,
        STORE_N = 0x33,
        ARRAY_U8 = 0x34,
        ARRAY_U8_LOAD = 0x35,
        ARRAY_U8_STORE = 0x36,
        LOCAL_U8 = 0x37,
        LOCAL_U8_LOAD = 0x38,
        LOCAL_U8_STORE = 0x39,
        STATIC_U8 = 0x3A,
        STATIC_U8_LOAD = 0x3B,
        STATIC_U8_STORE = 0x3C,
        IADD_U8 = 0x3D,
        IMUL_U8 = 0x3E,
        IOFFSET = 0x3F,
        IOFFSET_U8 = 0x40,
        IOFFSET_U8_LOAD = 0x41,
        IOFFSET_U8_STORE = 0x42,
        PUSH_CONST_S16 = 0x43,
        IADD_S16 = 0x44,
        IMUL_S16 = 0x45,
        IOFFSET_S16 = 0x46,
        IOFFSET_S16_LOAD = 0x47,
        IOFFSET_S16_STORE = 0x48,
        ARRAY_U16 = 0x49,
        ARRAY_U16_LOAD = 0x4A,
        ARRAY_U16_STORE = 0x4B,
        LOCAL_U16 = 0x4C,
        LOCAL_U16_LOAD = 0x4D,
        LOCAL_U16_STORE = 0x4E,
        STATIC_U16 = 0x4F,
        STATIC_U16_LOAD = 0x50,
        STATIC_U16_STORE = 0x51,
        GLOBAL_U16 = 0x52,
        GLOBAL_U16_LOAD = 0x53,
        GLOBAL_U16_STORE = 0x54,
        J = 0x55,
        JZ = 0x56,
        IEQ_JZ = 0x57,
        INE_JZ = 0x58,
        IGT_JZ = 0x59,
        IGE_JZ = 0x5A,
        ILT_JZ = 0x5B,
        ILE_JZ = 0x5C,
        CALL = 0x5D,
        GLOBAL_U24 = 0x5E,
        GLOBAL_U24_LOAD = 0x5F,
        GLOBAL_U24_STORE = 0x60,
        PUSH_CONST_U24 = 0x61,
        SWITCH = 0x62,
        STRING = 0x63,
        STRINGHASH = 0x64,
        TEXT_LABEL_ASSIGN_STRING = 0x65,
        TEXT_LABEL_ASSIGN_INT = 0x66,
        TEXT_LABEL_APPEND_STRING = 0x67,
        TEXT_LABEL_APPEND_INT = 0x68,
        TEXT_LABEL_COPY = 0x69,
        CATCH = 0x6A,
        THROW = 0x6B,
        CALLINDIRECT = 0x6C,
        PUSH_CONST_M1 = 0x6D,
        PUSH_CONST_0 = 0x6E,
        PUSH_CONST_1 = 0x6F,
        PUSH_CONST_2 = 0x70,
        PUSH_CONST_3 = 0x71,
        PUSH_CONST_4 = 0x72,
        PUSH_CONST_5 = 0x73,
        PUSH_CONST_6 = 0x74,
        PUSH_CONST_7 = 0x75,
        PUSH_CONST_FM1 = 0x76,
        PUSH_CONST_F0 = 0x77,
        PUSH_CONST_F1 = 0x78,
        PUSH_CONST_F2 = 0x79,
        PUSH_CONST_F3 = 0x7A,
        PUSH_CONST_F4 = 0x7B,
        PUSH_CONST_F5 = 0x7C,
        PUSH_CONST_F6 = 0x7D,
        PUSH_CONST_F7 = 0x7E,
        // TODO: document and support IS_BIT_SET opcode (IBITTEST?), added in b2612
        IBITTEST = 0x7F,
    }

    public static class OpcodeExtensions
    {
        public const int NumberOfOpcodes = 128;

        public static bool IsInvalid(this Opcode opcode)
            => (byte)opcode is >= NumberOfOpcodes;

        public static bool IsJump(this Opcode opcode)
            => opcode is Opcode.J or Opcode.JZ or
                         Opcode.IEQ_JZ or Opcode.INE_JZ or Opcode.IGT_JZ or
                         Opcode.IGE_JZ or Opcode.ILT_JZ or Opcode.ILE_JZ;

        public static bool IsControlFlow(this Opcode opcode)
            => IsJump(opcode) ||
               opcode is Opcode.LEAVE or Opcode.CALL or Opcode.SWITCH or Opcode.THROW or Opcode.CALLINDIRECT;

        public static string Mnemonic(this Opcode opcode) => opcode.ToString();

        /// <returns>
        /// The byte size of a instruction with this <paramref name="opcode"/>; or, <c>0</c> if the size is variable (i.e. <paramref name="opcode"/> is <see cref="Opcode.CALL"/> or <see cref="Opcode.SWITCH"/>).
        /// </returns>
        public static int ConstantByteSize(this Opcode opcode)
            => (int)opcode < NumberOfOpcodes ? ByteSizeTable[(int)opcode] : 1;

        /// <returns>
        /// The length in bytes of an instruction with this <paramref name="opcode"/> and <paramref name="bytecode"/>.
        /// </returns>
        public static int ByteSize(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            return ByteSize(bytecode);
        }

        /// <returns>
        /// The length in bytes of an instruction with this <paramref name="bytecode"/>.
        /// </returns>
        public static int ByteSize(ReadOnlySpan<byte> bytecode)
        {
            var opcode = (Opcode)bytecode[0];
            var s = opcode switch
            {
                Opcode.ENTER => bytecode[4] + 5,
                Opcode.SWITCH => 6 * bytecode[1] + 2,
                _ => opcode.ConstantByteSize(),
            };

            return s;
        }

        public static ReadOnlySpan<byte> GetInstructionSpan(ReadOnlySpan<byte> code, int address)
        {
            var opcode = (Opcode)code[address];
            var inst = code[address..];
            var instLength = opcode.ByteSize(inst);
            return inst[..instLength]; // trim to instruction length
        }

        public static byte GetU8Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode, int operandIndex = 0)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

            // TODO: check opcodes with U8 operands
            //if (opcode is Opcode.PUSH_CONST_U8)
            //{
            return bytecode[1 + operandIndex];
            //}
            //else
            //{
            //    throw new ArgumentException($"The opcode {opcode} does not have a U8 operand.", nameof(opcode));
            //}
        }

        public static short GetS16Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

            // TODO: check opcodes with U16 operands
            //if (opcode is Opcode.PUSH_CONST_U16)
            //{
            return BinaryPrimitives.ReadInt16LittleEndian(bytecode[1..]);
            //}
            //else
            //{
            //    throw new ArgumentException($"The opcode {opcode} does not have a U16 operand.", nameof(opcode));
            //}
        }

        public static ushort GetU16Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

            // TODO: check opcodes with U16 operands
            //if (opcode is Opcode.PUSH_CONST_U16)
            //{
            return BinaryPrimitives.ReadUInt16LittleEndian(bytecode[1..]);
            //}
            //else
            //{
            //    throw new ArgumentException($"The opcode {opcode} does not have a U16 operand.", nameof(opcode));
            //}
        }

        public static uint GetU24Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

            // TODO: check opcodes with U24 operands
            //if (opcode is )
            //{                var lo = inst[0];
            var lo = bytecode[1];
            var mi = bytecode[2];
            var hi = bytecode[3];
            
            return (uint)((hi << 16) | (mi << 8) | lo);
            //}
            //else
            //{
            //    throw new ArgumentException($"The opcode {opcode} does not have a U32 operand.", nameof(opcode));
            //}
        }

        public static uint GetU32Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

            // TODO: check opcodes with U32 operands
            //if (opcode is )
            //{
            return BinaryPrimitives.ReadUInt32LittleEndian(bytecode[1..]);
            //}
            //else
            //{
            //    throw new ArgumentException($"The opcode {opcode} does not have a U32 operand.", nameof(opcode));
            //}
        }

        public static float GetFloatOperand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            if (opcode is not Opcode.PUSH_CONST_F)
            {
                throw new ArgumentException($"The opcode {opcode} does not have a FLOAT operand.", nameof(opcode));
            }

            return BinaryPrimitives.ReadSingleLittleEndian(bytecode[1..]);
        }

        public static int GetSwitchNumberOfCases(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            ThrowIfNotExpectedOpcode(Opcode.SWITCH, bytecode);
            return bytecode[1];
        }

        public static SwitchCasesEnumerator GetSwitchOperands(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            return new(bytecode);
        }

        public static (byte ParamCount, ushort FrameSize) GetEnterOperands(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            ThrowIfNotExpectedOpcode(Opcode.ENTER, bytecode);
            return (bytecode[1], BinaryPrimitives.ReadUInt16LittleEndian(bytecode[2..]));
        }

        public static string? GetEnterFunctionName(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            ThrowIfNotExpectedOpcode(Opcode.ENTER, bytecode);

            if (bytecode[4] > 2)
            {
                var nameSlice = bytecode[5..^1];
                while (nameSlice[0] == 0xFF) { nameSlice = nameSlice[1..]; }

                return Encoding.UTF8.GetString(nameSlice);
            }

            return null;
        }

        public static (byte ParamCount, byte ReturnCount) GetLeaveOperands(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            ThrowIfNotExpectedOpcode(Opcode.LEAVE, bytecode);
            return (bytecode[1], bytecode[2]);
        }

        public static (byte ParamCount, byte ReturnCount, ushort CommandIndex) GetNativeOperands(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
                        ThrowIfNotExpectedOpcode(Opcode.NATIVE, bytecode);

            var paramReturnCounts = bytecode[0];
            var nativeIndexHi = bytecode[2];
            var nativeIndexLo = bytecode[3];

            var paramCount = (paramReturnCounts >> 2) & 0x3F;
            var returnCount = paramReturnCounts & 0x3;
            var nativeIndex = (nativeIndexHi << 8) | nativeIndexLo;

            return ((byte)paramCount, (byte)returnCount, (ushort)nativeIndex);
        }

        public static byte GetTextLabelLength(this Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
            if ((Opcode)bytecode[0] is not (Opcode.TEXT_LABEL_ASSIGN_STRING or Opcode.TEXT_LABEL_ASSIGN_INT or
                                            Opcode.TEXT_LABEL_APPEND_STRING or Opcode.TEXT_LABEL_APPEND_INT))
            {
                throw new ArgumentException($"The instruction opcode is not a TEXT_LABEL_ASSIGN/APPEND opcode.", nameof(bytecode));
            }
            return bytecode[1];
        }

        internal static void ThrowIfOpcodeDoesNotMatch(Opcode opcode, ReadOnlySpan<byte> bytecode)
        {
            if ((byte)opcode != bytecode[0])
            {
                throw new ArgumentException($"The opcode {opcode} does not match the bytecode {bytecode[0]:X2}.", nameof(bytecode));
            }
        }

        internal static void ThrowIfNotExpectedOpcode(Opcode expectedOpcode, ReadOnlySpan<byte> bytecode)
        {
            if (bytecode[0] != (byte)expectedOpcode)
            {
                throw new ArgumentException($"The instruction opcode is not {expectedOpcode}.", nameof(bytecode));
            }
        }

        public ref struct SwitchCasesEnumerator
        {
            private readonly ReadOnlySpan<byte> bytecode;
            private (uint Value, short JumpOffset, int OffsetInInstruction) current;
            private int index;

            public SwitchCasesEnumerator(ReadOnlySpan<byte> bytecode)
            {
                ThrowIfNotExpectedOpcode(Opcode.SWITCH, bytecode);

                this.bytecode = bytecode;
                current = default;
                index = 0;
            }

            public (uint Value, short JumpOffset, int OffsetInInstruction) Current => current;

            public SwitchCasesEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                var numCases = bytecode[1];
                if (index >= numCases)
                {
                    current = default;
                    return false;
                }

                var caseOffset = 2 + index * 6;
                var caseValue = BinaryPrimitives.ReadUInt32LittleEndian(bytecode[caseOffset..(caseOffset + 4)]);
                var caseJumpOffset = BinaryPrimitives.ReadInt16LittleEndian(bytecode[(caseOffset + 4)..]);

                current = (caseValue, caseJumpOffset, caseOffset);
                index++;
                return true;
            }
        }

        /// <returns>
        /// The number of operands required by <see cref="opcode"/>; or, <c>-1</c> if it accepts a variable number of operands (i.e. <paramref name="opcode"/> is <see cref="Opcode.SWITCH"/>).
        /// </returns>
        public static int NumberOfOperands(this Opcode opcode)
            => (int)opcode < NumberOfOpcodes ? NumberOfOperandsTable[(int)opcode] : throw new ArgumentException($"Unknown opcode '{opcode}'", nameof(opcode));

        private static readonly sbyte[] NumberOfOperandsTable = new sbyte[NumberOfOpcodes]
        {
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,1,2,3,1,1,0,0,3,2,2,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,0,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,-1,0,0,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        };

        private static readonly byte[] ByteSizeTable = new byte[NumberOfOpcodes]
        {
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,2,3,4,5,5,1,1,4,0,3,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,
            2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,4,4,4,
            4,4,0,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        };
    }
}
