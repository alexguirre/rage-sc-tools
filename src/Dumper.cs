namespace ScTools
{
    using System;
    using System.IO;
    using System.Text;
    using System.Runtime.CompilerServices;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;

    internal class Dumper
    {
        public Script Script { get; }

        public Dumper(Script sc)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
        }

        public void Dump(TextWriter w, bool showMetadata, bool showDisassembly, bool showOffsets, bool showBytes, bool showInstructions)
        {
            w = w ?? throw new ArgumentNullException(nameof(w));

            if (showMetadata)
            {
                Script sc = Script;
                w.WriteLine("Name = {0} (0x{1:X8})", sc.Name, sc.NameHash);
                w.WriteLine("Hash = 0x{0:X8}", sc.Hash);
                w.WriteLine("Statics Count = {0}", sc.StaticsCount);
                if (sc.Statics != null)
                {
                    int i = 0;
                    foreach (ScriptValue v in sc.Statics)
                    {
                        w.WriteLine("\t[{0}] = {1:X16} ({2}) ({3})", i++, v.AsInt64, v.AsInt32, v.AsFloat);
                    }
                }
                {
                    w.WriteLine("Globals Length And Block = {0}", sc.GlobalsLengthAndBlock);
                    w.WriteLine("Globals Length           = {0}", sc.GlobalsLength);
                    w.WriteLine("Globals Block            = {0}", sc.GlobalsBlock);

                    if (sc.GlobalsPages != null)
                    {
                        uint pageIndex = 0;
                        foreach (var page in sc.GlobalsPages)
                        {
                            uint i = 0;
                            foreach (ScriptValue g in page.Data)
                            {
                                uint globalId = (sc.GlobalsBlock << 18) | (pageIndex << 14) | i;

                                w.WriteLine("\t[{0}] = {1:X16} ({2}) ({3})", globalId, g.AsInt64, g.AsInt32, g.AsFloat);

                                i++;
                            }
                            pageIndex++;
                        }
                    }
                }
                w.WriteLine("Natives Count = {0}", sc.NativesCount);
                if (sc.Natives != null)
                {
                    int i = 0;
                    foreach (ulong hash in sc.Natives)
                    {
                        w.WriteLine("\t{0:X16} -> {1:X16}", hash, sc.NativeHash(i));
                        i++;
                    }
                }
                w.WriteLine("Num Refs = {0}", sc.NumRefs);
                w.WriteLine("Strings Length = {0}", sc.StringsLength);
                foreach (uint sid in sc.StringIds())
                {
                    w.WriteLine("\t[{0}] = '{1}'", sid, sc.String(sid).Escape());
                }
                w.WriteLine("Code Length = {0}", sc.CodeLength);
            }

            if (showDisassembly)
            {
                Disassemble(w, showOffsets, showBytes, showInstructions);
            }
        }

        private void Disassemble(TextWriter w, bool showOffsets, bool showBytes, bool showInstructions)
        {
            w = w ?? throw new ArgumentNullException(nameof(w));
            w.WriteLine("Disassembly:");

            StringBuilder lineSB = new StringBuilder();

            for (uint ip = 0; ip < Script.CodeLength;)
            {
                lineSB.Clear();

                uint size = Instruction.SizeOf(Script, ip);

                // write offset
                if (showOffsets)
                {
                    lineSB.Append(ip.ToString("000000"));
                    lineSB.Append(" : ");
                }

                // write bytes
                if (showBytes)
                {
                    for (uint offset = 0; offset < size; offset++)
                    {
                        lineSB.Append(Script.IP(ip + offset).ToString("X2"));
                        if (offset < size - 1)
                        {
                            lineSB.Append(' ');
                        }
                    }

                    lineSB.Append(' ', Math.Max(32 - lineSB.Length, 4));
                }

                // write instruction
                if (showInstructions)
                {
                    DisassembleRawInstructionAt(lineSB, Script, ip);
                }

                lineSB.AppendLine();

                w.Write(lineSB);

                ip += size;
            }
        }

        private static void DisassembleRawInstructionAt(StringBuilder sb, Script sc, uint ip)
        {
            byte inst = sc.IP(ip);
            if (inst >= ScriptAssembly.Instruction.NumberOfInstructions)
            {
                return;
            }

            sb.Append(ScriptAssembly.Instruction.Set[inst].Mnemonic);

            ip++;
            foreach (char f in InstructionRawFormats[inst])
            {
                switch (f)
                {
                    case '$':
                        {
                            byte emptyStr = 0;
                            ref byte str = ref (sc.IP(ip) == 0 ? ref emptyStr : ref sc.IP(ip + 1));
                            str = ref (str == 255 ? ref Unsafe.Add(ref str, 1) : ref str);

                            sb.Append("[ ");

                            const int MaxLength = 42;
                            int n = MaxLength;
                            while (str != 0 && n != 0)
                            {
                                byte c = str;

                                if ((char)c == '\r')
                                {
                                    sb.Append("\\r");
                                }
                                else if ((char)c == '\n')
                                {
                                    sb.Append("\\n");
                                }
                                else
                                {
                                    sb.Append((char)c);
                                }

                                str = ref Unsafe.Add(ref str, 1);
                                n--;
                            }

                            if (str != 0)
                            {
                                sb.Append("...");
                            }

                            sb.Append(']');
                        }
                        break;
                    case 'R':
                        {
                            short v = sc.IP<short>(ip);
                            ip += 2;

                            sb.Append(' ');
                            sb.Append((ip + v).ToString("000000"));
                            sb.Append(' ');
                            sb.Append('(');
                            sb.Append(v.ToString("+#;-#"));
                            sb.Append(')');
                        }
                        break;
                    case 'S':
                        {
                            const byte MaxCases = 32;
                            byte count = sc.IP(ip);

                            sb.Append(' ');
                            sb.Append('[');
                            sb.Append(count);
                            sb.Append(']');

                            if (count > 0)
                            {
                                uint c = Math.Min(MaxCases, count);

                                uint currIP = ip + 7;
                                for (uint i = 0; i < c; i++, currIP += 6)
                                {
                                    uint caseValue = sc.IP<uint>(currIP - 6);
                                    short offset = sc.IP<short>(currIP - 2);

                                    sb.Append(' ');
                                    sb.Append(caseValue);
                                    sb.Append(':');
                                    sb.Append((currIP + offset).ToString("000000"));
                                }

                                ip += 1 + 6u * count;
                            }
                            else
                            {
                                ip++;
                            }

                            if (count > MaxCases)
                            {
                                sb.Append("... ");
                            }
                        }
                        break;
                    case 'a':
                        {
                            int v = sc.IP<int>(ip) & 0xFFFFFF;
                            ip += 3;

                            sb.Append(' ');
                            sb.Append(v.ToString("000000"));
                        }
                        break;
                    case 'b':
                        {
                            byte v = sc.IP(ip);
                            ip += 1;

                            sb.Append(' ');
                            sb.Append(v);
                        }
                        break;
                    case 'd':
                        {
                            int v = sc.IP<int>(ip);
                            ip += 4;

                            sb.Append(' ');
                            sb.Append(v);
                            sb.Append("(0x");
                            sb.Append(unchecked((uint)v).ToString("X"));
                            sb.Append(')');
                        }
                        break;
                    case 'f':
                        {
                            float v = sc.IP<float>(ip);
                            ip += 4;

                            sb.Append(' ');
                            sb.Append(v);
                        }
                        break;
                    case 'h':
                    case 's':
                        {
                            short v = sc.IP<short>(ip);
                            ip += 2;

                            sb.Append(' ');
                            sb.Append(v);
                        }
                        break;
                }
            }
        }

        private const int InstructionCount = ScriptAssembly.Instruction.NumberOfInstructions;

        private static readonly string[] InstructionRawFormats = new string[InstructionCount]
        {
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "b",
            "bb",
            "bbb",
            "d",
            "f",
            "",
            "",
            "bbb",
            "bs$",
            "bb",
            "",
            "",
            "",
            "",
            "",
            "b",
            "b",
            "b",
            "b",
            "b",
            "b",
            "b",
            "b",
            "b",
            "b",
            "b",
            "",
            "b",
            "b",
            "b",
            "s",
            "s",
            "s",
            "s",
            "s",
            "s",
            "h",
            "h",
            "h",
            "h",
            "h",
            "h",
            "h",
            "h",
            "h",
            "h",
            "h",
            "h",
            "R",
            "R",
            "R",
            "R",
            "R",
            "R",
            "R",
            "R",
            "a",
            "a",
            "a",
            "a",
            "a",
            "S",
            "",
            "",
            "b",
            "b",
            "b",
            "b",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
        };
    }
}
