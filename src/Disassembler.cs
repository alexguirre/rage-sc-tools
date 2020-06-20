namespace ScTools
{
    using System;
    using System.IO;
    using System.Text;
    using System.Runtime.CompilerServices;
    using ScTools.GameFiles;

    internal class Disassembler
    {
        public Script Script { get; }
        public bool Raw { get; set; } = true;
        public bool ShowOffsets { get; set; } = true;
        public bool ShowBytes { get; set; } = true;

        public Disassembler(Script sc)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
        }

        public void Disassemble(TextWriter writer) => DisassembleRaw(writer);

        private void DisassembleRaw(TextWriter writer)
        {
            StringBuilder lineSB = new StringBuilder();

            for (uint ip = 0; ip < Script.CodeLength;)
            {
                lineSB.Clear();

                uint size = GetSizeOfInstructionAt(Script, ip);

                // write offset
                if (ShowOffsets)
                {
                    lineSB.Append(ip.ToString("000000"));
                    lineSB.Append(" : ");
                }

                // write bytes
                if (ShowBytes)
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
                DisassembleInstructionAt(lineSB, Script, ip);

                lineSB.AppendLine();

                writer.Write(lineSB);

                ip += size;
            }
        }

        private static void DisassembleInstructionAt(StringBuilder sb, Script sc, uint ip)
        {
            byte inst = sc.IP(ip);
            if (inst >= InstructionNames.Length)
            {
                return;
            }

            sb.Append(InstructionNames[inst]);

            ip++;
            foreach (char f in InstructionFormats[inst])
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

        private static uint GetSizeOfInstructionAt(Script sc, uint ip)
        {
            byte inst = sc.IP(ip);
            uint s = inst < InstructionCount ? InstructionSizes[inst] : 0u;
            if (s == 0)
            {
                s = inst switch
                {
                    0x2D => (uint)sc.IP(ip + 4) + 5, // ENTER
                    0x62 => 6 * (uint)sc.IP(ip + 1) + 2, // SWITCH
                    _ => throw new InvalidOperationException($"Unknown instruction 0x{inst:X}"),
                };
            }

            return s;
        }

        private const int InstructionCount = 127;

        private static readonly byte[] InstructionSizes = new byte[InstructionCount]
        {
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,2,3,4,5,5,1,1,4,0,3,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,
            2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,4,4,4,
            4,4,0,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        };

        private static readonly string[] InstructionNames = new string[InstructionCount]
        {
            "NOP",
            "IADD",
            "ISUB",
            "IMUL",
            "IDIV",
            "IMOD",
            "INOT",
            "INEG",
            "IEQ",
            "INE",
            "IGT",
            "IGE",
            "ILT",
            "ILE",
            "FADD",
            "FSUB",
            "FMUL",
            "FDIV",
            "FMOD",
            "FNEG",
            "FEQ",
            "FNE",
            "FGT",
            "FGE",
            "FLT",
            "FLE",
            "VADD",
            "VSUB",
            "VMUL",
            "VDIV",
            "VNEG",
            "IAND",
            "IOR",
            "IXOR",
            "I2F",
            "F2I",
            "F2V",
            "PUSH_CONST_U8",
            "PUSH_CONST_U8_U8",
            "PUSH_CONST_U8_U8_U8",
            "PUSH_CONST_U32",
            "PUSH_CONST_F",
            "DUP",
            "DROP",
            "NATIVE",
            "ENTER",
            "LEAVE",
            "LOAD",
            "STORE",
            "STORE_REV",
            "LOAD_N",
            "STORE_N",
            "ARRAY_U8",
            "ARRAY_U8_LOAD",
            "ARRAY_U8_STORE",
            "LOCAL_U8",
            "LOCAL_U8_LOAD",
            "LOCAL_U8_STORE",
            "STATIC_U8",
            "STATIC_U8_LOAD",
            "STATIC_U8_STORE",
            "IADD_U8",
            "IMUL_U8",
            "IOFFSET",
            "IOFFSET_U8",
            "IOFFSET_U8_LOAD",
            "IOFFSET_U8_STORE",
            "PUSH_CONST_S16",
            "IADD_S16",
            "IMUL_S16",
            "IOFFSET_S16",
            "IOFFSET_S16_LOAD",
            "IOFFSET_S16_STORE",
            "ARRAY_U16",
            "ARRAY_U16_LOAD",
            "ARRAY_U16_STORE",
            "LOCAL_U16",
            "LOCAL_U16_LOAD",
            "LOCAL_U16_STORE",
            "STATIC_U16",
            "STATIC_U16_LOAD",
            "STATIC_U16_STORE",
            "GLOBAL_U16",
            "GLOBAL_U16_LOAD",
            "GLOBAL_U16_STORE",
            "J",
            "JZ",
            "IEQ_JZ",
            "INE_JZ",
            "IGT_JZ",
            "IGE_JZ",
            "ILT_JZ",
            "ILE_JZ",
            "CALL",
            "GLOBAL_U24",
            "GLOBAL_U24_LOAD",
            "GLOBAL_U24_STORE",
            "PUSH_CONST_U24",
            "SWITCH",
            "STRING",
            "STRINGHASH",
            "TEXT_LABEL_ASSIGN_STRING",
            "TEXT_LABEL_ASSIGN_INT",
            "TEXT_LABEL_APPEND_STRING",
            "TEXT_LABEL_APPEND_INT",
            "TEXT_LABEL_COPY",
            "CATCH",
            "THROW",
            "CALLINDIRECT",
            "PUSH_CONST_M1",
            "PUSH_CONST_0",
            "PUSH_CONST_1",
            "PUSH_CONST_2",
            "PUSH_CONST_3",
            "PUSH_CONST_4",
            "PUSH_CONST_5",
            "PUSH_CONST_6",
            "PUSH_CONST_7",
            "PUSH_CONST_FM1",
            "PUSH_CONST_F0",
            "PUSH_CONST_F1",
            "PUSH_CONST_F2",
            "PUSH_CONST_F3",
            "PUSH_CONST_F4",
            "PUSH_CONST_F5",
            "PUSH_CONST_F6",
            "PUSH_CONST_F7",
        };

        private static readonly string[] InstructionFormats = new string[InstructionCount]
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
