namespace ScTools.Tests.ScriptLang.CodeGen;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using ScTools.ScriptAssembly;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using Xunit;

public abstract class CodeGenTestsBase
{
    public static IEnumerable<object[]> GetAllHandleTypes() => HandleType.All.Select(h => new object[] { h });
    public static IEnumerable<object[]> GetAllTextLabelTypes() => TextLabelType.All.Select(tl => new object[] { tl });

    protected static string IntToPushInst(int value)
    {
        switch (value)
        {
            case -1: return Opcode.PUSH_CONST_M1.ToString();
            case 0: return Opcode.PUSH_CONST_0.ToString();
            case 1: return Opcode.PUSH_CONST_1.ToString();
            case 2: return Opcode.PUSH_CONST_2.ToString();
            case 3: return Opcode.PUSH_CONST_3.ToString();
            case 4: return Opcode.PUSH_CONST_4.ToString();
            case 5: return Opcode.PUSH_CONST_5.ToString();
            case 6: return Opcode.PUSH_CONST_6.ToString();
            case 7: return Opcode.PUSH_CONST_7.ToString();

            case >= byte.MinValue and <= byte.MaxValue:
                return $"{Opcode.PUSH_CONST_U8} {(byte)value}";

            case >= short.MinValue and <= short.MaxValue:
                return $"{Opcode.PUSH_CONST_S16} {(short)value}";

            case >= 0 and <= 0x00FFFFFF:
                return $"{Opcode.PUSH_CONST_U24} {unchecked((uint)value)}";

            default:
                return $"{Opcode.PUSH_CONST_U32} {unchecked((uint)value)}";
        }
    }

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
        var p = new Parser(l, d);
        var s = new SemanticsAnalyzer(d);

        var u = p.ParseCompilationUnit();
        Assert.False(d.HasErrors);
        u.Accept(s);
        Assert.False(d.HasErrors);

        var compiledScripts = ScriptCompiler.Compile(u);
        Assert.False(d.HasErrors);
        var compiledScript = Assert.Single(compiledScripts);

        using var expectedAssemblyReader = new StringReader(expectedAssembly);
        var expectedAssembler = Assembler.Assemble(expectedAssemblyReader, "test_expected.scasm", nativeDB, options: new() { IncludeFunctionNames = true });

        using StringWriter sourceDumpWriter = new(), expectedDumpWriter = new();
        Dumper.Dump(compiledScript, sourceDumpWriter, true, true, true, true, true);
        Dumper.Dump(expectedAssembler.OutputScript, expectedDumpWriter, true, true, true, true, true);

        string sourceDump = sourceDumpWriter.ToString(), expectedDump = expectedDumpWriter.ToString();

        Util.AssertScriptsAreEqual(compiledScript, expectedAssembler.OutputScript);
    }
}
