namespace ScTools.ScriptAssembly
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using ScTools.GameFiles;

    internal static partial class Assembler
    {
        /// <summary>
        /// Defines the interface used for assembling <see cref="Instruction"/>s.
        /// </summary>
        public interface ICodeBuilder
        {
            /// <summary>
            /// The label associated to the current instruction.
            /// </summary>
            public string Label { get; }
            public AssemblerOptions Options { get; }

            public void U8(byte v);
            public void U16(ushort v);
            public void U24(uint v);
            public void U32(uint v);
            public void S16(short v);
            public void F32(float v);
            public void RelativeTarget(string label);
            public void Target(string label);
        }

        /// <summary>
        /// Defines the interface used for assembling <see cref="HighLevelInstruction"/>s.
        /// </summary>
        public interface IHighLevelCodeBuilder
        {
            public NativeDB NativeDB { get; }

            /// <summary>
            /// Assembles a low-level instruction with the specified operands.
            /// </summary>
            /// <param name="inst">The instruction to assemble.</param>
            /// <param name="operands">The operands of the instruction.</param>
            public void Sink(in Instruction inst, ReadOnlySpan<Operand> operands);

            public uint AddOrGetString(ReadOnlySpan<char> str);
            public ushort AddOrGetNative(ulong hash);
        }

        public sealed class CodeBuilder : ICodeBuilder, IHighLevelCodeBuilder
        {
            private readonly AssemblerContext assembler;
            private readonly List<byte[]> pages = new List<byte[]>();
            private string currentLabel = null;
            private string currentGlobalLabel = null;
            private readonly Dictionary<string, uint> labels = new Dictionary<string, uint>(); // key = label name, value = IP
            private readonly List<(string TargetLabel, uint IP)> targetLabels = new List<(string, uint)>();
            private readonly List<(string TargetLabel, uint IP)> relativeTargetLabels = new List<(string, uint)>();
            private uint length = 0;
            private readonly List<byte> buffer = new List<byte>();

            private bool inInstruction = false;

            public CodeBuilder(AssemblerContext assembler)
            {
                this.assembler = assembler;
            }

            private bool IsLocalLabel(string label) => label[0] == '.';

            private string NormalizeLabelName(string label)
            {
                if (IsLocalLabel(label))
                {
                    Debug.Assert(currentGlobalLabel != null);
                    label = currentGlobalLabel + label; // prepend the name of the global label
                }

                return label;
            }

            public void AddLabel(string label)
            {
                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new ArgumentException("Empty label", nameof(label));
                }

                bool isGlobal = true;
                if (IsLocalLabel(label))
                {
                    if (currentGlobalLabel == null)
                    {
                        throw new InvalidOperationException($"Cannot define local label '{label}' without a previous global label");
                    }

                    isGlobal = false;
                }

                label = NormalizeLabelName(label);

                if (!labels.TryAdd(label, length))
                {
                    throw new InvalidOperationException($"Label '{label}' is repeated");
                }

                currentLabel = label;
                if (isGlobal)
                {
                    currentGlobalLabel = label;
                }
            }

            public void BeginInstruction()
            {
                Debug.Assert(!inInstruction);

                buffer.Clear();
                inInstruction = true;
            }

            public void Add(byte v)
            {
                Debug.Assert(inInstruction);

                buffer.Add(v);

                Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");
            }

            public void Add(ushort v)
            {
                Debug.Assert(inInstruction);

                buffer.Add((byte)(v & 0xFF));
                buffer.Add((byte)(v >> 8));

                Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");
            }

            public void Add(short v)
            {
                Debug.Assert(inInstruction);

                buffer.Add((byte)(v & 0xFF));
                buffer.Add((byte)(v >> 8));

                Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");
            }

            public void Add(uint v)
            {
                Debug.Assert(inInstruction);

                buffer.Add((byte)(v & 0xFF));
                buffer.Add((byte)((v >> 8) & 0xFF));
                buffer.Add((byte)((v >> 16) & 0xFF));
                buffer.Add((byte)(v >> 24));

                Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");
            }

            public void AddU24(uint v)
            {
                Debug.Assert(inInstruction);

                buffer.Add((byte)(v & 0xFF));
                buffer.Add((byte)((v >> 8) & 0xFF));
                buffer.Add((byte)((v >> 16) & 0xFF));

                Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");
            }

            public unsafe void Add(float v) => Add(*(uint*)&v);

            public void AddTarget(string label)
            {
                Debug.Assert(inInstruction);

                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new ArgumentException("null or empty label", nameof(label));
                }

                label = NormalizeLabelName(label);

                // TODO: what happens if this is done in the page boundary where the instruction doesn't fit?
                // the IP value may no longer match the instruction position and FixupTargetLabels will write the address in the wrong position
                targetLabels.Add((label, length + (uint)buffer.Count));
                buffer.Add(0);
                buffer.Add(0);
                buffer.Add(0);

                Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");
            }

            public void AddRelativeTarget(string label)
            {
                Debug.Assert(inInstruction);

                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new ArgumentException("null or empty label", nameof(label));
                }

                label = NormalizeLabelName(label);

                // TODO: what happens if this is done in the page boundary where the instruction doesn't fit?
                // the IP value may no longer match the instruction position and FixupRelativeTargetLabels will write the address in the wrong position
                relativeTargetLabels.Add((label, length + (uint)buffer.Count));
                buffer.Add(0);
                buffer.Add(0);

                Debug.Assert(buffer.Count < Script.MaxPageLength / 2, "instruction too long");
            }

            public void EndInstruction()
            {
                Debug.Assert(inInstruction);

                uint pageIndex = length >> 14;
                if (pageIndex >= pages.Count)
                {
                    AddPage();
                }

                uint offset = length & 0x3FFF;
                byte[] page = pages[(int)pageIndex];

                if (offset + buffer.Count > page.Length)
                {
                    // the instruction doesn't fit in the current page, skip until the next one (page is already zeroed out/filled with NOPs)
                    length += (uint)page.Length - offset;
                    pageIndex = length >> 14;
                    offset = length & 0x3FFF;
                    AddPage();
                    page = pages[(int)pageIndex];
                }

                buffer.CopyTo(page, (int)offset);
                length += (uint)buffer.Count;
                currentLabel = null;

                inInstruction = false;
            }

            private uint GetLabelIP(string label)
            {
                if (!labels.TryGetValue(label, out uint ip))
                {
                    throw new ArgumentException($"Unknown label '{label}'", label);
                }

                return ip;
            }

            private void FixupTargetLabels()
            {
                foreach (var (targetLabel, targetIP) in targetLabels)
                {
                    uint ip = GetLabelIP(targetLabel);

                    byte[] targetPage = pages[(int)(targetIP >> 14)];
                    uint targetOffset = targetIP & 0x3FFF;

                    targetPage[targetOffset + 0] = (byte)(ip & 0xFF);
                    targetPage[targetOffset + 1] = (byte)((ip >> 8) & 0xFF);
                    targetPage[targetOffset + 2] = (byte)(ip >> 16);
                }

                targetLabels.Clear();
            }

            private void FixupRelativeTargetLabels()
            {
                foreach (var (targetLabel, targetIP) in relativeTargetLabels)
                {
                    uint ip = GetLabelIP(targetLabel);

                    byte[] targetPage = pages[(int)(targetIP >> 14)];
                    uint targetOffset = targetIP & 0x3FFF;

                    short relIP = (short)((int)ip - (int)(targetIP + 2));
                    targetPage[targetOffset + 0] = (byte)(relIP & 0xFF);
                    targetPage[targetOffset + 1] = (byte)(relIP >> 8);
                }

                relativeTargetLabels.Clear();
            }

            public ScriptPage<byte>[] ToPages(out uint codeLength)
            {
                FixupTargetLabels();
                FixupRelativeTargetLabels();

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

            #region ICodeBuilder Implementation
            string ICodeBuilder.Label => currentLabel;
            AssemblerOptions ICodeBuilder.Options => assembler.Options;

            void ICodeBuilder.U8(byte v) => Add(v);
            void ICodeBuilder.U16(ushort v) => Add(v);
            void ICodeBuilder.U24(uint v) => AddU24(v);
            void ICodeBuilder.U32(uint v) => Add(v);
            void ICodeBuilder.S16(short v) => Add(v);
            void ICodeBuilder.F32(float v) => Add(v);
            void ICodeBuilder.RelativeTarget(string label) => AddRelativeTarget(label);
            void ICodeBuilder.Target(string label) => AddTarget(label);
            #endregion // ICodeBuilder Implementation

            #region IHighLevelCodeBuilder Implementation
            NativeDB IHighLevelCodeBuilder.NativeDB => assembler.NativeDB;

            void IHighLevelCodeBuilder.Sink(in Instruction inst, ReadOnlySpan<Operand> operands)
            {
                BeginInstruction();
                inst.Assemble(operands, this);
                EndInstruction();
            }

            uint IHighLevelCodeBuilder.AddOrGetString(ReadOnlySpan<char> str) => assembler.AddOrGetString(str);
            ushort IHighLevelCodeBuilder.AddOrGetNative(ulong hash) => assembler.AddOrGetNative(hash);
            #endregion // IHighLevelCodeBuilder Implementation
        }
    }
}
