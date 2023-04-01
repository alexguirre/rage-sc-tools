namespace ScTools.Decompiler.IR;

using System;
using System.Collections.Generic;

public class IRCode
{
    public IRInstruction? Head { get; protected set; }
    public IRInstruction? Tail { get; protected set; }
    private readonly List<IRInstruction> instructions = new();

    public void AppendInstruction(IRInstruction instruction)
    {
        if (Head is null)
        {
            if (instruction.Address != 0)
            {
                throw new ArgumentException("Address of first instruction must be 0", nameof(instruction));
            }

            Head = instruction;
            Tail = instruction;
        }
        else
        {
            if (instruction.Address < Tail!.Address)
            {
                throw new ArgumentException("Instructions must be appended in order", nameof(instruction));
            }

            var tail = Tail!;
            tail.Next = instruction;
            instruction.Previous = tail;
            Tail = instruction;
        }
        instructions.Add(instruction);
    }

    public IRInstruction? FindInstructionAt(int address)
    {
        // Use binary search on the instruction addresses
        IRInstruction? foundInstruction = null;
        var left = 0;
        var right = instructions.Count - 1;
        while (left <= right)
        {
            var middle = (left + right) / 2;
            var instruction = instructions[middle];
            if (instruction.Address == address)
            {
                foundInstruction = instruction;
                break;
            }
            else if (instruction.Address < address)
            {
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        if (foundInstruction is not null)
        {
            // Multiple instructions can have the same address (when a real instruction is split into multiple IR instructions)
            // Find the first instruction with the same address
            while (foundInstruction.Previous is not null && foundInstruction.Previous.Address == address)
            {
                foundInstruction = foundInstruction.Previous;
            }
        }
        
        return foundInstruction;
    }
}
