namespace ScTools.ScriptLang.CodeGen;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using ScTools.GameFiles;
using ScTools.ScriptAssembly;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

public sealed partial class CodeEmitter
{
    private enum LabelReferenceKind { Relative, Absolute }
    private record struct LabelReference(InstructionReference Instruction, int OperandOffset, LabelReferenceKind Kind);
    private record struct LabelInfo(InstructionReference? Instruction, List<LabelReference> UnresolvedReferences);

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

    private readonly StatementEmitter stmtEmitter;
    private readonly ValueEmitter valueEmitter;
    private readonly AddressEmitter addressEmitter;
    //private readonly PatternOptimizer optimizer;
    private const bool IncludeFunctionNames = true;

    private readonly CodeBuffer codeBuffer = new();
    private InstructionReference? instructionToUpdateInNextFlush = null;
    private readonly List<byte> instructionBuffer = new();

    private readonly HashSet<FunctionDeclaration> usedFunctions = new();
    private readonly Queue<FunctionDeclaration> functionsToCompile = new();
    private readonly HashSet<VarDeclaration> usedStatics = new();
    private readonly List<VarDeclaration> statics = new();
    private readonly Dictionary<VarDeclaration, int> staticsOffsets = new();

    private readonly List<LabelInfo> labels = new();
    private readonly Dictionary<string, int> functionLabelNameToIndex = new(ParserNew.CaseInsensitiveComparer);
    private readonly Dictionary<string, int> localLabelNameToIndex = new(ParserNew.CaseInsensitiveComparer);

    private byte currentFunctionArgCount;
    private int currentFunctionFrameSize = 0;
    private TypeInfo? currentFunctionReturnType = null;
    private readonly Dictionary<VarDeclaration, int> currentFunctionAllocatedLocals = new();

    public StringsTable Strings { get; } = new();

    public CodeEmitter()
    {
        stmtEmitter = new(this);
        valueEmitter = new(this);
        addressEmitter = new(this);
    }

    public ScriptPageArray<byte> ToCodePages() => codeBuffer.ToCodePages(labels);

    #region Byte Emitters
    private void EmitBytes(ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
        {
            instructionBuffer.Add(b);
        }
    }

    private void EmitU8(byte v)
    {
        instructionBuffer.Add(v);
    }

    private void EmitU16(ushort v)
    {
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)(v >> 8));
    }

    private void EmitS16(short v) => EmitU16(unchecked((ushort)v));

    private void EmitU32(uint v)
    {
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)((v >> 8) & 0xFF));
        instructionBuffer.Add((byte)((v >> 16) & 0xFF));
        instructionBuffer.Add((byte)(v >> 24));
    }

    private void EmitU24(uint v)
    {
        Debug.Assert((v & 0xFFFFFF) == v);
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)((v >> 8) & 0xFF));
        instructionBuffer.Add((byte)((v >> 16) & 0xFF));
    }

    private unsafe void EmitF32(float v) => EmitU32(*(uint*)&v);

    private void EmitOpcode(Opcode v) => EmitU8((byte)v);

    /// <summary>
    /// Clears the current instruction buffer.
    /// </summary>
    private void Drop()
    {
        instructionBuffer.Clear();
    }



    /// <summary>
    /// Writes the current instruction buffer to the segment.
    /// </summary>
    private InstructionReference Flush()
    {
        InstructionReference instRef;
        if (instructionToUpdateInNextFlush is null)
        {
            instRef = codeBuffer.Append(instructionBuffer);
        }
        else
        {
            codeBuffer.Update(instructionToUpdateInNextFlush, instructionBuffer);
            instRef = instructionToUpdateInNextFlush;
            instructionToUpdateInNextFlush = null;
        }

        Drop();
        return instRef;
    }
    #endregion Byte Emitters

    #region Instruction Emitters
    /// <summary>
    /// Emits an instruction of length 0. Used as label marker.
    /// </summary>
    private InstructionReference EmitLabelMarker()
        => Flush();
    private InstructionReference EmitInst(Opcode opcode)
    {
        EmitOpcode(opcode);
        return Flush();
    }
    private InstructionReference EmitInstU8(Opcode opcode, byte operand)
    {
        EmitOpcode(opcode);
        EmitU8(operand);
        return Flush();
    }
    private InstructionReference EmitInstU8U8(Opcode opcode, byte operand1, byte operand2)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU8(operand2);
        return Flush();
    }
    private InstructionReference EmitInstU8U8U8(Opcode opcode, byte operand1, byte operand2, byte operand3)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU8(operand2);
        EmitU8(operand3);
        return Flush();
    }
    private InstructionReference EmitInstS16(Opcode opcode, short operand)
    {
        EmitOpcode(opcode);
        EmitS16(operand);
        return Flush();
    }
    private InstructionReference EmitInstU16(Opcode opcode, ushort operand)
    {
        EmitOpcode(opcode);
        EmitU16(operand);
        return Flush();
    }
    private InstructionReference EmitInstU24(Opcode opcode, uint operand)
    {
        EmitOpcode(opcode);
        EmitU24(operand);
        return Flush();
    }
    private InstructionReference EmitInstU32(Opcode opcode, uint operand)
    {
        EmitOpcode(opcode);
        EmitU32(operand);
        return Flush();
    }
    private InstructionReference EmitInstF32(Opcode opcode, float operand)
    {
        EmitOpcode(opcode);
        EmitF32(operand);
        return Flush();
    }

    private InstructionReference EmitNop() => EmitInst(Opcode.NOP);
    private InstructionReference EmitIAdd() => EmitInst(Opcode.IADD);
    private InstructionReference EmitIAddU8(byte value) => EmitInstU8(Opcode.IADD_U8, value);
    private InstructionReference EmitIAddS16(short value) => EmitInstS16(Opcode.IADD_S16, value);
    private InstructionReference EmitISub() => EmitInst(Opcode.ISUB);
    private InstructionReference EmitIMul() => EmitInst(Opcode.IMUL);
    private InstructionReference EmitIMulU8(byte value) => EmitInstU8(Opcode.IMUL_U8, value);
    private InstructionReference EmitIMulS16(short value) => EmitInstS16(Opcode.IMUL_S16, value);
    private InstructionReference EmitIDiv() => EmitInst(Opcode.IDIV);
    private InstructionReference EmitIMod() => EmitInst(Opcode.IMOD);
    private InstructionReference EmitINot() => EmitInst(Opcode.INOT);
    private InstructionReference EmitINeg() => EmitInst(Opcode.INEG);
    private InstructionReference EmitIEq() => EmitInst(Opcode.IEQ);
    private InstructionReference EmitINe() => EmitInst(Opcode.INE);
    private InstructionReference EmitIGt() => EmitInst(Opcode.IGT);
    private InstructionReference EmitIGe() => EmitInst(Opcode.IGE);
    private InstructionReference EmitILt() => EmitInst(Opcode.ILT);
    private InstructionReference EmitILe() => EmitInst(Opcode.ILE);
    private InstructionReference EmitFAdd() => EmitInst(Opcode.FADD);
    private InstructionReference EmitFSub() => EmitInst(Opcode.FSUB);
    private InstructionReference EmitFMul() => EmitInst(Opcode.FMUL);
    private InstructionReference EmitFDiv() => EmitInst(Opcode.FDIV);
    private InstructionReference EmitFMod() => EmitInst(Opcode.FMOD);
    private InstructionReference EmitFNeg() => EmitInst(Opcode.FNEG);
    private InstructionReference EmitFEq() => EmitInst(Opcode.FEQ);
    private InstructionReference EmitFNe() => EmitInst(Opcode.FNE);
    private InstructionReference EmitFGt() => EmitInst(Opcode.FGT);
    private InstructionReference EmitFGe() => EmitInst(Opcode.FGE);
    private InstructionReference EmitFLt() => EmitInst(Opcode.FLT);
    private InstructionReference EmitFLe() => EmitInst(Opcode.FLE);
    private InstructionReference EmitVAdd() => EmitInst(Opcode.VADD);
    private InstructionReference EmitVSub() => EmitInst(Opcode.VSUB);
    private InstructionReference EmitVMul() => EmitInst(Opcode.VMUL);
    private InstructionReference EmitVDiv() => EmitInst(Opcode.VDIV);
    private InstructionReference EmitIAnd() => EmitInst(Opcode.IAND);
    private InstructionReference EmitIOr() => EmitInst(Opcode.IOR);
    private InstructionReference EmitIXor() => EmitInst(Opcode.IXOR);
    private InstructionReference EmitI2F() => EmitInst(Opcode.I2F);
    private InstructionReference EmitF2I() => EmitInst(Opcode.F2I);
    private InstructionReference EmitF2V() => EmitInst(Opcode.F2V);
    private InstructionReference EmitDup() => EmitInst(Opcode.DUP);
    private InstructionReference EmitDrop() => EmitInst(Opcode.DROP);
    private InstructionReference EmitLoad() => EmitInst(Opcode.LOAD);
    private InstructionReference EmitLoadN() => EmitInst(Opcode.LOAD_N);
    private InstructionReference EmitStore() => EmitInst(Opcode.STORE);
    private InstructionReference EmitStoreN() => EmitInst(Opcode.STORE_N);
    private InstructionReference EmitStoreRev() => EmitInst(Opcode.STORE_REV);
    private InstructionReference EmitString() => EmitInst(Opcode.STRING);
    private InstructionReference EmitStringHash() => EmitInst(Opcode.STRINGHASH);
    private InstructionReference EmitPushConstM1() => EmitInst(Opcode.PUSH_CONST_M1);
    private InstructionReference EmitPushConst0() => EmitInst(Opcode.PUSH_CONST_0);
    private InstructionReference EmitPushConst1() => EmitInst(Opcode.PUSH_CONST_1);
    private InstructionReference EmitPushConst2() => EmitInst(Opcode.PUSH_CONST_2);
    private InstructionReference EmitPushConst3() => EmitInst(Opcode.PUSH_CONST_3);
    private InstructionReference EmitPushConst4() => EmitInst(Opcode.PUSH_CONST_4);
    private InstructionReference EmitPushConst5() => EmitInst(Opcode.PUSH_CONST_5);
    private InstructionReference EmitPushConst6() => EmitInst(Opcode.PUSH_CONST_6);
    private InstructionReference EmitPushConst7() => EmitInst(Opcode.PUSH_CONST_7);
    private InstructionReference EmitPushConstFM1() => EmitInst(Opcode.PUSH_CONST_FM1);
    private InstructionReference EmitPushConstF0() => EmitInst(Opcode.PUSH_CONST_F0);
    private InstructionReference EmitPushConstF1() => EmitInst(Opcode.PUSH_CONST_F1);
    private InstructionReference EmitPushConstF2() => EmitInst(Opcode.PUSH_CONST_F2);
    private InstructionReference EmitPushConstF3() => EmitInst(Opcode.PUSH_CONST_F3);
    private InstructionReference EmitPushConstF4() => EmitInst(Opcode.PUSH_CONST_F4);
    private InstructionReference EmitPushConstF5() => EmitInst(Opcode.PUSH_CONST_F5);
    private InstructionReference EmitPushConstF6() => EmitInst(Opcode.PUSH_CONST_F6);
    private InstructionReference EmitPushConstF7() => EmitInst(Opcode.PUSH_CONST_F7);
    private InstructionReference EmitPushConstU8(byte value) => EmitInstU8(Opcode.PUSH_CONST_U8, value);
    private InstructionReference EmitPushConstU8U8(byte value1, byte value2) => EmitInstU8U8(Opcode.PUSH_CONST_U8_U8, value1, value2);
    private InstructionReference EmitPushConstU8U8U8(byte value1, byte value2, byte value3) => EmitInstU8U8U8(Opcode.PUSH_CONST_U8_U8_U8, value1, value2, value3);
    private InstructionReference EmitPushConstS16(short value) => EmitInstS16(Opcode.PUSH_CONST_S16, value);
    private InstructionReference EmitPushConstU24(uint value) => EmitInstU24(Opcode.PUSH_CONST_U24, value);
    private InstructionReference EmitPushConstU32(uint value) => EmitInstU32(Opcode.PUSH_CONST_U32, value);
    private InstructionReference EmitPushConstF(float value) => EmitInstF32(Opcode.PUSH_CONST_F, value);
    private InstructionReference EmitArrayU8(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8, itemSize);
    private InstructionReference EmitArrayU8Load(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8_LOAD, itemSize);
    private InstructionReference EmitArrayU8Store(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8_STORE, itemSize);
    private InstructionReference EmitArrayU16(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16, itemSize);
    private InstructionReference EmitArrayU16Load(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16_LOAD, itemSize);
    private InstructionReference EmitArrayU16Store(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16_STORE, itemSize);
    private InstructionReference EmitLocalU8(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8, frameOffset);
    private InstructionReference EmitLocalU8Load(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8_LOAD, frameOffset);
    private InstructionReference EmitLocalU8Store(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8_STORE, frameOffset);
    private InstructionReference EmitLocalU16(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16, frameOffset);
    private InstructionReference EmitLocalU16Load(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16_LOAD, frameOffset);
    private InstructionReference EmitLocalU16Store(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16_STORE, frameOffset);
    private InstructionReference EmitStaticU8(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8, staticOffset);
    private InstructionReference EmitStaticU8Load(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8_LOAD, staticOffset);
    private InstructionReference EmitStaticU8Store(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8_STORE, staticOffset);
    private InstructionReference EmitStaticU16(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16, staticOffset);
    private InstructionReference EmitStaticU16Load(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16_LOAD, staticOffset);
    private InstructionReference EmitStaticU16Store(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16_STORE, staticOffset);
    private InstructionReference EmitGlobalU16(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16, globalOffset);
    private InstructionReference EmitGlobalU16Load(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16_LOAD, globalOffset);
    private InstructionReference EmitGlobalU16Store(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16_STORE, globalOffset);
    private InstructionReference EmitGlobalU24(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24, globalOffset);
    private InstructionReference EmitGlobalU24Load(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24_LOAD, globalOffset);
    private InstructionReference EmitGlobalU24Store(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24_STORE, globalOffset);
    private InstructionReference EmitIOffset() => EmitInst(Opcode.IOFFSET);
    private InstructionReference EmitIOffsetU8(byte offset) => EmitInstU8(Opcode.IOFFSET_U8, offset);
    private InstructionReference EmitIOffsetU8Load(byte offset) => EmitInstU8(Opcode.IOFFSET_U8_LOAD, offset);
    private InstructionReference EmitIOffsetU8Store(byte offset) => EmitInstU8(Opcode.IOFFSET_U8_STORE, offset);
    private InstructionReference EmitIOffsetS16(short offset) => EmitInstS16(Opcode.IOFFSET_S16, offset);
    private InstructionReference EmitIOffsetS16Load(short offset) => EmitInstS16(Opcode.IOFFSET_S16_LOAD, offset);
    private InstructionReference EmitIOffsetS16Store(short offset) => EmitInstS16(Opcode.IOFFSET_S16_STORE, offset);
    private InstructionReference EmitTextLabelAssignString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_STRING, textLabelLength);
    private InstructionReference EmitTextLabelAssignInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_INT, textLabelLength);
    private InstructionReference EmitTextLabelAppendString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_STRING, textLabelLength);
    private InstructionReference EmitTextLabelAppendInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_INT, textLabelLength);
    private InstructionReference EmitTextLabelCopy() => EmitInst(Opcode.TEXT_LABEL_COPY);
    private InstructionReference EmitNative(byte argCount, byte returnCount, ushort nativeIndex)
    {
        Debug.Assert((argCount & 0x3F) == argCount); // arg count max bits 6
        Debug.Assert((returnCount & 0x3) == returnCount); // arg count max bits 2
        return EmitInstU8U8U8(Opcode.NATIVE,
            operand1: (byte)((argCount & 0x3F) << 2 | (returnCount & 0x3)),
            operand2: (byte)((nativeIndex >> 8) & 0xFF),
            operand3: (byte)(nativeIndex & 0xFF));
    }
    private InstructionReference EmitEnter(byte argCount, ushort frameSize, string? name)
    {
        EmitOpcode(Opcode.ENTER);
        EmitU8(argCount);
        EmitU16(frameSize);
        if (IncludeFunctionNames && name is not null)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name).AsSpan();
            nameBytes = nameBytes[..Math.Min(nameBytes.Length, byte.MaxValue - 1)]; // limit length to 255 bytes (including null terminators)
            EmitU8((byte)(nameBytes.Length + 1));
            EmitBytes(nameBytes);
            EmitU8(0); // null terminator
        }
        else
        {
            EmitU8(0);
        }
        return Flush();
    }
    private InstructionReference EmitLeave(byte argCount, byte returnCount) => EmitInstU8U8(Opcode.LEAVE, argCount, returnCount);
    private InstructionReference EmitJ(short relativeOffset) => EmitInstS16(Opcode.J, relativeOffset);
    private InstructionReference EmitJZ(short relativeOffset) => EmitInstS16(Opcode.JZ, relativeOffset);
    private InstructionReference EmitIEqJZ(short relativeOffset) => EmitInstS16(Opcode.IEQ_JZ, relativeOffset);
    private InstructionReference EmitINeJZ(short relativeOffset) => EmitInstS16(Opcode.INE_JZ, relativeOffset);
    private InstructionReference EmitIGtJZ(short relativeOffset) => EmitInstS16(Opcode.IGT_JZ, relativeOffset);
    private InstructionReference EmitIGeJZ(short relativeOffset) => EmitInstS16(Opcode.IGE_JZ, relativeOffset);
    private InstructionReference EmitILtJZ(short relativeOffset) => EmitInstS16(Opcode.ILT_JZ, relativeOffset);
    private InstructionReference EmitILeJZ(short relativeOffset) => EmitInstS16(Opcode.ILE_JZ, relativeOffset);
    private InstructionReference EmitCall(uint functionOffset) => EmitInstU24(Opcode.CALL, functionOffset);
    private InstructionReference EmitCallIndirect() => EmitInst(Opcode.CALLINDIRECT);
    private InstructionReference EmitSwitch(byte numCases) // cases will be backfilled later
    {
        EmitOpcode(Opcode.SWITCH);
        EmitU8(numCases);
        for (int i = 0; i < numCases; i++)
        {
            EmitU32(0); // value
            EmitS16(0); // label relative offset
        }
        return Flush();
    }
    private InstructionReference EmitCatch() => EmitInst(Opcode.CATCH);
    private InstructionReference EmitThrow() => EmitInst(Opcode.THROW);
    #endregion Instruction Emitters

    #region Public High-Level Emitters

    public void EmitScript(ScriptDeclaration script)
    {
        EmitScriptEntryPoint(script);

        while (functionsToCompile.TryDequeue(out var function))
        {
            EmitFunction(function);
        }

        new PatternOptimizer().Optimize(codeBuffer);
    }

    public void EmitScriptEntryPoint(ScriptDeclaration script)
    {
        // TODO: emit static initializers at the beginning of the SCRIPT entrypoint
        EmitFunctionCommon("SCRIPT", script.Parameters, script.Body, VoidType.Instance);
    }

    public void EmitFunction(FunctionDeclaration function)
    {
        Label(function.Name, isFunctionLabel: true);
        EmitFunctionCommon(function.Name, function.Parameters, function.Body, ((FunctionType)function.Semantics.ValueType!).Return);
    }

    private void EmitFunctionCommon(string name, ImmutableArray<VarDeclaration> parameters, ImmutableArray<IStatement> body, TypeInfo returnType)
    {
        currentFunctionReturnType = returnType;
        Debug.Assert(returnType.SizeOf <= byte.MaxValue, $"Return type too big (sizeof: {returnType.SizeOf})");

        currentFunctionFrameSize = 0;
        currentFunctionAllocatedLocals.Clear();
        localLabelNameToIndex.Clear();

        // allocate space for parameters
        var argCount = 0;
        foreach (var p in parameters)
        {
            if (p.Kind is VarKind.ScriptParameter) continue; // ScriptParameters are stored as static variables

            var paramSize = p.Semantics.ValueType!.SizeOf;
            AllocateFrameSpaceForVar(p);
            argCount += paramSize;
        }
        Debug.Assert(argCount <= byte.MaxValue, $"Too many parameters (argCount: {argCount})");
        currentFunctionArgCount = (byte)argCount;

        // allocate space required by the engine to store the return address and caller frame address
        AllocateFrameSpace(2);

        // prologue (frame size is not yet known, it is update after emitting the function code)
        var enter = EmitEnter((byte)argCount, frameSize: 0, name);

        // body
        EmitStatementBlock(body);

        // epilogue
        if (body.LastOrDefault() is not ReturnStatement)
        {
            EmitEpilogue();
        }

        // update frame size in ENTER instruction
        instructionToUpdateInNextFlush = enter;
        EmitEnter((byte)argCount, (ushort)currentFunctionFrameSize, name);
    }

    public void EmitEpilogue()
    {
        Debug.Assert(currentFunctionReturnType is not null);
        EmitLeave(currentFunctionArgCount, (byte)currentFunctionReturnType.SizeOf);
    }

    public int AllocateFrameSpace(int size)
    {
        var offset = currentFunctionFrameSize;
        currentFunctionFrameSize += size;
        Debug.Assert(currentFunctionFrameSize <= ushort.MaxValue, $"Function frame size is too big");
        return offset;
    }
    public int AllocateFrameSpaceForVar(VarDeclaration varDecl)
    {
        Debug.Assert(varDecl.Kind is VarKind.Local or VarKind.Parameter);

        if (currentFunctionAllocatedLocals.ContainsKey(varDecl))
        {
            throw new ArgumentException($"Var '{varDecl.Name}' is already allocated", nameof(varDecl));
        }

        var size = varDecl.Semantics.ValueType!.SizeOf;
        var offset = AllocateFrameSpace(size);
        currentFunctionAllocatedLocals.Add(varDecl, offset);
        return offset;
    }

    public void EmitStatementBlock(ImmutableArray<IStatement> statements)
    {
        foreach (var stmt in statements) EmitStatement(stmt);
    }
    public void EmitStatement(IStatement stmt)
    {
        if (stmt.Label is not null)
        {
            Label(stmt.Label.Name);
        }

        stmt.Accept(stmtEmitter);
    }
    public void EmitValue(IExpression expr) => expr.Accept(valueEmitter);
    public void EmitValueAndDrop(IExpression expr)
    {
        EmitValue(expr);
        var valueSize = expr.Semantics.Type!.SizeOf;
        for (int i = 0; i < valueSize; i++)
        {
            EmitDrop();
        }
    }
    public void EmitAddress(IExpression expr) => expr.Accept(addressEmitter);


    public void EmitJump(string label)
    {
        var inst = EmitJ(0);
        ReferenceLabel(label, inst, 1, LabelReferenceKind.Relative, isFunctionLabel: false);
    }
    public void EmitJumpIfZero(string label)
    {
        var inst = EmitJZ(0);
        ReferenceLabel(label, inst, 1, LabelReferenceKind.Relative, isFunctionLabel: false);
    }

    public void EmitCall(FunctionDeclaration function)
    {
        var inst = EmitCall(0);
        ReferenceLabel(function.Name, inst, 1, LabelReferenceKind.Absolute, isFunctionLabel: true);
        OnFunctionFound(function);
    }

    public void EmitFunctionAddress(FunctionDeclaration function)
    {
        var inst = EmitPushConstU24(0);
        ReferenceLabel(function.Name, inst, 1, LabelReferenceKind.Absolute, isFunctionLabel: true);
        OnFunctionFound(function);
    }

    //public void EmitNativeCall(int argsSize, int returnSize, string label) => Emit(Opcode.NATIVE, argsSize, returnSize, label);

    public void EmitSwitch(IEnumerable<ValueSwitchCase> valueCases)
    {
        throw new NotImplementedException(nameof(EmitSwitch));
        var cases = valueCases.ToArray();
        Debug.Assert(cases.Length <= byte.MaxValue, $"Too many SWITCH cases (numCases: {cases.Length})");
        var inst = EmitSwitch((byte)cases.Length);
        for (int i = 0; i < cases.Length; i++)
        {
            var @case = cases[i];
            var valueOffset = 1 + i * 6;
            // TODO: fill value from @case.Value
            var labelOffset = valueOffset + 4;
            ReferenceLabel(@case.Semantics.Label!, inst, labelOffset, LabelReferenceKind.Relative, isFunctionLabel: false);
        }
    }

    public void EmitLoadFrom(IExpression lvalueExpr)
    {
        Debug.Assert(lvalueExpr.Semantics.ValueKind.Is(ValueKind.Addressable));

        var size = lvalueExpr.Type!.SizeOf;
        if (size == 1)
        {
            EmitAddress(lvalueExpr);
            EmitLoad();
        }
        else
        {
            EmitPushInt(size);
            EmitAddress(lvalueExpr);
            EmitLoadN();
        }
    }

    public void EmitStoreAt(IExpression lvalueExpr)
    {
        Debug.Assert(lvalueExpr.Semantics.ValueKind.Is(ValueKind.Addressable));

        var size = lvalueExpr.Type!.SizeOf;
        if (size == 1)
        {
            EmitAddress(lvalueExpr);
            EmitStore();
        }
        else
        {
            EmitPushInt(size);
            EmitAddress(lvalueExpr);
            EmitStoreN();
        }
    }

    public void EmitStoreAt(VarDeclaration varDecl)
    {
        Debug.Assert(varDecl.Kind is VarKind.Local or VarKind.Static or VarKind.Global);

        var size = varDecl.Semantics.ValueType!.SizeOf;
        if (size == 1)
        {
            EmitVarAddress(varDecl);
            EmitStore();
        }
        else
        {
            EmitPushInt(size);
            EmitVarAddress(varDecl);
            EmitStoreN();
        }
    }

    public void EmitArrayIndexing(IndexingExpression expr)
    {
        EmitValue(expr.Index);
        EmitAddress(expr.Array);

        var itemSize = expr.Semantics.Type!.SizeOf;
        switch (itemSize)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                EmitArrayU8((byte)itemSize);
                break;

            case >= ushort.MinValue and <= ushort.MaxValue:
                EmitArrayU16((ushort)itemSize);
                break;

            default:
                Debug.Assert(false, $"Array item size too big (itemSize: {itemSize})");
                break;
        }
    }

    public void EmitOffset(int offset)
    {
        switch (offset)
        {
            case 0:
                // offset doesn't change, don't need to emit anything
                break;

            case >= byte.MinValue and <= byte.MaxValue:
                EmitIOffsetU8((byte)offset);
                break;

            case >= short.MinValue and <= short.MaxValue:
                EmitIOffsetS16((short)offset);
                break;

            default:
                EmitPushInt(offset);
                EmitIOffset();
                break;
        }
    }

    private void EmitGlobalAddress(VarDeclaration declaration)
    {
        throw new NotImplementedException(nameof(EmitGlobalAddress));
        // TODO: EmitGlobal
        //switch (varDecl.Address)
        //{
        //    case >= 0 and <= 0x0000FFFF:
        //        CG.Emit(Opcode.GLOBAL_U16, varDecl.Address);
        //        break;

        //    case >= 0 and <= 0x00FFFFFF:
        //        CG.Emit(Opcode.GLOBAL_U24, varDecl.Address);
        //        break;

        //    default: Debug.Assert(false, "Global var address too big"); break;
        //}
    }
    private void EmitStaticAddress(VarDeclaration declaration)
    {
        throw new NotImplementedException(nameof(EmitStaticAddress));
        // TODO: EmitStaticAddress
        //switch (varDecl.Address)
        //{
        //    case >= 0 and <= 0x000000FF:
        //        CG.Emit(Opcode.STATIC_U8, varDecl.Address);
        //        break;

        //    case >= 0 and <= 0x0000FFFF:
        //        CG.Emit(Opcode.STATIC_U16, varDecl.Address);
        //        break;

        //    default: Debug.Assert(false, "Static var address too big"); break;
        //}
    }
    private void EmitScriptParameterAddress(VarDeclaration declaration) => EmitStaticAddress(declaration);
    private void EmitLocalAddress(VarDeclaration declaration)
    {
        var address = currentFunctionAllocatedLocals[declaration];
        switch (address)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                EmitLocalU8((byte)address);
                break;

            case >= ushort.MinValue and <= ushort.MaxValue:
                EmitLocalU16((ushort)address);
                break;

            default: Debug.Assert(false, "Local var address too big"); break;
        }
    }
    private void EmitParameterAddress(VarDeclaration declaration)
    {
        if (declaration.IsReference)
        {
            // parameter passed by reference, the address is its value
            var paramAddress = currentFunctionAllocatedLocals[declaration];
            switch (paramAddress)
            {
                case >= byte.MinValue and <= byte.MaxValue:
                    EmitLocalU8Load((byte)paramAddress);
                    break;

                case >= ushort.MinValue and <= ushort.MaxValue:
                    EmitLocalU16Load((ushort)paramAddress);
                    break;

                default: Debug.Assert(false, "Parameter address too big"); break;
            }
        }
        else
        {
            // parameter passed by value, treat it as a local variable
            EmitLocalAddress(declaration);
        }
    }

    public void EmitVarAddress(VarDeclaration declaration)
    {
        switch (declaration.Kind)
        {
            case VarKind.Global: EmitGlobalAddress(declaration); break;
            case VarKind.Static: EmitStaticAddress(declaration); break;
            case VarKind.ScriptParameter: EmitScriptParameterAddress(declaration); break;
            case VarKind.Local: EmitLocalAddress(declaration); break;
            case VarKind.Parameter: EmitParameterAddress(declaration); break;

            case VarKind.Constant: Debug.Assert(false, "Cannot get address of constant var"); break;
            case VarKind.Field: Debug.Assert(false, "Cannot get address of field directly"); break;
            default: Debug.Assert(false, "Unknown var kind"); break;
        }
    }

    /// <summary>
    /// Emits code to default initialize a local variable.
    /// </summary>
    public void EmitDefaultInit(VarDeclaration declaration)
    {
        Debug.Assert(declaration.Kind is VarKind.Local);
        EmitLocalAddress(declaration);
        EmitDefaultInitNoPushAddress(declaration.Semantics.ValueType!);
        EmitDrop(); // drop local address
    }

    private void EmitDefaultInitNoPushAddress(TypeInfo type)
    {
        switch (type)
        {
            case StructType ty: EmitDefaultInitStruct(ty); break;
            case ArrayType ty: EmitDefaultInitArray(ty); break;
            default: throw new ArgumentException($"Cannot default initialize type '{type.ToPrettyString()}'", nameof(type));
        }
    }

    private void EmitDefaultInitStruct(StructType structTy)
    {
        foreach (var (field, fieldDecl) in structTy.Fields.Zip(structTy.Declaration.Fields))
        {
            var hasInitializer = fieldDecl.Initializer is not null && !(fieldDecl.Initializer.Type?.IsError ?? true);
            if (hasInitializer || field.Type.IsDefaultInitialized())
            {
                EmitDup(); // duplicate struct address
                EmitOffset(field.Offset); // advance to field offset

                // initialize field
                if (hasInitializer)
                {
                    var initValue = fieldDecl.Semantics.ConstantValue;
                    Debug.Assert(initValue is not null);
                    switch (field.Type)
                    {
                        case IntType or FloatType or BoolType or StringType:
                            EmitPushConst(initValue);
                            EmitStoreRev();
                            break;
                        case VectorType:
                            var (x, y, z) = initValue.VectorValue;
                            EmitDup(); // duplicate VECTOR address
                            EmitPushFloat(x);
                            EmitStoreRev(); // store X
                            EmitOffset(1); // advance to Y offset
                            EmitPushFloat(y);
                            EmitStoreRev(); // store Y
                            EmitOffset(1); // advance to Z offset
                            EmitPushFloat(z);
                            EmitStoreRev(); // store Z
                            EmitDrop();
                            break;

                        default:
                            Debug.Assert(false, "No other type is supported as constant");
                            break;
                    }
                }
                else
                {
                    Debug.Assert(field.Type.IsDefaultInitialized());
                    EmitDefaultInitNoPushAddress(field.Type);
                }

                EmitDrop(); // drop duplicated address
            }
        }
    }

    private void EmitDefaultInitArray(ArrayType arrayTy)
    {
        // write array size
        EmitPushInt(arrayTy.Length);
        EmitStoreRev();

        if (arrayTy.Item.IsDefaultInitialized())
        {
            EmitDup(); // duplicate array address
            EmitOffset(1); // advance duplicated address to the first item (skip array size)
            var itemSize = arrayTy.Item.SizeOf;
            for (int i = 0; i < arrayTy.Length; i++)
            {
                EmitDefaultInitNoPushAddress(arrayTy.Item); // initialize item
                EmitOffset(itemSize); // advance to the next item
            }
            EmitDrop(); // drop duplicated address
        }
    }

    public void EmitCastIntToFloat() => EmitI2F();
    public void EmitCastFloatToInt() => EmitF2I();
    public void EmitCastFloatToVector() => EmitF2V();

    public void EmitPushString(string value)
    {
        var offset = Strings.GetOffsetOf(value);
        EmitPushInt(offset);
        EmitString();
    }

    public void EmitPushNull() => EmitPushInt(0);
    public void EmitPushBool(bool value) => EmitPushInt(value ? 1 : 0);
    public void EmitPushInt(int value)
    {
        switch (value)
        {
            case -1: EmitPushConstM1(); break;
            case 0: EmitPushConst0(); break;
            case 1: EmitPushConst1(); break;
            case 2: EmitPushConst2(); break;
            case 3: EmitPushConst3(); break;
            case 4: EmitPushConst4(); break;
            case 5: EmitPushConst5(); break;
            case 6: EmitPushConst6(); break;
            case 7: EmitPushConst7(); break;

            case >= byte.MinValue and <= byte.MaxValue:
                EmitPushConstU8((byte)value);
                break;

            case >= short.MinValue and <= short.MaxValue:
                EmitPushConstS16((short)value);
                break;

            case >= 0 and <= 0x00FFFFFF:
                EmitPushConstU24(unchecked((uint)value));
                break;

            default:
                EmitPushConstU32(unchecked((uint)value));
                break;
        }
    }

    public void EmitPushFloat(float value)
    {
        switch (value)
        {
            case -1.0f: EmitPushConstFM1(); break;
            case 0.0f: EmitPushConstF0(); break;
            case 1.0f: EmitPushConstF1(); break;
            case 2.0f: EmitPushConstF2(); break;
            case 3.0f: EmitPushConstF3(); break;
            case 4.0f: EmitPushConstF4(); break;
            case 5.0f: EmitPushConstF5(); break;
            case 6.0f: EmitPushConstF6(); break;
            case 7.0f: EmitPushConstF7(); break;
            default: EmitPushConstF(value); break;
        }
    }

    public void EmitPushConst(ConstantValue value)
    {
        value.Match(
            caseNull: EmitPushNull,
            caseInt: EmitPushInt,
            caseFloat: EmitPushFloat,
            caseBool: EmitPushBool,
            caseString: EmitPushString,
            caseVector: (x, y, z) =>
            {
                EmitPushFloat(x);
                EmitPushFloat(y);
                EmitPushFloat(z);
            });
    }
    #endregion Public High-Level Emitters

    public void Label(string name, bool isFunctionLabel = false)
    {
        var nameToIndex = isFunctionLabel ? functionLabelNameToIndex : localLabelNameToIndex;

        var labelInstRef = EmitLabelMarker();
        if (nameToIndex.TryGetValue(name, out var idx))
        {
            if (labels[idx].Instruction is not null)
            {
                // the label appeared before and the references are already resolved, so this is a re-definition
                Debug.Assert(false, $"Label name '{name}' is repeated");
            }

            labels[idx] = labels[idx] with { Instruction = labelInstRef };
        }
        else
        {
            nameToIndex.Add(name, labels.Count);
            labels.Add(new(Instruction: labelInstRef, UnresolvedReferences: new()));
        }
    }

    private void ReferenceLabel(string label, InstructionReference instruction, int operandOffset, LabelReferenceKind kind, bool isFunctionLabel)
    {
        var nameToIndex = isFunctionLabel ? functionLabelNameToIndex : localLabelNameToIndex;

        if (nameToIndex.TryGetValue(label, out var idx))
        {
            labels[idx].UnresolvedReferences.Add(new(instruction, operandOffset, kind));
        }
        else
        {
            // first time this label is referenced
            nameToIndex.Add(label, labels.Count);
            labels.Add(new(Instruction: null, UnresolvedReferences: new() { new(instruction, operandOffset, kind) }));
        }
    }

    private void OnFunctionFound(FunctionDeclaration function)
    {
        if (usedFunctions.Add(function))
        {
            functionsToCompile.Enqueue(function);
        }
    }
}