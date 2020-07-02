namespace ScTools.ScriptAssembly
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using HLInst = HighLevelInstruction;
    using Tokens = TokenEnumerator;
    using Code = Assembler.IHighLevelCodeBuilder;

    internal readonly struct HighLevelInstruction
    {
        public const int NumberOfInstructions = 3;
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

        private static void I_PushString(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.String);

            string str = o[0].String;
            SinkPushString(str, c);
        }

        private static void I_CallNative(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.Label);

            if (c.NativeDB == null)
            {
                throw new InvalidOperationException("A nativeDB is required when using the CALL_NATIVE instruction");
            }

            string nativeName = o[0].Label;
            NativeCommand n = c.NativeDB.Natives.FirstOrDefault(n => nativeName.Equals(n.Name));

            if (n == default)
            {
                throw new InvalidOperationException($"Unknown native command '{nativeName}'");
            }

            byte paramCount = n.ParameterCount;
            byte returnValueCount = n.ReturnValueCount;
            ushort idx = c.AddOrGetNative(n.CurrentHash);

            c.Sink(Instruction.NATIVE, new[] { new Operand(paramCount), new Operand(returnValueCount), new Operand(idx) });
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
                    case OperandType.U32: SinkPushUInt(o[k].U32, c); break;
                    case OperandType.F32: SinkPushFloat(o[k].F32, c); break;
                    case OperandType.String: SinkPushString(o[k].String, c); break;
                }
            }
        }

        private static void SinkPushUInt(uint v, Code code)
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

            code.Sink(inst.Item1, inst.Item2);
        }

        private static void SinkPushFloat(float v, Code code)
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

            code.Sink(inst.Item1, inst.Item2);
        }

        private static void SinkPushString(ReadOnlySpan<char> str, Code code)
        {
            uint strId = code.AddOrGetString(str);
            SinkPushUInt(strId, code);
            code.Sink(Instruction.STRING, ReadOnlySpan<Operand>.Empty);
        }

        private static string NextString(in HLInst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s =>
        {
            if (!Token.IsString(s, out var lbl))
            {
                throw new FormatException("Not a string");
            }

            return lbl.Unescape();
        }, "string");

        private static string NextNativeName(in HLInst i, ref Tokens t, int operand) => NextValue(i, ref t, operand, s => s.ToString(), "native command");

        private delegate T ParseValue<T>(ReadOnlySpan<char> str);
        private static T NextValue<T>(in HLInst i, ref Tokens t, int operand, ParseValue<T> parse, string typeName = null)
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
