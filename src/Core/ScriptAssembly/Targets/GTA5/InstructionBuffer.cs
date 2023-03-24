namespace ScTools.ScriptAssembly.Targets.GTA5;

using System.Runtime.InteropServices;

using GameFiles.GTA5;
using ScTools.ScriptAssembly;

internal class InstructionBuffer : InstructionBuffer<OpcodeV10, LabelInfo, ScriptPageTable<byte>>
{
    public override ScriptPageTable<byte> Finish(IEnumerable<LabelInfo> labels)
    {
        var segment = new SegmentBuilder(sizeof(byte), isPaged: true);
        var codeBuffer = CollectionsMarshal.AsSpan(Buffer);
        var finalInstructions = new List<InstructionInfo>(capacity: InstructionsInfo.Count);

        foreach (var (instOffset, instLength) in InstructionsInfo)
        {
            if (instLength == 0)
            {
                finalInstructions.Add(new(segment.Length, 0));
                continue;
            }

            int offset = (int)(segment.Length & (Script.MaxPageLength - 1));

            var instructionBuffer = codeBuffer.Slice(instOffset, instLength);
            OpcodeV10 opcode = (OpcodeV10)instructionBuffer[0];

            // At page boundary a NOP may be required for the interpreter to switch to the next page,
            // the interpreter only does this with control flow instructions and NOP
            // If the NOP is needed, skip 1 byte at the end of the page
            bool needsNopAtBoundary = !opcode.IsControlFlow() &&
                                        opcode != OpcodeV10.NOP;

            if (offset + instructionBuffer.Length > (Script.MaxPageLength - (needsNopAtBoundary ? 1u : 0))) // the instruction doesn't fit in the current page
            {
                var bytesUntilNextPage = (int)Script.MaxPageLength - offset; // padding needed to skip to the next page
                var requiredNops = bytesUntilNextPage;

                const int JumpInstructionSize = 3;
                if (bytesUntilNextPage > JumpInstructionSize)
                {
                    // if there is enough space for a J instruction, add it to jump to the next page
                    short relIP = (short)(Script.MaxPageLength - (offset + JumpInstructionSize)); // get how many bytes until the next page
                    segment.Byte((byte)OpcodeV10.J);
                    segment.Byte((byte)(relIP & 0xFF));
                    segment.Byte((byte)(relIP >> 8));
                    requiredNops -= JumpInstructionSize;
                }

                // NOP what is left of the current page
                segment.Bytes(new byte[requiredNops]);
            }

            var finalInstOffset = segment.Length;
            var finalInstLength = instructionBuffer.Length;
            finalInstructions.Add(new(finalInstOffset, finalInstLength));
            segment.Bytes(instructionBuffer);
        }

        BackfillLabels(labels, segment.RawDataBuffer, finalInstructions);

        return segment.ToPages<byte>();
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
        switch (reference.Kind)
        {
            case LabelReferenceKind.Absolute:
                var destU24 = instructionBuffer[reference.OperandOffset..(reference.OperandOffset + 3)];
                Debug.Assert(destU24[0] == 0 && destU24[1] == 0 && destU24[2] == 0);
                destU24[0] = (byte)(labelOffset & 0xFF);
                destU24[1] = (byte)((labelOffset >> 8) & 0xFF);
                destU24[2] = (byte)((labelOffset >> 16) & 0xFF);
                return;
            case LabelReferenceKind.Relative:
                var destS16 = instructionBuffer[reference.OperandOffset..(reference.OperandOffset + 2)];
                Debug.Assert(destS16[0] == 0 && destS16[1] == 0);
                var relOffset = AbsoluteAddressToOperandRelativeOffset(labelOffset, instOffset + reference.OperandOffset);
                destS16[0] = (byte)(relOffset & 0xFF);
                destS16[1] = (byte)(relOffset >> 8);
                return;
        }
    }

    private static short AbsoluteAddressToOperandRelativeOffset(int absoluteAddress, int operandAddress)
    {
        var relOffset = absoluteAddress - (operandAddress + 2);
        if (relOffset < short.MinValue || relOffset > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteAddress), "Address is too far");
        }

        return (short)relOffset;
    }
}
