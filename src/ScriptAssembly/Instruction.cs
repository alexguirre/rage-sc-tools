namespace ScTools.ScriptAssembly
{
    using System;
    using System.Diagnostics;
    using ScTools.GameFiles;

    using Inst = Instruction;
    using Code = CodeGen.IByteCodeBuilder;

    public enum OperandType
    {
        U32,
        U64,
        F32,
        Identifier,
        SwitchCase,
        String,
    }

    public readonly struct Operand
    {
        public OperandType Type { get; }
        public uint U32 { get; }
        public ulong U64 { get; }
        public float F32 { get; }
        public string Identifier => String;
        public string String { get; }
        public (uint Value, string Label) SwitchCase { get; }

        private Operand(OperandType type) : this() { Type = type; }
        public Operand(uint u32) : this(OperandType.U32) { U32 = u32; }
        public Operand(ulong u64) : this(OperandType.U64) { U64 = u64; }
        public Operand(float f32) : this(OperandType.F32) { F32 = f32; }
        public Operand(string str, OperandType type) : this(type)
        {
            if (type != OperandType.Identifier && type != OperandType.String)
            {
                throw new ArgumentException("Incorrect OperandType for a string value. Must be Identifier or String", nameof(type));
            }

            String = str;
        }
        public Operand((uint Value, string Label) switchCase) : this(OperandType.SwitchCase) { SwitchCase = switchCase; }

        public byte AsU8()
        {
            if (Type != OperandType.U32)
            {
                throw new InvalidOperationException("Type is not U32");
            }

            if (U32 > byte.MaxValue)
            {
                throw new InvalidOperationException("U32 value exceeds maximum U8 value");
            }

            return (byte)(U32 & 0xFF);
        }

        public ushort AsU16()
        {
            if (Type != OperandType.U32)
            {
                throw new InvalidOperationException("Type is not U32");
            }

            if (U32 > ushort.MaxValue)
            {
                throw new InvalidOperationException("U32 value exceeds maximum U16 value");
            }

            return (ushort)(U32 & 0xFFFF);
        }

        public short AsS16()
        {
            if (Type != OperandType.U32)
            {
                throw new InvalidOperationException("Type is not U32");
            }

            uint v = U32;
            if (v > ushort.MaxValue)
            {
                throw new InvalidOperationException("U32 value exceeds maximum S16 value");
            }

            return unchecked((short)(v & 0xFFFF));
        }

        public uint AsU24()
        {
            if (Type != OperandType.U32)
            {
                throw new InvalidOperationException("Type is not U32");
            }

            if (U32 > 0x00FFFFFF)
            {
                throw new InvalidOperationException("U32 value exceeds maximum U24 value");
            }

            return U32 & 0x00FFFFFF;
        }
    }

    public readonly struct Instruction
    {
        public const int NumberOfInstructions = 127;
        public const int MaxOperands = byte.MaxValue;

        public delegate void CodeAssembler(in Inst inst, ReadOnlySpan<Operand> operands, Code code);
        public delegate void CodeDecoder(in Inst inst, IInstructionDecoder decoder);

        public Opcode Opcode { get; }
        public string Mnemonic { get; }
        public uint MnemonicHash { get; }
        public CodeAssembler Assembler { get; }
        public CodeDecoder Decoder { get; }
        public bool IsJump { get; }
        public bool IsControlFlow { get; }

        public bool IsValid => Mnemonic != null;

        private Instruction(string mnemonic, Opcode opcode, CodeAssembler assembler, CodeDecoder decoder, bool isJump = false, bool isControlFlow = false)
        {
            Debug.Assert((byte)opcode < NumberOfInstructions);

            Opcode = opcode;
            Mnemonic = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
            MnemonicHash = mnemonic.ToHash();
            Assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
            Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            IsJump = isJump;
            IsControlFlow = isJump || isControlFlow;

            Debug.Assert(SetStorage[(byte)opcode].Mnemonic == null); // ensure we haven't repeated an opcode

            SetStorage[(byte)opcode] = this;
        }

        public void Assemble(ReadOnlySpan<Operand> operands, Code code) => Assembler(this, operands, code);
        public void Decode(IInstructionDecoder decoder) => Decoder(this, decoder);

        public static uint SizeOf(Script sc, uint ip)
        {
            byte inst = sc.IP(ip);
            uint s = inst < NumberOfInstructions ? InstructionSizes[inst] : 0u;
            if (s == 0)
            {
                s = inst switch
                {
                    0x2D => (uint)sc.IP(ip + 4) + 5, // ENTER
                    0x62 => 6 * (uint)sc.IP(ip + 1) + 2, // SWITCH
                    _ => throw new InvalidOperationException($"Unknown instruction 0x{inst:X} at IP {ip}"),
                };
            }

            return s;
        }

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

            CheckOperands(!Invalid.IsValid);
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
        public static readonly Inst NOP = new Inst(nameof(NOP), Opcode.NOP, I, D);
        public static readonly Inst IADD = new Inst(nameof(IADD), Opcode.IADD, I, D);
        public static readonly Inst ISUB = new Inst(nameof(ISUB), Opcode.ISUB, I, D);
        public static readonly Inst IMUL = new Inst(nameof(IMUL), Opcode.IMUL, I, D);
        public static readonly Inst IDIV = new Inst(nameof(IDIV), Opcode.IDIV, I, D);
        public static readonly Inst IMOD = new Inst(nameof(IMOD), Opcode.IMOD, I, D);
        public static readonly Inst INOT = new Inst(nameof(INOT), Opcode.INOT, I, D);
        public static readonly Inst INEG = new Inst(nameof(INEG), Opcode.INEG, I, D);
        public static readonly Inst IEQ = new Inst(nameof(IEQ), Opcode.IEQ, I, D);
        public static readonly Inst INE = new Inst(nameof(INE), Opcode.INE, I, D);
        public static readonly Inst IGT = new Inst(nameof(IGT), Opcode.IGT, I, D);
        public static readonly Inst IGE = new Inst(nameof(IGE), Opcode.IGE, I, D);
        public static readonly Inst ILT = new Inst(nameof(ILT), Opcode.ILT, I, D);
        public static readonly Inst ILE = new Inst(nameof(ILE), Opcode.ILE, I, D);
        public static readonly Inst FADD = new Inst(nameof(FADD), Opcode.FADD, I, D);
        public static readonly Inst FSUB = new Inst(nameof(FSUB), Opcode.FSUB, I, D);
        public static readonly Inst FMUL = new Inst(nameof(FMUL), Opcode.FMUL, I, D);
        public static readonly Inst FDIV = new Inst(nameof(FDIV), Opcode.FDIV, I, D);
        public static readonly Inst FMOD = new Inst(nameof(FMOD), Opcode.FMOD, I, D);
        public static readonly Inst FNEG = new Inst(nameof(FNEG), Opcode.FNEG, I, D);
        public static readonly Inst FEQ = new Inst(nameof(FEQ), Opcode.FEQ, I, D);
        public static readonly Inst FNE = new Inst(nameof(FNE), Opcode.FNE, I, D);
        public static readonly Inst FGT = new Inst(nameof(FGT), Opcode.FGT, I, D);
        public static readonly Inst FGE = new Inst(nameof(FGE), Opcode.FGE, I, D);
        public static readonly Inst FLT = new Inst(nameof(FLT), Opcode.FLT, I, D);
        public static readonly Inst FLE = new Inst(nameof(FLE), Opcode.FLE, I, D);
        public static readonly Inst VADD = new Inst(nameof(VADD), Opcode.VADD, I, D);
        public static readonly Inst VSUB = new Inst(nameof(VSUB), Opcode.VSUB, I, D);
        public static readonly Inst VMUL = new Inst(nameof(VMUL), Opcode.VMUL, I, D);
        public static readonly Inst VDIV = new Inst(nameof(VDIV), Opcode.VDIV, I, D);
        public static readonly Inst VNEG = new Inst(nameof(VNEG), Opcode.VNEG, I, D);
        public static readonly Inst IAND = new Inst(nameof(IAND), Opcode.IAND, I, D);
        public static readonly Inst IOR = new Inst(nameof(IOR), Opcode.IOR, I, D);
        public static readonly Inst IXOR = new Inst(nameof(IXOR), Opcode.IXOR, I, D);
        public static readonly Inst I2F = new Inst(nameof(I2F), Opcode.I2F, I, D);
        public static readonly Inst F2I = new Inst(nameof(F2I), Opcode.F2I, I, D);
        public static readonly Inst F2V = new Inst(nameof(F2V), Opcode.F2V, I, D);
        public static readonly Inst PUSH_CONST_U8 = new Inst(nameof(PUSH_CONST_U8), Opcode.PUSH_CONST_U8, I_b, D_b);
        public static readonly Inst PUSH_CONST_U8_U8 = new Inst(nameof(PUSH_CONST_U8_U8), Opcode.PUSH_CONST_U8_U8, I_b_b, D_b_b);
        public static readonly Inst PUSH_CONST_U8_U8_U8 = new Inst(nameof(PUSH_CONST_U8_U8_U8), Opcode.PUSH_CONST_U8_U8_U8, I_b_b_b, D_b_b_b);
        public static readonly Inst PUSH_CONST_U32 = new Inst(nameof(PUSH_CONST_U32), Opcode.PUSH_CONST_U32, I_u32, D_u32);
        public static readonly Inst PUSH_CONST_F = new Inst(nameof(PUSH_CONST_F), Opcode.PUSH_CONST_F, I_f, D_f);
        public static readonly Inst DUP = new Inst(nameof(DUP), Opcode.DUP, I, D);
        public static readonly Inst DROP = new Inst(nameof(DROP), Opcode.DROP, I, D);
        public static readonly Inst NATIVE = new Inst(nameof(NATIVE), Opcode.NATIVE, I_native, D_native);
        public static readonly Inst ENTER = new Inst(nameof(ENTER), Opcode.ENTER, I_enter, D_enter);
        public static readonly Inst LEAVE = new Inst(nameof(LEAVE), Opcode.LEAVE, I_b_b, D_b_b, isControlFlow: true);
        public static readonly Inst LOAD = new Inst(nameof(LOAD), Opcode.LOAD, I, D);
        public static readonly Inst STORE = new Inst(nameof(STORE), Opcode.STORE, I, D);
        public static readonly Inst STORE_REV = new Inst(nameof(STORE_REV), Opcode.STORE_REV, I, D);
        public static readonly Inst LOAD_N = new Inst(nameof(LOAD_N), Opcode.LOAD_N, I, D);
        public static readonly Inst STORE_N = new Inst(nameof(STORE_N), Opcode.STORE_N, I, D);
        public static readonly Inst ARRAY_U8 = new Inst(nameof(ARRAY_U8), Opcode.ARRAY_U8, I_b, D_b);
        public static readonly Inst ARRAY_U8_LOAD = new Inst(nameof(ARRAY_U8_LOAD), Opcode.ARRAY_U8_LOAD, I_b, D_b);
        public static readonly Inst ARRAY_U8_STORE = new Inst(nameof(ARRAY_U8_STORE), Opcode.ARRAY_U8_STORE, I_b, D_b);
        public static readonly Inst LOCAL_U8 = new Inst(nameof(LOCAL_U8), Opcode.LOCAL_U8, I_b, D_b);
        public static readonly Inst LOCAL_U8_LOAD = new Inst(nameof(LOCAL_U8_LOAD), Opcode.LOCAL_U8_LOAD, I_b, D_b);
        public static readonly Inst LOCAL_U8_STORE = new Inst(nameof(LOCAL_U8_STORE), Opcode.LOCAL_U8_STORE, I_b, D_b);
        public static readonly Inst STATIC_U8 = new Inst(nameof(STATIC_U8), Opcode.STATIC_U8, I_b, D_b);
        public static readonly Inst STATIC_U8_LOAD = new Inst(nameof(STATIC_U8_LOAD), Opcode.STATIC_U8_LOAD, I_b, D_b);
        public static readonly Inst STATIC_U8_STORE = new Inst(nameof(STATIC_U8_STORE), Opcode.STATIC_U8_STORE, I_b, D_b);
        public static readonly Inst IADD_U8 = new Inst(nameof(IADD_U8), Opcode.IADD_U8, I_b, D_b);
        public static readonly Inst IMUL_U8 = new Inst(nameof(IMUL_U8), Opcode.IMUL_U8, I_b, D_b);
        public static readonly Inst IOFFSET = new Inst(nameof(IOFFSET), Opcode.IOFFSET, I, D);
        public static readonly Inst IOFFSET_U8 = new Inst(nameof(IOFFSET_U8), Opcode.IOFFSET_U8, I_b, D_b);
        public static readonly Inst IOFFSET_U8_LOAD = new Inst(nameof(IOFFSET_U8_LOAD), Opcode.IOFFSET_U8_LOAD, I_b, D_b);
        public static readonly Inst IOFFSET_U8_STORE = new Inst(nameof(IOFFSET_U8_STORE), Opcode.IOFFSET_U8_STORE, I_b, D_b);
        public static readonly Inst PUSH_CONST_S16 = new Inst(nameof(PUSH_CONST_S16), Opcode.PUSH_CONST_S16, I_s16, D_s16);
        public static readonly Inst IADD_S16 = new Inst(nameof(IADD_S16), Opcode.IADD_S16, I_s16, D_s16);
        public static readonly Inst IMUL_S16 = new Inst(nameof(IMUL_S16), Opcode.IMUL_S16, I_s16, D_s16);
        public static readonly Inst IOFFSET_S16 = new Inst(nameof(IOFFSET_S16), Opcode.IOFFSET_S16, I_s16, D_s16);
        public static readonly Inst IOFFSET_S16_LOAD = new Inst(nameof(IOFFSET_S16_LOAD), Opcode.IOFFSET_S16_LOAD, I_s16, D_s16);
        public static readonly Inst IOFFSET_S16_STORE = new Inst(nameof(IOFFSET_S16_STORE), Opcode.IOFFSET_S16_STORE, I_s16, D_s16);
        public static readonly Inst ARRAY_U16 = new Inst(nameof(ARRAY_U16), Opcode.ARRAY_U16, I_u16, D_u16);
        public static readonly Inst ARRAY_U16_LOAD = new Inst(nameof(ARRAY_U16_LOAD), Opcode.ARRAY_U16_LOAD, I_u16, D_u16);
        public static readonly Inst ARRAY_U16_STORE = new Inst(nameof(ARRAY_U16_STORE), Opcode.ARRAY_U16_STORE, I_u16, D_u16);
        public static readonly Inst LOCAL_U16 = new Inst(nameof(LOCAL_U16), Opcode.LOCAL_U16, I_u16, D_u16);
        public static readonly Inst LOCAL_U16_LOAD = new Inst(nameof(LOCAL_U16_LOAD), Opcode.LOCAL_U16_LOAD, I_u16, D_u16);
        public static readonly Inst LOCAL_U16_STORE = new Inst(nameof(LOCAL_U16_STORE), Opcode.LOCAL_U16_STORE, I_u16, D_u16);
        public static readonly Inst STATIC_U16 = new Inst(nameof(STATIC_U16), Opcode.STATIC_U16, I_u16, D_u16);
        public static readonly Inst STATIC_U16_LOAD = new Inst(nameof(STATIC_U16_LOAD), Opcode.STATIC_U16_LOAD, I_u16, D_u16);
        public static readonly Inst STATIC_U16_STORE = new Inst(nameof(STATIC_U16_STORE), Opcode.STATIC_U16_STORE, I_u16, D_u16);
        public static readonly Inst GLOBAL_U16 = new Inst(nameof(GLOBAL_U16), Opcode.GLOBAL_U16, I_u16, D_u16);
        public static readonly Inst GLOBAL_U16_LOAD = new Inst(nameof(GLOBAL_U16_LOAD), Opcode.GLOBAL_U16_LOAD, I_u16, D_u16);
        public static readonly Inst GLOBAL_U16_STORE = new Inst(nameof(GLOBAL_U16_STORE), Opcode.GLOBAL_U16_STORE, I_u16, D_u16);
        public static readonly Inst J = new Inst(nameof(J), Opcode.J, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst JZ = new Inst(nameof(JZ), Opcode.JZ, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst IEQ_JZ = new Inst(nameof(IEQ_JZ), Opcode.IEQ_JZ, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst INE_JZ = new Inst(nameof(INE_JZ), Opcode.INE_JZ, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst IGT_JZ = new Inst(nameof(IGT_JZ), Opcode.IGT_JZ, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst IGE_JZ = new Inst(nameof(IGE_JZ), Opcode.IGE_JZ, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst ILT_JZ = new Inst(nameof(ILT_JZ), Opcode.ILT_JZ, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst ILE_JZ = new Inst(nameof(ILE_JZ), Opcode.ILE_JZ, I_labelTarget, D_labelTarget, isJump: true);
        public static readonly Inst CALL = new Inst(nameof(CALL), Opcode.CALL, I_functionTarget, D_functionTarget, isControlFlow: true);
        public static readonly Inst GLOBAL_U24 = new Inst(nameof(GLOBAL_U24), Opcode.GLOBAL_U24, I_u24, D_u24);
        public static readonly Inst GLOBAL_U24_LOAD = new Inst(nameof(GLOBAL_U24_LOAD), Opcode.GLOBAL_U24_LOAD, I_u24, D_u24);
        public static readonly Inst GLOBAL_U24_STORE = new Inst(nameof(GLOBAL_U24_STORE), Opcode.GLOBAL_U24_STORE, I_u24, D_u24);
        public static readonly Inst PUSH_CONST_U24 = new Inst(nameof(PUSH_CONST_U24), Opcode.PUSH_CONST_U24, I_u24, D_u24);
        public static readonly Inst SWITCH = new Inst(nameof(SWITCH), Opcode.SWITCH, I_switch, D_switch, isControlFlow: true);
        public static readonly Inst STRING = new Inst(nameof(STRING), Opcode.STRING, I, D);
        public static readonly Inst STRINGHASH = new Inst(nameof(STRINGHASH), Opcode.STRINGHASH, I, D);
        public static readonly Inst TEXT_LABEL_ASSIGN_STRING = new Inst(nameof(TEXT_LABEL_ASSIGN_STRING), Opcode.TEXT_LABEL_ASSIGN_STRING, I_b, D_b);
        public static readonly Inst TEXT_LABEL_ASSIGN_INT = new Inst(nameof(TEXT_LABEL_ASSIGN_INT), Opcode.TEXT_LABEL_ASSIGN_INT, I_b, D_b);
        public static readonly Inst TEXT_LABEL_APPEND_STRING = new Inst(nameof(TEXT_LABEL_APPEND_STRING), Opcode.TEXT_LABEL_APPEND_STRING, I_b, D_b);
        public static readonly Inst TEXT_LABEL_APPEND_INT = new Inst(nameof(TEXT_LABEL_APPEND_INT), Opcode.TEXT_LABEL_APPEND_INT, I_b, D_b);
        public static readonly Inst TEXT_LABEL_COPY = new Inst(nameof(TEXT_LABEL_COPY), Opcode.TEXT_LABEL_COPY, I, D);
        public static readonly Inst CATCH = new Inst(nameof(CATCH), Opcode.CATCH, I, D);
        public static readonly Inst THROW = new Inst(nameof(THROW), Opcode.THROW, I, D, isControlFlow: true);
        public static readonly Inst CALLINDIRECT = new Inst(nameof(CALLINDIRECT), Opcode.CALLINDIRECT, I, D, isControlFlow: true);
        public static readonly Inst PUSH_CONST_M1 = new Inst(nameof(PUSH_CONST_M1), Opcode.PUSH_CONST_M1, I, D);
        public static readonly Inst PUSH_CONST_0 = new Inst(nameof(PUSH_CONST_0), Opcode.PUSH_CONST_0, I, D);
        public static readonly Inst PUSH_CONST_1 = new Inst(nameof(PUSH_CONST_1), Opcode.PUSH_CONST_1, I, D);
        public static readonly Inst PUSH_CONST_2 = new Inst(nameof(PUSH_CONST_2), Opcode.PUSH_CONST_2, I, D);
        public static readonly Inst PUSH_CONST_3 = new Inst(nameof(PUSH_CONST_3), Opcode.PUSH_CONST_3, I, D);
        public static readonly Inst PUSH_CONST_4 = new Inst(nameof(PUSH_CONST_4), Opcode.PUSH_CONST_4, I, D);
        public static readonly Inst PUSH_CONST_5 = new Inst(nameof(PUSH_CONST_5), Opcode.PUSH_CONST_5, I, D);
        public static readonly Inst PUSH_CONST_6 = new Inst(nameof(PUSH_CONST_6), Opcode.PUSH_CONST_6, I, D);
        public static readonly Inst PUSH_CONST_7 = new Inst(nameof(PUSH_CONST_7), Opcode.PUSH_CONST_7, I, D);
        public static readonly Inst PUSH_CONST_FM1 = new Inst(nameof(PUSH_CONST_FM1), Opcode.PUSH_CONST_FM1, I, D);
        public static readonly Inst PUSH_CONST_F0 = new Inst(nameof(PUSH_CONST_F0), Opcode.PUSH_CONST_F0, I, D);
        public static readonly Inst PUSH_CONST_F1 = new Inst(nameof(PUSH_CONST_F1), Opcode.PUSH_CONST_F1, I, D);
        public static readonly Inst PUSH_CONST_F2 = new Inst(nameof(PUSH_CONST_F2), Opcode.PUSH_CONST_F2, I, D);
        public static readonly Inst PUSH_CONST_F3 = new Inst(nameof(PUSH_CONST_F3), Opcode.PUSH_CONST_F3, I, D);
        public static readonly Inst PUSH_CONST_F4 = new Inst(nameof(PUSH_CONST_F4), Opcode.PUSH_CONST_F4, I, D);
        public static readonly Inst PUSH_CONST_F5 = new Inst(nameof(PUSH_CONST_F5), Opcode.PUSH_CONST_F5, I, D);
        public static readonly Inst PUSH_CONST_F6 = new Inst(nameof(PUSH_CONST_F6), Opcode.PUSH_CONST_F6, I, D);
        public static readonly Inst PUSH_CONST_F7 = new Inst(nameof(PUSH_CONST_F7), Opcode.PUSH_CONST_F7, I, D);

        private static void CheckOperands(bool valid)
        {
            if (!valid)
            {
                throw new ArgumentException("Incorrect operands");
            }
        }

        private static void I(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 0);

            c.Opcode(i.Opcode);
        }

        private static void D(in Inst i, IInstructionDecoder d)
        {
        }

        private static void I_b(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.U8(o[0].AsU8());
        }

        private static void D_b(in Inst i, IInstructionDecoder d)
        {
            d.U8(d.Get(1));
        }

        private static void I_b_b(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 2 && o[0].Type == OperandType.U32 && o[1].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.U8(o[0].AsU8());
            c.U8(o[1].AsU8());
        }

        private static void D_b_b(in Inst i, IInstructionDecoder d)
        {
            d.U8(d.Get(1));
            d.U8(d.Get(2));
        }

        private static void I_b_b_b(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 3 && o[0].Type == OperandType.U32 && o[1].Type == OperandType.U32 && o[2].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.U8(o[0].AsU8());
            c.U8(o[1].AsU8());
            c.U8(o[2].AsU8());
        }

        private static void D_b_b_b(in Inst i, IInstructionDecoder d)
        {
            d.U8(d.Get(1));
            d.U8(d.Get(2));
            d.U8(d.Get(3));
        }

        private static void I_u16(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.U16(o[0].AsU16());
        }

        private static void D_u16(in Inst i, IInstructionDecoder d)
        {
            d.U16(d.Get<ushort>(1));
        }

        private static void I_s16(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.S16(o[0].AsS16());
        }

        private static void D_s16(in Inst i, IInstructionDecoder d)
        {
            d.S16(d.Get<short>(1));
        }

        private static void I_u24(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.U24(o[0].AsU24());
        }

        private static void D_u24(in Inst i, IInstructionDecoder d)
        {
            d.U24((d.Get<uint>(0) & 0xFFFFFF00) >> 8);
        }

        private static void I_u32(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.U32(o[0].U32);
        }

        private static void D_u32(in Inst i, IInstructionDecoder d)
        {
            d.U32(d.Get<uint>(1));
        }

        private static void I_f(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.F32);

            c.Opcode(i.Opcode);
            c.F32(o[0].F32);
        }

        private static void D_f(in Inst i, IInstructionDecoder d)
        {
            d.F32(d.Get<float>(1));
        }

        private const byte NativeArgCountMask = 0x3F;
        private const byte NativeReturnValueCountMask = 0x3;

        private static void I_native(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 3 && o[0].Type == OperandType.U32 && o[1].Type == OperandType.U32 && o[2].Type == OperandType.U32);

            byte op1 = o[0].AsU8();
            byte op2 = o[1].AsU8();
            ushort op3 = o[2].AsU16();

            if (op1 != (op1 & NativeArgCountMask))
            {
                throw new ArgumentException($"Operand 1 (argument count) of {i.Mnemonic} instruction exceeds maximum value {NativeArgCountMask}");
            }

            if (op2 != (op2 & NativeReturnValueCountMask))
            {
                throw new ArgumentException($"Operand 2 (return value count) of {i.Mnemonic} instruction exceeds maximum value {NativeReturnValueCountMask}");
            }

            c.Opcode(i.Opcode);
            c.U8((byte)((op1 & NativeArgCountMask) << 2 | (op2 & NativeReturnValueCountMask)));
            c.U8((byte)(op3 >> 8));
            c.U8((byte)(op3 & 0xFF));
        }

        private static void D_native(in Inst i, IInstructionDecoder d)
        {
            byte b1 = d.Get(1);
            byte b2 = d.Get(2);
            byte b3 = d.Get(3);

            byte argCount = (byte)((b1 >> 2) & NativeArgCountMask);
            byte returnCount = (byte)(b1 & NativeReturnValueCountMask);
            ushort nativeIndex = (ushort)(b2 << 8 | b3);

            d.U8(argCount);
            d.U8(returnCount);
            d.U16(nativeIndex);
        }

        private static void I_enter(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 2 && o[0].Type == OperandType.U32 && o[1].Type == OperandType.U32);

            c.Opcode(i.Opcode);
            c.U8(o[0].AsU8());
            c.U16(o[1].AsU16());
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

        private static void D_enter(in Inst i, IInstructionDecoder d)
        {
            d.U8(d.Get(1));
            d.U16(d.Get<ushort>(2));
        }

        private static void I_labelTarget(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.Identifier);

            c.Opcode(i.Opcode);
            c.LabelTarget(o[0].Identifier);
        }

        private static void D_labelTarget(in Inst i, IInstructionDecoder d)
        {
            short offset = d.Get<short>(1);
            d.LabelTarget((uint)(d.IP + 3 + offset));
        }

        private static void I_functionTarget(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            CheckOperands(o.Length == 1 && o[0].Type == OperandType.Identifier);

            c.Opcode(i.Opcode);
            c.FunctionTarget(o[0].Identifier);
        }

        private static void D_functionTarget(in Inst i, IInstructionDecoder d)
        {
            d.FunctionTarget((d.Get<uint>(0) & 0xFFFFFF00) >> 8);
        }

        private static void I_switch(in Inst i, ReadOnlySpan<Operand> o, Code c)
        {
            c.Opcode(i.Opcode);
            c.U8((byte)o.Length);
            for (int k = 0; k < o.Length; k++)
            {
                CheckOperands(o[k].Type == OperandType.SwitchCase);

                c.U32(o[k].SwitchCase.Value);
                c.LabelTarget(o[k].SwitchCase.Label);
            }
        }

        private static void D_switch(in Inst i, IInstructionDecoder d)
        {
            byte caseCount = d.Get(1);
            for (int k = 0, offset = 2; k < caseCount; k++, offset += 6)
            {
                uint v = d.Get<uint>((uint)(offset + 0));
                short rel = d.Get<short>((uint)(offset + 4));

                d.SwitchCase(v, (uint)(d.IP + offset + 6 + rel));
            }
        }

        private static readonly byte[] InstructionSizes = new byte[NumberOfInstructions]
        {
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,2,3,4,5,5,1,1,4,0,3,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,
            2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,4,4,4,
            4,4,0,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        };
    }
}
