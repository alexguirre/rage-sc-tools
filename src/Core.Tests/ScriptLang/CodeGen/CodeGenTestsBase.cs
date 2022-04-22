namespace ScTools.Tests.ScriptLang.CodeGen;

using System.IO;
using System.Linq;

using ScTools.ScriptAssembly;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;

using Xunit;

public abstract class CodeGenTestsBase
{
    protected static void CompileScript(string scriptSource, string expectedAssembly, string declarationsSource = "", NativeDB? nativeDB = null)
        => CompileRaw(
            source: $@"
                {declarationsSource}
                SCRIPT test_script
                {scriptSource}
                ENDSCRIPT",

            expectedAssembly: $@"
                .script_name test_script
                .code
                SCRIPT:
                {expectedAssembly}",
                
            nativeDB: nativeDB);

    protected static void CompileRaw(string source, string expectedAssembly, NativeDB? nativeDB = null)
    {
        nativeDB ??= NativeDB.Empty;

        var d = new DiagnosticsReport();
        var l = new Lexer("codegen_tests.sc", source, d);
        var p = new ParserNew(l, d);
        var s = new SemanticsAnalyzer(d);

        var u = p.ParseCompilationUnit();
        Assert.False(d.HasErrors);
        u.Accept(s);
        Assert.False(d.HasErrors);

        var c = new ScriptCompiler(u.Declarations.OfType<ScriptDeclaration>().Single());
        var compiledScript = c.Compile();
        Assert.False(d.HasErrors);

        using var expectedAssemblyReader = new StringReader(expectedAssembly);
        var expectedAssembler = Assembler.Assemble(expectedAssemblyReader, "test_expected.scasm", nativeDB, options: new() { IncludeFunctionNames = true });

        using StringWriter sourceDumpWriter = new(), expectedDumpWriter = new();
        new Dumper(compiledScript).Dump(sourceDumpWriter, true, true, true, true, true);
        new Dumper(expectedAssembler.OutputScript).Dump(expectedDumpWriter, true, true, true, true, true);

        string sourceDump = sourceDumpWriter.ToString(), expectedDump = expectedDumpWriter.ToString();

        Util.AssertScriptsAreEqual(compiledScript, expectedAssembler.OutputScript);
    }
}
