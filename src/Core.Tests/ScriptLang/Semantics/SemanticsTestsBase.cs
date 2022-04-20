namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Generic;
using System.Linq;

using static Xunit.Assert;

public abstract class SemanticsTestsBase
{
    public static IEnumerable<object[]> GetAllHandleTypes() => HandleType.All.Select(h => new object[] { h });
    public static IEnumerable<object[]> GetAllTextLabelTypes() => TextLabelType.All.Select(h => new object[] { h });

    protected static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics, int expectedNumMatchingErrors = 1)
    {
        var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
        Equal(expectedNumMatchingErrors, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
    }

    protected const string TestFileName = "semantics_tests.sc";

    protected static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
        => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

    protected static ParserNew ParserFor(string source)
    {
        var lexer = new Lexer(TestFileName, source, new DiagnosticsReport());
        return new(lexer, lexer.Diagnostics);
    }

    protected static SemanticsAnalyzer Analyze(string source)
        => AnalyzeAndAst(source).Semantics;

    protected static (SemanticsAnalyzer Semantics, CompilationUnit Ast) AnalyzeAndAst(string source)
    {
        var p = ParserFor(source);

        var u = p.ParseCompilationUnit();
        var s = new SemanticsAnalyzer(p.Diagnostics);
        u.Accept(s);
        return (s, u);
    }
}
