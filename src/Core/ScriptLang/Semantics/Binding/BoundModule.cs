#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.CodeGen;

    public sealed class BoundModule : BoundNode
    {
        public string Name { get; set; } = "unknown";
        public IList<BoundStatic> Statics { get; } = new List<BoundStatic>();
        public IList<BoundFunction> Functions { get; } = new List<BoundFunction>();

        public Script Assemble(NativeDB? nativeDB)
        {
            var staticsTotalSize = Statics.Sum(s => s.Var.Type.SizeOf);
            var sc = new Script
            {
                Hash = 0,
                ArgsCount = 0,
                StaticsCount = (uint)staticsTotalSize,
                Statics = new ScriptValue[staticsTotalSize],
                GlobalsLengthAndBlock = 0,
                NativesCount = 0,
                Name = Name,
                NameHash = Name.ToHash(),
                StringsLength = 0,
            };

            // initialize static vars values
            foreach (var s in Statics.Where(s => s.Initializer != null))
            {
                var defaultValue = Evaluator.Evaluate(s.Initializer!);
                Debug.Assert(defaultValue.Length == s.Var.Type.SizeOf);

                var dest = sc.Statics.AsSpan(s.Var.Location, s.Var.Type.SizeOf);
                defaultValue.CopyTo(dest);
            }

            // emit byte code
            var code = new ByteCodeBuilder(nativeDB);
            foreach (var func in Functions)
            {
                func.Emit(code);
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
    }
}
