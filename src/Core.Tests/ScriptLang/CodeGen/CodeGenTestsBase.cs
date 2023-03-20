using ScTools.ScriptAssembly.Targets.Five;

namespace ScTools.Tests.ScriptLang.CodeGen;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using ScTools.GameFiles;
using ScTools.ScriptAssembly.Targets.Five;
using ScTools.ScriptAssembly.Targets.NY;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;
using ScTools.ScriptLang.Workspace;

public abstract class CodeGenTestsBase
{
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

    protected static string IntToPushInstIV(int value)
    {
        switch (value)
        {
            case >= -16 and <= 159: return ((Opcode)((int)Opcode.PUSH_CONST_0 + value)).ToString();

            case >= ushort.MinValue and <= ushort.MaxValue:
                return $"{Opcode.PUSH_CONST_U16} {unchecked((ushort)value)}";

            default:
                return $"{Opcode.PUSH_CONST_U32} {unchecked((uint)value)}";
        }
    }

    protected static string IntToLocalIV(int value)
    {
        switch (value)
        {
            case >= 0 and <= 6: return ((Opcode)((int)Opcode.LOCAL_0 + value)).ToString();

            default:
                return $@"{IntToPushInstIV(value)}
                          {Opcode.LOCAL}";
        }
    }

    protected static string IntToGlobalIV(int value)
    {
        return $@"{IntToPushInstIV(value)}
                  {Opcode.GLOBAL}";
    }

    protected static void CompileScript(string scriptSource, string expectedAssembly, string declarationsSource = "", string? expectedAssemblyIV = null, NativeDB? nativeDB = null)
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
            
            nativeDB: nativeDB);

    protected static void CompileRaw(string source, string expectedAssembly, string? expectedAssemblyIV = null, NativeDB? nativeDB = null)
    {
        nativeDB ??= NativeDB.Empty;

        var d = new DiagnosticsReport();
        var l = new Lexer("codegen_tests.sc", source, d);
        var p = new Parser(l, d);
        var s = new SemanticsAnalyzer(d);

        var u = p.ParseCompilationUnit();
        False(d.HasErrors);
        u.Accept(s);
        False(d.HasErrors);

        // GTA V codegen
        {
            var compiledScript = ScriptCompiler.Compile(u, new(Game.GTAV, Platform.x64));
            False(d.HasErrors);
            NotNull(compiledScript);
            var compiledScriptGTAV = IsType<GameFiles.Five.Script>(compiledScript);

            using var expectedAssemblyReader = new StringReader(expectedAssembly);
            var expectedAssembler = ScTools.ScriptAssembly.Targets.Five.Assembler.Assemble(expectedAssemblyReader, "test_expected.scasm", nativeDB,
                options: new() { IncludeFunctionNames = true });
            False(expectedAssembler.Diagnostics.HasErrors);

            string sourceDump = new DumperFiveV10().DumpToString(compiledScriptGTAV);
            string expectedDump = new DumperFiveV10().DumpToString(expectedAssembler.OutputScript);

            Util.AssertScriptsAreEqual(compiledScriptGTAV, expectedAssembler.OutputScript);
        }
        
        // GTA IV codegen
        if (expectedAssemblyIV is not null)
        {
            var compiledScript = ScriptCompiler.Compile(u, new(Game.GTAIV, Platform.x86));
            False(d.HasErrors);
            NotNull(compiledScript);
            var compiledScriptGTAIV = IsType<GameFiles.ScriptNY>(compiledScript);

            using var expectedAssemblyReader = new StringReader(expectedAssemblyIV);
            var expectedAssembler = ScTools.ScriptAssembly.Targets.NY.Assembler.Assemble(expectedAssemblyReader, "test_expected_ny.scasm", nativeDB);
            False(expectedAssembler.Diagnostics.HasErrors);

            string sourceDump = compiledScriptGTAIV.DumpToString();
            string expectedDump = expectedAssembler.OutputScript.DumpToString();

            Util.AssertScriptsAreEqual(compiledScriptGTAIV, expectedAssembler.OutputScript);
        }
    }
}
