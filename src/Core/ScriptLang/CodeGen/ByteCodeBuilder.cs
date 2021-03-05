#nullable enable
namespace ScTools.ScriptLang.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;
    using ScTools.ScriptAssembly.CodeGen;
    using ScTools.ScriptLang.Semantics.Symbols;
    using ScTools.ScriptLang.Semantics.Binding;

    // TODO: ByteCodeBuilder is very similar to ScriptAssembly.CodeGen.CodeBuilder, refactor
    public sealed class ByteCodeBuilder : IByteCodeBuilder
    {
        private readonly Compilation compilation;
        private readonly IList<ulong> nativeHashes = new List<ulong>(); // hashes are already translated
        private readonly StringPagesBuilder strings = new StringPagesBuilder();

        private readonly List<byte[]> pages = new List<byte[]>(); // bytecode pages so far
        private uint length = 0; // byte count of all the code

        // bytes of the current instruction
        private readonly List<byte> buffer = new List<byte>();

        private BoundModule? currentModule = null;
        private BoundFunction? currentFunction = null;
        private string? currentLabel = null;

        // functions addresses and labels
        private readonly Dictionary<string, (uint IP, Dictionary<string, uint> Labels)> functions = new Dictionary<string, (uint, Dictionary<string, uint>)>();

        // addresses that need fixup
        private readonly List<(string TargetFunctionName, uint IP)> functionTargets = new List<(string, uint)>();
        private readonly List<(string FunctionName, string TargetLabel, uint IP)> labelTargets = new List<(string, string, uint)>();
        private readonly List<int> functionTargetsInCurrentInstruction = new List<int>(); // index of functionTargets
        private readonly List<int> labelTargetsInCurrentInstruction = new List<int>(); // index of labelTargets

        private bool inInstruction = false;
        private bool InModule => currentModule != null;
        private bool InFunction => InModule && currentFunction != null;

        public ByteCodeBuilder(Compilation compilation)
        {
            this.compilation = compilation;
        }

        public void BeginModule(BoundModule module)
        {
            Debug.Assert(!InModule);

            currentModule = module;
        }

        public void EndModule()
        {
            Debug.Assert(InModule);

            currentModule = null;
        }

        public void BeginFunction(BoundFunction function)
        {
            Debug.Assert(!InFunction);

            currentFunction = function;
            currentLabel = function.Function.Name;
            functions.Add(function.Function.Name, (length, new Dictionary<string, uint>()));
        }

        public void EndFunction()
        {
            Debug.Assert(InFunction);

            currentFunction = null;
        }

        public void Emit(Opcode opcode) => Emit(opcode.Instruction(), Array.Empty<Operand>());
        public void Emit(Opcode opcode, ReadOnlySpan<Operand> operands) => Emit(opcode.Instruction(), operands);

        public void Emit(in Instruction instruction, ReadOnlySpan<Operand> operands)
        {
            Debug.Assert(InFunction);

            BeginInstruction();
            instruction.Assemble(operands, this);
            EndInstruction();
        }

        public void EmitPushInt(int v)
        {
            var inst = v switch
            {
                -1 => (Opcode.PUSH_CONST_M1, Array.Empty<Operand>()),
                0 => (Opcode.PUSH_CONST_0, Array.Empty<Operand>()),
                1 => (Opcode.PUSH_CONST_1, Array.Empty<Operand>()),
                2 => (Opcode.PUSH_CONST_2, Array.Empty<Operand>()),
                3 => (Opcode.PUSH_CONST_3, Array.Empty<Operand>()),
                4 => (Opcode.PUSH_CONST_4, Array.Empty<Operand>()),
                5 => (Opcode.PUSH_CONST_5, Array.Empty<Operand>()),
                6 => (Opcode.PUSH_CONST_6, Array.Empty<Operand>()),
                7 => (Opcode.PUSH_CONST_7, Array.Empty<Operand>()),
                _ when v <= byte.MaxValue => (Opcode.PUSH_CONST_U8, new[] { new Operand(unchecked((uint)v)) }),
                _ when v >= short.MinValue && v <= short.MaxValue => (Opcode.PUSH_CONST_S16, new[] { new Operand(unchecked((uint)v)) }),
                _ when v <= 0x00FFFFFF => (Opcode.PUSH_CONST_U24, new[] { new Operand(unchecked((uint)v)) }),
                _ => (Opcode.PUSH_CONST_U32, new[] { new Operand(unchecked((uint)v)) }),
            };

            Emit(inst.Item1, inst.Item2);
        }

        public void EmitPushUInt(uint v)
        {
            // same as EmitPushInt but without allowing negative values with PUSH_CONST_S16
            var inst = v switch
            {
                0xFFFFFFFF /* -1 */ => (Opcode.PUSH_CONST_M1, Array.Empty<Operand>()),
                0 => (Opcode.PUSH_CONST_0, Array.Empty<Operand>()),
                1 => (Opcode.PUSH_CONST_1, Array.Empty<Operand>()),
                2 => (Opcode.PUSH_CONST_2, Array.Empty<Operand>()),
                3 => (Opcode.PUSH_CONST_3, Array.Empty<Operand>()),
                4 => (Opcode.PUSH_CONST_4, Array.Empty<Operand>()),
                5 => (Opcode.PUSH_CONST_5, Array.Empty<Operand>()),
                6 => (Opcode.PUSH_CONST_6, Array.Empty<Operand>()),
                7 => (Opcode.PUSH_CONST_7, Array.Empty<Operand>()),
                _ when v <= byte.MaxValue => (Opcode.PUSH_CONST_U8, new[] { new Operand(v) }),
                _ when v <= short.MaxValue => (Opcode.PUSH_CONST_S16, new[] { new Operand(v) }),
                _ when v <= 0x00FFFFFF => (Opcode.PUSH_CONST_U24, new[] { new Operand(v) }),
                _ => (Opcode.PUSH_CONST_U32, new[] { new Operand(v) }),
            };

            Emit(inst.Item1, inst.Item2);
        }

        public void EmitPushFloat(float v)
        {
            var inst = v switch
            {
                -1.0f => (Opcode.PUSH_CONST_FM1, Array.Empty<Operand>()),
                0.0f => (Opcode.PUSH_CONST_F0, Array.Empty<Operand>()),
                1.0f => (Opcode.PUSH_CONST_F1, Array.Empty<Operand>()),
                2.0f => (Opcode.PUSH_CONST_F2, Array.Empty<Operand>()),
                3.0f => (Opcode.PUSH_CONST_F3, Array.Empty<Operand>()),
                4.0f => (Opcode.PUSH_CONST_F4, Array.Empty<Operand>()),
                5.0f => (Opcode.PUSH_CONST_F5, Array.Empty<Operand>()),
                6.0f => (Opcode.PUSH_CONST_F6, Array.Empty<Operand>()),
                7.0f => (Opcode.PUSH_CONST_F7, Array.Empty<Operand>()),
                _ => (Opcode.PUSH_CONST_F, new[] { new Operand(v) }),
            };

            Emit(inst.Item1, inst.Item2);
        }

        public void EmitPushString(string v)
        {
            uint strId = strings.AddOrGet(v);
            EmitPushUInt(strId);
            Emit(Opcode.STRING, ReadOnlySpan<Operand>.Empty);
        }

        public void EmitFuncAddr(FunctionSymbol func)
        {
            BeginInstruction();
            InstructionU8((byte)Opcode.PUSH_CONST_U24);
            InstructionFunctionTarget(func.Name);
            EndInstruction();
        }

        private void EmitVarInst(int location, Opcode opcodeU8, Opcode opcodeU16, bool local)
        {
            var v = unchecked((uint)location);
            var inst = v switch
            {
                _ when v <= 0x00FF => (opcodeU8, new[] { new Operand(v) }),
                _ when v <= 0xFFFF => (opcodeU16, new[] { new Operand(v) }),
                _ => throw new InvalidOperationException($"{(local ? "Local" : "Static")} at '{location}' out of bounds")
            };

            Emit(inst.Item1, inst.Item2);
        }

        private void EmitLocal(int location, Opcode opcodeU8, Opcode opcodeU16) => EmitVarInst(location, opcodeU8, opcodeU16, true);
        private void EmitStatic(int location, Opcode opcodeU8, Opcode opcodeU16) => EmitVarInst(location, opcodeU8, opcodeU16, false);
        private void EmitGlobal(int location, Opcode opcodeU16, Opcode opcodeU24)
        {
            var v = unchecked((uint)location);
            var inst = v switch
            {
                _ when v <= 0x00FFFF => (opcodeU16, new[] { new Operand(v) }),
                _ when v <= 0xFFFFFF => (opcodeU24, new[] { new Operand(v) }),
                _ => throw new InvalidOperationException($"Global at '{location}' out of bounds")
            };

            Emit(inst.Item1, inst.Item2);
        }

        public void EmitLocalAddr(int location) => EmitLocal(location, Opcode.LOCAL_U8, Opcode.LOCAL_U16);
        public void EmitLocalLoad(int location) => EmitLocal(location, Opcode.LOCAL_U8_LOAD, Opcode.LOCAL_U16_LOAD);
        public void EmitLocalStore(int location) => EmitLocal(location, Opcode.LOCAL_U8_STORE, Opcode.LOCAL_U16_STORE);

        public void EmitLocalStoreN(int location, int n)
        {
            if (n == 1)
            {
                EmitLocalStore(location);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                EmitLocalAddr(location);
                Emit(Opcode.STORE_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitLocalLoadN(int location, int n)
        {
            if (n == 1)
            {
                EmitLocalLoad(location);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                EmitLocalAddr(location);
                Emit(Opcode.LOAD_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitLocalAddr(VariableSymbol var) => EmitLocalAddr(GetLocalLocation(var));
        public void EmitLocalStore(VariableSymbol var) => EmitLocalStoreN(GetLocalLocation(var), var.Type.SizeOf);
        public void EmitLocalLoad(VariableSymbol var) => EmitLocalLoadN(GetLocalLocation(var), var.Type.SizeOf);

        private int GetLocalLocation(VariableSymbol var)
        {
            Debug.Assert(InFunction);
            Debug.Assert(var.IsLocal);

            var res = currentFunction!.GetLocalLocation(var);
            Debug.Assert(res.HasValue);
            return res.Value;
        }

        public void EmitStaticAddr(int location) => EmitStatic(location, Opcode.STATIC_U8, Opcode.STATIC_U16);
        public void EmitStaticLoad(int location) => EmitStatic(location, Opcode.STATIC_U8_LOAD, Opcode.STATIC_U16_LOAD);
        public void EmitStaticStore(int location) => EmitStatic(location, Opcode.STATIC_U8_STORE, Opcode.STATIC_U16_STORE);

        public void EmitStaticStoreN(int location, int n)
        {
            if (n == 1)
            {
                EmitStaticStore(location);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                EmitStaticAddr(location);
                Emit(Opcode.STORE_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitStaticLoadN(int location, int n)
        {
            if (n == 1)
            {
                EmitStaticLoad(location);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                EmitStaticAddr(location);
                Emit(Opcode.LOAD_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitStaticAddr(VariableSymbol var) => EmitStaticAddr(GetStaticLocation(var));
        public void EmitStaticStore(VariableSymbol var) => EmitStaticStoreN(GetStaticLocation(var), var.Type.SizeOf);
        public void EmitStaticLoad(VariableSymbol var) => EmitStaticLoadN(GetStaticLocation(var), var.Type.SizeOf);

        private int GetStaticLocation(VariableSymbol var)
        {
            Debug.Assert(InModule);
            Debug.Assert(var.IsStatic);

            var res = compilation.GetStaticLocation(var);
            Debug.Assert(res.HasValue);
            return res.Value;
        }

        public void EmitGlobalAddr(int location) => EmitGlobal(location, Opcode.GLOBAL_U16, Opcode.GLOBAL_U24);
        public void EmitGlobalLoad(int location) => EmitGlobal(location, Opcode.GLOBAL_U16_LOAD, Opcode.GLOBAL_U24_LOAD);
        public void EmitGlobalStore(int location) => EmitGlobal(location, Opcode.GLOBAL_U16_STORE, Opcode.GLOBAL_U24_STORE);

        public void EmitGlobalStoreN(int location, int n)
        {
            if (n == 1)
            {
                EmitGlobalStore(location);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                EmitGlobalAddr(location);
                Emit(Opcode.STORE_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitGlobalLoadN(int location, int n)
        {
            if (n == 1)
            {
                EmitGlobalLoad(location);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                EmitGlobalAddr(location);
                Emit(Opcode.LOAD_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitGlobalAddr(VariableSymbol var) => EmitGlobalAddr(GetGlobalLocation(var));
        public void EmitGlobalStore(VariableSymbol var) => EmitGlobalStoreN(GetGlobalLocation(var), var.Type.SizeOf);
        public void EmitGlobalLoad(VariableSymbol var) => EmitGlobalLoadN(GetGlobalLocation(var), var.Type.SizeOf);

        private int GetGlobalLocation(VariableSymbol var)
        {
            Debug.Assert(InModule);
            Debug.Assert(var.IsGlobal);

            var res = compilation.GetGlobalLocation(var);
            Debug.Assert(res.HasValue);
            return res.Value;
        }

        private void EmitOffsetInst(int offset, Opcode opcodeU8, Opcode opcodeS16)
        {
            var v = unchecked((uint)offset);
            var inst = v switch
            {
                var l when l <= byte.MaxValue => (opcodeU8, new[] { new Operand(v) }),
                var l when l <= short.MaxValue => (opcodeS16, new[] { new Operand(v) }),
                // TODO: use IOFFSET for offsets bigger than short.MaxValue
                _ => throw new InvalidOperationException($"{offset} out of bounds")
            };

            Emit(inst.Item1, inst.Item2);
        }

        public void EmitOffsetAddr(int offset) => EmitOffsetInst(offset, Opcode.IOFFSET_U8, Opcode.IOFFSET_S16);
        public void EmitOffsetLoad(int offset) => EmitOffsetInst(offset, Opcode.IOFFSET_U8_LOAD, Opcode.IOFFSET_S16_LOAD);
        public void EmitOffsetStore(int offset) => EmitOffsetInst(offset, Opcode.IOFFSET_U8_STORE, Opcode.IOFFSET_S16_STORE);

        public void EmitOffsetStoreN(int offset, int n, Action emitAddr)
        {
            if (n == 1)
            {
                emitAddr();
                EmitOffsetStore(offset);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                emitAddr();
                EmitOffsetAddr(offset);
                Emit(Opcode.STORE_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitOffsetLoadN(int offset, int n, Action emitAddr)
        {
            if (n == 1)
            {
                emitAddr();
                EmitOffsetLoad(offset);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                emitAddr();
                EmitOffsetAddr(offset);
                Emit(Opcode.LOAD_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        private void EmitArrayInst(int itemSize, Opcode opcodeU8, Opcode opcodeU16)
        {
            var v = unchecked((uint)itemSize);
            var inst = v switch
            {
                var l when l <= byte.MaxValue => (opcodeU8, new[] { new Operand(v) }),
                var l when l <= ushort.MaxValue => (opcodeU16, new[] { new Operand(v) }),
                _ => throw new InvalidOperationException($"{itemSize} out of bounds")
            };

            Emit(inst.Item1, inst.Item2);
        }

        public void EmitArrayAddr(int itemSize) => EmitArrayInst(itemSize, Opcode.ARRAY_U8, Opcode.ARRAY_U16);
        public void EmitArrayLoad(int itemSize) => EmitArrayInst(itemSize, Opcode.ARRAY_U8_LOAD, Opcode.ARRAY_U16_LOAD);
        public void EmitArrayStore(int itemSize) => EmitArrayInst(itemSize, Opcode.ARRAY_U8_STORE, Opcode.ARRAY_U16_STORE);

        public void EmitAddrStoreN(int n, Action emitAddr)
        {
            if (n == 1)
            {
                emitAddr();
                Emit(Opcode.STORE);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                emitAddr();
                Emit(Opcode.STORE_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitAddrLoadN(int n, Action emitAddr)
        {
            if (n == 1)
            {
                emitAddr();
                Emit(Opcode.LOAD);
            }
            else
            {
                EmitPushUInt(unchecked((uint)n));
                emitAddr();
                Emit(Opcode.LOAD_N, ReadOnlySpan<Operand>.Empty);
            }
        }

        public void EmitAddrStoreRev() => Emit(Opcode.STORE_REV);

        public void EmitDrop() => Emit(Opcode.DROP);

        public void EmitCall(DefinedFunctionSymbol function) => Emit(Opcode.CALL, new[] { new Operand(function.Name, OperandType.Identifier) });

        public void EmitIndirectCall() => Emit(Opcode.CALLINDIRECT);

        public void EmitNative(NativeFunctionSymbol function)
        {
            var nativeDB = compilation.NativeDB;
            Debug.Assert(nativeDB != null);

            var hash = nativeDB.FindOriginalHash(function.Name) ?? throw new InvalidOperationException($"Unknown native '{function.Name}'");
            var translatedHash = nativeDB.TranslateHash(hash, GameBuild.Latest);
            if (translatedHash == 0)
            {
                throw new InvalidOperationException($"Unknown native hash '{hash:X16}' ('{function.Name}')");
            }

            byte paramCount = (byte)function.Type.Parameters.Sum(p => p.Type.SizeOf);
            byte returnValueCount = (byte)(function.Type.ReturnType?.SizeOf ?? 0);
            ushort idx = IndexOfNative(translatedHash);
            Emit(Opcode.NATIVE, new[] { new Operand(paramCount), new Operand(returnValueCount), new Operand(idx) });

        }

        private ushort IndexOfNative(ulong translatedHash)
        {
            for (int i = 0; i < nativeHashes.Count; i++)
            {
                if (nativeHashes[i] == translatedHash)
                {
                    return (ushort)i;
                }
            }

            var index = nativeHashes.Count;
            nativeHashes.Add(translatedHash);

            if (nativeHashes.Count > ushort.MaxValue)
            {
                throw new InvalidOperationException("Too many natives");
            }

            return (ushort)index;
        }

        public void EmitJump(string label) => Emit(Opcode.J, new[] { new Operand(label, OperandType.Identifier) });
        public void EmitJumpIfZero(string label) => Emit(Opcode.JZ, new[] { new Operand(label, OperandType.Identifier) });

        public void EmitSwitch(IEnumerable<(int Value, string Label)> cases) => Emit(Opcode.SWITCH, cases.Select(c => new Operand((unchecked((uint)c.Value), c.Label))).ToArray());

        public void EmitPrologue(int localArgsSize, int localsSize)
        {
            // every function needs at least 2 locals (return address + function frame number)
            const uint MinLocals = 2;

            uint argsSize = (uint)localArgsSize;
            uint totalLocalsSize = (uint)localsSize + argsSize + MinLocals;
            Emit(Opcode.ENTER, new[] { new Operand(argsSize), new Operand(totalLocalsSize) });
        }

        public void EmitEpilogue(int localArgsSize, Semantics.Type? returnType)
        {
            uint argsSize = (uint)localArgsSize;
            uint returnSize = (uint)(returnType?.SizeOf ?? 0);
            Emit(Opcode.LEAVE, new[] { new Operand(argsSize), new Operand(returnSize) });
        }

        public void AddLabel(string label)
        {
            Debug.Assert(InFunction);

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Empty label", nameof(label));
            }

            if (!functions[currentFunction!.Function.Name].Labels.TryAdd(label, length))
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
            labelTargets.Add((currentFunction!.Function.Name, label, length + (uint)buffer.Count));
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
                if ((page.Length - offset) > JumpInstructionSize)
                {
                    // if there is enough space for a J instruction, add it to jump to the next page
                    short relIP = (short)(Script.MaxPageLength - (offset + 3)); // get how many bytes until the next page
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

        public uint GetFunctionIP(string function)
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


        public ScriptPage<byte>[] GetStringsPages(out uint stringsLength)
            => strings.ToPages(out stringsLength);
        
        public ulong[] GetUsedNativesEncoded()
        {
            static ulong RotateHash(ulong hash, int index, uint codeLength)
            {
                byte rotate = (byte)(((uint)index + codeLength) & 0x3F);
                return hash >> rotate | hash << (64 - rotate);
            }

            return nativeHashes.Select((h, i) => RotateHash(h, i, length)).ToArray();
        }

        #region IByteCodeBuilder Implementation
        string? IByteCodeBuilder.Label => currentLabel;
        CodeGenOptions IByteCodeBuilder.Options => new CodeGenOptions(includeFunctionNames: true);

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
    }
}
