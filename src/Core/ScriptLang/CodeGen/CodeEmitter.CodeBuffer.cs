namespace ScTools.ScriptLang.CodeGen;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ScTools.GameFiles.Five;
using ScTools.ScriptAssembly;

public partial class CodeEmitter
{
    private record struct InstructionInfo(int Offset, int Length);
    private sealed class InstructionReference
    {
        public int Index { get; set; }
    }

    private sealed class CodeBuffer
    {
        private readonly List<byte> buffer = new(capacity: (int)Script.MaxPageLength);
        private readonly List<InstructionInfo> instructions = new(capacity: (int)Script.MaxPageLength / 4);
        private readonly List<InstructionReference> references = new(capacity: (int)Script.MaxPageLength / 4);

        public int NumberOfInstructions => instructions.Count;
        public InstructionReference GetRef(int instructionIndex) => references[instructionIndex];
        public bool IsEmpty(InstructionReference instruction) => instructions[instruction.Index].Length == 0;
        public Opcode GetOpcode(InstructionReference instruction) => (Opcode)buffer[instructions[instruction.Index].Offset];
        public byte GetByte(InstructionReference instruction, int offset) => buffer[instructions[instruction.Index].Offset + offset];
        public List<byte> GetBytes(InstructionReference instruction)
        {
            var (instOffset, instLength) = instructions[instruction.Index];
            return buffer.GetRange(instOffset, instLength);
        }

        private InstructionReference InsertInstruction(int index, List<byte> instructionBytes)
        {
            var instOffset = buffer.Count;
            var instLength = instructionBytes.Count;
            var instRef = new InstructionReference { Index = index };
            instructions.Insert(index, new(instOffset, instLength));
            references.Insert(index, instRef);
            buffer.AddRange(instructionBytes);
            UpdateReferences(start: instRef.Index + 1);
            return instRef;
        }

        public InstructionReference InsertBefore(InstructionReference instruction, List<byte> instructionBytes)
            => InsertInstruction(instruction.Index, instructionBytes);

        public InstructionReference InsertAfter(InstructionReference instruction, List<byte> instructionBytes)
            => InsertInstruction(instruction.Index + 1, instructionBytes);

        public InstructionReference Append(List<byte> instructionBytes)
            => InsertInstruction(instructions.Count, instructionBytes);

        public void Update(InstructionReference instruction, List<byte> newInstructionBytes)
        {
            var (instOffset, instLength) = instructions[instruction.Index];
            var newInstLength = newInstructionBytes.Count;
            if (newInstLength <= instLength)
            {
                // can be updated in-place
                for (int i = 0; i < newInstructionBytes.Count; i++)
                {
                    buffer[instOffset + i] = newInstructionBytes[i];
                }
                instructions[instruction.Index] = new(instOffset, newInstLength);
            }
            else
            {
                // append bytes to the end
                var newInstOffset = buffer.Count;
                instructions[instruction.Index] = new(newInstOffset, newInstLength);
                buffer.AddRange(newInstructionBytes);
            }
        }

        public void Remove(InstructionReference instruction)
        {
            instructions[instruction.Index] = instructions[instruction.Index] with { Length = 0 };
        }

        private void UpdateReferences(int start = 0)
        {
            for (int i = start; i < references.Count; i++)
            {
                references[i].Index = i;
            }
        }

        public ScriptPageArray<byte> ToCodePages(List<LabelInfo> labels)
        {
            var segment = new SegmentBuilder(sizeof(byte), isPaged: true);
            var codeBuffer = CollectionsMarshal.AsSpan(buffer);
            var finalInstructions = new List<InstructionInfo>(capacity: instructions.Count);

            foreach (var (instOffset, instLength) in instructions)
            {
                if (instLength == 0)
                {
                    finalInstructions.Add(new(segment.Length, 0));
                    continue;
                }

                int offset = (int)(segment.Length & (Script.MaxPageLength - 1));

                var instructionBuffer = codeBuffer.Slice(instOffset, instLength);
                Opcode opcode = (Opcode)instructionBuffer[0];

                // At page boundary a NOP may be required for the interpreter to switch to the next page,
                // the interpreter only does this with control flow instructions and NOP
                // If the NOP is needed, skip 1 byte at the end of the page
                bool needsNopAtBoundary = !opcode.IsControlFlow() &&
                                          opcode != Opcode.NOP;

                if (offset + instructionBuffer.Length > (Script.MaxPageLength - (needsNopAtBoundary ? 1u : 0))) // the instruction doesn't fit in the current page
                {
                    var bytesUntilNextPage = (int)Script.MaxPageLength - offset; // padding needed to skip to the next page
                    var requiredNops = bytesUntilNextPage;

                    const int JumpInstructionSize = 3;
                    if (bytesUntilNextPage > JumpInstructionSize)
                    {
                        // if there is enough space for a J instruction, add it to jump to the next page
                        short relIP = (short)(Script.MaxPageLength - (offset + JumpInstructionSize)); // get how many bytes until the next page
                        segment.Byte((byte)Opcode.J);
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

        private static void BackfillLabels(List<LabelInfo> labels, Span<byte> codeBuffer, List<InstructionInfo> instructions)
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
}
