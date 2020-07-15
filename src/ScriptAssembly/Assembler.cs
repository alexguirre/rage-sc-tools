namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.CodeGen;
    using ScTools.ScriptAssembly.Definitions;
    using ScTools.ScriptAssembly.Grammar;
    using ScTools.ScriptAssembly.Grammar.Visitors;
    using ScTools.ScriptAssembly.Types;

    public sealed class AssemblerContext
    {
        private readonly Script sc;
        private bool nameSet = false;

        public NativeDB NativeDB { get; }
        public CodeBuilder Code { get; }
        public CodeGenOptions CodeGenOptions { get; }
        public StringPagesBuilder Strings { get; } = new StringPagesBuilder();
        public IList<ulong> NativeHashes { get; } = new List<ulong>();
        public Dictionary<string, uint> Statics { get; } = new Dictionary<string, uint>();
        public ScriptValue[] StaticValues => sc.Statics;
        public Registry Symbols { get; } = new Registry();

        public AssemblerContext(Script sc, NativeDB nativeDB, CodeGenOptions codeGenOptions)
        {
            this.sc = sc ?? throw new ArgumentNullException(nameof(sc));
            Code = new CodeBuilder(this);
            NativeDB = nativeDB;
            CodeGenOptions = codeGenOptions;
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

        public void SetArgsCount(uint count)
        {
            if (sc.ArgsCount != 0)
            {
                throw new InvalidOperationException("Args count was already set");
            }

            sc.ArgsCount = count;
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

        public uint GetStaticOffset(string name) => Statics[name];

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

        public uint AddString(string str) => Strings.Add(str);
        public uint AddOrGetString(string str) => Strings.AddOrGet(str);
    }

    internal static partial class Assembler
    {
        public static Script Assemble(string input, NativeDB nativeDB, CodeGenOptions codeGenOptions)
        {
            AntlrInputStream inputStream = new AntlrInputStream(input);

            ScAsmLexer lexer = new ScAsmLexer(inputStream);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            ScAsmParser parser = new ScAsmParser(tokens);

            return parser.script().Accept(new ScriptVisitor(nativeDB, codeGenOptions));
        }

        private sealed class ScriptVisitor : ScAsmBaseVisitor<Script>
        {
            private readonly NativeDB nativeDB;
            private readonly CodeGenOptions codeGenOptions;

            public ScriptVisitor(NativeDB nativeDB, CodeGenOptions codeGenOptions)
            {
                this.nativeDB = nativeDB;
                this.codeGenOptions = codeGenOptions;
            }

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

                var assemblerContext = new AssemblerContext(sc, nativeDB, codeGenOptions);

                var reg = assemblerContext.Symbols;
                RegisterStructs.Visit(context, reg);
                RegisterStaticFields.Visit(context, reg);
                RegisterArgs.Visit(context, reg);
                RegisterFunctions.Visit(context, reg);


                {
                    uint argsSize = (uint)reg.Args.Sum(a => a.Type.SizeOf);
                    uint staticsSize = (uint)reg.StaticFields.Sum(sf => sf.Type.SizeOf) + argsSize;

                    assemblerContext.SetStaticsCount(staticsSize);
                    assemblerContext.SetArgsCount(argsSize);

                    static void init(AssemblerContext c, StaticFieldDefinition sf, ref uint offset)
                    {
                        c.Statics.Add(sf.Name, offset);

                        uint size = sf.Type.SizeOf;
                        sf.InitializeValue(c.StaticValues.AsSpan()[(int)offset..(int)(offset + size)]);
                        offset += size;
                    }

                    uint offset = 0;
                    foreach (StaticFieldDefinition sf in reg.StaticFields)
                    {
                        init(assemblerContext, sf, ref offset);
                    }

                    foreach (ArgDefinition a in reg.Args)
                    {
                        init(assemblerContext, a, ref offset);
                    }
                }

                var directiveVisitor = new DirectiveVisitor(assemblerContext);
                foreach (var d in context.statement().Select(stat => stat.directive()).Where(d => d != null))
                {
                    d.Accept(directiveVisitor);
                }

                // TODO: globals

                if (!reg.Functions.Any(f => f.IsEntrypoint))
                {
                    throw new InvalidOperationException($"Missing entrypoint: no function named '{FunctionDefinition.EntrypointName}'");
                }

                var code = assemblerContext.Code;
                foreach (var f in reg.Functions)
                {
                    code.BeginFunction(f);
                    foreach (var statement in f.Statements)
                    {
                        if (statement.Label != null)
                        {
                            code.AddLabel(statement.Label);
                        }

                        if (statement.Mnemonic != null)
                        {
                            uint mnemonicHash = statement.Mnemonic.ToHash();
                            ref readonly var inst = ref Instruction.FindByMnemonic(mnemonicHash);
                            if (inst.IsValid)
                            {
                                code.Emit(inst, statement.Operands.AsSpan());
                            }
                            else
                            {
                                ref readonly var hlInst = ref HighLevelInstruction.FindByMnemonic(mnemonicHash);
                                if (hlInst.IsValid)
                                {
                                    hlInst.Assemble(statement.Operands.AsSpan(), code);
                                }
                                else
                                {
                                    throw new ArgumentException($"Unknown instruction '{statement.Mnemonic}'");
                                }
                            }
                        }
                    }
                    code.EndFunction();
                }

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

            private sealed class DirectiveVisitor : ScAsmBaseVisitor<bool>
            {
                private readonly AssemblerContext assemblerContext;

                public DirectiveVisitor(AssemblerContext assemblerContext) => this.assemblerContext = assemblerContext ?? throw new ArgumentNullException(nameof(assemblerContext));

                public override bool VisitDirective([NotNull] ScAsmParser.DirectiveContext context)
                {
                    string directive = context.identifier().GetText();
                    int dirIndex = Directives.Find(directive.ToHash());

                    if (dirIndex != -1)
                    {
                        Operand[] operands = ParseOperands.Visit(context.operandList(), null, null);
                        Directive dir = Directives.Set[dirIndex];
                        dir.Callback(dir, assemblerContext, operands);
                        return true;
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown directive '{directive}'");
                    }
                }
            }
        }
    }
}
