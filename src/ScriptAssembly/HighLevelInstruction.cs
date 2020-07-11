namespace ScTools.ScriptAssembly
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using HLInst = HighLevelInstruction;
    using Code = CodeGen.IHighLevelCodeBuilder;

    public readonly struct HighLevelInstruction
    {
        public const int NumberOfInstructions = 5;
        public const int MaxOperands = byte.MaxValue;

        public delegate void CodeAssembler(in HLInst inst, ReadOnlySpan<Operand> operands, Code code);

        public UniqueId Id { get; }
        public string Mnemonic { get; }
        public uint MnemonicHash { get; }
        public CodeAssembler Assembler { get; }

        public bool IsValid => Mnemonic != null;

        private HighLevelInstruction(string mnemonic, UniqueId id, CodeAssembler assembler)
        {
            Debug.Assert((byte)id < NumberOfInstructions);

            Id = id;
            Mnemonic = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
            MnemonicHash = mnemonic.ToHash();
            Assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));

            Debug.Assert(SetStorage[(byte)id].Mnemonic == null); // ensure we haven't repeated an opcode

            SetStorage[(byte)id] = this;
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
        public static readonly HLInst PUSH_CONST = new HLInst(nameof(PUSH_CONST), UniqueId.PUSH_CONST, I_PushConst);
        public static readonly HLInst CALL_NATIVE = new HLInst(nameof(CALL_NATIVE), UniqueId.CALL_NATIVE, I_CallNative);
        public static readonly HLInst STATIC = new HLInst(nameof(STATIC), UniqueId.STATIC, I_Static);
        public static readonly HLInst STATIC_LOAD = new HLInst(nameof(STATIC_LOAD), UniqueId.STATIC_LOAD, I_Static);
        public static readonly HLInst STATIC_STORE = new HLInst(nameof(STATIC_STORE), UniqueId.STATIC_STORE, I_Static);

        public enum UniqueId : byte
        {
            PUSH_CONST = 0,
            CALL_NATIVE,
            STATIC,
            STATIC_LOAD,
            STATIC_STORE,
        }

        private static void I_NotImplemented(in HLInst i, ReadOnlySpan<Operand> o, Code c) => throw new NotImplementedException();

        private static void I_PushConst(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length > 0);

            // TODO: emit PUSH_CONST_U8_U8 and PUSH_CONST_U8_U8_U8 in PUSH_CONST
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

            c.Emit(Opcode.NATIVE, new[] { new Operand(paramCount), new Operand(returnValueCount), new Operand(idx) });
        }

        private static void I_Static(in HLInst i, ReadOnlySpan<Operand> o, Code c)
        {
            Debug.Assert(o.Length == 1 && o[0].Type == OperandType.Identifier);

            uint offset = c.GetStaticOffset(o[0].Identifier);

            Opcode op = (Index: i.Id, offset <= byte.MaxValue) switch
            {
                (UniqueId.STATIC, true) => Opcode.STATIC_U8,
                (UniqueId.STATIC_LOAD, true) => Opcode.STATIC_U8_LOAD,
                (UniqueId.STATIC_STORE, true) => Opcode.STATIC_U8_STORE,
                (UniqueId.STATIC, false) => Opcode.STATIC_U16,
                (UniqueId.STATIC_LOAD, false) => Opcode.STATIC_U16_LOAD,
                (UniqueId.STATIC_STORE, false) => Opcode.STATIC_U16_STORE,
                _ => throw new InvalidOperationException()
            };

            c.Emit(op, new[] { new Operand(offset) });
        }

        private static void EmitPushUInt(uint v, Code code)
        {
            var inst = v switch
            {
                0xFFFFFFFF /* -1 */ => (Opcode.PUSH_CONST_M1, Array.Empty<Operand>()),
                0 => (Opcode.PUSH_CONST_0, Array.Empty<Operand>()),
                1 => (Opcode.PUSH_CONST_1, Array.Empty<Operand>()),
                2 => (Opcode.PUSH_CONST_2, Array.Empty<Operand>()),
                3 => (Opcode.PUSH_CONST_3, Array.Empty<Operand>()),
                4 => (Opcode.PUSH_CONST_4, Array.Empty<Operand>()),
                5 => (Opcode.PUSH_CONST_5, Array.Empty<Operand>()),
                6 => (Opcode.PUSH_CONST_6, Array.Empty<Operand>()),
                7 => (Opcode.PUSH_CONST_7, Array.Empty<Operand>()),
                _ when v <= byte.MaxValue => (Opcode.PUSH_CONST_U8, new[] { new Operand(v) }),
                _ when v <= ushort.MaxValue => (Opcode.PUSH_CONST_S16, new[] { new Operand(v) }),
                _ when v <= 0x00FFFFFF => (Opcode.PUSH_CONST_U24, new[] { new Operand(v) }),
                _ => (Opcode.PUSH_CONST_U32, new[] { new Operand(v) }),
            };

            code.Emit(inst.Item1, inst.Item2);
        }

        private static void EmitPushFloat(float v, Code code)
        {
            var inst = v switch
            {
                -1.0f => (Opcode.PUSH_CONST_FM1, Array.Empty<Operand>()),
                0.0f => (Opcode.PUSH_CONST_F0, Array.Empty<Operand>()),
                1.0f => (Opcode.PUSH_CONST_F1, Array.Empty<Operand>()),
                2.0f => (Opcode.PUSH_CONST_F2, Array.Empty<Operand>()),
                3.0f => (Opcode.PUSH_CONST_F3, Array.Empty<Operand>()),
                4.0f => (Opcode.PUSH_CONST_F4, Array.Empty<Operand>()),
                5.0f => (Opcode.PUSH_CONST_F5, Array.Empty<Operand>()),
                6.0f => (Opcode.PUSH_CONST_F6, Array.Empty<Operand>()),
                7.0f => (Opcode.PUSH_CONST_F7, Array.Empty<Operand>()),
                _ => (Opcode.PUSH_CONST_F, new[] { new Operand(v) }),
            };

            code.Emit(inst.Item1, inst.Item2);
        }

        private static void EmitPushString(ReadOnlySpan<char> str, Code code)
        {
            uint strId = code.AddOrGetString(str);
            EmitPushUInt(strId, code);
            code.Emit(Opcode.STRING, ReadOnlySpan<Operand>.Empty);
        }
    }
}
