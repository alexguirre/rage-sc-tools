namespace ScTools.GameFiles.GTA4;

using System;
using System.IO;
using System.Text;
using System.Buffers.Binary;
using ScTools.ScriptAssembly;
using ScriptAssembly.Targets.GTA4;

internal static class Dumper
{
    public static void Dump(Script sc, TextWriter w,  DumpOptions options)
    {
        sc = sc ?? throw new ArgumentNullException(nameof(sc));

        if (options.IncludeMetadata)
        {
            w.WriteLine("Magic = 0x{0:X8}", sc.Magic);
            w.WriteLine("Args Count = {0}", sc.ArgsCount);
            w.WriteLine("Statics Count = {0}", sc.StaticsCount);
            if (sc.Statics != null)
            {
                int i = 0;
                foreach (ScriptValue32 v in sc.Statics)
                {
                    w.WriteLine("\t[{0}] = {1:X8} ({2}) ({3})", i++, v.AsInt32, v.AsInt32, v.AsFloat);
                }
            }
            w.WriteLine("Globals Signature = 0x{0:X8}", sc.GlobalsSignature);
            w.WriteLine("Globals Count = {0}", sc.GlobalsCount);
            if (sc.Globals != null)
            {
                int i = 0;
                foreach (ScriptValue32 v in sc.Globals)
                {
                    w.WriteLine("\t[{0}] = {1:X8} ({2}) ({3})", i++, v.AsInt32, v.AsInt32, v.AsFloat);
                }
            }
            w.WriteLine("Code Length = {0}", sc.CodeLength);
        }

        if (options.IncludeDisassembly)
        {
            Disassemble(sc, w, options);
        }

        if (options.IncludeIR)
        {
            w.WriteLine("IR Disassembly:");
            var ir = ScTools.Decompiler.Script.FromGTA4(sc);
            ScTools.Decompiler.IR.IRPrinter.PrintAll(ir.IR.Head, w, options.IncludeOffsets);
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
                    lineSB.Append(sc.Code![ip + offset].ToString("X2"));
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
        var inst = sc.Code.AsSpan((int)ip, (int)SizeOf(sc, ip));
        var opcode = (Opcode)inst[0];

        if (opcode.IsInvalid())
        {
            sb.Append($"<invalid opcode {opcode:X2}>");
            return;
        }

        sb.Append(opcode.Mnemonic());

        switch (opcode)
        {
            case Opcode.STRING:
                var str = Encoding.UTF8.GetString(inst[2..^1]).Escape();
                sb.Append($" '{str}'");
                break;

            case Opcode.PUSH_CONST_U16:
                var u16 = BinaryPrimitives.ReadUInt16LittleEndian(inst[1..]);
                sb.Append($" {u16} (0x{u16:X})");
                break;

            case Opcode.PUSH_CONST_U32:
                var u32 = BinaryPrimitives.ReadUInt32LittleEndian(inst[1..]);
                sb.Append($" {unchecked((int)u32)} (0x{u32:X})");
                break;

            case Opcode.PUSH_CONST_F:
                var f = BinaryPrimitives.ReadSingleLittleEndian(inst[1..]);
                sb.Append($" {f}");
                break;

            case Opcode.J:
            case Opcode.JZ:
            case Opcode.JNZ:
            case Opcode.CALL:
                var addr = BinaryPrimitives.ReadUInt32LittleEndian(inst[1..]);
                sb.Append($" {addr:000000}");
                break;

            case Opcode.SWITCH:
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    sb.Append($" {c.Value}:{c.JumpAddress:000000}");
                }
                break;

            case Opcode.ENTER:
                sb.Append($" {inst[1]} {BinaryPrimitives.ReadUInt16LittleEndian(inst[2..])}");
                break;

            case Opcode.LEAVE:
                sb.Append($" {inst[1]} {inst[2]}");
                break;

            case Opcode.NATIVE:
                sb.Append($" {inst[1]} {inst[2]} 0x{BinaryPrimitives.ReadUInt32LittleEndian(inst[3..]):X8}");
                break;

            default:
                if (inst.Length > 1)
                {
                    // just print the bytes
                    for (int i = 1; i < inst.Length; i++)
                    {
                        sb.Append($" {inst[i]:X2}");
                    }
                }
                break;
        }
    }

    public static uint SizeOf(Script sc, uint ip)
    {
        Opcode opcode = (Opcode)sc.Code![ip];
        uint s = (uint)opcode.ConstantByteSize();
        if (s == 0)
        {
            s = opcode switch
            {
                Opcode.SWITCH => 8 * (uint)sc.Code![ip + 1] + 2,
                Opcode.STRING => (uint)sc.Code![ip + 1] + 2,
                _ => 1,
            };
        }

        return s;
    }
}
