#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;

    using ScTools.GameFiles;

    public class Disassembler
    {
        public Script Script { get; }

        public Disassembler(Script sc)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
        }

        public void Disassemble(TextWriter w)
        {
            var sc = Script;

            w.WriteLine(".script_name {0}", sc.Name);
            if (sc.Hash != 0)
            {
                w.WriteLine(".script_hash 0x{0:X8}", sc.Hash);
            }

            if (sc.GlobalsLengthAndBlock != 0)
            {
                w.WriteLine(".global_block {0}", sc.GlobalsBlock);
            
                if (sc.GlobalsLength != 0)
                {
                    w.WriteLine(".global");
                    var repeatedValue = 0;
                    var repeatedCount = 0;
                    foreach (var page in sc.GlobalsPages)
                    {
                        for (int i = 0; i < page.Data.Length; i++)
                        {
                            if (page.Data[i].AsUInt64 > uint.MaxValue)
                            {
                                throw new InvalidOperationException();
                            }

                            if (page.Data[i].AsInt32 == repeatedValue)
                            {
                                repeatedCount++;
                            }
                            else
                            {
                                if (repeatedCount == 1)
                                {
                                    w.WriteLine(".int {0}", repeatedValue);
                                }
                                else
                                {
                                    w.WriteLine(".int {0} times ({1})", repeatedCount, repeatedValue);
                                }

                                repeatedValue = page.Data[i].AsInt32;
                                repeatedCount = 1;
                            }
                        }
                    }
                }
            }

            if (sc.StaticsCount != 0)
            {
                w.WriteLine(".static");
                var repeatedValue = 0;
                var repeatedCount = 0;
                for (int i = 0; i < (sc.StaticsCount - sc.ArgsCount); i++)
                {
                    if (sc.Statics[i].AsUInt64 > uint.MaxValue)
                    {
                        throw new InvalidOperationException();
                    }

                    if (sc.Statics[i].AsInt32 == repeatedValue)
                    {
                        repeatedCount++;
                    }
                    else
                    {
                        if (repeatedCount == 1)
                        {
                            w.WriteLine(".int {0}", repeatedValue);
                        }
                        else
                        {
                            w.WriteLine(".int {0} times ({1})", repeatedCount, repeatedValue);
                        }

                        repeatedValue = sc.Statics[i].AsInt32;
                        repeatedCount = 1;
                    }
                }
            }

            if (sc.ArgsCount != 0)
            {
                w.WriteLine(".arg");
                var repeatedValue = 0;
                var repeatedCount = 0;
                for (int i = (int)(sc.StaticsCount - sc.ArgsCount); i < sc.StaticsCount; i++)
                {
                    if (sc.Statics[i].AsUInt64 > uint.MaxValue)
                    {
                        throw new InvalidOperationException();
                    }

                    if (sc.Statics[i].AsInt32 == repeatedValue)
                    {
                        repeatedCount++;
                    }
                    else
                    {
                        if (repeatedCount == 1)
                        {
                            w.WriteLine(".int {0}", repeatedValue);
                        }
                        else
                        {
                            w.WriteLine(".int {0} times ({1})", repeatedCount, repeatedValue);
                        }

                        repeatedValue = sc.Statics[i].AsInt32;
                        repeatedCount = 1;
                    }
                }
            }

            if (sc.StringsLength != 0)
            {
                w.WriteLine(".string");
                foreach (uint sid in sc.StringIds())
                {
                    w.WriteLine(".str \"{0}\"", sc.String(sid).Escape());
                }
            }

            if (sc.NativesCount != 0)
            {
                w.WriteLine(".include");
                for (int i = 0; i < sc.NativesCount; i++)
                {
                    w.WriteLine(".native 0x{0:X16}", sc.NativeHash(i));
                }
            }

            if (sc.CodeLength != 0)
            {
                w.WriteLine(".code");
                foreach (var page in sc.CodePages)
                {
                    DisassembleCodePage(w, page);
                }
            }
        }

        private void DisassembleCodePage(TextWriter w, ScriptPage<byte> codePage)
        {
            var code = codePage.Data.AsSpan();
            while (!code.IsEmpty)
            {
                DisassembleInstruction(w, ref code);
            }
        }

        private void DisassembleInstruction(TextWriter w, ref Span<byte> code)
        {
            var opcode = (Opcode)code[0];
            code = code[1..];

            w.Write(opcode.ToString());
            if (opcode.GetNumberOfOperands() != 0)
            {
                w.Write(' ');
            }

            switch (opcode)
            {
                case Opcode.PUSH_CONST_U8:
                case Opcode.ARRAY_U8:
                case Opcode.ARRAY_U8_LOAD:
                case Opcode.ARRAY_U8_STORE:
                case Opcode.LOCAL_U8:
                case Opcode.LOCAL_U8_LOAD:
                case Opcode.LOCAL_U8_STORE:
                case Opcode.STATIC_U8:
                case Opcode.STATIC_U8_LOAD:
                case Opcode.STATIC_U8_STORE:
                case Opcode.IADD_U8:
                case Opcode.IMUL_U8:
                case Opcode.IOFFSET_U8:
                case Opcode.IOFFSET_U8_LOAD:
                case Opcode.IOFFSET_U8_STORE:
                case Opcode.TEXT_LABEL_ASSIGN_STRING:
                case Opcode.TEXT_LABEL_ASSIGN_INT:
                case Opcode.TEXT_LABEL_APPEND_STRING:
                case Opcode.TEXT_LABEL_APPEND_INT:
                    w.Write(code[0]);
                    code = code[1..];
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.LEAVE:
                    w.Write(code[0]);
                    w.Write(", ");
                    w.Write(code[1]);
                    code = code[2..];
                    break;
                case Opcode.PUSH_CONST_U8_U8_U8:
                    w.Write(code[0]);
                    w.Write(", ");
                    w.Write(code[1]);
                    w.Write(", ");
                    w.Write(code[2]);
                    code = code[3..];
                    break;
                case Opcode.PUSH_CONST_U32:
                    w.Write(MemoryMarshal.Read<uint>(code));
                    code = code[4..];
                    break;
                case Opcode.PUSH_CONST_F:
                    // TODO: improve floats formatting
                    w.Write(MemoryMarshal.Read<float>(code).ToString("F9", CultureInfo.InvariantCulture));
                    code = code[4..];
                    break;
                case Opcode.NATIVE:
                    var argReturn = code[0];
                    var nativeIndexHi = code[1];
                    var nativeIndexLo = code[2];

                    var argCount = (argReturn >> 2) & 0x3F;
                    var returnCount = argReturn & 0x3;
                    var nativeIndex = (nativeIndexHi << 8) | nativeIndexLo;
                    w.Write(argCount);
                    w.Write(", ");
                    w.Write(returnCount);
                    w.Write(", ");
                    w.Write(nativeIndex);
                    code = code[3..];
                    break;
                case Opcode.ENTER:
                    w.Write(code[0]);
                    w.Write(", ");
                    w.Write(MemoryMarshal.Read<ushort>(code[1..]));
                    var nameLen = code[3];  // TODO: get label name from here
                    code = code[(4 + nameLen)..];
                    break;
                case Opcode.PUSH_CONST_S16:
                case Opcode.IADD_S16:
                case Opcode.IMUL_S16:
                case Opcode.IOFFSET_S16:
                case Opcode.IOFFSET_S16_LOAD:
                case Opcode.IOFFSET_S16_STORE:
                    w.Write(MemoryMarshal.Read<short>(code));
                    code = code[2..];
                    break;
                case Opcode.ARRAY_U16:
                case Opcode.ARRAY_U16_LOAD:
                case Opcode.ARRAY_U16_STORE:
                case Opcode.LOCAL_U16:
                case Opcode.LOCAL_U16_LOAD:
                case Opcode.LOCAL_U16_STORE:
                case Opcode.STATIC_U16:
                case Opcode.STATIC_U16_LOAD:
                case Opcode.STATIC_U16_STORE:
                case Opcode.GLOBAL_U16:
                case Opcode.GLOBAL_U16_LOAD:
                case Opcode.GLOBAL_U16_STORE:
                    w.Write(MemoryMarshal.Read<ushort>(code));
                    code = code[2..];
                    break;
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.IEQ_JZ:
                case Opcode.INE_JZ:
                case Opcode.IGT_JZ:
                case Opcode.IGE_JZ:
                case Opcode.ILT_JZ:
                case Opcode.ILE_JZ:
                    w.Write(MemoryMarshal.Read<short>(code));
                    code = code[2..];
                    break;
                case Opcode.CALL:
                case Opcode.GLOBAL_U24:
                case Opcode.GLOBAL_U24_LOAD:
                case Opcode.GLOBAL_U24_STORE:
                case Opcode.PUSH_CONST_U24:
                    var lo = code[0];
                    var mi = code[1];
                    var hi = code[2];

                    var value = (hi << 16) | (mi << 8) | lo;
                    w.Write(value);
                    code = code[3..];
                    break;
                case Opcode.SWITCH:
                    var caseCount = code[0];
                    code = code[1..];
                    for (int i = 0; i < caseCount; i++, code = code[6..])
                    {
                        var caseValue = MemoryMarshal.Read<uint>(code);
                        var caseJumpTo = MemoryMarshal.Read<short>(code[4..]);

                        if (i != 0)
                        {
                            w.Write(", ");
                        }
                        w.Write("{0}:{1}", caseValue, caseJumpTo);
                    }
                    break;
            }

            w.WriteLine();
        }

        public static void Disassemble(TextWriter output, Script sc)
        {
            var a = new Disassembler(sc);
            a.Disassemble(output);
        }
    }
}
