namespace ScTools.ScriptAssembly.Targets.RDR2;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Instruction set used with <see cref="GameFiles.RDR2.Script"/>.
/// </summary>
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
    IADD_U8 = 0x3D, // or IOFFSET_U8
    IOFFSET_U8_LOAD = 0x3E,
    IOFFSET_U8_STORE = 0x3F,
    IMUL_U8 = 0x40,
    PUSH_CONST_S16 = 0x41,
    IADD_S16 = 0x42, // or IOFFSET_S16
    IOFFSET_S16_LOAD = 0x43,
    IOFFSET_S16_STORE = 0x44,
    IMUL_S16 = 0x45,
    ARRAY_U16 = 0x46,
    ARRAY_U16_LOAD = 0x47,
    ARRAY_U16_STORE = 0x48,
    LOCAL_U16 = 0x49,
    LOCAL_U16_LOAD = 0x4A,
    LOCAL_U16_STORE = 0x4B,
    STATIC_U16 = 0x4C,
    STATIC_U16_LOAD = 0x4D,
    STATIC_U16_STORE = 0x4E,
    GLOBAL_U16 = 0x4F,
    GLOBAL_U16_LOAD = 0x50,
    GLOBAL_U16_STORE = 0x51,
    CALL_0 = 0x52, // base address = 0x00000
    CALL_1 = 0x53, //                0x10000
    CALL_2 = 0x54, //                0x20000
    CALL_3 = 0x55, //                0x30000
    CALL_4 = 0x56, //                0x40000
    CALL_5 = 0x57, //                0x50000
    CALL_6 = 0x58, //                0x60000
    CALL_7 = 0x59, //                0x70000
    CALL_8 = 0x5A, //                0x80000
    CALL_9 = 0x5B, //                0x90000
    CALL_A = 0x5C, //                0xA0000
    CALL_B = 0x5D, //                0xB0000
    CALL_C = 0x5E, //                0xC0000
    CALL_D = 0x5F, //                0xD0000
    CALL_E = 0x60, //                0xE0000
    CALL_F = 0x61, //                0xF0000
    J = 0x62,
    JZ = 0x63,
    IEQ_JZ = 0x64,
    INE_JZ = 0x65,
    IGT_JZ = 0x66,
    IGE_JZ = 0x67,
    ILT_JZ = 0x68,
    ILE_JZ = 0x69,
    GLOBAL_U24 = 0x6A,
    GLOBAL_U24_LOAD = 0x6B,
    GLOBAL_U24_STORE = 0x6C,
    PUSH_CONST_U24 = 0x6D,
    SWITCH = 0x6E,
    STRING = 0x6F,     // pushes the string memory address without length prefix
    STRING_U32 = 0x70, // pushes the string memory address but the first 4 bytes are the length
    NULL = 0x71,
    TEXT_LABEL_ASSIGN_STRING = 0x72,
    TEXT_LABEL_ASSIGN_INT = 0x73,
    TEXT_LABEL_APPEND_STRING = 0x74,
    TEXT_LABEL_APPEND_INT = 0x75,
    TEXT_LABEL_COPY = 0x76,
    CATCH = 0x77,
    THROW = 0x78,
    CALLINDIRECT = 0x79,
    LEAVE_0_0 = 0x7A, // LEAVE_paramCount_returnCount
    LEAVE_0_1 = 0x7B,
    LEAVE_0_2 = 0x7C,
    LEAVE_0_3 = 0x7D,
    LEAVE_1_0 = 0x7E,
    LEAVE_1_1 = 0x7F,
    LEAVE_1_2 = 0x80,
    LEAVE_1_3 = 0x81,
    LEAVE_2_0 = 0x82,
    LEAVE_2_1 = 0x83,
    LEAVE_2_2 = 0x84,
    LEAVE_2_3 = 0x85,
    LEAVE_3_0 = 0x86,
    LEAVE_3_1 = 0x87,
    LEAVE_3_2 = 0x88,
    LEAVE_3_3 = 0x89,
    PUSH_CONST_M1 = 0x8A,
    PUSH_CONST_0 = 0x8B,
    PUSH_CONST_1 = 0x8C,
    PUSH_CONST_2 = 0x8D,
    PUSH_CONST_3 = 0x8E,
    PUSH_CONST_4 = 0x8F,
    PUSH_CONST_5 = 0x90,
    PUSH_CONST_6 = 0x91,
    PUSH_CONST_7 = 0x92,
    PUSH_CONST_FM1 = 0x93,
    PUSH_CONST_F0 = 0x94,
    PUSH_CONST_F1 = 0x95,
    PUSH_CONST_F2 = 0x96,
    PUSH_CONST_F3 = 0x97,
    PUSH_CONST_F4 = 0x98,
    PUSH_CONST_F5 = 0x99,
    PUSH_CONST_F6 = 0x9A,
    PUSH_CONST_F7 = 0x9B,
    __UNK_9C = 0x9C,
    __UNK_9D = 0x9D,
    __UNK_9E = 0x9E,
    __UNK_9F = 0x9F,
    __UNK_A0 = 0xA0,
    __UNK_A1 = 0xA1,
    __UNK_A2 = 0xA2,
    __UNK_A3 = 0xA3,
    __UNK_A4 = 0xA4,
    __UNK_A5 = 0xA5,
    __UNK_A6 = 0xA6,
    __UNK_A7 = 0xA7,
    __UNK_A8 = 0xA8,
    __UNK_A9 = 0xA9,
    __UNK_AA = 0xAA,
    __UNK_AB = 0xAB,
    __UNK_AC = 0xAC,
    __UNK_AD = 0xAD,
    __UNK_AE = 0xAE,
}

public static class OpcodeExtensions
{
    public static bool IsInvalid(this Opcode opcode)
        => opcode > Opcode.__UNK_AE;

    public static string Mnemonic(this Opcode opcode) => opcode.ToString();

    /// <returns>
    /// The number of operands required by <see cref="opcode"/>; or, <c>-1</c> if it accepts a variable number of operands (i.e. <paramref name="opcode"/> is <see cref="Opcode.SWITCH"/>).
    /// </returns>
    public static bool HasOperands(this Opcode opcode) => opcode.ConstantByteSize() != 1;

    /// <returns>
    /// The byte size of a instruction with this <paramref name="opcode"/>; or, <c>0</c> if the size depends on its operands.
    /// </returns>
    public static int ConstantByteSize(this Opcode opcode)
        => opcode switch
        {
            Opcode.PUSH_CONST_U8 or
            Opcode.ARRAY_U8 or
            Opcode.ARRAY_U8_LOAD or
            Opcode.ARRAY_U8_STORE or
            Opcode.LOCAL_U8 or
            Opcode.LOCAL_U8_LOAD or
            Opcode.LOCAL_U8_STORE or
            Opcode.STATIC_U8 or
            Opcode.STATIC_U8_LOAD or
            Opcode.STATIC_U8_STORE or
            Opcode.IADD_U8 or
            Opcode.IOFFSET_U8_LOAD or
            Opcode.IOFFSET_U8_STORE or
            Opcode.IMUL_U8 or
            Opcode.TEXT_LABEL_ASSIGN_STRING or
            Opcode.TEXT_LABEL_ASSIGN_INT or
            Opcode.TEXT_LABEL_APPEND_STRING or
            Opcode.TEXT_LABEL_APPEND_INT => 2,

            Opcode.PUSH_CONST_U8_U8 or
            Opcode.NATIVE or
            Opcode.LEAVE or
            Opcode.PUSH_CONST_S16 or
            Opcode.IADD_S16 or
            Opcode.IOFFSET_S16_LOAD or
            Opcode.IOFFSET_S16_STORE or
            Opcode.IMUL_S16 or
            Opcode.ARRAY_U16 or
            Opcode.ARRAY_U16_LOAD or
            Opcode.ARRAY_U16_STORE or
            Opcode.LOCAL_U16 or
            Opcode.LOCAL_U16_LOAD or
            Opcode.LOCAL_U16_STORE or
            Opcode.STATIC_U16 or
            Opcode.STATIC_U16_LOAD or
            Opcode.STATIC_U16_STORE or
            Opcode.GLOBAL_U16 or
            Opcode.GLOBAL_U16_LOAD or
            Opcode.GLOBAL_U16_STORE or
            (>= Opcode.CALL_0 and <= Opcode.CALL_F) or
            (>= Opcode.J and <= Opcode.ILE_JZ) => 3,

            Opcode.PUSH_CONST_U8_U8_U8 or
            Opcode.GLOBAL_U24 or
            Opcode.GLOBAL_U24_LOAD or
            Opcode.GLOBAL_U24_STORE or
            Opcode.PUSH_CONST_U24 => 4,

            Opcode.PUSH_CONST_U32 or
            Opcode.PUSH_CONST_F => 5,

            Opcode.ENTER or
            Opcode.SWITCH or
            Opcode.STRING or
            Opcode.STRING_U32 => 0,

            _ => 1,
        };

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
            Opcode.STRING => bytecode[1] + 2,
            Opcode.STRING_U32 => (int)(BinaryPrimitives.ReadUInt32LittleEndian(bytecode[1..]) + 5),
            _ => opcode.ConstantByteSize(),
        };

        return s;
    }

    public static Span<byte> GetInstructionSpan(Span<byte> code, int address)
    {
        var opcode = (Opcode)code[address];
        var inst = code[address..];
        var instLength = opcode.ByteSize(inst);
        return inst[..instLength]; // trim to instruction length
    }

    public static ReadOnlySpan<byte> GetInstructionSpan(ReadOnlySpan<byte> code, int address)
    {
        var opcode = (Opcode)code[address];
        var inst = code[address..];
        var instLength = opcode.ByteSize(inst);
        return inst[..instLength]; // trim to instruction length
    }

    public static string GetStringOperand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        if (opcode is Opcode.STRING)
        {
            return Encoding.UTF8.GetString(bytecode[2..^1]);
        }
        else if (opcode is Opcode.STRING_U32)
        {
            return Encoding.UTF8.GetString(bytecode[5..^1]);
        }
        else
        {
            throw new ArgumentException($"The opcode {opcode} does not have a string operand.", nameof(opcode));
        }
    }

    public static byte GetU8Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode, int operandIndex = 0)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        return bytecode[1 + operandIndex];
    }

    public static short GetS16Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        return BinaryPrimitives.ReadInt16LittleEndian(bytecode[1..]);
    }

    public static ushort GetU16Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        return BinaryPrimitives.ReadUInt16LittleEndian(bytecode[1..]);
    }

    public static uint GetU24Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        var lo = bytecode[1];
        var mi = bytecode[2];
        var hi = bytecode[3];

        return (uint)((hi << 16) | (mi << 8) | lo);
    }

    public static uint GetU32Operand(this Opcode opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        return BinaryPrimitives.ReadUInt32LittleEndian(bytecode[1..]);
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

    public static (int Base, ushort Offset, int Address) GetCallTarget(this Opcode opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        if (opcode is not (>= Opcode.CALL_0 and <= Opcode.CALL_F))
        {
            throw new ArgumentException($"The opcode {opcode} does not have a FLOAT operand.", nameof(opcode));
        }

        var callBase = ((int)opcode - (int)Opcode.CALL_0) << 16;
        var callOffset = BinaryPrimitives.ReadUInt16LittleEndian(bytecode[1..]);
        var callAddress = callBase | callOffset;
        return (callBase, callOffset, callAddress);
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

        if (bytecode[4] > 0)
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
        return opcode switch
        {
            Opcode.LEAVE => (bytecode[1], bytecode[2]),
            Opcode.LEAVE_0_0 => (0, 0),
            Opcode.LEAVE_0_1 => (0, 1),
            Opcode.LEAVE_0_2 => (0, 2),
            Opcode.LEAVE_0_3 => (0, 3),
            Opcode.LEAVE_1_0 => (1, 0),
            Opcode.LEAVE_1_1 => (1, 1),
            Opcode.LEAVE_1_2 => (1, 2),
            Opcode.LEAVE_1_3 => (1, 3),
            Opcode.LEAVE_2_0 => (2, 0),
            Opcode.LEAVE_2_1 => (2, 1),
            Opcode.LEAVE_2_2 => (2, 2),
            Opcode.LEAVE_2_3 => (2, 3),
            Opcode.LEAVE_3_0 => (3, 0),
            Opcode.LEAVE_3_1 => (3, 1),
            Opcode.LEAVE_3_2 => (3, 2),
            Opcode.LEAVE_3_3 => (3, 3),
            _ => throw new ArgumentException($"The instruction opcode is not a LEAVE opcode.", nameof(bytecode))
        };
    }

    public static (byte ParamCount, byte ReturnCount, ushort CommandIndex) GetNativeOperands(this Opcode opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        ThrowIfNotExpectedOpcode(Opcode.NATIVE, bytecode);

        var paramReturnCountsNativeIndexHi = bytecode[1];
        var nativeIndexHi = (paramReturnCountsNativeIndexHi >> 6) & 0x3;
        var nativeIndexLo = bytecode[2];

        var paramCount = (paramReturnCountsNativeIndexHi >> 1) & 0x1F;
        var returnCount = paramReturnCountsNativeIndexHi & 0x1;
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

    public readonly record struct SwitchCase(uint Value, short JumpOffset, int CaseIndex)
    {
        public int OffsetWithinInstruction => 2 + CaseIndex * 6;
        public int GetJumpTargetAddress(int switchBaseAddress) => switchBaseAddress + OffsetWithinInstruction + 6 + JumpOffset;
    }

    public ref struct SwitchCasesEnumerator
    {
        private readonly ReadOnlySpan<byte> bytecode;
        private SwitchCase current;
        private int index;

        public SwitchCasesEnumerator(ReadOnlySpan<byte> bytecode)
        {
            ThrowIfNotExpectedOpcode(Opcode.SWITCH, bytecode);

            this.bytecode = bytecode;
            current = default;
            index = 0;
        }

        public SwitchCase Current => current;

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

            current = new(caseValue, caseJumpOffset, index);
            index++;
            return true;
        }
    }
}
