using ScTools.ScriptAssembly.Targets.Five;

namespace ScTools.Tests.ScriptLang.CodeGen;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using ScTools.GameFiles;
using ScTools.ScriptAssembly.Targets.Five;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;
using ScTools.ScriptLang.Workspace;

using Xunit;

public abstract class CodeGenTestsBase
{
    public static IEnumerable<object[]> GetAllHandleTypes() => HandleType.All.Select(h => new object[] { h });
    public static IEnumerable<object[]> GetAllTextLabelTypes64Bit() => TextLabelType.All64.Select(tl => new object[] { tl });

    protected static string IntToPushInst(int value)
    {
        switch (value)
        {
            case -1: return OpcodeV10.PUSH_CONST_M1.ToString();
            case 0: return OpcodeV10.PUSH_CONST_0.ToString();
            case 1: return OpcodeV10.PUSH_CONST_1.ToString();
            case 2: return OpcodeV10.PUSH_CONST_2.ToString();
            case 3: return OpcodeV10.PUSH_CONST_3.ToString();
            case 4: return OpcodeV10.PUSH_CONST_4.ToString();
            case 5: return OpcodeV10.PUSH_CONST_5.ToString();
            case 6: return OpcodeV10.PUSH_CONST_6.ToString();
            case 7: return OpcodeV10.PUSH_CONST_7.ToString();

            case >= byte.MinValue and <= byte.MaxValue:
                return $"{OpcodeV10.PUSH_CONST_U8} {(byte)value}";

            case >= short.MinValue and <= short.MaxValue:
                return $"{OpcodeV10.PUSH_CONST_S16} {(short)value}";

            case >= 0 and <= 0x00FFFFFF:
                return $"{OpcodeV10.PUSH_CONST_U24} {unchecked((uint)value)}";

            default:
                return $"{OpcodeV10.PUSH_CONST_U32} {unchecked((uint)value)}";
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
                .script_name 'test_script'
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

        var compiledScripts = ScriptCompiler.Compile(u, new(Game.GTAV, Platform.x64));
        Assert.False(d.HasErrors);
        var compiledScript = Assert.Single(compiledScripts);
        var compiledScriptGTAV = IsType<GameFiles.Five.Script>(compiledScript);

        using var expectedAssemblyReader = new StringReader(expectedAssembly);
        var expectedAssembler = Assembler.Assemble(expectedAssemblyReader, "test_expected.scasm", nativeDB, options: new() { IncludeFunctionNames = true });

        string sourceDump = new DumperFiveV12().DumpToString(compiledScriptGTAV);
        string expectedDump = new DumperFiveV12().DumpToString(expectedAssembler.OutputScript);

        Util.AssertScriptsAreEqual(compiledScriptGTAV, expectedAssembler.OutputScript);
    }
}
