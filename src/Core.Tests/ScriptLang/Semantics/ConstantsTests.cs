namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Types;

public class ConstantsTests : SemanticsTestsBase
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("NULL", 0)]
    [InlineData("`foo` - 1", 0x238678DD - 1)]
    [InlineData("(1 + 2 * 3) & 0xFE", (1 + 2 * 3) & 0xFE)]
    [InlineData("123 ^ 456", 123 ^ 456)]
    [InlineData("123 | 456", 123 | 456)]
    [InlineData("F2I(2.5)", 2)]
    public void IntInitializerExpressionIsEvaluated(string initializerExpr, int expected)
    {
        var s = Analyze(
            @$"CONST_INT foo {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expected);
    }

    [Theory]
    [InlineData("1.5", 1.5f)]
    [InlineData("1", 1.0f)]
    [InlineData("NULL", 0.0f)]
    [InlineData("1.0 + 2.0 * 3.0", 1.0f + 2.0f * 3.0f)]
    [InlineData("1 + 2 * 3", (float)(1 + 2 * 3))] // INT expression is promoted to FLOAT
    [InlineData("1 + 2.0 * 3", 1 + 2.0f * 3)]
    [InlineData("I2F(2)", 2.0f)]
    public void FloatInitializerExpressionIsEvaluated(string initializerExpr, float expected)
    {
        var s = Analyze(
            @$"CONST_FLOAT foo {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", FloatType.Instance, expected);
    }

    [Fact]
    public void CannotInitializeToFunctionInvocation()
    {
        var s = Analyze(
            @$"FUNC INT foo()
                RETURN 1
               ENDFUNC

               CONST_INT bar foo()"
        );

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (5, 30), (5, 34), s.Diagnostics);
    }

    [Fact]
    public void CannotInitializeToStaticVariable()
    {
        var s = Analyze(
            @$"INT foo = 1
               CONST_INT bar foo"
        );

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (2, 30), (2, 32), s.Diagnostics);
    }

    [Fact]
    public void CanInitializeToConstant()
    {
        var s = Analyze(
            @$"CONST_INT foo 1
               CONST_INT bar foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, 1);
        AssertConst(s, "bar", IntType.Instance, 1);
    }

    [Fact]
    public void CannotInitializeToConstantDefinedAfter()
    {
        var s = Analyze(
            @$"CONST_INT bar foo
               CONST_INT foo 1"
        );

        AssertConst(s, "foo", IntType.Instance, 1);

        CheckError(ErrorCode.SemanticUndefinedSymbol, (1, 15), (1, 17), s.Diagnostics);
    }
}
