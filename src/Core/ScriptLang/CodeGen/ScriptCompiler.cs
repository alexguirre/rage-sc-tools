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
        var globalsAllocator = new GlobalsAllocator(AreGlobalsIndexed(target));
        AddImportedStaticsAndGlobals(staticAllocator, globalsAllocator, compilationUnit);
        foreach (var decl in compilationUnit.Declarations)
        {
            switch (decl)
            {
                case VarDeclaration { Kind: VarKind.Static } staticVar:
                    staticAllocator.Allocate(staticVar);
                    break;
                case GlobalBlockDeclaration globalsDecl:
                    globalsAllocator.Allocate(globalsDecl);
                    break;
                case ScriptDeclaration scriptDecl:
                    return CompileScript(scriptDecl, staticAllocator, globalsAllocator, target);
            }
        }

        // no script found in the compilation unit
        return null;
    }

    private static IScript CompileScript(ScriptDeclaration scriptDecl, VarAllocator statics, GlobalsAllocator globals, Workspace.BuildTarget target)
    {
        var codeEmitter = CodeEmitterFactory.CreateForTarget(target, new(statics, globals));
        return codeEmitter.EmitScript(scriptDecl);
    }

    private static void AddImportedStaticsAndGlobals(VarAllocator staticAllocator, GlobalsAllocator globalsAllocator, CompilationUnit compilationUnit)
    {
        var importedSet = new HashSet<CompilationUnit>();
        AddImportedStaticsAndGlobalsRecursive(staticAllocator, globalsAllocator, compilationUnit, importedSet);

        static void AddImportedStaticsAndGlobalsRecursive(VarAllocator staticAllocator, GlobalsAllocator globalsAllocator, CompilationUnit compilationUnit, HashSet<CompilationUnit> importedSet)
        {
            if (!importedSet.Add(compilationUnit))
            {
                // already imported
                return;
            }

            foreach (var import in compilationUnit.Usings.Select(u => u.Semantics.ImportedCompilationUnit).Where(i => i is not null))
            {
                Debug.Assert(import is not null);
                AddImportedStaticsAndGlobals(staticAllocator, globalsAllocator, import);

                foreach (var decl in import.Declarations)
                {
                    switch (decl)
                    {
                        case VarDeclaration { Kind: VarKind.Static } staticVar:
                            staticAllocator.Allocate(staticVar);
                            break;
                        case GlobalBlockDeclaration globalsDecl:
                            globalsAllocator.Allocate(globalsDecl);
                            break;
                    }
                }
            }
        }
    }

    // TODO: move AreGlobalsIndexed to a central place where build targets configurations would be defined
    private static bool AreGlobalsIndexed(Workspace.BuildTarget target)
        => target.Game switch
        {
            Workspace.Game.GTA5 => true,
            _ => false,
        };
}
