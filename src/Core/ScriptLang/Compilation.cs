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
        private readonly Dictionary<VariableSymbol, int /*location*/> allocatedStatics = new();

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

            var main = MainModule.SymbolTable!.Lookup(FunctionSymbol.MainName) as FunctionSymbol;
            if (main == null)
            {
                MainModule.Diagnostics.AddError(MainModule.FilePath, $"Missing '{FunctionSymbol.MainName}' procedure", SourceRange.Unknown);
            }
            else if (!main.IsMain)
            {
                MainModule.Diagnostics.AddError(MainModule.FilePath, $"Incorrect signature for '{FunctionSymbol.MainName}' procedure, expected 'PROC {FunctionSymbol.MainName}()'", main.Source);
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

            var statics = MainModule.BoundModule!.Statics
                            .Concat(ImportedModules.SelectMany(m => m.BoundModule!.Statics));
            var staticsTotalSize = AllocateStatics(statics);

            var sc = new Script
            {
                Hash = 0,
                ArgsCount = 0,
                StaticsCount = (uint)staticsTotalSize,
                Statics = new ScriptValue[staticsTotalSize],
                GlobalsLengthAndBlock = 0,
                NativesCount = 0,
                Name = MainModule.BoundModule.Name,
                NameHash = MainModule.BoundModule.Name.ToHash(),
                StringsLength = 0,
            };

            // initialize static vars values
            foreach (var s in statics)
            {
                InitializeStaticArraysLengths(s.Type, allocatedStatics[s], sc.Statics.AsSpan());
                if (s.Initializer != null)
                {
                    var defaultValue = Evaluator.Evaluate(s.Initializer!);
                    Debug.Assert(defaultValue.Length == s.Type.SizeOf);

                    var dest = sc.Statics.AsSpan(allocatedStatics[s], s.Type.SizeOf);
                    defaultValue.CopyTo(dest);
                }
            }

            // emit byte code
            var code = new ByteCodeBuilder(this);
            MainModule.BoundModule.Emit(code);
            foreach (var m in ImportedModules)
            {
                m.BoundModule!.Emit(code);
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
