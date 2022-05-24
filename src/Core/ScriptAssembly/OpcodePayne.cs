using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace ScTools.ScriptAssembly;

/// <summary>
/// Instruction set used with <see cref="GameFiles.ScriptPayne"/>.
/// </summary>
public enum OpcodePayne : byte
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
    J = 0x22,
    JZ = 0x23,
    JNZ = 0x24,
    I2F = 0x25,
    F2I = 0x26,
    F2V = 0x27,
    PUSH_CONST_U16 = 0x28,
    PUSH_CONST_U32 = 0x29,
    PUSH_CONST_F = 0x2A,
    DUP = 0x2B,
    DROP = 0x2C,
    NATIVE = 0x2D,
    CALL = 0x2E,
    ENTER = 0x2F,
    LEAVE = 0x30,
    LOAD = 0x31,
    STORE = 0x32,
    STORE_REV = 0x33,
    LOAD_N = 0x34,
    STORE_N = 0x35,
    LOCAL_0 = 0x36,
    LOCAL_1 = 0x37,
    LOCAL_2 = 0x38,
    LOCAL_3 = 0x39,
    LOCAL_4 = 0x3A,
    LOCAL_5 = 0x3B,
    LOCAL_6 = 0x3C,
    LOCAL_7 = 0x3D,
    LOCAL = 0x3E,
    STATIC = 0x3F,
    GLOBAL = 0x40,
    ARRAY = 0x41,
    SWITCH = 0x42,
    STRING = 0x43,
    NULL = 0x44,
    TEXT_LABEL_ASSIGN_STRING = 0x45,
    TEXT_LABEL_ASSIGN_INT = 0x46,
    TEXT_LABEL_APPEND_STRING = 0x47,
    TEXT_LABEL_APPEND_INT = 0x48,
    CATCH = 0x49,
    THROW = 0x4A,
    TEXT_LABEL_COPY = 0x4B,
    CALLINDIRECT = 0x4C,

    PUSH_CONST_M16 = 0x50,
    PUSH_CONST_M15 = 0x51,
    PUSH_CONST_M14 = 0x52,
    PUSH_CONST_M13 = 0x53,
    PUSH_CONST_M12 = 0x54,
    PUSH_CONST_M11 = 0x55,
    PUSH_CONST_M10 = 0x56,
    PUSH_CONST_M9 = 0x57,
    PUSH_CONST_M8 = 0x58,
    PUSH_CONST_M7 = 0x59,
    PUSH_CONST_M6 = 0x5A,
    PUSH_CONST_M5 = 0x5B,
    PUSH_CONST_M4 = 0x5C,
    PUSH_CONST_M3 = 0x5D,
    PUSH_CONST_M2 = 0x5E,
    PUSH_CONST_M1 = 0x5F,
    PUSH_CONST_0 = 0x60,
    PUSH_CONST_1 = 0x61,
    PUSH_CONST_2 = 0x62,
    PUSH_CONST_3 = 0x63,
    PUSH_CONST_4 = 0x64,
    PUSH_CONST_5 = 0x65,
    PUSH_CONST_6 = 0x66,
    PUSH_CONST_7 = 0x67,
    PUSH_CONST_8 = 0x68,
    PUSH_CONST_9 = 0x69,
    PUSH_CONST_10 = 0x6A,
    PUSH_CONST_11 = 0x6B,
    PUSH_CONST_12 = 0x6C,
    PUSH_CONST_13 = 0x6D,
    PUSH_CONST_14 = 0x6E,
    PUSH_CONST_15 = 0x6F,
    PUSH_CONST_16 = 0x70,
    PUSH_CONST_17 = 0x71,
    PUSH_CONST_18 = 0x72,
    PUSH_CONST_19 = 0x73,
    PUSH_CONST_20 = 0x74,
    PUSH_CONST_21 = 0x75,
    PUSH_CONST_22 = 0x76,
    PUSH_CONST_23 = 0x77,
    PUSH_CONST_24 = 0x78,
    PUSH_CONST_25 = 0x79,
    PUSH_CONST_26 = 0x7A,
    PUSH_CONST_27 = 0x7B,
    PUSH_CONST_28 = 0x7C,
    PUSH_CONST_29 = 0x7D,
    PUSH_CONST_30 = 0x7E,
    PUSH_CONST_31 = 0x7F,
    PUSH_CONST_32 = 0x80,
    PUSH_CONST_33 = 0x81,
    PUSH_CONST_34 = 0x82,
    PUSH_CONST_35 = 0x83,
    PUSH_CONST_36 = 0x84,
    PUSH_CONST_37 = 0x85,
    PUSH_CONST_38 = 0x86,
    PUSH_CONST_39 = 0x87,
    PUSH_CONST_40 = 0x88,
    PUSH_CONST_41 = 0x89,
    PUSH_CONST_42 = 0x8A,
    PUSH_CONST_43 = 0x8B,
    PUSH_CONST_44 = 0x8C,
    PUSH_CONST_45 = 0x8D,
    PUSH_CONST_46 = 0x8E,
    PUSH_CONST_47 = 0x8F,
    PUSH_CONST_48 = 0x90,
    PUSH_CONST_49 = 0x91,
    PUSH_CONST_50 = 0x92,
    PUSH_CONST_51 = 0x93,
    PUSH_CONST_52 = 0x94,
    PUSH_CONST_53 = 0x95,
    PUSH_CONST_54 = 0x96,
    PUSH_CONST_55 = 0x97,
    PUSH_CONST_56 = 0x98,
    PUSH_CONST_57 = 0x99,
    PUSH_CONST_58 = 0x9A,
    PUSH_CONST_59 = 0x9B,
    PUSH_CONST_60 = 0x9C,
    PUSH_CONST_61 = 0x9D,
    PUSH_CONST_62 = 0x9E,
    PUSH_CONST_63 = 0x9F,
    PUSH_CONST_64 = 0xA0,
    PUSH_CONST_65 = 0xA1,
    PUSH_CONST_66 = 0xA2,
    PUSH_CONST_67 = 0xA3,
    PUSH_CONST_68 = 0xA4,
    PUSH_CONST_69 = 0xA5,
    PUSH_CONST_70 = 0xA6,
    PUSH_CONST_71 = 0xA7,
    PUSH_CONST_72 = 0xA8,
    PUSH_CONST_73 = 0xA9,
    PUSH_CONST_74 = 0xAA,
    PUSH_CONST_75 = 0xAB,
    PUSH_CONST_76 = 0xAC,
    PUSH_CONST_77 = 0xAD,
    PUSH_CONST_78 = 0xAE,
    PUSH_CONST_79 = 0xAF,
    PUSH_CONST_80 = 0xB0,
    PUSH_CONST_81 = 0xB1,
    PUSH_CONST_82 = 0xB2,
    PUSH_CONST_83 = 0xB3,
    PUSH_CONST_84 = 0xB4,
    PUSH_CONST_85 = 0xB5,
    PUSH_CONST_86 = 0xB6,
    PUSH_CONST_87 = 0xB7,
    PUSH_CONST_88 = 0xB8,
    PUSH_CONST_89 = 0xB9,
    PUSH_CONST_90 = 0xBA,
    PUSH_CONST_91 = 0xBB,
    PUSH_CONST_92 = 0xBC,
    PUSH_CONST_93 = 0xBD,
    PUSH_CONST_94 = 0xBE,
    PUSH_CONST_95 = 0xBF,
    PUSH_CONST_96 = 0xC0,
    PUSH_CONST_97 = 0xC1,
    PUSH_CONST_98 = 0xC2,
    PUSH_CONST_99 = 0xC3,
    PUSH_CONST_100 = 0xC4,
    PUSH_CONST_101 = 0xC5,
    PUSH_CONST_102 = 0xC6,
    PUSH_CONST_103 = 0xC7,
    PUSH_CONST_104 = 0xC8,
    PUSH_CONST_105 = 0xC9,
    PUSH_CONST_106 = 0xCA,
    PUSH_CONST_107 = 0xCB,
    PUSH_CONST_108 = 0xCC,
    PUSH_CONST_109 = 0xCD,
    PUSH_CONST_110 = 0xCE,
    PUSH_CONST_111 = 0xCF,
    PUSH_CONST_112 = 0xD0,
    PUSH_CONST_113 = 0xD1,
    PUSH_CONST_114 = 0xD2,
    PUSH_CONST_115 = 0xD3,
    PUSH_CONST_116 = 0xD4,
    PUSH_CONST_117 = 0xD5,
    PUSH_CONST_118 = 0xD6,
    PUSH_CONST_119 = 0xD7,
    PUSH_CONST_120 = 0xD8,
    PUSH_CONST_121 = 0xD9,
    PUSH_CONST_122 = 0xDA,
    PUSH_CONST_123 = 0xDB,
    PUSH_CONST_124 = 0xDC,
    PUSH_CONST_125 = 0xDD,
    PUSH_CONST_126 = 0xDE,
    PUSH_CONST_127 = 0xDF,
    PUSH_CONST_128 = 0xE0,
    PUSH_CONST_129 = 0xE1,
    PUSH_CONST_130 = 0xE2,
    PUSH_CONST_131 = 0xE3,
    PUSH_CONST_132 = 0xE4,
    PUSH_CONST_133 = 0xE5,
    PUSH_CONST_134 = 0xE6,
    PUSH_CONST_135 = 0xE7,
    PUSH_CONST_136 = 0xE8,
    PUSH_CONST_137 = 0xE9,
    PUSH_CONST_138 = 0xEA,
    PUSH_CONST_139 = 0xEB,
    PUSH_CONST_140 = 0xEC,
    PUSH_CONST_141 = 0xED,
    PUSH_CONST_142 = 0xEE,
    PUSH_CONST_143 = 0xEF,
    PUSH_CONST_144 = 0xF0,
    PUSH_CONST_145 = 0xF1,
    PUSH_CONST_146 = 0xF2,
    PUSH_CONST_147 = 0xF3,
    PUSH_CONST_148 = 0xF4,
    PUSH_CONST_149 = 0xF5,
    PUSH_CONST_150 = 0xF6,
    PUSH_CONST_151 = 0xF7,
    PUSH_CONST_152 = 0xF8,
    PUSH_CONST_153 = 0xF9,
    PUSH_CONST_154 = 0xFA,
    PUSH_CONST_155 = 0xFB,
    PUSH_CONST_156 = 0xFC,
    PUSH_CONST_157 = 0xFD,
    PUSH_CONST_158 = 0xFE,
    PUSH_CONST_159 = 0xFF,
}

public static class OpcodePayneExtensions
{
    public static bool IsInvalid(this OpcodePayne opcode)
        => opcode is > OpcodePayne.CALLINDIRECT and < OpcodePayne.PUSH_CONST_M16;

    public static bool IsJump(this OpcodePayne opcode)
        => opcode is OpcodePayne.J or OpcodePayne.JZ or OpcodePayne.JNZ;

    public static bool IsControlFlow(this OpcodePayne opcode)
        => IsJump(opcode) ||
            opcode is OpcodePayne.LEAVE or OpcodePayne.CALL or OpcodePayne.SWITCH or OpcodePayne.THROW;

    public static string Mnemonic(this OpcodePayne opcode) => opcode.ToString();


    /// <returns>
    /// The number of operands required by <see cref="opcode"/>; or, <c>-1</c> if it accepts a variable number of operands (i.e. <paramref name="opcode"/> is <see cref="Opcode.SWITCH"/>).
    /// </returns>
    public static int NumberOfOperands(this OpcodePayne opcode)
        => opcode switch
        {
            OpcodePayne.LEAVE or
            OpcodePayne.ENTER => 2,

            OpcodePayne.J or
            OpcodePayne.JZ or
            OpcodePayne.JNZ or
            OpcodePayne.PUSH_CONST_U16 or
            OpcodePayne.PUSH_CONST_U32 or
            OpcodePayne.PUSH_CONST_F or
            OpcodePayne.CALL or
            OpcodePayne.NATIVE => 7,

            OpcodePayne.TEXT_LABEL_ASSIGN_STRING or
            OpcodePayne.TEXT_LABEL_ASSIGN_INT or
            OpcodePayne.TEXT_LABEL_APPEND_STRING or
            OpcodePayne.TEXT_LABEL_APPEND_INT or
            OpcodePayne.STRING => 1,

            OpcodePayne.SWITCH => -1,

            _ => 0,
        };

    /// <returns>
    /// The byte size of a instruction with this <paramref name="opcode"/>; or, <c>0</c> if the size depends on its operands.
    /// </returns>
    public static int ConstantByteSize(this OpcodePayne opcode)
        => opcode switch
        {
            OpcodePayne.TEXT_LABEL_ASSIGN_STRING or
            OpcodePayne.TEXT_LABEL_ASSIGN_INT or
            OpcodePayne.TEXT_LABEL_APPEND_STRING or
            OpcodePayne.TEXT_LABEL_APPEND_INT => 2,

            OpcodePayne.PUSH_CONST_U16 or
            OpcodePayne.LEAVE => 3,

            OpcodePayne.J or
            OpcodePayne.JZ or
            OpcodePayne.JNZ or
            OpcodePayne.PUSH_CONST_U32 or
            OpcodePayne.PUSH_CONST_F or
            OpcodePayne.CALL => 5,

            OpcodePayne.NATIVE => 7,

            OpcodePayne.ENTER or
            OpcodePayne.SWITCH or
            OpcodePayne.STRING => 0,

            _ => 1,
        };

    /// <returns>
    /// The length in bytes of an instruction with this <paramref name="opcode"/> and <paramref name="bytecode"/>.
    /// </returns>
    public static int ByteSize(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        return ByteSize(bytecode);
    }

    /// <returns>
    /// The length in bytes of an instruction with this <paramref name="bytecode"/>.
    /// </returns>
    public static int ByteSize(ReadOnlySpan<byte> bytecode)
    {
        var opcode = (OpcodePayne)bytecode[0];
        var s = opcode switch
        {
            OpcodePayne.ENTER => bytecode[4] + 5,
            OpcodePayne.SWITCH => 8 * bytecode[1] + 2,
            OpcodePayne.STRING => SizeOfSTRING(bytecode),
            _ => opcode.ConstantByteSize(),
        };

        return s;

        static int SizeOfSTRING(ReadOnlySpan<byte> bytecode)
        {
            int size = 2;
            int strLength = bytecode[1];
            if (strLength == 0)
            {
                // long string
                strLength = bytecode[2] | (bytecode[3] << 8);
                size += 2;
            }
            size += strLength;
            return size;
        }
    }

    public static ReadOnlySpan<byte> GetInstructionSpan(ReadOnlySpan<byte> code, int address)
    {
        var opcode = (OpcodePayne)code[address];
        var inst = code[address..];
        var instLength = opcode.ByteSize(inst);
        return inst[..instLength]; // trim to instruction length
    }

    public static string GetStringOperand(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        if (opcode is OpcodePayne.STRING)
        {
            var strStart = 2;
            var strLength = bytecode[1];
            if (strLength == 0)
            {
                // long string, skip the 2 bytes that store the length
                strStart += 2;
            }

            return Encoding.UTF8.GetString(bytecode[strStart..^1]);
        }
        else
        {
            throw new ArgumentException($"The opcode {opcode} does not have a string operand.", nameof(opcode));
        }
    }

    public static ushort GetU16Operand(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        if (opcode is OpcodePayne.PUSH_CONST_U16)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(bytecode[1..]);
        }
        else
        {
            throw new ArgumentException($"The opcode {opcode} does not have a U16 operand.", nameof(opcode));
        }
    }

    public static uint GetU32Operand(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        if (opcode is OpcodePayne.PUSH_CONST_U32 or 
                      OpcodePayne.J or OpcodePayne.JZ or OpcodePayne.JNZ or
                      OpcodePayne.CALL)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(bytecode[1..]);
        }
        else
        {
            throw new ArgumentException($"The opcode {opcode} does not have a U32 operand.", nameof(opcode));
        }
    }

    public static float GetFloatOperand(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);

        if (opcode is OpcodePayne.PUSH_CONST_F)
        {
            return BinaryPrimitives.ReadSingleLittleEndian(bytecode[1..]);
        }
        else
        {
            throw new ArgumentException($"The opcode {opcode} does not have a FLOAT operand.", nameof(opcode));
        }
    }

    public static int GetSwitchNumberOfCases(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        ThrowIfNotExpectedOpcode(OpcodePayne.SWITCH, bytecode);
        return bytecode[1];
    }

    public static SwitchCasesEnumerator GetSwitchOperands(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        return new(bytecode);
    }

    public static (byte ParamCount, ushort FrameSize) GetEnterOperands(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        ThrowIfNotExpectedOpcode(OpcodePayne.ENTER, bytecode);
        return (bytecode[1], BinaryPrimitives.ReadUInt16LittleEndian(bytecode[2..]));
    }

    public static string? GetEnterFunctionName(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        ThrowIfNotExpectedOpcode(OpcodePayne.ENTER, bytecode);

        if (bytecode[4] > 2)
        {
            var nameSlice = bytecode[5..^1];
            while (nameSlice[0] == 0xFF) { nameSlice = nameSlice[1..]; }

            return Encoding.UTF8.GetString(nameSlice);
        }

        return null;
    }

    public static (byte ParamCount, byte ReturnCount) GetLeaveOperands(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        ThrowIfNotExpectedOpcode(OpcodePayne.LEAVE, bytecode);
        return (bytecode[1], bytecode[2]);
    }

    public static (byte ParamCount, byte ReturnCount, uint CommandHash) GetNativeOperands(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        ThrowIfNotExpectedOpcode(OpcodePayne.NATIVE, bytecode);
        return (bytecode[1], bytecode[2], BinaryPrimitives.ReadUInt32LittleEndian(bytecode[3..]));
    }

    public static byte GetTextLabelLength(this OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        ThrowIfOpcodeDoesNotMatch(opcode, bytecode);
        if ((OpcodePayne)bytecode[0] is not (OpcodePayne.TEXT_LABEL_ASSIGN_STRING or OpcodePayne.TEXT_LABEL_ASSIGN_INT or
                                             OpcodePayne.TEXT_LABEL_APPEND_STRING or OpcodePayne.TEXT_LABEL_APPEND_INT))
        {
            throw new ArgumentException($"The instruction opcode is not a TEXT_LABEL_ASSIGN/APPEND opcode.", nameof(bytecode));
        }
        return bytecode[1];
    }

    internal static void ThrowIfOpcodeDoesNotMatch(OpcodePayne opcode, ReadOnlySpan<byte> bytecode)
    {
        if ((byte)opcode != bytecode[0])
        {
            throw new ArgumentException($"The opcode {opcode} does not match the bytecode {bytecode[0]:X2}.", nameof(bytecode));
        }
    }

    internal static void ThrowIfNotExpectedOpcode(OpcodePayne expectedOpcode, ReadOnlySpan<byte> bytecode)
    {
        if (bytecode[0] != (byte)expectedOpcode)
        {
            throw new ArgumentException($"The instruction opcode is not {expectedOpcode}.", nameof(bytecode));
        }
    }

    public readonly record struct SwitchCase(uint Value, int JumpAddress, int CaseIndex);

    public ref struct SwitchCasesEnumerator
    {
        private readonly ReadOnlySpan<byte> bytecode;
        private SwitchCase current;
        private int index;

        public SwitchCasesEnumerator(ReadOnlySpan<byte> bytecode)
        {
            ThrowIfNotExpectedOpcode(OpcodePayne.SWITCH, bytecode);

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

            var caseOffset = 2 + index * 8;
            var caseValue = BinaryPrimitives.ReadUInt32LittleEndian(bytecode[caseOffset..(caseOffset + 4)]);
            var caseJumpAddr = BinaryPrimitives.ReadInt32LittleEndian(bytecode[(caseOffset + 4)..]);
            
            current = new(caseValue, caseJumpAddr, index);
            index++;
            return true;
        }
    }
}
