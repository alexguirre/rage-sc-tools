namespace ScTools
{
    using System;

    internal partial class Assembler
    {
        private delegate void InstructionBuilder(TokenEnumerator tokens, string label, CodeBuilder builder);

        private readonly struct Instruction
        {
            public string Mnemonic { get; }
            public InstructionBuilder Builder { get; }

            public Instruction(string mnemonic, InstructionBuilder builder)
            {
                Mnemonic = mnemonic;
                Builder = builder;
            }
        }

        private static void NoMoreTokens(TokenEnumerator tokens, string instMnemonic)
        {
            if (tokens.MoveNext())
            {
                throw new AssemblerSyntaxException($"Unknown token '{tokens.Current.ToString()}' after instruction '{instMnemonic}'");
            }
        }

        private static readonly Instruction[] Instructions = new[]
        {
            new Instruction(
                "ILT",
                (t, l, b) =>
                {
                    b.BeginInstruction(l);
                    b.Add(0x0C);
                    b.EndInstruction();
                    NoMoreTokens(t, "ILT");
                }),
            new Instruction(
                "PUSH_CONST_U8",
                (t, l, b) =>
                {
                    var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("PUSH_CONST_U8 instruction is missing operand");
                    byte op = 0;
                    try
                    {
                        op = byte.Parse(opStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand of PUSH_CONST_U8 instruction is not a valid uint8 value", e);
                    }

                    b.BeginInstruction(l);
                    b.Add(0x25);
                    b.Add(op);
                    b.EndInstruction();
                    NoMoreTokens(t, "PUSH_CONST_U8");
                }),
            new Instruction(
                "PUSH_CONST_U32",
                (t, l, b) =>
                {
                    var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("PUSH_CONST_U32 instruction is missing operand");
                    uint op = 0;
                    try
                    {
                        op = uint.Parse(opStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand of PUSH_CONST_U32 instruction is not a valid uint32 value", e);
                    }

                    b.BeginInstruction(l);
                    b.Add(0x28);
                    b.Add(op);
                    b.EndInstruction();
                    NoMoreTokens(t, "PUSH_CONST_U32");
                }),
            new Instruction(
                "ENTER",
                (t, l, b) =>
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
                    b.Add(0); // TODO: write label here
                    b.EndInstruction();
                    NoMoreTokens(t, "ENTER");
                }),
            new Instruction(
                "LEAVE",
                (t, l, b) =>
                {
                    var op1Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("LEAVE instruction is missing operand 1");
                    var op2Str = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("LEAVE instruction is missing operand 2");
                    byte op1 = 0;
                    try
                    {
                        op1 = byte.Parse(op1Str);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand 1 of LEAVE instruction is not a valid uint8 value", e);
                    }
                    byte op2 = 0;
                    try
                    {
                        op2 = byte.Parse(op2Str);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand 2 of LEAVE instruction is not a valid uint8 value", e);
                    }

                    b.BeginInstruction(l);
                    b.Add(0x2E);
                    b.Add(op1);
                    b.Add(op2);
                    b.EndInstruction();
                    NoMoreTokens(t, "LEAVE");
                }),
            new Instruction(
                "LOCAL_U8_LOAD",
                (t, l, b) =>
                {
                    var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("LOCAL_U8_LOAD instruction is missing operand");
                    byte op = 0;
                    try
                    {
                        op = byte.Parse(opStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand of LOCAL_U8_LOAD instruction is not a valid uint8 value", e);
                    }

                    b.BeginInstruction(l);
                    b.Add(0x38);
                    b.Add(op);
                    b.EndInstruction();
                    NoMoreTokens(t, "LOCAL_U8_LOAD");
                }),
            new Instruction(
                "GLOBAL_U16_STORE",
                (t, l, b) =>
                {
                    var opStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException("GLOBAL_U16_STORE instruction is missing operand");
                    ushort op = 0;
                    try
                    {
                        op = ushort.Parse(opStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException("Operand of GLOBAL_U16_STORE instruction is not a valid uint16 value", e);
                    }

                    b.BeginInstruction(l);
                    b.Add(0x54);
                    b.Add(op);
                    b.EndInstruction();
                    NoMoreTokens(t, "GLOBAL_U16_STORE");
                }),
            new Instruction(
                "CALL",
                (t, l, b) =>
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
                    NoMoreTokens(t, "CALL");
                }),
            new Instruction(
                "PUSH_CONST_3",
                (t, l, b) =>
                {
                    b.BeginInstruction(l);
                    b.Add(0x71);
                    b.EndInstruction();
                    NoMoreTokens(t, "PUSH_CONST_3");
                }),
        };
    }
}
