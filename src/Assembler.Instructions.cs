namespace ScTools
{
    using System;

    internal partial class Assembler
    {
        private delegate void InstructionBuilder(Inst inst, TokenEnumerator tokens, string label, CodeBuilder builder);

        private readonly struct Inst
        {
            public string Mnemonic { get; }
            public InstructionBuilder Builder { get; }

            public Inst(string mnemonic, InstructionBuilder builder)
            {
                Mnemonic = mnemonic;
                Builder = builder;
            }

            public static void NoMoreTokens(Inst i, TokenEnumerator tokens)
            {
                if (tokens.MoveNext())
                {
                    throw new AssemblerSyntaxException($"Unknown token '{tokens.Current.ToString()}' after instruction '{i.Mnemonic}'");
                }
            }

            public static InstructionBuilder I(byte v)
            => (i, t, l, b) =>
            {
                b.BeginInstruction(l);
                b.Add(v);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_b(byte v)
            => (i, t, l, b) =>
            {
                var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand");
                byte op = 0;
                try
                {
                    op = byte.Parse(opStr);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand of {i.Mnemonic} instruction is not a valid uint8 value", e);
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.Add(op);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_b_b(byte v)
            => (i, t, l, b) =>
            {
                var op1Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand 1");
                var op2Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand 2");
                byte op1 = 0;
                try
                {
                    op1 = byte.Parse(op1Str);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand 1 of {i.Mnemonic} instruction is not a valid uint8 value", e);
                }
                byte op2 = 0;
                try
                {
                    op2 = byte.Parse(op2Str);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand 2 of {i.Mnemonic} instruction is not a valid uint8 value", e);
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.Add(op1);
                b.Add(op2);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_b_b_b(byte v)
            => (i, t, l, b) =>
            {
                var op1Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand 1");
                var op2Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand 2");
                var op3Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand 3");
                byte op1 = 0;
                try
                {
                    op1 = byte.Parse(op1Str);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand 1 of {i.Mnemonic} instruction is not a valid uint8 value", e);
                }
                byte op2 = 0;
                try
                {
                    op2 = byte.Parse(op2Str);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand 2 of {i.Mnemonic} instruction is not a valid uint8 value", e);
                }
                byte op3 = 0;
                try
                {
                    op3 = byte.Parse(op3Str);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand 3 of {i.Mnemonic} instruction is not a valid uint8 value", e);
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.Add(op1);
                b.Add(op2);
                b.Add(op3);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_s16(byte v)
            => (i, t, l, b) =>
            {
                var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand");
                short op = 0;
                try
                {
                    op = short.Parse(opStr);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand of {i.Mnemonic} instruction is not a valid int16 value", e);
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.Add(op);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_u16(byte v)
            => (i, t, l, b) =>
            {
                var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand");
                ushort op = 0;
                try
                {
                    op = ushort.Parse(opStr);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand of {i.Mnemonic} instruction is not a valid uint16 value", e);
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.Add(op);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_u24(byte v)
            => (i, t, l, b) =>
            {
                var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand");
                uint op = 0;
                try
                {
                    op = uint.Parse(opStr);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand of {i.Mnemonic} instruction is not a valid uint24 value", e);
                }

                if (op > 0xFFFFFF)
                {
                    throw new AssemblerSyntaxException($"Operand of {i.Mnemonic} instruction is not a valid uint24 value");
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.AddU24(op);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_u32(byte v)
            => (i, t, l, b) =>
            {
                var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand");
                uint op = 0;
                try
                {
                    op = uint.Parse(opStr);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand of {i.Mnemonic} instruction is not a valid uint32 value", e);
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.Add(op);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };

            public static InstructionBuilder I_f(byte v)
            => (i, t, l, b) =>
            {
                var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"{i.Mnemonic} instruction is missing operand");
                float op = 0.0f;
                try
                {
                    op = float.Parse(opStr);
                }
                catch (Exception e) when (e is FormatException || e is OverflowException)
                {
                    throw new AssemblerSyntaxException($"Operand of {i.Mnemonic} instruction is not a valid float32 value", e);
                }

                b.BeginInstruction(l);
                b.Add(v);
                b.Add(op);
                b.EndInstruction();
                NoMoreTokens(i, t);
            };
        }

        private static readonly Inst[] Instructions = new[]
        {
            new Inst("NOP", Inst.I(0x00)),
            new Inst("IADD", Inst.I(0x01)),
            new Inst("ISUB", Inst.I(0x02)),
            new Inst("IMUL", Inst.I(0x03)),
            new Inst("IDIV", Inst.I(0x04)),
            new Inst("IMOD", Inst.I(0x05)),
            new Inst("INOT", Inst.I(0x06)),
            new Inst("INEG", Inst.I(0x07)),
            new Inst("IEQ", Inst.I(0x08)),
            new Inst("INE", Inst.I(0x09)),
            new Inst("IGT", Inst.I(0x0A)),
            new Inst("IGE", Inst.I(0x0B)),
            new Inst("ILT", Inst.I(0x0C)),
            new Inst("ILE", Inst.I(0x0D)),
            new Inst("FADD", Inst.I(0x0E)),
            new Inst("FSUB", Inst.I(0x0F)),
            new Inst("FMUL", Inst.I(0x10)),
            new Inst("FDIV", Inst.I(0x11)),
            new Inst("FMOD", Inst.I(0x12)),
            new Inst("FNEG", Inst.I(0x13)),
            new Inst("FEQ", Inst.I(0x14)),
            new Inst("FNE", Inst.I(0x15)),
            new Inst("FGT", Inst.I(0x16)),
            new Inst("FGE", Inst.I(0x17)),
            new Inst("FLT", Inst.I(0x18)),
            new Inst("FLE", Inst.I(0x19)),
            new Inst("VADD", Inst.I(0x1A)),
            new Inst("VSUB", Inst.I(0x1B)),
            new Inst("VMUL", Inst.I(0x1C)),
            new Inst("VDIV", Inst.I(0x1D)),
            new Inst("VNEG", Inst.I(0x1E)),
            new Inst("IAND", Inst.I(0x1F)),
            new Inst("IOR", Inst.I(0x20)),
            new Inst("IXOR", Inst.I(0x21)),
            new Inst("I2F", Inst.I(0x22)),
            new Inst("F2I", Inst.I(0x23)),
            new Inst("F2V", Inst.I(0x24)),
            new Inst("PUSH_CONST_U8", Inst.I_b(0x25)),
            new Inst("PUSH_CONST_U8_U8", Inst.I_b_b(0x26)),
            new Inst("PUSH_CONST_U8_U8_U8", Inst.I_b_b_b(0x27)),
            new Inst("PUSH_CONST_U32", Inst.I_u32(0x28)),
            new Inst("PUSH_CONST_F", Inst.I_f(0x29)),
            new Inst("DUP", Inst.I(0x2A)),
            new Inst("DROP", Inst.I(0x2B)),
            //new Inst("NATIVE", ), // TODO: NATIVE instruction
            new Inst(
                "ENTER",
                (i, t, l, b) =>
                {
                    var op1Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("ENTER instruction is missing operand 1");
                    var op2Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("ENTER instruction is missing operand 2");
                    byte op1 = 0;
                    try
                    {
                        op1 = byte.Parse(op1Str);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand 1 of ENTER instruction is not a valid uint8 value", e);
                    }
                    ushort op2 = 0;
                    try
                    {
                        op2 = ushort.Parse(op2Str);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand 2 of ENTER instruction is not a valid uint16 value", e);
                    }

                    b.BeginInstruction(l);
                    b.Add(0x2D);
                    b.Add(op1);
                    b.Add(op2);
                    if (!string.IsNullOrWhiteSpace(l))
                    {
                        // if there is label, write it as the function name
                        // TODO: option to disable writing the function name
                        int length = Math.Min(l.Length, 254);
                        b.Add((byte)(length + 1));
                        for (int j = 0; j < length; j++)
                        {
                            b.Add((byte)l[j]);
                        }
                        b.Add(0); // null terminator
                    }
                    else
                    {
                        b.Add(0);
                    }
                    b.EndInstruction();
                    Inst.NoMoreTokens(i, t);
                }),
            new Inst("LEAVE", Inst.I_b_b(0x2E)),
            new Inst("LOAD", Inst.I(0x2F)),
            new Inst("STORE", Inst.I(0x30)),
            new Inst("STORE", Inst.I(0x31)),
            new Inst("LOAD_N", Inst.I(0x32)),
            new Inst("STORE_N", Inst.I(0x33)),
            new Inst("ARRAY_U8", Inst.I_b(0x34)),
            new Inst("ARRAY_U8_LOAD", Inst.I_b(0x35)),
            new Inst("ARRAY_U8_STORE", Inst.I_b(0x36)),
            new Inst("LOCAL_U8", Inst.I_b(0x37)),
            new Inst("LOCAL_U8_LOAD", Inst.I_b(0x38)),
            new Inst("LOCAL_U8_STORE", Inst.I_b(0x39)),
            new Inst("STATIC_U8", Inst.I_b(0x3A)),
            new Inst("STATIC_U8_LOAD", Inst.I_b(0x3B)),
            new Inst("STATIC_U8_STORE", Inst.I_b(0x3C)),
            new Inst("IADD_U8", Inst.I_b(0x3D)),
            new Inst("IMUL_U8", Inst.I_b(0x3E)),
            new Inst("IOFFSET", Inst.I(0x3F)),
            new Inst("IOFFSET_U8", Inst.I_b(0x40)),
            new Inst("IOFFSET_U8_LOAD", Inst.I_b(0x41)),
            new Inst("IOFFSET_U8_STORE", Inst.I_b(0x42)),
            new Inst("PUSH_CONST_S16", Inst.I_s16(0x43)),
            new Inst("IADD_S16", Inst.I_s16(0x44)),
            new Inst("IMUL_S16", Inst.I_s16(0x45)),
            new Inst("IOFFSET_S16", Inst.I_s16(0x40)),
            new Inst("IOFFSET_S16_LOAD", Inst.I_s16(0x41)),
            new Inst("IOFFSET_S16_STORE", Inst.I_s16(0x42)),
            new Inst("ARRAY_U16", Inst.I_u16(0x49)),
            new Inst("ARRAY_U16_LOAD", Inst.I_u16(0x4A)),
            new Inst("ARRAY_U16_STORE", Inst.I_u16(0x4B)),
            new Inst("LOCAL_U16", Inst.I_u16(0x4C)),
            new Inst("LOCAL_U16_LOAD", Inst.I_u16(0x4D)),
            new Inst("LOCAL_U16_STORE", Inst.I_u16(0x4E)),
            new Inst("STATIC_U16", Inst.I_u16(0x4F)),
            new Inst("STATIC_U16_LOAD", Inst.I_u16(0x50)),
            new Inst("STATIC_U16_STORE", Inst.I_u16(0x51)),
            new Inst("GLOBAL_U16", Inst.I_u16(0x52)),
            new Inst("GLOBAL_U16_LOAD", Inst.I_u16(0x53)),
            new Inst("GLOBAL_U16_STORE", Inst.I_u16(0x54)),
            // ... // TODO: jump instructions
            new Inst(
                "CALL",
                (i, t, l, b) =>
                {
                    const char TargetLabelPrefix = '@';

                    var op1Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("CALL instruction is missing operand 1");
                    if (op1Str[0]  != TargetLabelPrefix)
                    {
                        throw new AssemblerSyntaxException("Operand 1 of CALL instruction is not a valid label");
                    }

                    b.BeginInstruction(l);
                    b.Add(0x5D);
                    b.AddTarget(op1Str.Slice(1).ToString());
                    b.EndInstruction();
                    Inst.NoMoreTokens(i, t);
                }),
            new Inst("GLOBAL_U24", Inst.I_u24(0x5E)),
            new Inst("GLOBAL_U24_LOAD", Inst.I_u24(0x5F)),
            new Inst("GLOBAL_U24_STORE", Inst.I_u24(0x60)),
            new Inst("PUSH_CONST_U24", Inst.I_u24(0x61)),
            //new Inst("SWITCH", ), // TODO: SWITCH instruction
            new Inst("STRING", Inst.I(0x63)),
            new Inst("STRINGHASH", Inst.I(0x64)),
            new Inst("TEXT_LABEL_ASSIGN_STRING", Inst.I_b(0x65)),
            new Inst("TEXT_LABEL_ASSIGN_INT", Inst.I_b(0x66)),
            new Inst("TEXT_LABEL_APPEND_STRING", Inst.I_b(0x67)),
            new Inst("TEXT_LABEL_APPEND_INT", Inst.I_b(0x68)),
            new Inst("TEXT_LABEL_COPY", Inst.I(0x69)),
            new Inst("CATCH", Inst.I(0x6A)),
            new Inst("THROW", Inst.I(0x6B)),
            new Inst("CALLINDIRECT", Inst.I(0x6C)),
            new Inst("PUSH_CONST_M1", Inst.I(0x6D)),
            new Inst("PUSH_CONST_0", Inst.I(0x6E)),
            new Inst("PUSH_CONST_1", Inst.I(0x6F)),
            new Inst("PUSH_CONST_2", Inst.I(0x70)),
            new Inst("PUSH_CONST_3", Inst.I(0x71)),
            new Inst("PUSH_CONST_4", Inst.I(0x72)),
            new Inst("PUSH_CONST_5", Inst.I(0x73)),
            new Inst("PUSH_CONST_6", Inst.I(0x74)),
            new Inst("PUSH_CONST_7", Inst.I(0x75)),
            new Inst("PUSH_CONST_FM1", Inst.I(0x76)),
            new Inst("PUSH_CONST_F0", Inst.I(0x77)),
            new Inst("PUSH_CONST_F1", Inst.I(0x78)),
            new Inst("PUSH_CONST_F2", Inst.I(0x79)),
            new Inst("PUSH_CONST_F3", Inst.I(0x7A)),
            new Inst("PUSH_CONST_F4", Inst.I(0x7B)),
            new Inst("PUSH_CONST_F5", Inst.I(0x7C)),
            new Inst("PUSH_CONST_F6", Inst.I(0x7D)),
            new Inst("PUSH_CONST_F7", Inst.I(0x7E)),
        };
    }
}
