namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Linq;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Definitions;
    using ScTools.ScriptAssembly.Grammar;
    using ScTools.ScriptAssembly.Grammar.Visitors;

    internal readonly struct AssemblerOptions
    {
        public bool IncludeFunctionNames { get; }

        public AssemblerOptions(bool includeFunctionNames)
        {
            IncludeFunctionNames = includeFunctionNames;
        }
    }

    internal sealed class AssemblerContext
    {
        private readonly Script sc;
        private bool nameSet = false;

        public AssemblerOptions Options { get; }
        public NativeDB NativeDB { get; }
        public Assembler.CodeBuilder Code { get; }
        public Assembler.StringsPagesBuilder Strings { get; } = new Assembler.StringsPagesBuilder();
        public Operand[] OperandsBuffer { get; } = new Operand[Math.Max(Instruction.MaxOperands, HighLevelInstruction.MaxOperands)];
        public IList<ulong> NativeHashes { get; } = new List<ulong>();

        public AssemblerContext(Script sc)
        {
            this.sc = sc ?? throw new ArgumentNullException(nameof(sc));
            Code = new Assembler.CodeBuilder(this);
        }

        public void SetName(string name)
        {
            if (nameSet)
            {
                throw new InvalidOperationException("Name was already set");
            }

            sc.Name = name ?? throw new ArgumentNullException(nameof(name));
            sc.NameHash = name.ToHash();
            nameSet = true;
        }

        public void SetHash(uint hash)
        {
            if (sc.Hash != 0)
            {
                throw new InvalidOperationException("Hash was already set");
            }

            sc.Hash = hash;
        }

        public void SetStaticsCount(uint count)
        {
            if (sc.Statics != null)
            {
                throw new InvalidOperationException("Statics count was already set");
            }

            sc.Statics = new ScriptValue[count];
            sc.StaticsCount = count;
        }

        public void SetStaticValue(uint staticIndex, int value)
        {
            if (sc.Statics == null || staticIndex >= sc.Statics.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(staticIndex));
            }

            sc.Statics[staticIndex].AsInt32 = value;
        }

        public void SetStaticValue(uint staticIndex, float value)
        {
            if (sc.Statics == null || staticIndex >= sc.Statics.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(staticIndex));
            }

            sc.Statics[staticIndex].AsFloat = value;
        }

        public void SetGlobals(byte block, uint length)
        {
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

        public void SetGlobalValue(uint globalId, int value) => GetGlobalValue(globalId).AsInt32 = value;
        public void SetGlobalValue(uint globalId, float value) => GetGlobalValue(globalId).AsFloat = value;

        public ushort AddNative(ulong hash)
        {
            if (NativeHashes.Contains(hash))
            {
                throw new ArgumentException($"Native hash {hash:X16} is repeated", nameof(hash));
            }

            int index = NativeHashes.Count;
            NativeHashes.Add(hash);

            if (NativeHashes.Count > ushort.MaxValue)
            {
                throw new InvalidOperationException("Too many natives");
            }

            return (ushort)index;
        }

        public ushort AddOrGetNative(ulong hash)
        {
            for (int i = 0; i < NativeHashes.Count; i++)
            {
                if (NativeHashes[i] == hash)
                {
                    return (ushort)i;
                }
            }

            return AddNative(hash);
        }

        public uint AddString(ReadOnlySpan<char> str) => Strings.Add(str);
        public uint AddOrGetString(ReadOnlySpan<char> str) => Strings.Add(str); // TODO: handle repeated strings
    }

    internal static partial class Assembler
    {
        public static Script Assemble(string input)
        {
            AntlrInputStream inputStream = new AntlrInputStream(input);

            ScAsmLexer lexer = new ScAsmLexer(inputStream);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            ScAsmParser parser = new ScAsmParser(tokens);

            //Console.WriteLine(parser.script().ToStringTree());
            return parser.script().Accept(new ScriptVisitor());
        }

        private sealed class ScriptVisitor : ScAsmBaseVisitor<Script>
        {
            public override Script VisitScript([NotNull] ScAsmParser.ScriptContext context)
            {
                const string DefaultName = "unknown";

                var sc = new Script
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

                var assemblerContext = new AssemblerContext(sc);
                var directiveVisitor = new DirectiveVisitor(assemblerContext);
                var functionVisitor = new FunctionVisitor(assemblerContext);
                var structVisitor = new StructVisitor(assemblerContext);
                var staticFieldVisitor = new StaticFieldVisitor(assemblerContext);

                //var statements = context.statement();
                //foreach (var s in statements)
                //{
                //    _ = s switch
                //    {
                //        _ when s.directive() != null => s.directive().Accept(directiveVisitor),
                //        _ when s.function() != null => s.function().Accept(functionVisitor),
                //        _ when s.@struct() != null => s.@struct().Accept(structVisitor),
                //        _ when s.staticFieldDecl() != null => s.staticFieldDecl().Accept(staticFieldVisitor),
                //        _ => false,
                //    };
                //}

                Registry reg = new Registry();
                RegisterStructs.Visit(context, reg);
                RegisterStaticFields.Visit(context, reg);
                ;

                sc.CodePages = new ScriptPageArray<byte>
                {
                    Items = assemblerContext.Code.ToPages(out uint codeLength),
                };
                sc.CodeLength = codeLength;

                sc.StringsPages = new ScriptPageArray<byte>
                {
                    Items = assemblerContext.Strings.ToPages(out uint stringsLength),
                };
                sc.StringsLength = stringsLength;

                static ulong RotateHash(ulong hash, int index, uint codeLength)
                {
                    byte rotate = (byte)(((uint)index + codeLength) & 0x3F);
                    return hash >> rotate | hash << (64 - rotate);
                }

                sc.Natives = assemblerContext.NativeHashes.Select((h, i) => RotateHash(h, i, codeLength)).ToArray();
                sc.NativesCount = (uint)sc.Natives.Length;

                return sc;
            }
        }

        private sealed class DirectiveVisitor : ScAsmBaseVisitor<bool>
        {
            private readonly AssemblerContext assemblerContext;

            public DirectiveVisitor(AssemblerContext assemblerContext) => this.assemblerContext = assemblerContext ?? throw new ArgumentNullException(nameof(assemblerContext));

            public override bool VisitDirective([NotNull] ScAsmParser.DirectiveContext context)
            {
                Console.WriteLine("Directive\t'{0}'", context.identifier().GetText());

                return true;

                //string directive = context.identifier().GetText();
                //int dirIndex = Directives.Find(directive.ToHash());

                //if (dirIndex != -1)
                //{
                //    Operand[] operands = context.operandList().Accept(OperandListToArray.Instance);
                //    Directive dir = Directives.Set[dirIndex];
                //    dir.Callback(dir, assemblerContext, operands);
                //    return true;
                //}
                //else
                //{
                //    throw new ArgumentException($"Unknown directive '{directive}'");
                //}
            }
        }

        private class Function
        {
            public string Name { get; }
            public bool Naked { get; }
        }

        private sealed class FunctionVisitor : ScAsmBaseVisitor<bool>
        {
            private readonly AssemblerContext assemblerContext;

            public FunctionVisitor(AssemblerContext assemblerContext) => this.assemblerContext = assemblerContext ?? throw new ArgumentNullException(nameof(assemblerContext));

            public override bool VisitFunction([NotNull] ScAsmParser.FunctionContext context)
            {
                Console.WriteLine("Function\t'{0}'", context.identifier().GetText());
                Console.WriteLine("\tArgs:");
                foreach (var a in context.functionArgList()?.fieldDecl() ?? Enumerable.Empty<ScAsmParser.FieldDeclContext>())
                {
                    Console.WriteLine("\t\t{0} {1}", a.identifier().GetText(), a.type().GetText());
                }
                Console.WriteLine("\tLocals:");
                foreach (var l in context.functionLocalDecl().Select(d => d.fieldDecl()))
                {
                    Console.WriteLine("\t\t{0} {1}", l.identifier().GetText(), l.type().GetText());
                }

                return true;
            }
        }

        private sealed class StructVisitor : ScAsmBaseVisitor<bool>
        {
            private readonly AssemblerContext assemblerContext;

            public StructVisitor(AssemblerContext assemblerContext) => this.assemblerContext = assemblerContext ?? throw new ArgumentNullException(nameof(assemblerContext));

            public override bool VisitStruct([NotNull] ScAsmParser.StructContext context)
            {
                Console.WriteLine("Struct\t'{0}'", context.identifier().GetText());
                foreach (var f in context.fieldDecl())
                {
                    Console.WriteLine("\t\t{0} {1}", f.identifier().GetText(), f.type().GetText());
                }

                return true;
            }
        }

        private sealed class StaticFieldVisitor : ScAsmBaseVisitor<bool>
        {
            private readonly AssemblerContext assemblerContext;

            public StaticFieldVisitor(AssemblerContext assemblerContext) => this.assemblerContext = assemblerContext ?? throw new ArgumentNullException(nameof(assemblerContext));

            public override bool VisitStaticFieldDecl([NotNull] ScAsmParser.StaticFieldDeclContext context)
            {
                Console.Write("Static\t'{0}'", context.fieldDecl().identifier().GetText());
                if (context.staticFieldInitializer() != null)
                {
                    Console.Write("\t= {0} | {1}", context.staticFieldInitializer().integer()?.GetText(), context.staticFieldInitializer().@float()?.GetText());
                }
                Console.WriteLine();

                return true;
            }
        }

        private sealed class InstructionVisitor : ScAsmBaseVisitor<bool>
        {
            private readonly AssemblerContext assemblerContext;

            public InstructionVisitor(AssemblerContext assemblerContext) => this.assemblerContext = assemblerContext ?? throw new ArgumentNullException(nameof(assemblerContext));

            public override bool VisitInstruction([NotNull] ScAsmParser.InstructionContext context)
            {
                string mnemonic = context.identifier().GetText();
                uint mnemonicHash = mnemonic.ToHash();
                ref readonly var inst = ref Instruction.FindByMnemonic(mnemonicHash);
                if (inst.IsValid)
                {
                    Operand[] operands = context.operandList().Accept(OperandListToArray.Instance);
                    assemblerContext.Code.BeginInstruction();
                    inst.Assemble(operands, assemblerContext.Code);
                    assemblerContext.Code.EndInstruction();

                    return true;
                }

                
                ref readonly var hlinst = ref HighLevelInstruction.FindByMnemonic(mnemonicHash);
                if (hlinst.IsValid)
                {
                    Operand[] operands = context.operandList().Accept(OperandListToArray.Instance);
                    hlinst.Assemble(operands, assemblerContext.Code);

                    return true;
                }

                throw new ArgumentException($"Unknown instruction '{mnemonic}'");
            }
        }

        private sealed class OperandListToArray : ScAsmBaseVisitor<Operand[]>
        {
            public static OperandListToArray Instance { get; } = new OperandListToArray();

            public override Operand[] VisitOperandList([NotNull] ScAsmParser.OperandListContext context)
                => context.operand().Select(o => o switch
                {
                    _ when o.integer() != null => o.integer().Accept(IntegerToOperand.Instance),
                    _ when o.@float() != null => new Operand(float.Parse(o.@float().GetText())),
                    _ when o.@string() != null => new Operand(o.GetText().AsSpan()[1..^1].Unescape(), OperandType.String),
                    _ when o.identifier() != null => new Operand(o.GetText(), OperandType.Identifier),
                    _ => throw new InvalidOperationException()
                }).ToArray();
        }

        private sealed class IntegerToOperand : ScAsmBaseVisitor<Operand>
        {
            public static IntegerToOperand Instance { get; } = new IntegerToOperand();

            public override Operand VisitInteger([NotNull] ScAsmParser.IntegerContext context)
            {
                if (context.DECIMAL_INTEGER() != null)
                {
                    string str = context.DECIMAL_INTEGER().GetText();

                    return ToOperand(str[0] == '-' ?
                                        unchecked((ulong)long.Parse(str)) : // has '-', try to parse it as signed int and bit-cast it to unsigned
                                        ulong.Parse(str));
                }
                else
                {
                    Debug.Assert(context.HEX_INTEGER() != null);

                    string str = context.HEX_INTEGER().GetText();

                    return ToOperand(ulong.Parse(str.AsSpan()[2..], System.Globalization.NumberStyles.HexNumber)); // skip '0x' and parse it as hex
                }
            }

            private static Operand ToOperand(ulong v)
                => v < uint.MaxValue ?
                        new Operand(unchecked((uint)v)) :
                        new Operand(v);
        }





        public class StringsPagesBuilder
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
                int byteCount = System.Text.Encoding.UTF8.GetByteCount(str);
                Span<byte> buffer = byteCount <= MaxStackLimit ? stackalloc byte[byteCount] : new byte[byteCount];
                System.Text.Encoding.UTF8.GetBytes(str, buffer);
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
}
