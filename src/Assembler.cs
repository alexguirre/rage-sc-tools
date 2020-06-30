namespace ScTools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using ScTools.GameFiles;

    internal readonly struct AssemblerOptions
    {
        public bool IncludeFunctionNames { get; }

        public AssemblerOptions(bool includeFunctionNames)
        {
            IncludeFunctionNames = includeFunctionNames;
        }
    }

    internal partial class Assembler
    {
        private const char DirectiveChar = '$';

        private Script sc;
        private readonly AssemblerOptions options;
        private readonly NativeDB nativeDB;
        private readonly List<ulong> nativeHashes = new List<ulong>();
        private Operand[] operandsBuffer = null;
        private CodeBuilder code = null;
        private StringsPagesBuilder strings = null;

        public Assembler(AssemblerOptions options, NativeDB nativeDB)
        {
            this.options = options;
            this.nativeDB = nativeDB;
        }

        public Script Assemble(FileInfo inputFile)
        {
            const string DefaultName = "unknown";

            Script sc = new Script()
            {
                Hash = 0, // TODO: how is this hash calculated?
                ArgsCount = 0,
                StaticsCount = 0,
                GlobalsLengthAndBlock = 0,
                NativesCount = 0,
                Name = DefaultName,
                NameHash = DefaultName.ToHash(),
                StringsLength = 0,
            };

            this.sc = sc;
            Parse(inputFile);
            this.sc = null;
            return sc;
        }

        private void Parse(FileInfo inputFile)
        {
            nativeHashes.Clear();
            operandsBuffer = new Operand[Instruction.MaxOperands];
            code = new CodeBuilder(options);
            strings = new StringsPagesBuilder();

            using TextReader input = new StreamReader(inputFile.OpenRead());
            string line = null;
            int lineNumber = 1;

            while ((line = input.ReadLine()) != null)
            {
                try
                {
                    ParseLine(line);
                }
                catch (Exception e)
                {
                    throw new AssemblerSyntaxException(inputFile, lineNumber, e);
                }

                lineNumber++;
            }

            sc.CodePages = new ScriptPageArray<byte>
            {
                Items = code.ToPages(out uint codeLength),
            };
            sc.CodeLength = codeLength;

            sc.StringsPages = new ScriptPageArray<byte>
            {
                Items = strings.ToPages(out uint stringsLength),
            };
            sc.StringsLength = stringsLength;

            static ulong RotateHash(ulong hash, int index, uint codeLength)
            {
                byte rotate = (byte)(((uint)index + codeLength) & 0x3F);
                return hash >> rotate | hash << (64 - rotate);
            }

            sc.Natives = nativeHashes.Select((h, i) => RotateHash(h, i, codeLength)).ToArray();
            sc.NativesCount = (uint)sc.Natives.Length;

            Debug.Assert(sc.Natives.Select((h, i) => sc.NativeHash(i) == nativeHashes[i]).All(b => b));

            operandsBuffer = null;
            code = null;
            strings = null;
        }

        private void ParseLine(string line)
        {
            ReadOnlySpan<char> l = line;
            l = l.Trim();

            if (l.Length == 0)
            {
                return;
            }

            var tokens = Token.Tokenize(line).GetEnumerator();
            if (tokens.MoveNext())
            {
                bool parsed = ParseDirective(tokens) ||
                              ParseLabel(tokens) ||
                              ParseInstruction(tokens) ||
                              ParseHighLevelInstruction(tokens);

                if (!parsed)
                {
                    throw new ArgumentException($"Unknown syntax '{line}'");
                }
            }
        }

        private bool ParseLabel(TokenEnumerator tokens)
        {
            var token = tokens.Current;

            if (token[^1] == ':') // label
            {

                string lbl = token[0..^1].ToString(); // remove ':'
                code.AddLabel(lbl);

                if (tokens.MoveNext())
                {
                    throw new ArgumentException($"Unknown token '{tokens.Current.ToString()}' after label '{token.ToString()}'");
                }

                return true;
            }

            return false;
        }

        private bool ParseDirective(TokenEnumerator tokens)
        {
            var directive = tokens.Current;

            if (directive[0] != DirectiveChar)
            {
                return false;
            }

            directive = directive.Slice(1); // remove DirectiveChar

            int dirIndex = Directive.Find(directive.ToHash());
            if (dirIndex != -1)
            {
                ref Directive dir = ref Directives[dirIndex];
                dir.Callback(dir, tokens, this);
                return true;
            }
            else
            {
                throw new ArgumentException($"Unknown directive '{DirectiveChar}{directive.ToString()}'");
            }
        }

        private bool ParseInstruction(TokenEnumerator tokens)
        {
            ref readonly var inst = ref Instruction.FindByMnemonic(tokens.Current);
            if (inst.IsValid)
            {
                var operands = inst.Parser(inst, ref tokens, operandsBuffer);

                if (tokens.MoveNext())
                {
                    throw new ArgumentException($"Unknown token '{tokens.Current.ToString()}' after instruction '{inst.Mnemonic}'");
                }

                code.BeginInstruction();
                inst.Assemble(operands, code);
                code.EndInstruction();

                return true;
            }

            return false;
        }

        private bool ParseHighLevelInstruction(TokenEnumerator tokens)
        {
            static void AssemblePushUInt(uint v, CodeBuilder code)
            {
                var inst = v switch
                {
                    0xFFFFFFFF /* -1 */ => (Instruction.PUSH_CONST_M1, Array.Empty<Operand>()),
                    0 => (Instruction.PUSH_CONST_0, Array.Empty<Operand>()),
                    1 => (Instruction.PUSH_CONST_1, Array.Empty<Operand>()),
                    2 => (Instruction.PUSH_CONST_2, Array.Empty<Operand>()),
                    3 => (Instruction.PUSH_CONST_3, Array.Empty<Operand>()),
                    4 => (Instruction.PUSH_CONST_4, Array.Empty<Operand>()),
                    5 => (Instruction.PUSH_CONST_5, Array.Empty<Operand>()),
                    6 => (Instruction.PUSH_CONST_6, Array.Empty<Operand>()),
                    7 => (Instruction.PUSH_CONST_7, Array.Empty<Operand>()),
                    _ when v <= byte.MaxValue => (Instruction.PUSH_CONST_U8, new[] { new Operand((byte)v) }),
                    _ when v <= ushort.MaxValue => (Instruction.PUSH_CONST_S16, new[] { new Operand(unchecked((short)v)) }),
                    _ when v <= 0x00FFFFFF => (Instruction.PUSH_CONST_U24, new[] { new Operand(v, isU24: true) }),
                    _ => (Instruction.PUSH_CONST_U32, new[] { new Operand(v) }),
                };

                code.BeginInstruction();
                inst.Item1.Assemble(inst.Item2, code);
                code.EndInstruction();
            }

            static void AssemblePushString(ReadOnlySpan<char> str, CodeBuilder code, StringsPagesBuilder strings)
            {
                uint strId = strings.Add(str); // TODO: handle repeated strings

                AssemblePushUInt(strId, code);
                code.BeginInstruction();
                Instruction.STRING.Assemble(ReadOnlySpan<Operand>.Empty, code);
                code.EndInstruction();
            }

            // TODO: move these instructions somewhere else
            var mnemonic = tokens.Current;
            if (mnemonic.Equals("PUSH_STRING".AsSpan(), StringComparison.Ordinal))
            {
                var str = tokens.MoveNext() ? tokens.Current : throw new Exception();

                if (!Token.IsString(str, out str))
                {
                    throw new Exception();
                }

                AssemblePushString(str, code, strings);

                if (tokens.MoveNext())
                {
                    throw new ArgumentException($"Unknown token '{tokens.Current.ToString()}' after instruction 'PUSH_STRING'");
                }

                return true;
            }
            else if (mnemonic.Equals("CALL_NATIVE".AsSpan(), StringComparison.Ordinal))
            {
                if (nativeDB == null)
                {
                    throw new InvalidOperationException("A nativeDB is required when using the CALL_NATIVE instruction");
                }

                var nativeName = tokens.MoveNext() ? tokens.Current : throw new Exception();

                string nativeNameStr = nativeName.ToString(); // TODO: optimize
                NativeCommand n = nativeDB.Natives.FirstOrDefault(n => nativeNameStr.Equals(n.Name));

                if (n == default)
                {
                    throw new InvalidOperationException($"Unknown native command '{nativeName.ToString()}'");
                }

                byte paramCount = n.ParameterCount;
                byte returnValueCount = n.ReturnValueCount;
                ushort idx = (ushort)GetNativeIndexOrAdd(n.CurrentHash);

                code.BeginInstruction();
                Instruction.NATIVE.Assemble(new[] { new Operand(paramCount), new Operand(returnValueCount), new Operand(idx) }, code);
                code.EndInstruction();

                if (tokens.MoveNext())
                {
                    throw new ArgumentException($"Unknown token '{tokens.Current.ToString()}' after instruction 'CALL_NATIVE'");
                }

                return true;
            }
            else if (mnemonic.Equals("PUSH".AsSpan(), StringComparison.Ordinal))
            {
                var valueStr = tokens.MoveNext() ? tokens.Current : throw new Exception();

                if (uint.TryParse(valueStr, out uint asUInt))
                {
                    AssemblePushUInt(asUInt, code);
                }
                else if (int.TryParse(valueStr, out int asInt))
                {
                    AssemblePushUInt(unchecked((uint)asInt), code);
                }
                else if (float.TryParse(valueStr, out float asFloat))
                {
                    var inst = asFloat switch
                    {
                        -1.0f => (Instruction.PUSH_CONST_FM1, Array.Empty<Operand>()),
                        0.0f => (Instruction.PUSH_CONST_F0, Array.Empty<Operand>()),
                        1.0f => (Instruction.PUSH_CONST_F1, Array.Empty<Operand>()),
                        2.0f => (Instruction.PUSH_CONST_F2, Array.Empty<Operand>()),
                        3.0f => (Instruction.PUSH_CONST_F3, Array.Empty<Operand>()),
                        4.0f => (Instruction.PUSH_CONST_F4, Array.Empty<Operand>()),
                        5.0f => (Instruction.PUSH_CONST_F5, Array.Empty<Operand>()),
                        6.0f => (Instruction.PUSH_CONST_F6, Array.Empty<Operand>()),
                        7.0f => (Instruction.PUSH_CONST_F7, Array.Empty<Operand>()),
                        _ => (Instruction.PUSH_CONST_F, new[] { new Operand(asFloat) }),
                    };

                    code.BeginInstruction();
                    inst.Item1.Assemble(inst.Item2, code);
                    code.EndInstruction();
                }
                else if (Token.IsString(valueStr, out var asStr))
                {
                    AssemblePushString(asStr, code, strings);
                }

                if (tokens.MoveNext())
                {
                    throw new ArgumentException($"Unknown token '{tokens.Current.ToString()}' after instruction 'CALL_NATIVE'");
                }

                return true;
            }

            return false;
        }

        private void SetName(string name)
        {
            Debug.Assert(sc != null);

            sc.Name = name ?? throw new ArgumentNullException(nameof(name));
            sc.NameHash = name.ToHash();
        }

        private void SetHash(uint hash)
        {
            Debug.Assert(sc != null);

            if (sc.Hash != 0)
            {
                throw new InvalidOperationException("Hash was already set");
            }

            sc.Hash = hash;
        }

        private void SetStaticsCount(uint count)
        {
            Debug.Assert(sc != null);

            if (sc.Statics != null)
            {
                throw new InvalidOperationException("Statics count was already set");
            }

            sc.Statics = new ScriptValue[count];
            sc.StaticsCount = count;
        }

        private void SetStaticValue(uint staticIndex, int value)
        {
            Debug.Assert(sc != null);
            
            if (sc.Statics == null || staticIndex >= sc.Statics.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(staticIndex));
            }

            sc.Statics[staticIndex].AsInt32 = value;
        }

        private void SetStaticValue(uint staticIndex, float value)
        {
            Debug.Assert(sc != null);

            if (sc.Statics == null || staticIndex >= sc.Statics.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(staticIndex));
            }

            sc.Statics[staticIndex].AsFloat = value;
        }

        private void SetGlobals(byte block, uint length)
        {
            Debug.Assert(sc != null);

            if (block >= 64) // limit hardcoded in the game .exe (and max value that fits in GLOBAL_U24* instructions)
            {
                throw new ArgumentOutOfRangeException(nameof(block), "Block is greater than or equal to 64");
            }

            if (sc.GlobalsPages != null)
            {
                throw new InvalidOperationException("Globals already set");
            }

            uint pageCount = (length + 0x3FFF) >> 14;
            var pages = new ScriptPage<ScriptValue>[pageCount];
            for (int i = 0; i < pageCount; i++)
            {
                uint pageSize = i == pageCount - 1 ? (length & (Script.MaxPageLength - 1)) : Script.MaxPageLength;
                pages[i] = new ScriptPage<ScriptValue>() { Data = new ScriptValue[pageSize] };
            }

            sc.GlobalsPages = new ScriptPageArray<ScriptValue> { Items = pages };
            sc.GlobalsBlock = block;
            sc.GlobalsLength = length;
        }

        private ref ScriptValue GetGlobalValue(uint globalId)
        {
            Debug.Assert(sc != null);
            
            if (sc.GlobalsPages == null)
            {
                throw new InvalidOperationException($"Globals block and length are undefined");
            }

            uint globalBlock = globalId >> 18;
            uint pageIndex = (globalId >> 14) & 0xF;
            uint pageOffset = globalId & 0x3FFF;

            if (globalBlock != sc.GlobalsBlock)
            {
                throw new ArgumentException($"Block of global {globalId} (block {globalBlock}) does not match the block of this script (block {sc.GlobalsBlock})");
            }

            if (pageIndex >= sc.GlobalsPages.Count || pageOffset >= sc.GlobalsPages[pageIndex].Data.Length)
            {
                throw new ArgumentOutOfRangeException($"Global {globalId} exceeds the global block length");
            }

            return ref sc.GlobalsPages[pageIndex][pageOffset];
        }

        private void SetGlobalValue(uint globalId, int value) => GetGlobalValue(globalId).AsInt32 = value;
        private void SetGlobalValue(uint globalId, float value) => GetGlobalValue(globalId).AsFloat = value;

        private int AddNative(ulong hash)
        {
            Debug.Assert(sc != null);

            if (nativeHashes.Contains(hash))
            {
                throw new ArgumentException($"Native hash {hash:X16} is repeated", nameof(hash));
            }

            int index = nativeHashes.Count;
            nativeHashes.Add(hash);
            return index;
        }

        private int GetNativeIndexOrAdd(ulong hash)
        {
            for (int i = 0; i < nativeHashes.Count; i++)
            {
                if (nativeHashes[i] == hash)
                {
                    return i;
                }
            }

            return AddNative(hash);
        }

        private void AddString(ReadOnlySpan<char> str)
        {
            Debug.Assert(sc != null);

            strings.Add(str);
        }

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

        private sealed class CodeBuilder : ICodeBuilder
        {
            private readonly AssemblerOptions options;
            private readonly List<byte[]> pages = new List<byte[]>();
            private string currentLabel = null;
            private string currentGlobalLabel = null;
            private readonly Dictionary<string, uint> labels = new Dictionary<string, uint>(); // key = label name, value = IP
            private readonly List<(string TargetLabel, uint IP)> targetLabels = new List<(string, uint)>();
            private readonly List<(string TargetLabel, uint IP)> relativeTargetLabels = new List<(string, uint)>();
            private uint length = 0;
            private readonly List<byte> buffer = new List<byte>();

            private bool inInstruction = false;

            public CodeBuilder(AssemblerOptions options)
            {
                this.options = options;
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

            string ICodeBuilder.Label => currentLabel;
            AssemblerOptions ICodeBuilder.Options => options;
            void ICodeBuilder.U8(byte v) => Add(v);
            void ICodeBuilder.U16(ushort v) => Add(v);
            void ICodeBuilder.U24(uint v) => AddU24(v);
            void ICodeBuilder.U32(uint v) => Add(v);
            void ICodeBuilder.S16(short v) => Add(v);
            void ICodeBuilder.F32(float v) => Add(v);
            void ICodeBuilder.RelativeTarget(string label) => AddRelativeTarget(label);
            void ICodeBuilder.Target(string label) => AddTarget(label);
        }

        private class StringsPagesBuilder
        {
            private readonly List<byte[]> pages = new List<byte[]>();
            private uint length = 0;

            public uint Add(ReadOnlySpan<byte> chars)
            {
                uint strLength = (uint)chars.Length + 1; // + null terminator

                if (strLength >= Script.MaxPageLength / 2)
                {
                    // just a safe threshold for strings of half a page, they could be longer but we don't need them for now
                    throw new ArgumentException("string too long");
                }

                uint pageIndex = length >> 14;
                if (pageIndex >= pages.Count)
                {
                    AddPage();
                }

                uint offset = length & 0x3FFF;
                byte[] page = pages[(int)pageIndex];

                if (offset + strLength > page.Length)
                {
                    // the string doesn't fit in the current page, skip until the next one (page is already zeroed out)
                    length += (uint)page.Length - offset;
                    pageIndex = length >> 14;
                    offset = length & 0x3FFF;
                    AddPage();
                    page = pages[(int)pageIndex];
                }

                chars.CopyTo(page.AsSpan((int)offset));
                page[offset + chars.Length] = 0; // null terminator
                uint id = length;
                length += strLength;
                return id;
            }

            public uint Add(ReadOnlySpan<char> str)
            {
                const int MaxStackLimit = 0x200;
                int byteCount = Encoding.UTF8.GetByteCount(str);
                Span<byte> buffer = byteCount <= MaxStackLimit ? stackalloc byte[byteCount] : new byte[byteCount];
                Encoding.UTF8.GetBytes(str, buffer);
                return Add(buffer);
            }

            public uint Add(string str) => Add(str.AsSpan());

            public ScriptPage<byte>[] ToPages(out uint stringsLength)
            {
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

                stringsLength = length;
                return p;
            }

            private void AddPage() => pages.Add(NewPage());
            private byte[] NewPage(uint size = Script.MaxPageLength) => new byte[size];
        }
    }

    public class AssemblerSyntaxException : Exception
    {
        public FileInfo File { get; }
        public int Line { get; }

        public string UserMessage
        {
            get
            {
                string s = "Syntax error";
                if (InnerException != null)
                {
                    s += $": {InnerException.Message}";
                }

                s += $"{Environment.NewLine}   in {File}:line {Line}";

                return s;
            }
        }

        public AssemblerSyntaxException(FileInfo file, int line, Exception inner)
            : base($"Syntax error{Environment.NewLine}   in {file}:line {line}", inner)
        {
            File = file;
            Line = line;
        }
    }
}
