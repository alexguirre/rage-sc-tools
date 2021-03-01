#nullable enable
namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Binding;
    using ScTools.ScriptLang.Semantics.Symbols;

    public class Compilation : IUsingModuleResolver
    {
        public const string DefaultScriptName = "unknown";

        private readonly Dictionary<VariableSymbol, int /*location*/> allocatedStatics = new();
        private readonly List<GlobalBlock> globalBlocks = new();

        public Script? CompiledScript { get; private set; }
        public bool NeedsRecompilation { get; private set; }
        public Module? MainModule { get; private set; }
        public IList<Module> ImportedModules { get; } = new List<Module>();
        public NativeDB? NativeDB { get; set; }
        public IUsingSourceResolver? SourceResolver { get; set; }

        public bool HasErrors => (MainModule?.Diagnostics.HasErrors ?? false) || ImportedModules.Any(m => m.Diagnostics.HasErrors);

        public DiagnosticsReport GetAllDiagnostics()
        {
            var d = new DiagnosticsReport();
            if (MainModule != null)
            {
                d.AddFrom(MainModule.Diagnostics);
            }

            foreach (var m in ImportedModules)
            {
                d.AddFrom(m.Diagnostics);
            }

            return d;
        }

        public void SetMainModule(TextReader input, string filePath = "tmp.sc")
        {
            MainModule = new Module(filePath);
            MainModule.Parse(input);
            MainModule.DoFirstSemanticAnalysisPass(this);

            var main = MainModule.SymbolTable!.Lookup(DefinedFunctionSymbol.MainName) as DefinedFunctionSymbol;
            if (main == null)
            {
                MainModule.Diagnostics.AddError(MainModule.FilePath, $"Missing '{DefinedFunctionSymbol.MainName}' procedure", SourceRange.Unknown);
            }
            else if (!main.IsMain)
            {
                MainModule.Diagnostics.AddError(MainModule.FilePath, $"Incorrect signature for '{DefinedFunctionSymbol.MainName}' procedure, expected 'PROC {DefinedFunctionSymbol.MainName}()'", main.Source);
            }

            NeedsRecompilation = true;
        }

        public void PerformPendingAnalysis()
        {
            var modules = ImportedModules.Append(MainModule);
            foreach (var m in modules.Where(s => s!.State == ModuleState.SemanticAnalysisFirstPassDone))
            {
                m!.DoSecondSemanticAnalysisPass();
            }
            foreach (var m in modules.Where(s => s!.State == ModuleState.SemanticAnalysisSecondPassDone))
            {
                m!.DoBinding();
            }
        }

        public void Compile()
        {
            if (MainModule == null)
            {
                throw new InvalidOperationException("No main module assigned");
            }

            if (!NeedsRecompilation)
            {
                return;
            }

            PerformPendingAnalysis();
            CompiledScript = HasErrors ? null : DoCompile();
            NeedsRecompilation = false;
        }

        private Script DoCompile()
        {
            Debug.Assert(!HasErrors);
            Debug.Assert(MainModule != null);
            Debug.Assert(MainModule.State == ModuleState.Bound);
            Debug.Assert(ImportedModules.All(m => m.State == ModuleState.Bound));

            globalBlocks.Clear();
            globalBlocks.AddRange(MainModule.SymbolTable!.Symbols.OfType<GlobalBlock>()
                                    .Concat(ImportedModules.SelectMany(m => m.SymbolTable!.Symbols.OfType<GlobalBlock>())));

            var statics = MainModule.BoundModule!.Statics
                            .Concat(ImportedModules.SelectMany(m => m.BoundModule!.Statics));
            var staticsTotalSize = AllocateStatics(statics);

            var scName = MainModule.BoundModule.Name ?? DefaultScriptName;
            var sc = new Script
            {
                Hash = (uint)MainModule.BoundModule.Hash,
                ArgsCount = 0,
                StaticsCount = (uint)staticsTotalSize,
                Statics = new ScriptValue[staticsTotalSize],
                GlobalsLengthAndBlock = 0,
                NativesCount = 0,
                Name = scName,
                NameHash = scName.ToHash(),
                StringsLength = 0,
            };

            // emit byte code
            var code = new ByteCodeBuilder(this);
            MainModule.BoundModule.Emit(code);
            foreach (var m in ImportedModules)
            {
                m.BoundModule!.Emit(code);
            }

            // initialize static vars values
            foreach (var s in statics)
            {
                InitializeStaticArraysLengths(s.Type, allocatedStatics[s], sc.Statics.AsSpan());
                if (s.Initializer != null)
                {
                    var defaultValue = Evaluator.Evaluate(s.Initializer!, functionAddressResolver: func => code.GetFunctionIP(func.Name));
                    Debug.Assert(defaultValue.Length == s.Type.SizeOf);

                    var dest = sc.Statics.AsSpan(allocatedStatics[s], s.Type.SizeOf);
                    defaultValue.CopyTo(dest);
                }
            }

            // initialize global vars values
            var matchingGlobalBlocks = globalBlocks.FindAll(b => SymbolTable.CaseInsensitiveComparer.Equals(b.Owner, sc.Name));
            Debug.Assert(matchingGlobalBlocks.Count == 1, $"Script '{sc.Name}' is owner of more than one global block"); // TODO: report this as diagnostic error
            var globalBlock = matchingGlobalBlocks.SingleOrDefault();
            if (globalBlock != null)
            {
                sc.GlobalsBlock = (uint)globalBlock.Block;
                sc.GlobalsLength = (uint)globalBlock.SizeOf; // TODO

                var globals = new ScriptValue[globalBlock.SizeOf];
                int location = 0;
                foreach (var v in globalBlock.Variables)
                {
                    InitializeStaticArraysLengths(v.Type, location, globals.AsSpan());
                    var sizeOf = v.Type.SizeOf;
                    if (v.Initializer != null)
                    {
                        var defaultValue = Evaluator.Evaluate(v.Initializer);
                        Debug.Assert(defaultValue.Length == sizeOf);

                        var dest = globals.AsSpan(location, sizeOf);
                        defaultValue.CopyTo(dest);
                    }
                    location += sizeOf;
                }

                // create pages
                {
                    var pageCount = (globals.Length + Script.MaxPageLength) / Script.MaxPageLength;
                    var p = new ScriptPage<ScriptValue>[pageCount];
                    if (pageCount > 0)
                    {
                        for (int i = 0; i < pageCount - 1; i++)
                        {
                            p[i] = new ScriptPage<ScriptValue> { Data = new ScriptValue[Script.MaxPageLength] };
                            globals.AsSpan(i * (int)Script.MaxPageLength, (int)Script.MaxPageLength).CopyTo(p[i].Data);
                        }

                        p[^1] = new ScriptPage<ScriptValue> { Data = new ScriptValue[globals.Length & 0x3FFF] };
                        globals.AsSpan((int)((pageCount - 1) * Script.MaxPageLength), p[^1].Data.Length).CopyTo(p[^1].Data);
                    }
                    sc.GlobalsPages = new ScriptPageArray<ScriptValue> { Items = p };
                }
            }

            sc.CodePages = new ScriptPageArray<byte>
            {
                Items = code.ToPages(out uint codeLength),
            };
            sc.CodeLength = codeLength;

            sc.StringsPages = new ScriptPageArray<byte>
            {
                Items = code.GetStringsPages(out uint stringsLength),
            };
            sc.StringsLength = stringsLength;

            sc.Natives = code.GetUsedNativesEncoded();
            sc.NativesCount = (uint)sc.Natives.Length;

            return sc;
        }

        public int? GetGlobalLocation(VariableSymbol var) 
        {
            Debug.Assert(var.IsGlobal);

            var block = globalBlocks.Find(b => b.Contains(var));
            return block?.GetLocation(var);
        }

        public int? GetStaticLocation(VariableSymbol var)
            => allocatedStatics.TryGetValue(var, out int loc) ? loc : null;

        private int AllocateStatics(IEnumerable<VariableSymbol> statics)
        {
            allocatedStatics.Clear();

            int location = 0;
            foreach (var s in statics)
            {
                allocatedStatics.Add(s, location);
                location += s.Type.SizeOf;
            }

            return location; // return total size
        }

        private static void InitializeStaticArraysLengths(Semantics.Type ty, int location, Span<ScriptValue> statics)
        {
            switch (ty)
            {
                case ArrayType arrTy:
                    statics[location].AsInt32 = arrTy.Length;

                    if (arrTy.ItemType is ArrayType or StructType)
                    {
                        var itemSize = arrTy.ItemType.SizeOf;
                        location += 1;
                        for (int i = 0; i < arrTy.Length; i++)
                        {
                            InitializeStaticArraysLengths(arrTy.ItemType, location, statics);
                            location += itemSize;
                        }
                    }
                    break;
                case StructType strucTy:
                    for (int i = 0; i < strucTy.Fields.Count; i++)
                    {
                        var f = strucTy.Fields[i];
                        InitializeStaticArraysLengths(f.Type, location, statics);
                        location += f.Type.SizeOf;
                    }
                    break;
                default: break;
            }
        }

        Module? IUsingModuleResolver.Resolve(string usingPath)
        {
            if (SourceResolver == null)
            {
                return null;
            }

            if (!SourceResolver.IsValid(usingPath))
            {
                return null;
            }

            var module = ImportedModules.SingleOrDefault(m => m.FilePath == usingPath);
            if (module != null)
            {
                if (!SourceResolver.HasChanged(usingPath))
                {
                    return module;
                }
                else
                {
                    ImportedModules.Remove(module);
                    // TODO: what to do with modules that depend on it?
                }
            }

            using var reader = SourceResolver.Resolve(usingPath);
            module = new Module(usingPath);
            ImportedModules.Add(module);
            module.Parse(reader);
            module.DoFirstSemanticAnalysisPass(this);
            return module;
        }
    }
}
