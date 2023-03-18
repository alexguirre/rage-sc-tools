namespace ScTools.ScriptLang.CodeGen;

using ScTools.GameFiles;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;

using System;
using System.Collections.Generic;
using System.Diagnostics;

public static class ScriptCompiler
{
    public static IScript? Compile(CompilationUnit compilationUnit, Workspace.BuildTarget target)
    {
        var staticAllocator = new VarAllocator();
        AddImportedStatics(staticAllocator, compilationUnit);
        foreach (var decl in compilationUnit.Declarations)
        {
            if (decl is VarDeclaration { Kind: VarKind.Static } staticVar)
            {
                staticAllocator.Allocate(staticVar);
            }
            else if (decl is ScriptDeclaration scriptDecl)
            {
                return CompileScript(scriptDecl, staticAllocator, target);
            }
        }

        // no script found in the compilation unit
        return null;
    }

    private static IScript CompileScript(ScriptDeclaration scriptDecl, VarAllocator statics, Workspace.BuildTarget target)
    {
        var codeEmitter = CodeEmitterFactory.CreateForTarget(target, new(statics));
        return codeEmitter.EmitScript(scriptDecl);
    }

    private static void AddImportedStatics(VarAllocator staticAllocator, CompilationUnit compilationUnit)
    {
        var importedSet = new HashSet<CompilationUnit>();
        AddImportedStaticsRecursive(staticAllocator, compilationUnit, importedSet);

        static void AddImportedStaticsRecursive(VarAllocator staticAllocator, CompilationUnit compilationUnit, HashSet<CompilationUnit> importedSet)
        {
            if (!importedSet.Add(compilationUnit))
            {
                // already imported
                return;
            }

            foreach (var import in compilationUnit.Usings.Select(u => u.Semantics.ImportedCompilationUnit!).Where(i => i is not null))
            {
                AddImportedStatics(staticAllocator, import);

                foreach (var decl in import.Declarations)
                {
                    if (decl is VarDeclaration { Kind: VarKind.Static } staticVar)
                    {
                        staticAllocator.Allocate(staticVar);
                    }
                }
            }
        }
    }
}
