namespace ScTools.ScriptAssembly.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Definitions;

    public class CodeBuilder : IByteCodeBuilder, IHighLevelCodeBuilder
    {
        private readonly List<byte[]> pages = new List<byte[]>(); // bytecode pages so far
        private uint length = 0; // byte count of all the code
        
        // bytes of the current instruction
        private readonly List<byte> buffer = new List<byte>();

        private FunctionDefinition currentFunction = null;
        private string currentLabel = null;
        
        // functions addresses and labels
        private readonly Dictionary<string, (uint IP, Dictionary<string, uint> Labels)> functions = new Dictionary<string, (uint, Dictionary<string, uint>)>();
        
        // addresses that need fixup
        private readonly List<(string TargetFunctionName, uint IP)> functionTargets = new List<(string, uint)>();
        private readonly List<(string FunctionName, string TargetLabel, uint IP)> labelTargets = new List<(string, string, uint)>();
        private readonly List<int> functionTargetsInCurrentInstruction = new List<int>(); // index of functionTargets
        private readonly List<int> labelTargetsInCurrentInstruction = new List<int>(); // index of labelTargets

        private bool inInstruction = false;
        private bool InFunction => currentFunction != null;

        private readonly AssemblerContext context;

        public CodeBuilder(AssemblerContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void BeginFunction(FunctionDefinition function)
        {
            Debug.Assert(!InFunction);

            currentFunction = function ?? throw new ArgumentNullException(nameof(currentFunction));
            currentLabel = function.Name;
            functions.Add(function.Name, (length, new Dictionary<string, uint>()));

            if (!currentFunction.Naked)
            {
                EmitPrologue();
            }
        }

        public void EndFunction()
        {
            Debug.Assert(InFunction);

            if (!currentFunction.Naked)
            {
                EmitEpilogue();
            }

            currentFunction = null;
        }

        private uint LocalOffset(string name)
        {
            Debug.Assert(InFunction);

            if (currentFunction.Naked)
            {
                throw new InvalidOperationException("Named locals are not available in naked functions");
            }

            uint offset = 0;

            // look in function args
            for (int i = 0; i < currentFunction.Args.Length; i++)
            {
                if (currentFunction.Args[i].Name == name)
                {
                    return offset;
                }

                offset += currentFunction.Args[i].Type.SizeOf;
            }

            offset += 2; // see MinLocals in EmitPrologue

            // look in function locals
            for (int i = 0; i < currentFunction.Locals.Length; i++)
            {
                if (currentFunction.Locals[i].Name == name)
                {
                    return offset;
                }

                offset += currentFunction.Locals[i].Type.SizeOf;
            }

            throw new ArgumentException($"Unknown local '{name}'");
        }

        public void Emit(Opcode opcode, ReadOnlySpan<Operand> operands) => Emit(opcode.Instruction(), operands);

        public void Emit(in Instruction instruction, ReadOnlySpan<Operand> operands)
        {
            Debug.Assert(InFunction);

            BeginInstruction();
            instruction.Assemble(operands, this);
            EndInstruction();
        }

        private void EmitPrologue()
        {
            // TODO: verify, all ENTER instructions seem to have at least 2 locals (for return address + something else?)
            const uint MinLocals = 2;

            uint argsSize = (uint)currentFunction.Args.Sum(a => a.Type.SizeOf);
            uint localsSize = (uint)currentFunction.Locals.Sum(l => l.Type.SizeOf) + argsSize + MinLocals;
            Emit(Opcode.ENTER, new[] { new Operand(argsSize), new Operand(localsSize) });
        }

        private void EmitEpilogue()
        {
            uint argsSize = (uint)currentFunction.Args.Sum(a => a.Type.SizeOf);
            uint returnSize = currentFunction.ReturnType?.SizeOf ?? 0;
            Emit(Opcode.LEAVE, new[] { new Operand(argsSize), new Operand(returnSize) });
        }

        public void AddLabel(string label)
        {
            Debug.Assert(InFunction);

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Empty label", nameof(label));
            }

            if (!functions[currentFunction.Name].Labels.TryAdd(label, length))
            {
                throw new InvalidOperationException($"Label '{label}' is repeated");
            }

            currentLabel = label;
        }

        public void BeginInstruction()
        {
            Debug.Assert(InFunction && !inInstruction);

            buffer.Clear();
            inInstruction = true;
        }

        [Conditional("DEBUG")] private void InstructionPreCondition() => Debug.Assert(InFunction && inInstruction);
        [Conditional("DEBUG")] private void InstructionPostCondition() => Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");

        public void InstructionU8(byte v)
        {
            InstructionPreCondition();

            buffer.Add(v);

            InstructionPostCondition();
        }

        public void InstructionU16(ushort v)
        {
            InstructionPreCondition();

            buffer.Add((byte)(v & 0xFF));
            buffer.Add((byte)(v >> 8));

            InstructionPostCondition();
        }

        public void InstructionS16(short v) => InstructionU16(unchecked((ushort)v));

        public void InstructionU32(uint v)
        {
            InstructionPreCondition();

            buffer.Add((byte)(v & 0xFF));
            buffer.Add((byte)((v >> 8) & 0xFF));
            buffer.Add((byte)((v >> 16) & 0xFF));
            buffer.Add((byte)(v >> 24));

            InstructionPostCondition();
        }

        public void InstructionU24(uint v)
        {
            InstructionPreCondition();

            buffer.Add((byte)(v & 0xFF));
            buffer.Add((byte)((v >> 8) & 0xFF));
            buffer.Add((byte)((v >> 16) & 0xFF));

            InstructionPostCondition();
        }

        public unsafe void InstructionF32(float v) => InstructionU32(*(uint*)&v);

        public void InstructionFunctionTarget(string function)
        {
            InstructionPreCondition();

            if (string.IsNullOrWhiteSpace(function))
            {
                throw new ArgumentException("null or empty function name", nameof(function));
            }

            functionTargetsInCurrentInstruction.Add(functionTargets.Count);
            functionTargets.Add((function, length + (uint)buffer.Count));
            buffer.Add(0);
            buffer.Add(0);
            buffer.Add(0);

            InstructionPostCondition();
        }

        public void InstructionLabelTarget(string label)
        {
            InstructionPreCondition();

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("null or empty label", nameof(label));
            }

            labelTargetsInCurrentInstruction.Add(labelTargets.Count);
            labelTargets.Add((currentFunction.Name, label, length + (uint)buffer.Count));
            buffer.Add(0);
            buffer.Add(0);

            InstructionPostCondition();
        }

        public void EndInstruction()
        {
            Debug.Assert(InFunction && inInstruction);
            Debug.Assert(buffer.Count > 0);

            uint pageIndex = length >> 14;
            if (pageIndex >= pages.Count)
            {
                AddPage();
            }

            uint offset = length & 0x3FFF;
            byte[] page = pages[(int)pageIndex];

            Opcode opcode = (Opcode)buffer[0];

            // At page boundary a NOP may be required for the interpreter to switch to the next page,
            // the interpreter only does this with control flow instructions and NOP
            // If the NOP is needed, skip 1 byte at the end of the page
            bool needsNopAtBoundary = !opcode.IsControlFlow() &&
                                      opcode != Opcode.NOP;

            if (offset + buffer.Count > (page.Length - (needsNopAtBoundary ? 1 : 0))) // the instruction doesn't fit in the current page
            {
                const uint JumpInstructionSize = 3;
                if (buffer.Count > JumpInstructionSize)
                {
                    // if there is enough space for a J instruction, add it to jump to the next page
                    uint jumpIP = Script.MaxPageLength * (pageIndex + 1);
                    short relIP = (short)((int)jumpIP - (int)(offset + 3));
                    page[offset + 0] = (byte)Opcode.J; // NOTE: cannot use Emit here because we are still assembling an instruction
                    page[offset + 1] = (byte)(relIP & 0xFF);
                    page[offset + 2] = (byte)(relIP >> 8);
                }

                // skip what is left of the current page (page is already zeroed out/filled with NOPs)
                uint offsetAdded = (uint)page.Length - offset;
                length += offsetAdded;

                // fix IPs of label/function targets
                foreach (int i in functionTargetsInCurrentInstruction)
                {
                    functionTargets[i] = (functionTargets[i].TargetFunctionName, functionTargets[i].IP + offsetAdded);
                }
                foreach (int i in labelTargetsInCurrentInstruction)
                {
                    labelTargets[i] = (labelTargets[i].FunctionName, labelTargets[i].TargetLabel, labelTargets[i].IP + offsetAdded);
                }

                // add the new page
                pageIndex = length >> 14;
                offset = length & 0x3FFF;
                AddPage();
                page = pages[(int)pageIndex];
            }

            buffer.CopyTo(page, (int)offset);
            length += (uint)buffer.Count;

            currentLabel = null;
            functionTargetsInCurrentInstruction.Clear();
            labelTargetsInCurrentInstruction.Clear();
            inInstruction = false;
        }

        private uint GetFunctionIP(string function)
        {
            if (!functions.TryGetValue(function, out var data))
            {
                throw new ArgumentException($"Unknown function '{function}'", function);
            }

            return data.IP;
        }

        private uint GetLabelIP(string functionName, string label)
        {
            var labels = functions[functionName].Labels;
            if (!labels.TryGetValue(label, out uint ip))
            {
                throw new ArgumentException($"Unknown label '{label}'", label);
            }

            return ip;
        }

        private void FixupFunctionTargets()
        {
            foreach (var (targetLabel, targetIP) in functionTargets)
            {
                uint ip = GetFunctionIP(targetLabel);

                byte[] targetPage = pages[(int)(targetIP >> 14)];
                uint targetOffset = targetIP & 0x3FFF;

                targetPage[targetOffset + 0] = (byte)(ip & 0xFF);
                targetPage[targetOffset + 1] = (byte)((ip >> 8) & 0xFF);
                targetPage[targetOffset + 2] = (byte)(ip >> 16);
            }

            functionTargets.Clear();
        }

        private void FixupLabelTargets()
        {
            foreach (var (func, targetLabel, targetIP) in labelTargets)
            {
                uint ip = GetLabelIP(func, targetLabel);

                byte[] targetPage = pages[(int)(targetIP >> 14)];
                uint targetOffset = targetIP & 0x3FFF;

                short relIP = (short)((int)ip - (int)(targetIP + 2));
                targetPage[targetOffset + 0] = (byte)(relIP & 0xFF);
                targetPage[targetOffset + 1] = (byte)(relIP >> 8);
            }

            labelTargets.Clear();
        }

        public ScriptPage<byte>[] ToPages(out uint codeLength)
        {
            FixupFunctionTargets();
            FixupLabelTargets();

            var p = new ScriptPage<byte>[pages.Count];
            if (pages.Count > 0)
            {
                for (int i = 0; i < pages.Count - 1; i++)
                {
                    p[i] = new ScriptPage<byte> { Data = NewPage() };
                    pages[i].CopyTo(p[i].Data, 0);
                }

                p[^1] = new ScriptPage<byte> { Data = NewPage(length & 0x3FFF) };
                Array.Copy(pages[^1], p[^1].Data, p[^1].Data.Length);
            }

            codeLength = length;
            return p;
        }

        private void AddPage() => pages.Add(NewPage());
        private byte[] NewPage(uint size = Script.MaxPageLength) => new byte[size];

        #region IByteCodeBuilder Implementation
        string IByteCodeBuilder.Label => currentLabel;
        CodeGenOptions IByteCodeBuilder.Options => context.CodeGenOptions;

        void IByteCodeBuilder.Opcode(Opcode v) => InstructionU8((byte)v);
        void IByteCodeBuilder.U8(byte v) => InstructionU8(v);
        void IByteCodeBuilder.U16(ushort v) => InstructionU16(v);
        void IByteCodeBuilder.U24(uint v) => InstructionU24(v);
        void IByteCodeBuilder.U32(uint v) => InstructionU32(v);
        void IByteCodeBuilder.S16(short v) => InstructionS16(v);
        void IByteCodeBuilder.F32(float v) => InstructionF32(v);
        void IByteCodeBuilder.LabelTarget(string label) => InstructionLabelTarget(label);
        void IByteCodeBuilder.FunctionTarget(string function) => InstructionFunctionTarget(function);
        #endregion // IByteCodeBuilder Implementation

        #region IHighLevelCodeBuilder Implementation
        NativeDB IHighLevelCodeBuilder.NativeDB => context.NativeDB;
        CodeGenOptions IHighLevelCodeBuilder.Options => context.CodeGenOptions;
        Registry IHighLevelCodeBuilder.Symbols => context.Symbols;

        void IHighLevelCodeBuilder.Emit(Opcode opcode, ReadOnlySpan<Operand> operands) => Emit(opcode, operands);

        uint IHighLevelCodeBuilder.AddOrGetString(ReadOnlySpan<char> str) => context.AddOrGetString(str);
        ushort IHighLevelCodeBuilder.AddOrGetNative(ulong hash) => context.AddOrGetNative(hash);

        uint IHighLevelCodeBuilder.GetStaticOffset(string name) => context.GetStaticOffset(name);
        uint IHighLevelCodeBuilder.GetLocalOffset(string name) => LocalOffset(name);
        #endregion // IHighLevelCodeBuilder Implementation
    }
}
