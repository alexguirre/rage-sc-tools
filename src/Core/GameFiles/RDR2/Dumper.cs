namespace ScTools.GameFiles.RDR2;

using System;
using System.IO;
using System.Text;
using System.Buffers.Binary;
using ScTools.ScriptAssembly.Targets.RDR2;

internal static class Dumper
{
    public static void Dump(Script sc, TextWriter w, DumpOptions options)
    {
        sc = sc ?? throw new ArgumentNullException(nameof(sc));

        if (options.IncludeMetadata)
        {
            w.WriteLine("Magic = 0x{0:X8}", sc.Magic);
            w.WriteLine("AES Key ID = {0}", sc.AesKeyId);
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
            w.WriteLine("Natives Count = {0}", sc.NativesCount);
            if (sc.Natives != null)
            {
                foreach (uint hash in sc.Natives)
                {
                    w.WriteLine("\t0x{0:X8}", hash);
                }
            }
            w.WriteLine("Unknown_24h = 0x{0:X8}", sc.Unknown_24h);
            w.WriteLine("Unknown_28h = 0x{0:X8}", sc.Unknown_28h);
            w.WriteLine("Unknown_2Ch = 0x{0:X8}", sc.Unknown_2Ch);
            w.WriteLine("Code Length = {0}", sc.CodeLength);
        }

        if (options.IncludeDisassembly)
        {
            Disassemble(sc, w, options);
        }

        if (options.IncludeIR)
        {
            w.WriteLine("IR Disassembly:");
            var ir = ScTools.Decompiler.Script.FromRDR2(sc);
            ScTools.Decompiler.IR.IRPrinter.PrintAll(ir.Code.Head, w, options.IncludeOffsets);
        }
    }

    private static void Disassemble(Script sc, TextWriter w, DumpOptions options)
    {
        var code = sc.MergeCodePages();
        w.WriteLine("Disassembly:");

        var lineSB = new StringBuilder();

        for (var ip = 0; ip < sc.CodeLength;)
        {
            lineSB.Clear();

            var size = OpcodeTraits.ByteSize(code.AsSpan(ip));

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
                    lineSB.Append(code[ip + offset].ToString("X2"));
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
                DisassembleInstructionAt(lineSB, sc, code, ip);
            }

            lineSB.AppendLine();

            w.Write(lineSB);

            ip += size;
        }
    }

    private static void DisassembleInstructionAt(StringBuilder sb, Script sc, byte[] code, int ip)
    {
        var opcode = (Opcode)code[ip];
        if (opcode.IsInvalid())
        {
            sb.Append($"<invalid opcode {(byte)opcode:X2}>");
            return;
        }
        
        var inst = OpcodeTraits.GetInstructionSpan(code, ip);

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
            case Opcode.STRING_U32:
                sb.Append($" '{opcode.GetStringOperand(inst).Escape()}'");
                break;

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
            case Opcode.IOFFSET_U8_LOAD:
            case Opcode.IOFFSET_U8_STORE:
            case Opcode.IMUL_U8:
                sb.Append($" {opcode.GetU8Operand(inst)}");
                break;

            case Opcode.PUSH_CONST_U8_U8:
                sb.Append($" {opcode.GetU8Operand(inst, 0)} {opcode.GetU8Operand(inst, 1)}");
                break;

            case Opcode.PUSH_CONST_U8_U8_U8:
                sb.Append($" {opcode.GetU8Operand(inst, 0)} {opcode.GetU8Operand(inst, 1)} {opcode.GetU8Operand(inst, 2)}");
                break;

            case Opcode.PUSH_CONST_S16:
            case Opcode.IADD_S16:
            case Opcode.IOFFSET_S16_LOAD:
            case Opcode.IOFFSET_S16_STORE:
            case Opcode.IMUL_S16:
                var s16 = opcode.GetS16Operand(inst);
                sb.Append($" {s16} (0x{s16:X})");
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
                var u16 = opcode.GetU16Operand(inst);
                sb.Append($" {u16} (0x{u16:X})");
                break;

            case Opcode.PUSH_CONST_U24:
            case Opcode.GLOBAL_U24:
            case Opcode.GLOBAL_U24_LOAD:
            case Opcode.GLOBAL_U24_STORE:
                var u24 = opcode.GetU24Operand(inst);
                sb.Append($" {unchecked((int)u24)} (0x{u24:X})");
                break;

            case Opcode.PUSH_CONST_U32:
                var u32 = opcode.GetU32Operand(inst);
                sb.Append($" {unchecked((int)u32)} (0x{u32:X})");
                break;

            case Opcode.PUSH_CONST_F:
                sb.Append($" {opcode.GetFloatOperand(inst):G9}");
                break;

            case >= Opcode.CALL_0 and <= Opcode.CALL_F:
                var call = opcode.GetCallTarget(inst);
                sb.Append($" {call.Offset} [{call.Address:000000}]");
                break;

            case >= Opcode.J and <= Opcode.ILE_JZ:
                var jumpOffset = (int)opcode.GetS16Operand(inst);
                var jumpAddress = ip + 3 + jumpOffset;
                sb.Append($" {jumpAddress:000000}");
                break;

            case Opcode.SWITCH:
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    sb.Append($" {c.Value}:{c.GetJumpTargetAddress(ip):000000}");
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
                var nativeHashStr = native.CommandIndex < sc.Natives.Length ? $"0x{sc.Natives[native.CommandIndex]:X8}" : $"<out of bounds>";
                sb.Append($" {native.ParamCount} {native.ReturnCount} {native.CommandIndex} [{nativeHashStr}]");
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
