namespace ScTools.GameFiles.GTA5;

using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using GTA5;
using ScTools.ScriptAssembly;
using ScriptAssembly.Targets.GTA5;

internal interface IDumper
{
    void Dump(Script sc, TextWriter sink, DumpOptions options);
}

internal class Dumper<TOpcode, TOpcodeTraits> : IDumper
    where TOpcode : struct, Enum
    where TOpcodeTraits : IOpcodeTraitsGTA5<TOpcode>
{
    public void Dump(Script sc, TextWriter w, DumpOptions options)
    {
        sc = sc ?? throw new ArgumentNullException(nameof(sc));

        if (options.IncludeMetadata)
        {
            w.WriteLine("Name = {0} (0x{1:X8})", sc.Name, sc.NameHash);
            w.WriteLine("Hash = 0x{0:X8}", sc.GlobalsSignature);
            w.WriteLine("Statics Count = {0}", sc.StaticsCount);
            if (sc.Statics != null)
            {
                int i = 0;
                foreach (ScriptValue64 v in sc.Statics)
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
                        foreach (ScriptValue64 g in page.Data)
                        {
                            uint globalId = sc.GlobalsBlock << 18 | pageIndex << 14 | i;

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

        if (options.IncludeDisassembly)
        {
            Disassemble(sc, w, options);
        }
    }

    private static void Disassemble(Script sc, TextWriter w, DumpOptions options)
    {
        w.WriteLine("Disassembly:");

        var lineSB = new StringBuilder();

        for (uint ip = 0; ip < sc.CodeLength;)
        {
            lineSB.Clear();

            uint size = SizeOf(sc, ip);

            // write offset
            if (options.IncludeOffsets)
            {
                lineSB.Append(ip.ToString("000000"));
                lineSB.Append(" : ");
            }

            // write bytes
            if (options.IncludeBytes)
            {
                for (uint offset = 0; offset < size; offset++)
                {
                    lineSB.Append(sc.IP(ip + offset).ToString("X2"));
                    if (offset < size - 1)
                    {
                        lineSB.Append(' ');
                    }
                }

                lineSB.Append(' ', Math.Max(32 - lineSB.Length, 4));
            }

            // write instruction
            if (options.IncludeInstructions)
            {
                DisassembleInstructionAt(lineSB, sc, ip);
            }

            lineSB.AppendLine();

            w.Write(lineSB);

            ip += size;
        }
    }

    private static void DisassembleInstructionAt(StringBuilder sb, Script sc, uint ip)
    {
        byte inst = sc.IP(ip);
        if (inst >= TOpcodeTraits.NumberOfOpcodes)
        {
            return;
        }

        sb.Append(Unsafe.As<byte, TOpcode>(ref inst).ToString());

        ip++;
        foreach (char f in TOpcodeTraits.DumpOpcodeFormats[inst])
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

    public static uint SizeOf(Script sc, uint ip)
    {
        var opcode = Unsafe.As<byte, TOpcode>(ref sc.IP(ip));
        uint s = (uint)TOpcodeTraits.ConstantByteSize(opcode);
        if (s == 0)
        {
            if (EqualityComparer<TOpcode>.Default.Equals(opcode, TOpcodeTraits.ENTER))
            {
                s = (uint)sc.IP(ip + 4) + 5;
            }
            else if (EqualityComparer<TOpcode>.Default.Equals(opcode, TOpcodeTraits.SWITCH))
            {
                s = 6 * (uint)sc.IP(ip + 1) + 2;
            }
            else
            {
                s = 1;//throw new InvalidOperationException($"Unknown instruction 0x{inst:X} at IP {ip}")
            }
        }

        return s;
    }
}

internal  class DumperV10 : Dumper<OpcodeV10, OpcodeTraitsV10> { }
internal  class DumperV11 : Dumper<OpcodeV11, OpcodeTraitsV11> { }
internal  class DumperV12 : Dumper<OpcodeV12, OpcodeTraitsV12> { }
