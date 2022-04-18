namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Linq;
using Xunit;

using static Xunit.Assert;

public abstract class SemanticsTestsBase
{
    protected static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics, int expectedNumMatchingErrors = 1)
    {
        var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
        Equal(expectedNumMatchingErrors, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
    }

    protected const string TestFileName = "parser_tests.sc";

    protected static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
        => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

    protected static ParserNew ParserFor(string source)
    {
        var lexer = new Lexer(TestFileName, source, new DiagnosticsReport());
        return new(lexer, lexer.Diagnostics);
    }

    protected static SemanticsAnalyzer Analyze(string source)
    {
        var p = ParserFor(source);

        var u = p.ParseCompilationUnit();
        var s = new SemanticsAnalyzer(p.Diagnostics);
        u.Accept(s);
        return s;
    }
}
