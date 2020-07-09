namespace ScTools
{
    using System;
    using System.IO;
    using System.Text;
    using System.Diagnostics;
    using System.Collections.Generic;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;

    internal class Disassembler
    {
        public Script Script { get; }

        public Disassembler(Script sc)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
        }

        public void Disassemble(TextWriter writer)
        {
            writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Script sc = Script;

            writer.WriteLine("$NAME {0}", sc.Name);
            writer.WriteLine();

            writer.WriteLine("$STATICS {0}", sc.StaticsCount);
            for (int i = 0; i < sc.StaticsCount; i++)
            {
                ScriptValue v = sc.Statics[i];
                if (v.AsUInt64 != 0)
                {
                    writer.WriteLine("$STATIC_INT_INIT {0} {1}", i, v.AsInt32);

                    Debug.Assert(v.AsUInt32 == v.AsUInt64, "uint64 found");
                }
            }
            writer.WriteLine();

            if (sc.Hash != 0)
            {
                writer.WriteLine("$HASH 0x{0:X8}", sc.Hash);
            }
            if (sc.GlobalsLengthAndBlock != 0 && sc.GlobalsPages != null)
            {
                writer.WriteLine("$GLOBALS {0} {1}", sc.GlobalsBlock, sc.GlobalsLength);

                uint pageIndex = 0;
                foreach (var page in sc.GlobalsPages)
                {
                    uint i = 0;
                    foreach (ScriptValue g in page.Data)
                    {
                        uint globalId = (sc.GlobalsBlock << 18) | (pageIndex << 14) | i;
                        
                        if (g.AsUInt64 != 0)
                        {
                            writer.WriteLine("$GLOBAL_INT_INIT {0} {1}", globalId, g.AsInt32);

                            Debug.Assert(g.AsUInt32 == g.AsUInt64, "uint64 found");
                        }

                        i++;
                    }
                    pageIndex++;
                }
            }
            writer.WriteLine();

            for (int i = 0; i < sc.NativesCount; i++)
            {
                ulong hash = sc.NativeHash(i);
                writer.WriteLine("$NATIVE_DEF {0:X16}", hash);
            }
            writer.WriteLine();

            foreach (uint id in sc.StringIds())
            {
                writer.WriteLine("$STRING \"{0}\" ; offset: {1}", sc.String(id).Escape(), id);
            }
            writer.WriteLine();

            StringBuilder lineSB = new StringBuilder();
            int labelIndex = 0;
            var (labels, labelsDict) = ScanLabels(sc);
            for (uint ip = 0; ip <= sc.CodeLength; ip += ip < sc.CodeLength ? Instruction.SizeOf(sc, ip) : 1)
            {
                lineSB.Clear();

                string label = null;
                if (labelIndex < labels.Count && labels[labelIndex].IP == ip)
                {
                    label = labels[labelIndex].Name;
                    labelIndex++;
                }

                DisassembleInstructionAt(lineSB, sc, ip, label, labelsDict);

                lineSB.AppendLine();

                writer.Write(lineSB);
            }
        }

        /// <summary>
        /// Scans all the labels in the script (instructions referenced by other instructions).
        /// Returns a list of (IP, Name) tuples ordered by IP for sequential lookup and
        /// a dictionary with IP as the key and Name as the value for fast random lookups by IP.
        /// </summary>
        private static (IList<(uint IP, string Name)>, IDictionary<uint, string>) ScanLabels(Script sc)
        {
            HashSet<uint> labeledIPs = new HashSet<uint>();
            List<(uint IP, string)> labels = new List<(uint, string)>();
            Dictionary<uint, string> labelsDict = new Dictionary<uint, string>();

            for (uint ip = 0; ip < sc.CodeLength; ip += Instruction.SizeOf(sc, ip))
            {
                byte inst = sc.IP(ip);
                if (inst < InstructionCount && InstructionFormats[inst].Length > 0)
                {
                    void AddLabel(uint labelIP)
                    {
                        if (labelIP == 0xFFFFFFFF)
                        {
                            return;
                        }

                        if (labelIP >= sc.CodeLength)
                        {
                            Console.WriteLine($"[WARNING] Found jump to IP outside code bounds ({labelIP}) at IP ({ip})");
                        }

                        if (labeledIPs.Add(labelIP))
                        {
                            string name = null;
                            byte labelInst = labelIP < sc.CodeLength ? sc.IP(labelIP) : (byte)0; // some jumps may point to ip=codeLength (traps?)
                            if (labelInst == 0x2D)
                            {
                                byte nameLen = sc.IP(labelIP + 4);

                                if (nameLen > 0)
                                {
                                    StringBuilder nameSB = new StringBuilder(nameLen);
                                    for (uint i = 0; i < nameLen - 1; i++)
                                    {
                                        nameSB.Append((char)sc.IP(labelIP + 5 + i));
                                    }
                                    name = nameSB.ToString();
                                }
                                else
                                {
                                    name = labelIP switch
                                    {
                                        0 => "main",
                                        _ => labelIP.ToString("func_000000")
                                    };
                                }
                            }
                            else
                            {
                                name = labelIP.ToString("lbl_000000");
                            }

                            labels.Add((labelIP, name));
                            labelsDict.Add(labelIP, name);
                        }
                    }

                    if (InstructionFormats[inst][0] == 'S') // SWITCH
                    {
                        byte count = sc.IP(ip + 1);

                        if (count > 0)
                        {
                            uint currIP = ip + 8;
                            for (uint i = 0; i < count; i++, currIP += 6)
                            {
                                short offset = sc.IP<short>(currIP - 2);
                                uint targetIP = (uint)(currIP + offset);

                                AddLabel(targetIP);
                            }
                        }
                    }
                    else
                    {
                        uint labelIP = InstructionFormats[inst][0] switch
                        {
                            'E' => ip, // ENTER
                            'C' => sc.IP<uint>(ip + 1) & 0xFFFFFF, // CALL
                            'R' => (uint)(sc.IP<short>(ip + 1) + ip + 3), // relative label
                            _ => 0xFFFFFFFF,
                        };
                        AddLabel(labelIP);
                    }
                }
            }

            labels.Sort((a, b) => a.IP.CompareTo(b.IP));
            return (labels, labelsDict);
        }

        private static void DisassembleInstructionAt(StringBuilder sb, Script sc, uint ip, string label, IDictionary<uint, string> allLabels)
        {
            byte inst = ip < sc.CodeLength ? sc.IP(ip) : (byte)0;
            if (inst >= ScriptAssembly.Instruction.NumberOfInstructions)
            {
                return;
            }

            static void appendIndented(StringBuilder sb, int indent, string s)
            {
                const int TabSize = 4;
                sb.Append(' ', TabSize * indent);
                sb.Append(s);
            }

            if (label != null)
            {
                if (inst == 0x2D) // ENTER
                {
                    sb.AppendLine();
                    appendIndented(sb, 0, label);
                }
                else
                {
                    // TODO: write as local label
                    appendIndented(sb, 1, label);
                }
                sb.Append(':');
                sb.AppendLine();
            }

            if (ip >= sc.CodeLength)
            {
                return;
            }

            appendIndented(sb, 2, ScriptAssembly.Instruction.Set[inst].Mnemonic);

            ip++;
            foreach (char f in InstructionFormats[inst])
            {
                switch (f)
                {
                    case 'E': // ENTER
                        {
                            byte op1 = sc.IP(ip + 0);
                            ushort op2 = sc.IP<ushort>(ip + 1);
                            byte nameLen = sc.IP(ip + 3);

                            sb.Append(' ');
                            sb.Append(op1);
                            sb.Append(' ');
                            sb.Append(op2);

                            ip += (uint)(1 + 2 + 1 + nameLen);
                        }
                        break;
                    case 'R': // relative label
                        {
                            uint targetIP = (uint)(sc.IP<short>(ip) + ip + 2);

                            sb.Append(' ');
                            sb.Append(allLabels[targetIP]);
                        }
                        break;
                    case 'C': // CALL
                        {
                            uint targetIP = sc.IP<uint>(ip) & 0xFFFFFF;

                            sb.Append(' ');
                            sb.Append(allLabels[targetIP]);
                        }
                        break;
                    case 'N': // NATIVE
                        {
                            byte b1 = sc.IP(ip + 0);
                            byte b2 = sc.IP(ip + 1);
                            byte b3 = sc.IP(ip + 2);

                            byte argCount = (byte)((b1 >> 2) & 0x3F);
                            byte returnValueCount = (byte)(b1  & 0x3);
                            ushort nativeIndex = (ushort)((b2 << 8) | b3);

                            sb.Append(' ');
                            sb.Append(argCount);
                            sb.Append(' ');
                            sb.Append(returnValueCount);
                            sb.Append(' ');
                            sb.Append(nativeIndex);
                        }
                        break;
                    case 'S': // SWITCH
                        {
                            byte count = sc.IP(ip);

                            if (count > 0)
                            {
                                uint currIP = ip + 7;
                                for (uint i = 0; i < count; i++, currIP += 6)
                                {
                                    uint caseValue = sc.IP<uint>(currIP - 6);
                                    short offset = sc.IP<short>(currIP - 2);
                                    uint targetIP = (uint)(currIP + offset);

                                    sb.Append(' ');
                                    sb.Append(caseValue);
                                    sb.Append(':');
                                    sb.Append(allLabels[targetIP]);
                                }

                                ip += 1 + 6u * count;
                            }
                            else
                            {
                                ip++;
                            }
                        }
                        break;
                    case 'a': // uint24
                        {
                            uint v = sc.IP<uint>(ip) & 0xFFFFFF;
                            ip += 3;

                            sb.Append(' ');
                            sb.Append(v.ToString());
                        }
                        break;
                    case 'b': // byte
                        {
                            byte v = sc.IP(ip);
                            ip += 1;

                            sb.Append(' ');
                            sb.Append(v);
                        }
                        break;
                    case 'u': // uint32
                        {
                            uint v = sc.IP<uint>(ip);
                            ip += 4;

                            sb.Append(' ');
                            sb.Append(v);
                        }
                        break;
                    case 'f': // float
                        {
                            float v = sc.IP<float>(ip);
                            ip += 4;

                            sb.Append(' ');
                            sb.Append(v);
                        }
                        break;
                    case 'h': // ushort
                        {
                            ushort v = sc.IP<ushort>(ip);
                            ip += 2;

                            sb.Append(' ');
                            sb.Append(v);
                        }
                        break;
                    case 's': // short
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

        // needs to be in sync with Assembler.Instructions
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
            "u",
            "f",
            "",
            "",
            "N", // NATIVE
            "E", // ENTER
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
            "C", // CALL
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
