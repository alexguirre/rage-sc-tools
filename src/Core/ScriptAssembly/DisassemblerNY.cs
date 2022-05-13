#nullable enable
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

    private readonly byte[] code;
    private readonly Dictionary<uint, string> codeLabels = new();
    private readonly Dictionary<uint, string> staticsLabels = new();
    private readonly Dictionary<uint, string> nativeCommands;

    public string ScriptName { get; }
    public ScriptNY Script { get; }

    public DisassemblerNY(ScriptNY sc, string scriptName, Dictionary<uint, string> nativeCommands)
    {
        Script = sc ?? throw new ArgumentNullException(nameof(sc));
        ScriptName = scriptName;
        code = sc.Code ?? Array.Empty<byte>();

        this.nativeCommands = nativeCommands;
    }

    public void Disassemble(TextWriter w)
    {
        var sc = Script;

        IdentifyCodeLabels();
        IdentifyStaticsLabels();

        w.WriteLine(".script_name {0}", ScriptName);
        if (sc.GlobalsSignature != 0)
        {
            w.WriteLine(".globals_signature 0x{0:X8}", sc.GlobalsSignature);
        }

        WriteGlobalsSegment(w);

        WriteStaticsSegment(w);

        WriteArgsSegment(w);

        WriteCodeSegment(w);
    }

    private void WriteGlobalsValues(TextWriter w)
    {
        var sc = Script;
        int repeatedValue = 0;
        int repeatedCount = 0;
        foreach (var value in sc.Globals)
        {
            var v = value.AsInt32;
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

    private void WriteStaticsValues(TextWriter w, uint from, uint toExclusive)
    {
        var sc = Script;
        int repeatedValue = 0;
        int repeatedCount = 0;
        for (uint i = from; i < toExclusive; i++)
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
        WriteStaticsValues(w, from: 0, toExclusive: numStatics);
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
        WriteStaticsValues(w, from: sc.StaticsCount - sc.ArgsCount, toExclusive: sc.StaticsCount);
        w.WriteLine();
    }

    private void WriteCodeSegment(TextWriter w)
    {
        if (code.Length == 0)
        {
            return;
        }

        w.WriteLine(".code");
        IterateCode(inst =>
        {
            TryWriteLabel(inst.Address);

            DisassembleInstruction(w, inst, inst.Address, inst.Bytes);
        });

        // in case we have label pointing to the end of the code
        TryWriteLabel((uint)code.Length);


        void TryWriteLabel(uint address)
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

    private void DisassembleInstruction(TextWriter w, InstructionContext ctx, uint ip, ReadOnlySpan<byte> inst)
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
                w.Write(inst[1]);
                w.Write(", ");
                w.Write(inst[2]);
                break;
            case OpcodeNY.ENTER:
                w.Write(inst[1]);
                w.Write(", ");
                w.Write(MemoryMarshal.Read<ushort>(inst[2..]));
                break;
            case OpcodeNY.PUSH_CONST_U16:
                w.Write(MemoryMarshal.Read<ushort>(inst[1..]));
                break;
            case OpcodeNY.PUSH_CONST_U32:
                w.Write(MemoryMarshal.Read<uint>(inst[1..]));
                break;
            case OpcodeNY.PUSH_CONST_F:
                w.Write(MemoryMarshal.Read<float>(inst[1..]).ToString("G9", CultureInfo.InvariantCulture));
                break;
            case OpcodeNY.NATIVE:
                var argCount = inst[1];
                var returnCount = inst[2];
                var nativeHash = MemoryMarshal.Read<uint>(inst[3..]);
                if (nativeCommands.TryGetValue(nativeHash, out var nativeName))
                {
                    w.Write($"{argCount}, {returnCount}, {nativeName}");
                }
                else
                {
                    w.Write($"{argCount}, {returnCount}, 0x{nativeHash:X8}");
                }
                break;
            case OpcodeNY.J:
            case OpcodeNY.JZ:
            case OpcodeNY.JNZ:
            case OpcodeNY.CALL:
                var jumpAddress = MemoryMarshal.Read<uint>(inst[1..]);
                w.Write(codeLabels.TryGetValue(jumpAddress, out var label) ? label : jumpAddress);
                break;
            case OpcodeNY.SWITCH:
                var caseCount = inst[1];
                for (int i = 0; i < caseCount; i++)
                {
                    var caseOffset = 2 + i * 8;
                    var caseValue = BinaryPrimitives.ReadUInt32LittleEndian(inst[caseOffset..(caseOffset + 4)]);
                    var caseJumpAddr = BinaryPrimitives.ReadUInt32LittleEndian(inst[(caseOffset + 4)..]);

                    if (i != 0)
                    {
                        w.Write(", ");
                    }
                    w.Write("{0}:{1}", caseValue, codeLabels.TryGetValue(caseJumpAddr, out var caseLabel) ? caseLabel : caseJumpAddr);
                }
                break;
            case OpcodeNY.STRING:
                var str = Encoding.UTF8.GetString(inst[2..^1]).Escape();
                w.Write($" '{str}'");
                break;
        }

        w.WriteLine();
    }

    private void IdentifyCodeLabels()
    {
        codeLabels.Clear();

        if (code.Length != 0)
        {
            IterateCode(inst =>
            {
                switch (inst.Opcode)
                {
                    case OpcodeNY.J:
                    case OpcodeNY.JZ:
                    case OpcodeNY.JNZ:
                        var jumpAddress = MemoryMarshal.Read<uint>(inst.Bytes[1..]);
                        AddLabel(codeLabels, jumpAddress);
                        break;
                    case OpcodeNY.SWITCH:
                        var caseCount = inst.Bytes[1];
                        for (int i = 0; i < caseCount; i++)
                        {
                            var caseOffset = 2 + i * 8;
                            var caseValue = BinaryPrimitives.ReadUInt32LittleEndian(inst.Bytes[caseOffset..(caseOffset + 4)]);
                            var caseJumpAddr = BinaryPrimitives.ReadUInt32LittleEndian(inst.Bytes[(caseOffset + 4)..]);
                            AddLabel(codeLabels, caseJumpAddr);
                        }
                        break;
                    case OpcodeNY.ENTER:
                        var funcAddress = inst.Address;
                        var funcName = funcAddress == 0 ? "main" : null;
                        AddFuncLabel(codeLabels, funcAddress, funcName);
                        break;
                }
            });
        }

        static void AddFuncLabel(Dictionary<uint, string> codeLabels, uint address, string? name)
            => codeLabels.TryAdd(address, name ?? CodeFuncPrefix + address);
        static void AddLabel(Dictionary<uint, string> codeLabels, uint address)
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

    private delegate void IterateCodeCallback(InstructionContext instruction);
    private void IterateCode(IterateCodeCallback callback)
    {
        InstructionContext.CB previousCB = currInst =>
        {
            uint prevAddress = 0;
            uint address = 0;
            while (address < currInst.Address)
            {
                prevAddress = address;
                address += (uint)GetInstructionLength(code, address);
            }
            return GetInstructionContext(code, prevAddress, currInst.PreviousCB, currInst.NextCB);
        };
        InstructionContext.CB nextCB = currInst =>
        {
            var nextAddress = currInst.Address + (uint)currInst.Bytes.Length;
            return GetInstructionContext(code, nextAddress, currInst.PreviousCB, currInst.NextCB);
        };

        uint ip = 0;
        while (ip < code.Length)
        {
            var inst = GetInstructionContext(code, ip, previousCB, nextCB);
            callback(inst);
            ip += (uint)inst.Bytes.Length;
        }

        static InstructionContext GetInstructionContext(byte[] code, uint address, InstructionContext.CB previousCB, InstructionContext.CB nextCB)
            => address >= code.Length ? default : new()
            {
                Address = address,
                Bytes = code.AsSpan((int)address, GetInstructionLength(code, address)),
                PreviousCB = previousCB,
                NextCB = nextCB,
            };

        static int GetInstructionLength(byte[] code, uint address)
        {
            var opcode = (OpcodeNY)code[address];
            return opcode switch
            {
                OpcodeNY.SWITCH => 8 * code[address + 1] + 2,
                OpcodeNY.STRING => code[address + 1] + 2,
                _ => opcode.ByteSize(),
            };
        }
    }

    private readonly ref struct InstructionContext
    {
        public delegate InstructionContext CB(InstructionContext curr);

        public bool IsValid => Bytes.Length > 0;
        public uint Address { get; init; }
        public ReadOnlySpan<byte> Bytes { get; init; }
        public OpcodeNY Opcode => (OpcodeNY)Bytes[0];
        public CB PreviousCB { get; init; }
        public CB NextCB { get; init; }

        public InstructionContext Previous() => PreviousCB(this);
        public InstructionContext Next() => NextCB(this);
    }

    public static void Disassemble(TextWriter output, ScriptNY sc, string scriptName, Dictionary<uint, string> nativeCommands)
    {
        var a = new DisassemblerNY(sc, scriptName, nativeCommands);
        a.Disassemble(output);
    }
}
