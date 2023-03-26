namespace ScTools.Tests.ScriptLang.CodeGen;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using ScTools.GameFiles;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;
using ScTools.ScriptLang.Workspace;

using OpcodeGTA5 = ScTools.ScriptAssembly.Targets.GTA5.OpcodeV10;
using OpcodeGTA4 = ScTools.ScriptAssembly.Targets.GTA4.Opcode;
using OpcodeMP3 = ScTools.ScriptAssembly.Targets.MP3.Opcode;

public abstract class CodeGenTestsBase
{
    public static IEnumerable<object[]> GetAllTextLabelTypes64Bit() => TextLabelType.All64.Select(tl => new object[] { tl });

    protected static string IntToPushInst(int value)
    {
        switch (value)
        {
            case -1: return OpcodeGTA5.PUSH_CONST_M1.ToString();
            case 0: return OpcodeGTA5.PUSH_CONST_0.ToString();
            case 1: return OpcodeGTA5.PUSH_CONST_1.ToString();
            case 2: return OpcodeGTA5.PUSH_CONST_2.ToString();
            case 3: return OpcodeGTA5.PUSH_CONST_3.ToString();
            case 4: return OpcodeGTA5.PUSH_CONST_4.ToString();
            case 5: return OpcodeGTA5.PUSH_CONST_5.ToString();
            case 6: return OpcodeGTA5.PUSH_CONST_6.ToString();
            case 7: return OpcodeGTA5.PUSH_CONST_7.ToString();

            case >= byte.MinValue and <= byte.MaxValue:
                return $"{OpcodeGTA5.PUSH_CONST_U8} {(byte)value}";

            case >= short.MinValue and <= short.MaxValue:
                return $"{OpcodeGTA5.PUSH_CONST_S16} {(short)value}";

            case >= 0 and <= 0x00FFFFFF:
                return $"{OpcodeGTA5.PUSH_CONST_U24} {unchecked((uint)value)}";

            default:
                return $"{OpcodeGTA5.PUSH_CONST_U32} {unchecked((uint)value)}";
        }
    }

    protected static string IntToPushInstIV(int value)
    {
        switch (value)
        {
            case >= -16 and <= 159: return ((OpcodeGTA4)((int)OpcodeGTA4.PUSH_CONST_0 + value)).ToString();

            case >= ushort.MinValue and <= ushort.MaxValue:
                return $"{OpcodeGTA4.PUSH_CONST_U16} {unchecked((ushort)value)}";

            default:
                return $"{OpcodeGTA4.PUSH_CONST_U32} {unchecked((uint)value)}";
        }
    }

    protected static string IntToPushInstMP3(int value)
    {
        switch (value)
        {
            case >= -16 and <= 159: return ((OpcodeMP3)((int)OpcodeMP3.PUSH_CONST_0 + value)).ToString();

            case >= ushort.MinValue and <= ushort.MaxValue:
                return $"{OpcodeMP3.PUSH_CONST_U16} {unchecked((ushort)value)}";

            default:
                return $"{OpcodeMP3.PUSH_CONST_U32} {unchecked((uint)value)}";
        }
    }

    protected static string IntToLocalIV(int value)
    {
        switch (value)
        {
            case >= 0 and <= 6: return ((OpcodeGTA4)((int)OpcodeGTA4.LOCAL_0 + value)).ToString();

            default:
                return $@"{IntToPushInstIV(value)}
                          {OpcodeGTA4.LOCAL}";
        }
    }

    protected static string IntToLocalMP3(int value)
    {
        switch (value)
        {
            case >= 0 and <= 6: return ((OpcodeMP3)((int)OpcodeMP3.LOCAL_0 + value)).ToString();

            default:
                return $@"{IntToPushInstIV(value)}
                          {OpcodeMP3.LOCAL}";
        }
    }

    protected static string IntToGlobalIV(int value)
    {
        return $@"{IntToPushInstIV(value)}
                  {OpcodeGTA4.GLOBAL}";
    }

    protected static string IntToGlobalMP3(int value)
    {
        return $@"{IntToPushInstMP3(value)}
                  {OpcodeMP3.GLOBAL}";
    }

    protected static void CompileScript(string scriptSource, string expectedAssembly, string declarationsSource = "",
                                        string? expectedAssemblyIV = null, string? expectedAssemblyMP3 = null,
                                        NativeDB? nativeDB = null)
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

            expectedAssemblyIV: expectedAssemblyIV is null ? null : $@"
                .code
                SCRIPT:
                {expectedAssemblyIV}",

            expectedAssemblyMP3: expectedAssemblyMP3 is null ? null : $@"
                .code
                SCRIPT:
                {expectedAssemblyMP3}",
            
            nativeDB: nativeDB);

    protected static void CompileRaw(string source, string expectedAssembly, string? expectedAssemblyIV = null, string? expectedAssemblyMP3 = null, NativeDB? nativeDB = null)
    {
        nativeDB ??= NativeDB.Empty;

        var d = new DiagnosticsReport();
        var l = new Lexer("codegen_tests.sc", source, d);
        var p = new Parser(l, d, new(d));
        var s = new SemanticsAnalyzer(d);

        var u = p.ParseCompilationUnit();
        False(d.HasErrors);
        u.Accept(s);
        False(d.HasErrors);

        // GTA V codegen
        {
            var compiledScript = ScriptCompiler.Compile(u, new(Game.GTA5, Platform.x64));
            False(d.HasErrors);
            NotNull(compiledScript);
            var compiledScriptGTAV = IsType<GameFiles.GTA5.Script>(compiledScript);

            using var expectedAssemblyReader = new StringReader(expectedAssembly);
            var expectedAssembler = ScTools.ScriptAssembly.Targets.GTA5.Assembler.Assemble(expectedAssemblyReader, "test_expected_gta5.scasm", nativeDB,
                options: new() { IncludeFunctionNames = true });
            False(expectedAssembler.Diagnostics.HasErrors);

            string sourceDump = compiledScriptGTAV.DumpToString();
            string expectedDump = expectedAssembler.OutputScript.DumpToString();

            Util.AssertScriptsAreEqual(compiledScriptGTAV, expectedAssembler.OutputScript);
        }

        // GTA IV codegen
        if (expectedAssemblyIV is not null)
        {
            var compiledScript = ScriptCompiler.Compile(u, new(Game.GTA4, Platform.x86));
            False(d.HasErrors);
            NotNull(compiledScript);
            var compiledScriptGTAIV = IsType<GameFiles.GTA4.Script>(compiledScript);

            using var expectedAssemblyReader = new StringReader(expectedAssemblyIV);
            var expectedAssembler = ScTools.ScriptAssembly.Targets.GTA4.Assembler.Assemble(expectedAssemblyReader, "test_expected_gta4.scasm", nativeDB);
            False(expectedAssembler.Diagnostics.HasErrors);

            string sourceDump = compiledScriptGTAIV.DumpToString();
            string expectedDump = expectedAssembler.OutputScript.DumpToString();

            Util.AssertScriptsAreEqual(compiledScriptGTAIV, expectedAssembler.OutputScript);
        }

        // MP3 codegen
        if (expectedAssemblyMP3 is not null)
        {
            var compiledScript = ScriptCompiler.Compile(u, new(Game.MP3, Platform.x86));
            False(d.HasErrors);
            NotNull(compiledScript);
            var compiledScriptMP3 = IsType<GameFiles.MP3.Script>(compiledScript);

            using var expectedAssemblyReader = new StringReader(expectedAssemblyMP3);
            var expectedAssembler = ScTools.ScriptAssembly.Targets.MP3.Assembler.Assemble(expectedAssemblyReader, "test_expected_mp3.scasm", nativeDB);
            False(expectedAssembler.Diagnostics.HasErrors);
            
            string sourceDump = compiledScriptMP3.DumpToString();
            string expectedDump = expectedAssembler.OutputScript.DumpToString();

            Util.AssertScriptsAreEqual(compiledScriptMP3, expectedAssembler.OutputScript);
        }
    }
}
