namespace ScTools
{
    using System;
    using System.Diagnostics;

    using Inst = Instruction;
    using Tokens = TokenEnumerator;
    using Code = Assembler.ICodeBuilder;

    internal enum OperandType
    {
        U8,
        U16,
        S16,
        U24,
        U32,
        F32,
        Label,
        SwitchCase,
        String,
    }

    internal readonly struct Operand
    {
        public OperandType Type { get; }
        public byte U8 { get; }
        public ushort U16 { get; }
        public short S16 { get; }
        public uint U24 => U32;
        public uint U32 { get; }
        public float F32 { get; }
        public string Label => String;
        public (uint Value, string Label) SwitchCase { get; }
        public string String { get; }

        private Operand(OperandType type) : this() { Type = type; }
        public Operand(byte u8) : this(OperandType.U8) { U8 = u8; }
        public Operand(ushort u16) : this(OperandType.U16) { U16 = u16; }
        public Operand(short s16) : this(OperandType.S16) { S16 = s16; }
        public Operand(uint u32, OperandType type = OperandType.U32) : this(type)
        {
            if (type != OperandType.U24 && type != OperandType.U32)
            {
                throw new ArgumentException("Incorrect OperandType for a uint value. Must be U24 or U32", nameof(type));
            }

            U32 = u32;
        }
        public Operand(float f32) : this(OperandType.F32) { F32 = f32; }
        public Operand(string str, OperandType type) : this(type)
        {
            if (type != OperandType.Label && type != OperandType.String)
            {
                throw new ArgumentException("Incorrect OperandType for a string value. Must be Label or String", nameof(type));
            }

            String = str;
        }
        public Operand((uint Value, string Label) switchCase) : this(OperandType.SwitchCase) { SwitchCase = switchCase; }
    }

    internal readonly struct Instruction
    {
        public const int NumberOfInstructions = 127;
        public const int MaxOperands = byte.MaxValue;

        /// <param name="operandsBuffer">A buffer of size at least <see cref="MaxOperands"/>.</param>
        public delegate ReadOnlySpan<Operand> TokenParser(in Inst inst, ref Tokens tokens, Span<Operand> operandsBuffer);
        public delegate void CodeAssembler(in Inst inst, ReadOnlySpan<Operand> operands, Code code);

        public byte Opcode { get; }
        public string Mnemonic { get; }
        public uint MnemonicHash { get; }
        public TokenParser Parser { get; }
        public CodeAssembler Assembler { get; }

        public bool IsValid => Mnemonic != null;

        private Instruction(string mnemonic, byte opcode, TokenParser parser, CodeAssembler assembler)
        {
            Debug.Assert(opcode < NumberOfInstructions);

            Opcode = opcode;
            Mnemonic = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
            MnemonicHash = mnemonic.ToHash();
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            Assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));

            Debug.Assert(SetStorage[opcode].Mnemonic == null); // ensure we haven't repeated an opcode

            SetStorage[opcode] = this;
        }

        public ReadOnlySpan<Operand> Parse(ref Tokens tokens, Span<Operand> operandsBuffer) => Parser(this, ref tokens, operandsBuffer);
        public void Assemble(ReadOnlySpan<Operand> operands, Code code) => Assembler(this, operands, code);

        private static readonly Inst[] SetStorage = new Inst[NumberOfInstructions];
        private static readonly int[] SetSorted = new int[NumberOfInstructions]; // indices sorted based on MnemonicHash

        public static ReadOnlySpan<Inst> Set => SetStorage.AsSpan();

        static Instruction()
        {
            for (int i = 0; i < NumberOfInstructions; i++)
            {
                SetSorted[i] = i;
                Debug.Assert(SetStorage[i].IsValid);
            }

            Array.Sort(SetSorted, (a, b) => SetStorage[a].MnemonicHash.CompareTo(SetStorage[b].MnemonicHash));

            Debug.Assert(!Invalid.IsValid);
        }

        public static ref readonly Inst FindByMnemonic(ReadOnlySpan<char> mnemonic) => ref FindByMnemonic(mnemonic.ToHash());
        public static ref readonly Inst FindByMnemonic(uint mnemonicHash)
        {
            var indices = SetSorted;
            var set = Set;

            int left = 0;
            int right = NumberOfInstructions - 1;

            while (left <= right)
            {
                int middle = (left + right) / 2;
                ref readonly var inst = ref set[indices[middle]];
                uint middleKey = inst.MnemonicHash;
                int cmp = middleKey.CompareTo(mnemonicHash);
                if (cmp == 0)
                {
                    return ref inst;
                }
                else if (cmp < 0)
                {
                    left = middle + 1;
                }
                else
                {
                    right = middle - 1;
                }
            }

            return ref Invalid;
        }

        public static readonly Inst Invalid = default;
        public static readonly Inst NOP = new Inst(nameof(NOP), 0x00, I, I);
        public static readonly Inst IADD = new Inst(nameof(IADD), 0x01, I, I);
        public static readonly Inst ISUB = new Inst(nameof(ISUB), 0x02, I, I);
        public static readonly Inst IMUL = new Inst(nameof(IMUL), 0x03, I, I);
        public static readonly Inst IDIV = new Inst(nameof(IDIV), 0x04, I, I);
        public static readonly Inst IMOD = new Inst(nameof(IMOD), 0x05, I, I);
        public static readonly Inst INOT = new Inst(nameof(INOT), 0x06, I, I);
        public static readonly Inst INEG = new Inst(nameof(INEG), 0x07, I, I);
        public static readonly Inst IEQ = new Inst(nameof(IEQ), 0x08, I, I);
        public static readonly Inst INE = new Inst(nameof(INE), 0x09, I, I);
        public static readonly Inst IGT = new Inst(nameof(IGT), 0x0A, I, I);
        public static readonly Inst IGE = new Inst(nameof(IGE), 0x0B, I, I);
        public static readonly Inst ILT = new Inst(nameof(ILT), 0x0C, I, I);
        public static readonly Inst ILE = new Inst(nameof(ILE), 0x0D, I, I);
        public static readonly Inst FADD = new Inst(nameof(FADD), 0x0E, I, I);
        public static readonly Inst FSUB = new Inst(nameof(FSUB), 0x0F, I, I);
        public static readonly Inst FMUL = new Inst(nameof(FMUL), 0x10, I, I);
        public static readonly Inst FDIV = new Inst(nameof(FDIV), 0x11, I, I);
        public static readonly Inst FMOD = new Inst(nameof(FMOD), 0x12, I, I);
        public static readonly Inst FNEG = new Inst(nameof(FNEG), 0x13, I, I);
        public static readonly Inst FEQ = new Inst(nameof(FEQ), 0x14, I, I);
        public static readonly Inst FNE = new Inst(nameof(FNE), 0x15, I, I);
        public static readonly Inst FGT = new Inst(nameof(FGT), 0x16, I, I);
        public static readonly Inst FGE = new Inst(nameof(FGE), 0x17, I, I);
        public static readonly Inst FLT = new Inst(nameof(FLT), 0x18, I, I);
        public static readonly Inst FLE = new Inst(nameof(FLE), 0x19, I, I);
        public static readonly Inst VADD = new Inst(nameof(VADD), 0x1A, I, I);
        public static readonly Inst VSUB = new Inst(nameof(VSUB), 0x1B, I, I);
        public static readonly Inst VMUL = new Inst(nameof(VMUL), 0x1C, I, I);
        public static readonly Inst VDIV = new Inst(nameof(VDIV), 0x1D, I, I);
        public static readonly Inst VNEG = new Inst(nameof(VNEG), 0x1E, I, I);
        public static readonly Inst IAND = new Inst(nameof(IAND), 0x1F, I, I);
        public static readonly Inst IOR = new Inst(nameof(IOR), 0x20, I, I);
        public static readonly Inst IXOR = new Inst(nameof(IXOR), 0x21, I, I);
        public static readonly Inst I2F = new Inst(nameof(I2F), 0x22, I, I);
        public static readonly Inst F2I = new Inst(nameof(F2I), 0x23, I, I);
        public static readonly Inst F2V = new Inst(nameof(F2V), 0x24, I, I);
        public static readonly Inst PUSH_CONST_U8 = new Inst(nameof(PUSH_CONST_U8), 0x25, I_b, I_b);
        public static readonly Inst PUSH_CONST_U8_U8 = new Inst(nameof(PUSH_CONST_U8_U8), 0x26, I_b_b, I_b_b);
        public static readonly Inst PUSH_CONST_U8_U8_U8 = new Inst(nameof(PUSH_CONST_U8_U8_U8), 0x27, I_b_b_b, I_b_b_b);
        public static readonly Inst PUSH_CONST_U32 = new Inst(nameof(PUSH_CONST_U32), 0x28, I_u32, I_u32);
        public static readonly Inst PUSH_CONST_F = new Inst(nameof(PUSH_CONST_F), 0x29, I_f, I_f);
        public static readonly Inst DUP = new Inst(nameof(DUP), 0x2A, I, I);
        public static readonly Inst DROP = new Inst(nameof(DROP), 0x2B, I, I);
        public static readonly Inst NATIVE = new Inst(nameof(NATIVE), 0x2C, I_native, I_native);
        public static readonly Inst ENTER = new Inst(nameof(ENTER), 0x2D, I_enter, I_enter);
        public static readonly Inst LEAVE = new Inst(nameof(LEAVE), 0x2E, I_b_b, I_b_b);
        public static readonly Inst LOAD = new Inst(nameof(LOAD), 0x2F, I, I);
        public static readonly Inst STORE = new Inst(nameof(STORE), 0x30, I, I);
        public static readonly Inst STORE_REV = new Inst(nameof(STORE_REV), 0x31, I, I);
        public static readonly Inst LOAD_N = new Inst(nameof(LOAD_N), 0x32, I, I);
        public static readonly Inst STORE_N = new Inst(nameof(STORE_N), 0x33, I, I);
        public static readonly Inst ARRAY_U8 = new Inst(nameof(ARRAY_U8), 0x34, I_b, I_b);
        public static readonly Inst ARRAY_U8_LOAD = new Inst(nameof(ARRAY_U8_LOAD), 0x35, I_b, I_b);
        public static readonly Inst ARRAY_U8_STORE = new Inst(nameof(ARRAY_U8_STORE), 0x36, I_b, I_b);
        public static readonly Inst LOCAL_U8 = new Inst(nameof(LOCAL_U8), 0x37, I_b, I_b);
        public static readonly Inst LOCAL_U8_LOAD = new Inst(nameof(LOCAL_U8_LOAD), 0x38, I_b, I_b);
        public static readonly Inst LOCAL_U8_STORE = new Inst(nameof(LOCAL_U8_STORE), 0x39, I_b, I_b);
        public static readonly Inst STATIC_U8 = new Inst(nameof(STATIC_U8), 0x3A, I_b, I_b);
        public static readonly Inst STATIC_U8_LOAD = new Inst(nameof(STATIC_U8_LOAD), 0x3B, I_b, I_b);
        public static readonly Inst STATIC_U8_STORE = new Inst(nameof(STATIC_U8_STORE), 0x3C, I_b, I_b);
        public static readonly Inst IADD_U8 = new Inst(nameof(IADD_U8), 0x3D, I_b, I_b);
        public static readonly Inst IMUL_U8 = new Inst(nameof(IMUL_U8), 0x3E, I_b, I_b);
        public static readonly Inst IOFFSET = new Inst(nameof(IOFFSET), 0x3F, I, I);
        public static readonly Inst IOFFSET_U8 = new Inst(nameof(IOFFSET_U8), 0x40, I_b, I_b);
        public static readonly Inst IOFFSET_U8_LOAD = new Inst(nameof(IOFFSET_U8_LOAD), 0x41, I_b, I_b);
        public static readonly Inst IOFFSET_U8_STORE = new Inst(nameof(IOFFSET_U8_STORE), 0x42, I_b, I_b);
        public static readonly Inst PUSH_CONST_S16 = new Inst(nameof(PUSH_CONST_S16), 0x43, I_s16, I_s16);
        public static readonly Inst IADD_S16 = new Inst(nameof(IADD_S16), 0x44, I_s16, I_s16);
        public static readonly Inst IMUL_S16 = new Inst(nameof(IMUL_S16), 0x45, I_s16, I_s16);
        public static readonly Inst IOFFSET_S16 = new Inst(nameof(IOFFSET_S16), 0x46, I_s16, I_s16);
        public static readonly Inst IOFFSET_S16_LOAD = new Inst(nameof(IOFFSET_S16_LOAD), 0x47, I_s16, I_s16);
        public static readonly Inst IOFFSET_S16_STORE = new Inst(nameof(IOFFSET_S16_STORE), 0x48, I_s16, I_s16);
        public static readonly Inst ARRAY_U16 = new Inst(nameof(ARRAY_U16), 0x49, I_u16, I_u16);
        public static readonly Inst ARRAY_U16_LOAD = new Inst(nameof(ARRAY_U16_LOAD), 0x4A, I_u16, I_u16);
        public static readonly Inst ARRAY_U16_STORE = new Inst(nameof(ARRAY_U16_STORE), 0x4B, I_u16, I_u16);
        public static readonly Inst LOCAL_U16 = new Inst(nameof(LOCAL_U16), 0x4C, I_u16, I_u16);
        public static readonly Inst LOCAL_U16_LOAD = new Inst(nameof(LOCAL_U16_LOAD), 0x4D, I_u16, I_u16);
        public static readonly Inst LOCAL_U16_STORE = new Inst(nameof(LOCAL_U16_STORE), 0x4E, I_u16, I_u16);
        public static readonly Inst STATIC_U16 = new Inst(nameof(STATIC_U16), 0x4F, I_u16, I_u16);
        public static readonly Inst STATIC_U16_LOAD = new Inst(nameof(STATIC_U16_LOAD), 0x50, I_u16, I_u16);
        public static readonly Inst STATIC_U16_STORE = new Inst(nameof(STATIC_U16_STORE), 0x51, I_u16, I_u16);
        public static readonly Inst GLOBAL_U16 = new Inst(nameof(GLOBAL_U16), 0x52, I_u16, I_u16);
        public static readonly Inst GLOBAL_U16_LOAD = new Inst(nameof(GLOBAL_U16_LOAD), 0x53, I_u16, I_u16);
        public static readonly Inst GLOBAL_U16_STORE = new Inst(nameof(GLOBAL_U16_STORE), 0x54, I_u16, I_u16);
        public static readonly Inst J = new Inst(nameof(J), 0x55, I_relLabel, I_relLabel);
        public static readonly Inst JZ = new Inst(nameof(JZ), 0x56, I_relLabel, I_relLabel);
        public static readonly Inst IEQ_JZ = new Inst(nameof(IEQ_JZ), 0x57, I_relLabel, I_relLabel);
        public static readonly Inst INE_JZ = new Inst(nameof(INE_JZ), 0x58, I_relLabel, I_relLabel);
        public static readonly Inst IGT_JZ = new Inst(nameof(IGT_JZ), 0x59, I_relLabel, I_relLabel);
        public static readonly Inst IGE_JZ = new Inst(nameof(IGE_JZ), 0x5A, I_relLabel, I_relLabel);
        public static readonly Inst ILT_JZ = new Inst(nameof(ILT_JZ), 0x5B, I_relLabel, I_relLabel);
        public static readonly Inst ILE_JZ = new Inst(nameof(ILE_JZ), 0x5C, I_relLabel, I_relLabel);
        public static readonly Inst CALL = new Inst(nameof(CALL), 0x5D, I_absLabel, I_absLabel);
        public static readonly Inst GLOBAL_U24 = new Inst(nameof(GLOBAL_U24), 0x5E, I_u24, I_u24);
        public static readonly Inst GLOBAL_U24_LOAD = new Inst(nameof(GLOBAL_U24_LOAD), 0x5F, I_u24, I_u24);
        public static readonly Inst GLOBAL_U24_STORE = new Inst(nameof(GLOBAL_U24_STORE), 0x60, I_u24, I_u24);
        public static readonly Inst PUSH_CONST_U24 = new Inst(nameof(PUSH_CONST_U24), 0x61, I_u24, I_u24);
        public static readonly Inst SWITCH = new Inst(nameof(SWITCH), 0x62, I_switch, I_switch);
        public static readonly Inst STRING = new Inst(nameof(STRING), 0x63, I, I);
        public static readonly Inst STRINGHASH = new Inst(nameof(STRINGHASH), 0x64, I, I);
        public static readonly Inst TEXT_LABEL_ASSIGN_STRING = new Inst(nameof(TEXT_LABEL_ASSIGN_STRING), 0x65, I_b, I_b);
        public static readonly Inst TEXT_LABEL_ASSIGN_INT = new Inst(nameof(TEXT_LABEL_ASSIGN_INT), 0x66, I_b, I_b);
        public static readonly Inst TEXT_LABEL_APPEND_STRING = new Inst(nameof(TEXT_LABEL_APPEND_STRING), 0x67, I_b, I_b);
        public static readonly Inst TEXT_LABEL_APPEND_INT = new Inst(nameof(TEXT_LABEL_APPEND_INT), 0x68, I_b, I_b);
        public static readonly Inst TEXT_LABEL_COPY = new Inst(nameof(TEXT_LABEL_COPY), 0x69, I, I);
        public static readonly Inst CATCH = new Inst(nameof(CATCH), 0x6A, I, I);
        public static readonly Inst THROW = new Inst(nameof(THROW), 0x6B, I, I);
        public static readonly Inst CALLINDIRECT = new Inst(nameof(CALLINDIRECT), 0x6C, I, I);
        public static readonly Inst PUSH_CONST_M1 = new Inst(nameof(PUSH_CONST_M1), 0x6D, I, I);
        public static readonly Inst PUSH_CONST_0 = new Inst(nameof(PUSH_CONST_0), 0x6E, I, I);
        public static readonly Inst PUSH_CONST_1 = new Inst(nameof(PUSH_CONST_1), 0x6F, I, I);
        public static readonly Inst PUSH_CONST_2 = new Inst(nameof(PUSH_CONST_2), 0x70, I, I);
        public static readonly Inst PUSH_CONST_3 = new Inst(nameof(PUSH_CONST_3), 0x71, I, I);
        public static readonly Inst PUSH_CONST_4 = new Inst(nameof(PUSH_CONST_4), 0x72, I, I);
        public static readonly Inst PUSH_CONST_5 = new Inst(nameof(PUSH_CONST_5), 0x73, I, I);
        public static readonly Inst PUSH_CONST_6 = new Inst(nameof(PUSH_CONST_6), 0x74, I, I);
        public static readonly Inst PUSH_CONST_7 = new Inst(nameof(PUSH_CONST_7), 0x75, I, I);
        public static readonly Inst PUSH_CONST_FM1 = new Inst(nameof(PUSH_CONST_FM1), 0x76, I, I);
        public static readonly Inst PUSH_CONST_F0 = new Inst(nameof(PUSH_CONST_F0), 0x77, I, I);
        public static readonly Inst PUSH_CONST_F1 = new Inst(nameof(PUSH_CONST_F1), 0x78, I, I);
        public static readonly Inst PUSH_CONST_F2 = new Inst(nameof(PUSH_CONST_F2), 0x79, I, I);
        public static readonly Inst PUSH_CONST_F3 = new Inst(nameof(PUSH_CONST_F3), 0x7A, I, I);
        public static readonly Inst PUSH_CONST_F4 = new Inst(nameof(PUSH_CONST_F4), 0x7B, I, I);
        public static readonly Inst PUSH_CONST_F5 = new Inst(nameof(PUSH_CONST_F5), 0x7C, I, I);
        public static readonly Inst PUSH_CONST_F6 = new Inst(nameof(PUSH_CONST_F6), 0x7D, I, I);
        public static readonly Inst PUSH_CONST_F7 = new Inst(nameof(PUSH_CONST_F7), 0x7E, I, I);

        private static ReadOnlySpan<Operand> I(in Inst i, ref Tokens t, Span<Operand> o) => ReadOnlySpan<Operand>.Empty;

        private static void I(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 0);

            c.U8(i.Opcode);
        }

        private static ReadOnlySpan<Operand> I_b(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextByte(i, ref t, 0));
            return o[0..1];
        }

        private static void I_b(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.U8);

            c.U8(i.Opcode);
            c.U8(o[0].U8);
        }

        private static ReadOnlySpan<Operand> I_b_b(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextByte(i, ref t, 1));
            o[1] = new Operand(NextByte(i, ref t, 2));
            return o[0..2];
        }

        private static void I_b_b(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 2 && o[0].Type == OperandType.U8 && o[1].Type == OperandType.U8);

            c.U8(i.Opcode);
            c.U8(o[0].U8);
            c.U8(o[1].U8);
        }

        private static ReadOnlySpan<Operand> I_b_b_b(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextByte(i, ref t, 1));
            o[1] = new Operand(NextByte(i, ref t, 2));
            o[2] = new Operand(NextByte(i, ref t, 3));
            return o[0..3];
        }

        private static void I_b_b_b(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 3 && o[0].Type == OperandType.U8 && o[1].Type == OperandType.U8 && o[2].Type == OperandType.U8);

            c.U8(i.Opcode);
            c.U8(o[0].U8);
            c.U8(o[1].U8);
            c.U8(o[2].U8);
        }

        private static ReadOnlySpan<Operand> I_u16(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextUShort(i, ref t, 0));
            return o[0..1];
        }

        private static void I_u16(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.U16);

            c.U8(i.Opcode);
            c.U16(o[0].U16);
        }

        private static ReadOnlySpan<Operand> I_s16(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextShort(i, ref t, 0));
            return o[0..1];
        }

        private static void I_s16(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.S16);

            c.U8(i.Opcode);
            c.S16(o[0].S16);
        }

        private static ReadOnlySpan<Operand> I_u24(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextUInt24(i, ref t, 0), OperandType.U24);
            return o[0..1];
        }

        private static void I_u24(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.U24);

            c.U8(i.Opcode);
            c.U24(o[0].U24);
        }

        private static ReadOnlySpan<Operand> I_u32(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextUInt(i, ref t, 0));
            return o[0..1];
        }

        private static void I_u32(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.U32);

            c.U8(i.Opcode);
            c.U32(o[0].U32);
        }

        private static ReadOnlySpan<Operand> I_f(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextFloat(i, ref t, 0));
            return o[0..1];
        }

        private static void I_f(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.F32);

            c.U8(i.Opcode);
            c.F32(o[0].F32);
        }

        private static ReadOnlySpan<Operand> I_native(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextByte(i, ref t, 1));
            o[1] = new Operand(NextByte(i, ref t, 2));
            o[2] = new Operand(NextUShort(i, ref t, 3));
            return o[0..3];
        }

        private static void I_native(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 3 && o[0].Type == OperandType.U8 && o[1].Type == OperandType.U8 && o[2].Type == OperandType.U16);

            const byte ArgCountMask = 0x3F;
            const byte ReturnValueCountMask = 0x3;

            byte op1 = o[0].U8;
            byte op2 = o[1].U8;
            ushort op3 = o[2].U16;

            if (op1 != (op1 & ArgCountMask))
            {
                throw new ArgumentException($"Operand 1 (argument count) of {i.Mnemonic} instruction exceeds maximum value {ArgCountMask}");
            }

            if (op2 != (op2 & ReturnValueCountMask))
            {
                throw new ArgumentException($"Operand 2 (return value count) of {i.Mnemonic} instruction exceeds maximum value {ReturnValueCountMask}");
            }

            c.U8(i.Opcode);
            c.U8((byte)((op1 & ArgCountMask) << 2 | (op2 & ReturnValueCountMask)));
            c.U8((byte)(op3 >> 8));
            c.U8((byte)(op3 & 0xFF));
        }

        private static ReadOnlySpan<Operand> I_enter(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextByte(i, ref t, 1));
            o[1] = new Operand(NextUShort(i, ref t, 2));
            return o[0..2];
        }

        private static void I_enter(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 2 && o[0].Type == OperandType.U8 && o[1].Type == OperandType.U16);

            c.U8(i.Opcode);
            c.U8(o[0].U8);
            c.U16(o[1].U16);
            if (c.Options.IncludeFunctionNames && !string.IsNullOrWhiteSpace(c.Label))
            {
                // if there is label, write it as the function name
                int length = Math.Min(c.Label.Length, 254);
                c.U8((byte)(length + 1));
                for (int j = 0; j < length; j++)
                {
                    c.U8((byte)c.Label[j]);
                }
                c.U8(0); // null terminator
            }
            else
            {
                c.U8(0);
            }
        }

        private static ReadOnlySpan<Operand> I_relLabel(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextTargetLabel(i, ref t, 0), OperandType.Label);
            return o[0..1];
        }

        private static void I_relLabel(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.Label);

            c.U8(i.Opcode);
            c.RelativeTarget(o[0].Label);
        }

        private static ReadOnlySpan<Operand> I_absLabel(in Inst i, ref Tokens t, Span<Operand> o)
        {
            o[0] = new Operand(NextTargetLabel(i, ref t, 0), OperandType.Label);
            return o[0..1];
        }

        private static void I_absLabel(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.Label);

            c.U8(i.Opcode);
            c.Target(o[0].Label);
        }

        private static ReadOnlySpan<Operand> I_switch(in Inst i, ref Tokens t, Span<Operand> o)
        {
            static bool ParseCase(ReadOnlySpan<char> token, out uint value, out string targetLabel)
            {
                const string Separator = ":";

                int sepIndex = token.IndexOf(Separator.AsSpan());
                if (sepIndex != -1)
                {
                    var valueStr = token[0..sepIndex];
                    value = 0;
                    try
                    {
                        value = uint.Parse(valueStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Switch case value '{valueStr.ToString()}' is not a valid uint32 value", nameof(token), e);
                    }

                    var labelStr = token[(sepIndex + 1)..];
                    targetLabel = labelStr.ToString();
                    return true;
                }

                value = default;
                targetLabel = default;
                return false;
            }


            const int MaxCases = MaxOperands;

            int numCases = 0;
            while (t.MoveNext())
            {
                if (!ParseCase(t.Current, out var value, out var targetLabel))
                {
                    throw new ArgumentException($"Invalid switch case syntax '{t.Current.ToString()}'");
                }

                if (numCases >= MaxCases)
                {
                    throw new ArgumentException("Too many switch cases");
                }

                o[numCases] = new Operand((value, targetLabel));
                numCases++;
            }

            return o[0..numCases];
        }

        private static void I_switch(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            c.U8(i.Opcode);
            c.U8((byte)o.Length);
            for (int k = 0; k < o.Length; k++)
            {
                Debug.Assert(o[k].Type == OperandType.SwitchCase);

                c.U32(o[k].SwitchCase.Value);
                c.RelativeTarget(o[k].SwitchCase.Label);
            }
        }

        private static byte NextByte(in Inst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s => byte.Parse(s));
        private static ushort NextUShort(in Inst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s => ushort.Parse(s));
        private static short NextShort(in Inst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s => short.Parse(s));
        private static uint NextUInt24(in Inst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s =>
        {
            uint v = uint.Parse(s);
            if (v > 0xFFFFFF)
            {
                throw new OverflowException($"Value is greater than uint24 maximum value (0xFFFFFF)");
            }

            return v;
        });
        private static uint NextUInt(in Inst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s => uint.Parse(s));
        private static float NextFloat(in Inst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s => float.Parse(s));
        private static string NextTargetLabel(in Inst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s => 
        {
            if (!Token.IsTargetLabel(s, out var lbl))
            {
                throw new FormatException("Not a target label");
            }

            return lbl.ToString();
        }, "label");

        private delegate T ParseValue<T>(ReadOnlySpan<char> str);
        private static T NextValue<T>(in Inst i, ref Tokens t, int operand, ParseValue<T> parse, string typeName = null)
        {
            static string OpToStr(int operand) => operand == 0 ? "" : operand.ToString();

            var opStr = t.MoveNext() ? t.Current : throw new ArgumentException($"{i.Mnemonic} instruction is missing operand {OpToStr(operand)}");
            T op;
            try
            {
                op = parse(opStr);
            }
            catch (Exception e) when (e is FormatException || e is OverflowException)
            {
                throw new ArgumentException($"Operand{OpToStr(operand) + " "}of {i.Mnemonic} instruction is not a valid {typeName ?? typeof(T).Name}", e);
            }

            return op;
        }
    }
}
