namespace ScTools.GameFiles.MP3;

using System;
using System.IO;
using System.Text;
using System.Buffers.Binary;
using ScTools.ScriptAssembly.Targets.MP3;

internal static class Dumper
{
    public static void Dump(Script sc, TextWriter w, DumpOptions options)
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
            w.WriteLine("Unknown_18h = 0x{0:X8}", sc.Unknown_18h);
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

        for (var ip = 0; ip < sc.CodeLength;)
        {
            lineSB.Clear();

            var size = OpcodeExtensions.ByteSize(sc.Code.AsSpan(ip));

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

    private static void DisassembleInstructionAt(StringBuilder sb, Script sc, int ip)
    {
        var opcode = (Opcode)sc.Code[ip];
        if (opcode.IsInvalid())
        {
            sb.Append($"<invalid opcode {opcode:X2}>");
            return;
        }
        
        var inst = OpcodeExtensions.GetInstructionSpan(sc.Code, ip);

        sb.Append(opcode.Mnemonic());

        switch (opcode)
        {
            case Opcode.TEXT_LABEL_ASSIGN_STRING:
            case Opcode.TEXT_LABEL_ASSIGN_INT:
            case Opcode.TEXT_LABEL_APPEND_STRING:
            case Opcode.TEXT_LABEL_APPEND_INT:
                sb.Append($" {opcode.GetTextLabelLength(inst)}");
                break;
                
            case Opcode.STRING:
                sb.Append($" '{opcode.GetStringOperand(inst).Escape()}'");
                break;

            case Opcode.PUSH_CONST_U16:
                var u16 = opcode.GetU16Operand(inst);
                sb.Append($" {u16} (0x{u16:X})");
                break;

            case Opcode.PUSH_CONST_U32:
                var u32 = opcode.GetU32Operand(inst);
                sb.Append($" {unchecked((int)u32)} (0x{u32:X})");
                break;

            case Opcode.PUSH_CONST_F:
                var f = opcode.GetFloatOperand(inst);
                sb.Append($" {f}");
                break;

            case Opcode.J:
            case Opcode.JZ:
            case Opcode.JNZ:
            case Opcode.CALL:
                var addr = opcode.GetU32Operand(inst);
                sb.Append($" {addr:000000}");
                break;

            case Opcode.SWITCH:
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    sb.Append($" {c.Value}:{c.JumpAddress:000000}");
                }
                break;

            case Opcode.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                var funcName = opcode.GetEnterFunctionName(inst);
                sb.Append($" {enter.ParamCount} {enter.FrameSize}");
                if (funcName is not null)
                {
                    sb.Append($" [{funcName}]");
                }
                break;

            case Opcode.LEAVE:
                var leave = opcode.GetLeaveOperands(inst);
                sb.Append($" {leave.ParamCount} {leave.ReturnCount}");
                break;

            case Opcode.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                sb.Append($" {native.ParamCount} {native.ReturnCount} 0x{native.CommandHash:X8}");
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
}
