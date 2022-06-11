namespace ScTools.ScriptLang.CodeGen.Targets.NY;

using System.Buffers.Binary;
using System.Runtime.InteropServices;

using ScTools.GameFiles;
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
        private const int InitialBufferCapacity = 0x4000;

        private readonly List<byte> buffer = new(capacity: InitialBufferCapacity);
        private readonly List<InstructionInfo> instructions = new(capacity: InitialBufferCapacity / 4);
        private readonly List<InstructionReference> references = new(capacity: InitialBufferCapacity / 4);

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

        public byte[] ToCodeBuffer(List<LabelInfo> labels)
        {
            var codeBuffer = CollectionsMarshal.AsSpan(buffer);
            var finalCodeBuffer = new List<byte>(capacity: buffer.Count);
            var finalInstructions = new List<InstructionInfo>(capacity: instructions.Count);

            foreach (var (instOffset, instLength) in instructions)
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
            var destU32 = instructionBuffer[reference.OperandOffset..(reference.OperandOffset + 4)];
            BinaryPrimitives.WriteInt32LittleEndian(destU32, labelOffset);
        }
    }
}
