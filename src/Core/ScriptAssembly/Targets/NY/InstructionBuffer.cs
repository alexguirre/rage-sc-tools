namespace ScTools.ScriptAssembly.Targets.NY;

using System.Buffers.Binary;
using System.Runtime.InteropServices;

internal class InstructionBuffer : InstructionBuffer<Opcode, LabelInfo, byte[]>
{
    public override byte[] Finish(IEnumerable<LabelInfo> labels)
    {
        var codeBuffer = CollectionsMarshal.AsSpan(Buffer);
        var finalCodeBuffer = new List<byte>(capacity: Buffer.Count);
        var finalInstructions = new List<InstructionInfo>(capacity: InstructionsInfo.Count);

        foreach (var (instOffset, instLength) in InstructionsInfo)
        {
            if (instLength == 0)
            {
                finalInstructions.Add(new(finalCodeBuffer.Count, 0));
                continue;
            }

            var instructionBuffer = codeBuffer.Slice(instOffset, instLength);

            var finalInstOffset = finalCodeBuffer.Count;
            var finalInstLength = instructionBuffer.Length;
            finalInstructions.Add(new(finalInstOffset, finalInstLength));
            foreach (var b in instructionBuffer)
            {
                finalCodeBuffer.Add(b);
            }
        }

        var finalCodeBufferArray = finalCodeBuffer.ToArray();
        BackfillLabels(labels, finalCodeBufferArray, finalInstructions);
        return finalCodeBufferArray;
    }

    private static void BackfillLabels(IEnumerable<LabelInfo> labels, Span<byte> codeBuffer, List<InstructionInfo> instructions)
    {
        foreach (var label in labels)
        {
            foreach (var reference in label.UnresolvedReferences)
            {
                BackfillLabel(label, reference, codeBuffer, instructions);
            }
        }
    }

    private static void BackfillLabel(LabelInfo label, LabelReference reference, Span<byte> codeBuffer, List<InstructionInfo> instructions)
    {
        Debug.Assert(label.Instruction is not null, "All labels must have been resolved");

        var (labelOffset, _) = instructions[label.Instruction.Index];
        var (instOffset, instLength) = instructions[reference.Instruction.Index];
        Debug.Assert(reference.OperandOffset < instLength, "Operand offset out of instruction bounds");

        var instructionBuffer = codeBuffer.Slice(instOffset, instLength);
        var destU32 = instructionBuffer[reference.OperandOffset..(reference.OperandOffset + 4)];
        BinaryPrimitives.WriteInt32LittleEndian(destU32, labelOffset);
    }
}
