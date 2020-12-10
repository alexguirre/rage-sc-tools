namespace ScTools
{
    using System;
    using System.IO;
    using System.Text;
    using System.Runtime.CompilerServices;
    using ScTools.GameFiles;

    public class Dumper
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

                uint size = SizeOf(Script, ip);

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
                    DisassembleInstructionAt(lineSB, Script, ip);
                }

                lineSB.AppendLine();

                w.Write(lineSB);

                ip += size;
            }
        }

        private static void DisassembleInstructionAt(StringBuilder sb, Script sc, uint ip)
        {
            byte inst = sc.IP(ip);
            if (inst >= NumberOfOpcodes)
            {
                return;
            }

            sb.Append(((Opcode)inst).ToString());

            ip++;
            foreach (char f in OpcodesFormats[inst])
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

        public static uint SizeOf(Opcode opcode) => (int)opcode < NumberOfOpcodes ? OpcodeSizes[(int)opcode] : 0u;

        public static uint SizeOf(Script sc, uint ip)
        {
            byte inst = sc.IP(ip);
            uint s = SizeOf((Opcode)inst);
            if (s == 0)
            {
                s = inst switch
                {
                    0x2D => (uint)sc.IP(ip + 4) + 5, // ENTER
                    0x62 => 6 * (uint)sc.IP(ip + 1) + 2, // SWITCH
                    _ => 1//throw new InvalidOperationException($"Unknown instruction 0x{inst:X} at IP {ip}"),
                };
            }

            return s;
        }

        public const int NumberOfOpcodes = 127;

        public enum Opcode : byte
        {
            NOP = 0x00,
            IADD = 0x01,
            ISUB = 0x02,
            IMUL = 0x03,
            IDIV = 0x04,
            IMOD = 0x05,
            INOT = 0x06,
            INEG = 0x07,
            IEQ = 0x08,
            INE = 0x09,
            IGT = 0x0A,
            IGE = 0x0B,
            ILT = 0x0C,
            ILE = 0x0D,
            FADD = 0x0E,
            FSUB = 0x0F,
            FMUL = 0x10,
            FDIV = 0x11,
            FMOD = 0x12,
            FNEG = 0x13,
            FEQ = 0x14,
            FNE = 0x15,
            FGT = 0x16,
            FGE = 0x17,
            FLT = 0x18,
            FLE = 0x19,
            VADD = 0x1A,
            VSUB = 0x1B,
            VMUL = 0x1C,
            VDIV = 0x1D,
            VNEG = 0x1E,
            IAND = 0x1F,
            IOR = 0x20,
            IXOR = 0x21,
            I2F = 0x22,
            F2I = 0x23,
            F2V = 0x24,
            PUSH_CONST_U8 = 0x25,
            PUSH_CONST_U8_U8 = 0x26,
            PUSH_CONST_U8_U8_U8 = 0x27,
            PUSH_CONST_U32 = 0x28,
            PUSH_CONST_F = 0x29,
            DUP = 0x2A,
            DROP = 0x2B,
            NATIVE = 0x2C,
            ENTER = 0x2D,
            LEAVE = 0x2E,
            LOAD = 0x2F,
            STORE = 0x30,
            STORE_REV = 0x31,
            LOAD_N = 0x32,
            STORE_N = 0x33,
            ARRAY_U8 = 0x34,
            ARRAY_U8_LOAD = 0x35,
            ARRAY_U8_STORE = 0x36,
            LOCAL_U8 = 0x37,
            LOCAL_U8_LOAD = 0x38,
            LOCAL_U8_STORE = 0x39,
            STATIC_U8 = 0x3A,
            STATIC_U8_LOAD = 0x3B,
            STATIC_U8_STORE = 0x3C,
            IADD_U8 = 0x3D,
            IMUL_U8 = 0x3E,
            IOFFSET = 0x3F,
            IOFFSET_U8 = 0x40,
            IOFFSET_U8_LOAD = 0x41,
            IOFFSET_U8_STORE = 0x42,
            PUSH_CONST_S16 = 0x43,
            IADD_S16 = 0x44,
            IMUL_S16 = 0x45,
            IOFFSET_S16 = 0x46,
            IOFFSET_S16_LOAD = 0x47,
            IOFFSET_S16_STORE = 0x48,
            ARRAY_U16 = 0x49,
            ARRAY_U16_LOAD = 0x4A,
            ARRAY_U16_STORE = 0x4B,
            LOCAL_U16 = 0x4C,
            LOCAL_U16_LOAD = 0x4D,
            LOCAL_U16_STORE = 0x4E,
            STATIC_U16 = 0x4F,
            STATIC_U16_LOAD = 0x50,
            STATIC_U16_STORE = 0x51,
            GLOBAL_U16 = 0x52,
            GLOBAL_U16_LOAD = 0x53,
            GLOBAL_U16_STORE = 0x54,
            J = 0x55,
            JZ = 0x56,
            IEQ_JZ = 0x57,
            INE_JZ = 0x58,
            IGT_JZ = 0x59,
            IGE_JZ = 0x5A,
            ILT_JZ = 0x5B,
            ILE_JZ = 0x5C,
            CALL = 0x5D,
            GLOBAL_U24 = 0x5E,
            GLOBAL_U24_LOAD = 0x5F,
            GLOBAL_U24_STORE = 0x60,
            PUSH_CONST_U24 = 0x61,
            SWITCH = 0x62,
            STRING = 0x63,
            STRINGHASH = 0x64,
            TEXT_LABEL_ASSIGN_STRING = 0x65,
            TEXT_LABEL_ASSIGN_INT = 0x66,
            TEXT_LABEL_APPEND_STRING = 0x67,
            TEXT_LABEL_APPEND_INT = 0x68,
            TEXT_LABEL_COPY = 0x69,
            CATCH = 0x6A,
            THROW = 0x6B,
            CALLINDIRECT = 0x6C,
            PUSH_CONST_M1 = 0x6D,
            PUSH_CONST_0 = 0x6E,
            PUSH_CONST_1 = 0x6F,
            PUSH_CONST_2 = 0x70,
            PUSH_CONST_3 = 0x71,
            PUSH_CONST_4 = 0x72,
            PUSH_CONST_5 = 0x73,
            PUSH_CONST_6 = 0x74,
            PUSH_CONST_7 = 0x75,
            PUSH_CONST_FM1 = 0x76,
            PUSH_CONST_F0 = 0x77,
            PUSH_CONST_F1 = 0x78,
            PUSH_CONST_F2 = 0x79,
            PUSH_CONST_F3 = 0x7A,
            PUSH_CONST_F4 = 0x7B,
            PUSH_CONST_F5 = 0x7C,
            PUSH_CONST_F6 = 0x7D,
            PUSH_CONST_F7 = 0x7E,
        }

        private static readonly byte[] OpcodeSizes = new byte[NumberOfOpcodes]
        {
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,2,3,4,5,5,1,1,4,0,3,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,
            2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,4,4,4,
            4,4,0,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        };

        private static readonly string[] OpcodesFormats = new string[NumberOfOpcodes]
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
