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
            operandsBuffer = new Operand[Math.Max(Instruction.MaxOperands, HighLevelInstruction.MaxOperands)];
            code = new CodeBuilder(this);
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
            ref readonly var inst = ref HighLevelInstruction.FindByMnemonic(tokens.Current);
            if (inst.IsValid)
            {
                var operands = inst.Parser(inst, ref tokens, operandsBuffer);

                if (tokens.MoveNext())
                {
                    throw new ArgumentException($"Unknown token '{tokens.Current.ToString()}' after instruction '{inst.Mnemonic}'");
                }

                inst.Assemble(operands, code);

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

        private ushort AddNative(ulong hash)
        {
            Debug.Assert(sc != null);

            if (nativeHashes.Contains(hash))
            {
                throw new ArgumentException($"Native hash {hash:X16} is repeated", nameof(hash));
            }

            int index = nativeHashes.Count;
            nativeHashes.Add(hash);

            if (nativeHashes.Count > ushort.MaxValue)
            {
                throw new InvalidOperationException("Too many natives");
            }

            return (ushort)index;
        }

        private ushort AddOrGetNative(ulong hash)
        {
            for (int i = 0; i < nativeHashes.Count; i++)
            {
                if (nativeHashes[i] == hash)
                {
                    return (ushort)i;
                }
            }

            return AddNative(hash);
        }

        private uint AddString(ReadOnlySpan<char> str) => strings.Add(str);
        private uint AddOrGetString(ReadOnlySpan<char> str) => strings.Add(str); // TODO: handle repeated strings

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
