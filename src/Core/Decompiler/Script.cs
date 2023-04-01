namespace ScTools.Decompiler;

using ScTools.Decompiler.IR;

using System;
using System.Collections.Generic;

public class Script
{
    private readonly List<Function> functions = new();
    public IRCode Code { get; }
    public Function EntryFunction { get; }

    public Script(IRCode code)
    {
        if (code.Head is null || code.Tail is null)
        {
            throw new ArgumentException("IR code is missing instructions.", nameof(code));
        }

        Code = code;
        EntryFunction = new Function(code, code.Head!);
        functions.Add(EntryFunction);
    }

    public Function GetFunctionAt(int address)
    {
        foreach (var function in functions)
        {
            if (function.StartAddress == address)
            {
                return function;
            }
        }

        var inst = Code.FindInstructionAt(address);
        if (inst is null)
        {
            throw new ArgumentException($"Instruction at address {address:000000} not found.", nameof(address));
        }

        var func = new Function(Code, inst);
        functions.Add(func);
        return func;
    }

    public CallGraphNode BuildCallGraph() => CallGraphBuilder.BuildFrom(this, EntryFunction);

    public static Script FromGTA5(GameFiles.GTA5.Script script) => new(IRDisassemblerGTA5.Disassemble(script));
    public static Script FromRDR2(GameFiles.RDR2.Script script) => new(IRDisassemblerRDR2.Disassemble(script));
    public static Script FromMP3(GameFiles.MP3.Script script) => new(IRDisassemblerMP3.Disassemble(script));
    public static Script FromGTA4(GameFiles.GTA4.Script script) => new(IRDisassemblerGTA4.Disassemble(script));
}
