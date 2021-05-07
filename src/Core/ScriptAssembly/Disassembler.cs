#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;

    using ScTools.GameFiles;
    using System.Linq;

    public class Disassembler
    {
        private (string Label, ulong Hash)[] nativesTable = Array.Empty<(string, ulong)>();
        private (string Label, string String)[] stringsTable = Array.Empty<(string, string)>();
        private Dictionary<uint, int> stringIndicesById = new(); // value is index into stringsTable

        public Script Script { get; }
        public NativeDB? NativeDB { get; }

        public Disassembler(Script sc, NativeDB? nativeDB = null)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
            NativeDB = nativeDB;
        }

        public void Disassemble(TextWriter w)
        {
            var sc = Script;

            BuildNativesTable();
            BuildStringsTable();

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

                            if (repeatedCount > 0 && page.Data[i].AsInt32 == repeatedValue)
                            {
                                repeatedCount++;
                            }
                            else
                            {
                                if (repeatedCount == 1)
                                {
                                    w.WriteLine(".int {0}", repeatedValue);
                                }
                                else if (repeatedCount > 0)
                                {
                                    w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                                }

                                repeatedValue = page.Data[i].AsInt32;
                                repeatedCount = 1;
                            }
                        }
                    }

                    if (repeatedCount == 1)
                    {
                        w.WriteLine(".int {0}", repeatedValue);
                    }
                    else if (repeatedCount > 0)
                    {
                        w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
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

                    if (repeatedCount > 0 && sc.Statics[i].AsInt32 == repeatedValue)
                    {
                        repeatedCount++;
                    }
                    else
                    {
                        if (repeatedCount == 1)
                        {
                            w.WriteLine(".int {0}", repeatedValue);
                        }
                        else if (repeatedCount > 0)
                        {
                            w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                        }

                        repeatedValue = sc.Statics[i].AsInt32;
                        repeatedCount = 1;
                    }
                }

                if (repeatedCount == 1)
                {
                    w.WriteLine(".int {0}", repeatedValue);
                }
                else if (repeatedCount > 0)
                {
                    w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
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

                    if (repeatedCount > 0 && sc.Statics[i].AsInt32 == repeatedValue)
                    {
                        repeatedCount++;
                    }
                    else
                    {
                        if (repeatedCount == 1)
                        {
                            w.WriteLine(".int {0}", repeatedValue);
                        }
                        else if (repeatedCount > 0)
                        {
                            w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                        }

                        repeatedValue = sc.Statics[i].AsInt32;
                        repeatedCount = 1;
                    }
                }

                if (repeatedCount == 1)
                {
                    w.WriteLine(".int {0}", repeatedValue);
                }
                else if (repeatedCount > 0)
                {
                    w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                }
            }

            if (stringsTable.Length != 0)
            {
                w.WriteLine(".string");
                for (int i = 0; i < stringsTable.Length; i++)
                {
                    w.WriteLine("{0}:\t.str \"{1}\"", stringsTable[i].Label, stringsTable[i].String);
                }
            }

            if (nativesTable.Length != 0)
            {
                w.WriteLine(".include");
                for (int i = 0; i < nativesTable.Length; i++)
                {
                    w.WriteLine("{0}:\t.native 0x{1:X16}", nativesTable[i].Label, nativesTable[i].Hash);
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
                    w.Write(MemoryMarshal.Read<float>(code).ToString("R", CultureInfo.InvariantCulture));
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
                    if (nativeIndex >= 0 && nativeIndex < nativesTable.Length)
                    {
                        w.Write(nativesTable[nativeIndex].Label);
                    }
                    else
                    {
                        w.Write(nativeIndex);
                    }
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

        private void BuildNativesTable()
        {
            var sc = Script;

            nativesTable = new (string, ulong)[sc.NativesCount];
            for (int i = 0; i < sc.NativesCount; i++)
            {
                var hash = sc.NativeHash(i);
                var origHash = NativeDB?.FindOriginalHash(hash) ?? hash;
                var label = NativeDB?.GetDefinition(origHash)?.Name ?? $"_0x{origHash:X16}";

                nativesTable[i] = (label, origHash);
            }
        }

        private void BuildStringsTable()
        {
            var sc = Script;
            if (sc.StringsLength != 0)
            {
                var usedLabels = new Dictionary<string, int>();
                var table = new List<(string Label, string String)>();

                int i = 0;
                foreach (uint sid in sc.StringIds())
                {
                    var str = sc.String(sid).Escape();
                    var label = CreateLabelForString(str, usedLabels);
                    table.Add((label, str));
                    i++;
                    stringIndicesById.Add(sid, i);
                }

                stringsTable = table.ToArray();
            }

            static string CreateLabelForString(string s, Dictionary<string, int> usedLabels)
            {
                const string Prefix = "a";
                const int MaxLength = 25;

                var label = string.IsNullOrWhiteSpace(s) ?
                    Prefix + "EmptyString" :
                    Prefix + char.ToUpperInvariant(s[0]) + string.Concat(s.Skip(1).Where(IsIdentifierChar).Take(MaxLength));

                // check if the string label is repeated
                if (usedLabels.TryGetValue(label, out var n))
                {
                    usedLabels[label]++;
                    label += "_" + (n + 1);
                }
                else
                {
                    usedLabels.Add(label, 1);
                }

                return label;
            }

            // char is [a-zA-Z_0-9]
            static bool IsIdentifierChar(char c)
                => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9');
        }

        public static void Disassemble(TextWriter output, Script sc, NativeDB? nativeDB = null)
        {
            var a = new Disassembler(sc, nativeDB);
            a.Disassemble(output);
        }
    }
}
