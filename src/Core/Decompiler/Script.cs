namespace ScTools.Decompiler;

using ScTools.Decompiler.IR;

using System;
using System.Collections.Generic;

public class Script
{
    private readonly List<Function> functions = new();
    public IRScript IR { get; }
    public Function EntryFunction { get; }

    private Script(IRScript ir)
    {
        if (ir.Head is null || ir.Tail is null)
        {
            throw new ArgumentException("IR script is missing instructions.", nameof(ir));
        }

        IR = ir;
        EntryFunction = new Function(ir, ir.Head!);
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

        var inst = IR.FindInstructionAt(address);
        if (inst is null)
        {
            throw new ArgumentException($"Instruction at address {address:000000} not found.", nameof(address));
        }

        var func = new Function(IR, inst);
        functions.Add(func);
        return func;
    }

    public static Script FromFive(GameFiles.Five.Script script) => new(new IRDisassemblerFive(script).Disassemble());
    public static Script FromPayne(GameFiles.ScriptPayne script) => new(new IRDisassemblerPayne(script).Disassemble());
    public static Script FromNY(GameFiles.ScriptNY script) => new(new IRDisassemblerNY(script).Disassemble());
}
