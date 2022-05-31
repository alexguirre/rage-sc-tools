namespace ScTools.ScriptLang.CodeGen;

using ScTools.GameFiles.Five;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;

using System;
using System.Collections.Generic;
using System.Diagnostics;

internal static class ScriptCompiler
{
    public static Script[] Compile(CompilationUnit compilationUnit)
    {
        var scripts = new List<Script>();
        var staticAllocator = new VarAllocator();
        foreach (var decl in compilationUnit.Declarations)
        {
            if (decl is VarDeclaration { Kind: VarKind.Static } staticVar)
            {
                staticAllocator.Allocate(staticVar);
            }
            else if (decl is ScriptDeclaration scriptDecl)
            {
                scripts.Add(CompileScript(scriptDecl, staticAllocator));
            }
        }

        return scripts.ToArray();
    }

    private static Script CompileScript(ScriptDeclaration scriptDecl, VarAllocator statics)
    {
        var result = new Script()
        {
            Name = scriptDecl.Name,
            NameHash = scriptDecl.Name.ToLowercaseHash(),
            GlobalsSignature = 0, // TODO: include a way to set the hash in the SCRIPT declaration
        };

        var codeEmitter = new CodeEmitter(statics);
        codeEmitter.EmitScript(scriptDecl);

        result.CodePages = codeEmitter.ToCodePages();
        result.CodeLength = result.CodePages?.Length ?? 0;

        //OutputScript.GlobalsPages = globalSegmentBuilder.Length != 0 ? globalSegmentBuilder.ToPages<ScriptValue>() : null;
        //OutputScript.GlobalsLength = OutputScript.GlobalsPages?.Length ?? 0;

        result.Statics = codeEmitter.GetStaticSegment(out var argsCount);
        result.StaticsCount = (uint)(result.Statics?.Length ?? 0);
        result.ArgsCount = (uint)argsCount;

        result.Natives = Array.Empty<ulong>();
        result.NativesCount = 0;

        result.StringsPages = codeEmitter.Strings.ByteLength != 0 ? codeEmitter.Strings.ToPages() : null;
        result.StringsLength = result.StringsPages?.Length ?? 0;

        return result;
    }
}
