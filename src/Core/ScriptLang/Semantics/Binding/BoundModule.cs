#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections.Generic;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.CodeGen;

    public sealed class BoundModule : BoundNode
    {
        public string Name { get; set; } = "unknown";
        public int StaticVarsTotalSize { get; set; } = 0;
        public IList<BoundFunction> Functions { get; } = new List<BoundFunction>();

        public Script Assemble(NativeDB? nativeDB)
        {
            var sc = new Script
            {
                Hash = 0,
                ArgsCount = 0,
                StaticsCount = (uint)StaticVarsTotalSize,
                Statics = StaticVarsTotalSize > 0 ? new ScriptValue[StaticVarsTotalSize] : null,
                GlobalsLengthAndBlock = 0,
                NativesCount = 0,
                Name = Name,
                NameHash = Name.ToHash(),
                StringsLength = 0,
            };


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

            //sc.StringsPages = new ScriptPageArray<byte>
            //{
            //    Items = Strings.ToPages(out uint stringsLength),
            //};
            //sc.StringsLength = stringsLength;

            sc.Natives = code.GetUsedNativesEncoded();
            sc.NativesCount = (uint)sc.Natives.Length;

            return sc;
        }
    }
}
