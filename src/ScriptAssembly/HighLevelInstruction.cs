namespace ScTools.ScriptAssembly
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using HLInst = HighLevelInstruction;
    using Tokens = TokenEnumerator;
    using Code = CodeGen.IHighLevelCodeBuilder;

    public readonly struct HighLevelInstruction
    {
        public const int NumberOfInstructions = 6;
        public const int MaxOperands = byte.MaxValue;

        public delegate void CodeAssembler(in HLInst inst, ReadOnlySpan<Operand> operands, Code code);

        public int Index { get; }
        public string Mnemonic { get; }
        public uint MnemonicHash { get; }
        public CodeAssembler Assembler { get; }

        public bool IsValid => Mnemonic != null;

        private HighLevelInstruction(string mnemonic, int index, CodeAssembler assembler)
        {
            Debug.Assert(index < NumberOfInstructions);

            Index = index;
            Mnemonic = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
            MnemonicHash = mnemonic.ToHash();
            Assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));

            Debug.Assert(SetStorage[index].Mnemonic == null); // ensure we haven't repeated an opcode

            SetStorage[index] = this;
        }

        public void Assemble(ReadOnlySpan<Operand> operands, Code code) => Assembler(this, operands, code);

        private static readonly HLInst[] SetStorage = new HLInst[NumberOfInstructions];

        public static ReadOnlySpan<HLInst> Set => SetStorage.AsSpan();

        static HighLevelInstruction()
        {
            for (int i = 0; i < NumberOfInstructions; i++)
            {
                Debug.Assert(SetStorage[i].IsValid);
            }

            Debug.Assert(!Invalid.IsValid);
        }

        public static ref readonly HLInst FindByMnemonic(ReadOnlySpan<char> mnemonic) => ref FindByMnemonic(mnemonic.ToHash());
        public static ref readonly HLInst FindByMnemonic(uint mnemonicHash)
        {
            var set = Set;
            for (int i = 0; i < set.Length; i++)
            {
                if (set[i].MnemonicHash == mnemonicHash)
                {
                    return ref set[i];
                }
            }

            return ref Invalid;
        }

        public static readonly HLInst Invalid = default;
        public static readonly HLInst PUSH_STRING = new HLInst(nameof(PUSH_STRING), 0, I_PushString);
        public static readonly HLInst CALL_NATIVE = new HLInst(nameof(CALL_NATIVE), 1, I_CallNative);
        public static readonly HLInst PUSH = new HLInst(nameof(PUSH), 2, I_Push);
        public static readonly HLInst STATIC = new HLInst(nameof(STATIC), 3, I_Static);
        public static readonly HLInst STATIC_LOAD = new HLInst(nameof(STATIC_LOAD), 4, I_Static);
        public static readonly HLInst STATIC_STORE = new HLInst(nameof(STATIC_STORE), 5, I_Static);

        private static void I_NotImplemented(in HLInst i, ReadOnlySpan<Operand> o, Code c) => throw new NotImplementedException();

        private static void I_PushString(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.String);

            string str = o[0].String;
            EmitPushString(str, c);
        }

        private static void I_CallNative(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.Identifier);

            if (c.NativeDB == null)
            {
                throw new InvalidOperationException("A nativeDB is required when using the CALL_NATIVE instruction");
            }

            string nativeName = o[0].Identifier;
            NativeCommand n = c.NativeDB.Natives.FirstOrDefault(n => nativeName.Equals(n.Name));

            if (n == default)
            {
                throw new InvalidOperationException($"Unknown native command '{nativeName}'");
            }

            byte paramCount = n.ParameterCount;
            byte returnValueCount = n.ReturnValueCount;
            ushort idx = c.AddOrGetNative(n.CurrentHash);

            c.Emit(Instruction.NATIVE, new[] { new Operand(paramCount), new Operand(returnValueCount), new Operand(idx) });
        }

        private static void I_Push(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length > 0);

            for (int k = 0; k < o.Length; k++)
            {
                Debug.Assert(o[k].Type == OperandType.U32 ||
                             o[k].Type == OperandType.F32 ||
                             o[k].Type == OperandType.String);

                switch (o[k].Type)
                {
                    case OperandType.U32: EmitPushUInt(o[k].U32, c); break;
                    case OperandType.F32: EmitPushFloat(o[k].F32, c); break;
                    case OperandType.String: EmitPushString(o[k].String, c); break;
                }
            }
        }

        private static void I_Static(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.Identifier);

            uint offset = c.GetStaticOffset(o[0].Identifier);

            Opcode op;
            if (offset <= byte.MaxValue)
            {
                op = i.Index switch
                {
                    int idx when idx == STATIC.Index        => Opcode.STATIC_U8,
                    int idx when idx == STATIC_LOAD.Index   => Opcode.STATIC_U8_LOAD,
                    int idx when idx == STATIC_STORE.Index  => Opcode.STATIC_U8_STORE,
                };
            }
            else
            {
                op = i.Index switch
                {
                    int idx when idx == STATIC.Index        => Opcode.STATIC_U16,
                    int idx when idx == STATIC_LOAD.Index   => Opcode.STATIC_U16_LOAD,
                    int idx when idx == STATIC_STORE.Index  => Opcode.STATIC_U16_STORE,
                };
            }

            c.Emit(Instruction.Set[(byte)op], new[] { new Operand(offset) });
        }

        private static void EmitPushUInt(uint v, Code code)
        {
            var inst = v switch
            {
                0xFFFFFFFF /* -1 */ => (Instruction.PUSH_CONST_M1, Array.Empty<Operand>()),
                0 => (Instruction.PUSH_CONST_0, Array.Empty<Operand>()),
                1 => (Instruction.PUSH_CONST_1, Array.Empty<Operand>()),
                2 => (Instruction.PUSH_CONST_2, Array.Empty<Operand>()),
                3 => (Instruction.PUSH_CONST_3, Array.Empty<Operand>()),
                4 => (Instruction.PUSH_CONST_4, Array.Empty<Operand>()),
                5 => (Instruction.PUSH_CONST_5, Array.Empty<Operand>()),
                6 => (Instruction.PUSH_CONST_6, Array.Empty<Operand>()),
                7 => (Instruction.PUSH_CONST_7, Array.Empty<Operand>()),
                _ when v <= byte.MaxValue => (Instruction.PUSH_CONST_U8, new[] { new Operand(v) }),
                _ when v <= ushort.MaxValue => (Instruction.PUSH_CONST_S16, new[] { new Operand(v) }),
                _ when v <= 0x00FFFFFF => (Instruction.PUSH_CONST_U24, new[] { new Operand(v) }),
                _ => (Instruction.PUSH_CONST_U32, new[] { new Operand(v) }),
            };

            code.Emit(inst.Item1, inst.Item2);
        }

        private static void EmitPushFloat(float v, Code code)
        {
            var inst = v switch
            {
                -1.0f => (Instruction.PUSH_CONST_FM1, Array.Empty<Operand>()),
                0.0f => (Instruction.PUSH_CONST_F0, Array.Empty<Operand>()),
                1.0f => (Instruction.PUSH_CONST_F1, Array.Empty<Operand>()),
                2.0f => (Instruction.PUSH_CONST_F2, Array.Empty<Operand>()),
                3.0f => (Instruction.PUSH_CONST_F3, Array.Empty<Operand>()),
                4.0f => (Instruction.PUSH_CONST_F4, Array.Empty<Operand>()),
                5.0f => (Instruction.PUSH_CONST_F5, Array.Empty<Operand>()),
                6.0f => (Instruction.PUSH_CONST_F6, Array.Empty<Operand>()),
                7.0f => (Instruction.PUSH_CONST_F7, Array.Empty<Operand>()),
                _ => (Instruction.PUSH_CONST_F, new[] { new Operand(v) }),
            };

            code.Emit(inst.Item1, inst.Item2);
        }

        private static void EmitPushString(ReadOnlySpan<char> str, Code code)
        {
            uint strId = code.AddOrGetString(str);
            EmitPushUInt(strId, code);
            code.Emit(Instruction.STRING, ReadOnlySpan<Operand>.Empty);
        }
    }
}
