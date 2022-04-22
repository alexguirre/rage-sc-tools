namespace ScTools.ScriptLang.CodeGen
{
    using ScTools.GameFiles;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal sealed class ScriptCompiler
    {
        private readonly ScriptDeclaration script;
        private readonly CodeEmitter codeEmitter = new();

        public ScriptCompiler(ScriptDeclaration script)
        {
            this.script = script;
        }

        public Script Compile()
        {
            var result = new Script()
            {
                Name = script.Name,
                NameHash = script.Name.ToLowercaseHash(),
                Hash = 0, // TODO: include a way to set the hash in the SCRIPT declaration
            };

            codeEmitter.EmitScript(script);

            result.CodePages = codeEmitter.ToCodePages();
            result.CodeLength = result.CodePages?.Length ?? 0;

            //OutputScript.GlobalsPages = globalSegmentBuilder.Length != 0 ? globalSegmentBuilder.ToPages<ScriptValue>() : null;
            //OutputScript.GlobalsLength = OutputScript.GlobalsPages?.Length ?? 0;

            result.Statics = Array.Empty<ScriptValue>();
            result.StaticsCount = 0;
            result.ArgsCount = 0;

            result.Natives = Array.Empty<ulong>();
            result.NativesCount = 0;

            result.StringsPages = codeEmitter.Strings.ByteLength != 0 ? codeEmitter.Strings.ToPages() : null;
            result.StringsLength = result.StringsPages?.Length ?? 0;

            return result;
        }
    }
}
