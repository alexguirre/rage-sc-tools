namespace ScTools.ScriptAssembly;

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using ScTools.GameFiles;
using System.Buffers.Binary;
using System.Text;

public class DisassemblerNY
{
    private const string CodeFuncPrefix = "func_",
                         CodeLabelPrefix = "lbl_",
                         StaticLabelPrefix = "s_",
                         ArgLabelPrefix = "arg_";

    private readonly Dictionary<int, string> codeLabels = new();
    private readonly Dictionary<int, string> staticsLabels = new();
    private readonly Dictionary<uint, string> nativeCommands;

    public string ScriptName { get; }
    public ScriptNY Script { get; }

    public DisassemblerNY(ScriptNY sc, string scriptName, Dictionary<uint, string> nativeCommands)
    {
        Script = sc ?? throw new ArgumentNullException(nameof(sc));
        ScriptName = scriptName;

        this.nativeCommands = nativeCommands;
    }

    public void Disassemble(TextWriter w)
    {
        var sc = Script;

        IdentifyCodeLabels();
        IdentifyStaticsLabels();

        w.WriteLine(".script_name '{0}'", ScriptName.Escape());
        if (sc.GlobalsSignature != 0)
        {
            w.WriteLine(".globals_signature 0x{0:X8}", sc.GlobalsSignature);
        }

        WriteGlobalsSegment(w);

        WriteStaticsSegment(w);

        WriteArgsSegment(w);

        WriteCodeSegment(w);
    }

    private void WriteGlobalsSegment(TextWriter w)
    {
        var sc = Script;
        if (sc.GlobalsCount == 0)
        {
            return;
        }

        w.WriteLine(".global");
        var repeatedValue = 0;
        var repeatedCount = 0;
        foreach (var value in sc.Globals)
        {
            var v = value.AsInt32;
            if (repeatedCount > 0 && v == repeatedValue)
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

                repeatedValue = v;
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

    private void WriteStaticsValues(TextWriter w, int from, int toExclusive)
    {
        var sc = Script;
        int repeatedValue = 0;
        int repeatedCount = 0;
        for (int i = from; i < toExclusive; i++)
        {
            if (staticsLabels.TryGetValue(i, out var label))
            {
                FlushValue();
                w.WriteLine("\t{0}:", label);
            }

            var v = sc.Statics[i].AsInt32;
            if (repeatedCount > 0 && v != repeatedValue)
            {
                FlushValue();
            }

            repeatedValue = v;
            repeatedCount++;
        }

        FlushValue();

        void FlushValue()
        {
            if (repeatedCount > 1)
            {
                w.WriteLine("\t\t.int {0} dup ({1})", repeatedCount, repeatedValue);
            }
            else if (repeatedCount == 1)
            {
                w.WriteLine("\t\t.int {0}", repeatedValue);
            }

            repeatedCount = 0;
        }
    }

    private void WriteStaticsSegment(TextWriter w)
    {
        var sc = Script;
        var numStatics = sc.StaticsCount - sc.ArgsCount;
        if (numStatics == 0)
        {
            return;
        }

        w.WriteLine(".static");
        WriteStaticsValues(w, from: 0, toExclusive: (int)numStatics);
        w.WriteLine();
    }

    private void WriteArgsSegment(TextWriter w)
    {
        var sc = Script;
        if (sc.ArgsCount == 0)
        {
            return;
        }

        w.WriteLine(".arg");
        WriteStaticsValues(w, from: (int)(sc.StaticsCount - sc.ArgsCount), toExclusive: (int)sc.StaticsCount);
        w.WriteLine();
    }

    private void WriteCodeSegment(TextWriter w)
    {
        if (Script.CodeLength == 0)
        {
            return;
        }

        w.WriteLine(".code");
        foreach (var inst in Script.EnumerateInstructions())
        {
            TryWriteLabel(inst.Address);

            DisassembleInstruction(w, inst.Address, inst.Bytes);
        }

        // in case we have label pointing to the end of the code
        TryWriteLabel((int)Script.CodeLength);


        void TryWriteLabel(int address)
        {
            if (codeLabels.TryGetValue(address, out var label))
            {
                if (label.StartsWith(CodeLabelPrefix))
                {
                    w.WriteLine("\t{0}:", label);
                }
                else
                {
                    // add a new line to visually separate this function from the previous one
                    w.WriteLine();
                    w.WriteLine("{0}:", label);
                }
            }
        }
    }

    private void DisassembleInstruction(TextWriter w, int ip, ReadOnlySpan<byte> inst)
    {
        var opcode = (OpcodeNY)inst[0];

        w.Write("\t\t");
        w.Write(opcode.ToString());
        if (opcode.NumberOfOperands() != 0)
        {
            w.Write(' ');
        }

        switch (opcode)
        {
            case OpcodeNY.LEAVE:
                var leave = opcode.GetLeaveOperands(inst);
                w.Write($" {leave.ParamCount}, {leave.ReturnCount}");
                break;
            case OpcodeNY.ENTER:
                var enter = opcode.GetEnterOperands(inst);
                w.Write($" {enter.ParamCount}, {enter.FrameSize}");
                break;
            case OpcodeNY.PUSH_CONST_U16:
                w.Write(opcode.GetU16Operand(inst));
                break;
            case OpcodeNY.PUSH_CONST_U32:
                w.Write(opcode.GetU32Operand(inst));
                break;
            case OpcodeNY.PUSH_CONST_F:
                w.Write(opcode.GetFloatOperand(inst).ToString("G9", CultureInfo.InvariantCulture));
                break;
            case OpcodeNY.NATIVE:
                var native = opcode.GetNativeOperands(inst);
                if (nativeCommands.TryGetValue(native.CommandHash, out var nativeName))
                {
                    w.Write($"{native.ParamCount}, {native.ReturnCount}, {nativeName}");
                }
                else
                {
                    w.Write($"{native.ParamCount}, {native.ReturnCount}, 0x{native.CommandHash:X8}");
                }
                break;
            case OpcodeNY.J:
            case OpcodeNY.JZ:
            case OpcodeNY.JNZ:
            case OpcodeNY.CALL:
                var addr = opcode.GetU32Operand(inst);
                if (codeLabels.TryGetValue((int)addr, out var label))
                {
                    w.Write(label);
                }
                else
                {
                    w.Write(addr);
                }
                break;
            case OpcodeNY.SWITCH:
                var firstCase = true;
                foreach (var c in opcode.GetSwitchOperands(inst))
                {
                    if (!firstCase)
                    {
                        w.Write(", ");
                    }
                    firstCase = false;

                    if (codeLabels.TryGetValue(c.JumpAddress, out var caseLabel))
                    {
                        w.Write($"{c.Value}:{caseLabel}");
                    }
                    else
                    {
                        w.Write($"{c.Value}:{c.JumpAddress}");
                    }
                }
                break;
            case OpcodeNY.STRING:
                w.Write($"'{opcode.GetStringOperand(inst).Escape()}'");
                break;
            case OpcodeNY.TEXT_LABEL_ASSIGN_STRING:
            case OpcodeNY.TEXT_LABEL_ASSIGN_INT:
            case OpcodeNY.TEXT_LABEL_APPEND_STRING:
            case OpcodeNY.TEXT_LABEL_APPEND_INT:
                w.Write($"{opcode.GetTextLabelLength(inst)}");
                break;
        }

        w.WriteLine();
    }

    private void IdentifyCodeLabels()
    {
        codeLabels.Clear();

        if (Script.CodeLength != 0)
        {
            foreach (var inst in Script.EnumerateInstructions())
            {
                switch (inst.Opcode)
                {
                    case OpcodeNY.J:
                    case OpcodeNY.JZ:
                    case OpcodeNY.JNZ:
                        var jumpAddress = inst.Opcode.GetU32Operand(inst.Bytes);
                        AddLabel(codeLabels, (int)jumpAddress);
                        break;
                    case OpcodeNY.SWITCH:
                        foreach (var c in inst.Opcode.GetSwitchOperands(inst.Bytes))
                        {
                            AddLabel(codeLabels, c.JumpAddress);
                        }
                        break;
                    case OpcodeNY.ENTER:
                        var funcAddress = inst.Address;
                        var funcName = funcAddress == 0 ? "main" : null;
                        AddFuncLabel(codeLabels, funcAddress, funcName);
                        break;
                }
            }
        }

        static void AddFuncLabel(Dictionary<int, string> codeLabels, int address, string? name)
            => codeLabels.TryAdd(address, name ?? CodeFuncPrefix + address);
        static void AddLabel(Dictionary<int, string> codeLabels, int address)
            => codeLabels.TryAdd(address, CodeLabelPrefix + address);
    }

    private void IdentifyStaticsLabels()
    {
        //staticsLabels.Clear();

        //if (code.Length != 0)
        //{
        //    IterateCode(inst =>
        //    {
        //        uint? staticAddress = inst.Opcode switch
        //        {
        //            Opcode.STATIC_U8 or
        //            Opcode.STATIC_U8_LOAD or
        //            Opcode.STATIC_U8_STORE => inst.Bytes[1],

        //            Opcode.STATIC_U16 or
        //            Opcode.STATIC_U16_LOAD or
        //            Opcode.STATIC_U16_STORE => MemoryMarshal.Read<ushort>(inst.Bytes[1..]),

        //            _ => null,
        //        };

        //        if (staticAddress.HasValue)
        //        {
        //            AddStaticLabel(Script, staticsLabels, staticAddress.Value);
        //        }
        //    });
        //}

        //static void AddStaticLabel(Script sc, Dictionary<uint, string> statisLabels, uint address)
        //{
        //    var argsStart = sc.StaticsCount - sc.ArgsCount;
        //    var label = address < argsStart ?
        //        StaticLabelPrefix + address :
        //        ArgLabelPrefix + (address - argsStart);

        //    statisLabels.TryAdd(address, label);
        //}
    }

    public static void Disassemble(TextWriter output, ScriptNY sc, string scriptName, Dictionary<uint, string> nativeCommands)
    {
        var a = new DisassemblerNY(sc, scriptName, nativeCommands);
        a.Disassemble(output);
    }
}
