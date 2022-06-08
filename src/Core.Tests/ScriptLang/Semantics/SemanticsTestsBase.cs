namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Generic;
using System.Linq;

using static Xunit.Assert;

public abstract class SemanticsTestsBase
{
    public static IEnumerable<object[]> GetAllHandleTypes() => HandleType.All.Select(h => new object[] { h });
    public static IEnumerable<object[]> GetAllTextLabelTypes64Bit() => TextLabelType.All64.Select(tl => new object[] { tl });

    protected static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics, int expectedNumMatchingErrors = 1)
    {
        var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
        Equal(expectedNumMatchingErrors, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
    }

    protected const string TestFileName = "semantics_tests.sc";

    protected static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
        => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

    protected static Parser ParserFor(string source)
    {
        var lexer = new Lexer(TestFileName, source, new DiagnosticsReport());
        return new(lexer, lexer.Diagnostics);
    }

    protected static SemanticsAnalyzer Analyze(string source, IUsingResolver? usingResolver = null)
        => AnalyzeAndAst(source, usingResolver).Semantics;

    protected static (SemanticsAnalyzer Semantics, CompilationUnit Ast) AnalyzeAndAst(string source, IUsingResolver? usingResolver = null)
    {
        var p = ParserFor(source);

        var u = p.ParseCompilationUnit();
        var s = new SemanticsAnalyzer(p.Diagnostics, usingResolver);
        u.Accept(s);
        return (s, u);
    }

    protected static void AssertConst<T>(SemanticsAnalyzer s, string varName, TypeInfo expectedType, T expectedValue)
    {
        True(s.GetSymbolUnchecked(varName, out var declaration));
        True(declaration is VarDeclaration);
        if (declaration is VarDeclaration constVar)
        {
            Equal(VarKind.Constant, constVar.Kind);
            NotNull(constVar.Initializer);
            Equal(expectedType, constVar.Semantics.ValueType);
            NotNull(constVar.Semantics.ConstantValue);
            switch (expectedValue)
            {
                case int v: Equal(v, constVar.Semantics.ConstantValue!.IntValue); break;
                case float v: Equal(v, constVar.Semantics.ConstantValue!.FloatValue); break;
                case bool v: Equal(v, constVar.Semantics.ConstantValue!.BoolValue); break;
                case string v: Equal(v, constVar.Semantics.ConstantValue!.StringValue); break;
                case null: Null(constVar.Semantics.ConstantValue!.StringValue); break;
                default: throw new NotImplementedException();
            }
        }
    }

    protected static void AssertConstVec(SemanticsAnalyzer s, string varName, float expectedX, float expectedY, float expectedZ)
    {
        True(s.GetSymbolUnchecked(varName, out var declaration));
        True(declaration is VarDeclaration);
        if (declaration is VarDeclaration constVar)
        {
            Equal(VarKind.Constant, constVar.Kind);
            NotNull(constVar.Initializer);
            Equal(VectorType.Instance, constVar.Semantics.ValueType);
            NotNull(constVar.Semantics.ConstantValue);
            Equal(VectorType.Instance, constVar.Semantics.ConstantValue!.Type);
            var (x, y, z) = constVar.Semantics.ConstantValue!.VectorValue;
            Equal(expectedX, x);
            Equal(expectedY, y);
            Equal(expectedZ, z);
        }
    }
}
