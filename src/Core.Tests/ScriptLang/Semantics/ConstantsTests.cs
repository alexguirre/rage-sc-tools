namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;

using Xunit;

using static Xunit.Assert;

public class ConstantsTests : SemanticsTestsBase
{
    [Fact]
    public void ConstantRequiresInitializer()
    {
        var s = Analyze(
            @"CONST INT foo"
        );

        CheckError(ErrorCode.SemanticExpectedInitializer, (1, 11), (1, 13), s.Diagnostics);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("NULL", 0)]
    [InlineData("`foo` - 1", 0x238678DD - 1)]
    [InlineData("(1 + 2 * 3) & 0xFE", (1 + 2 * 3) & 0xFE)]
    public void IntInitializerExpressionIsEvaluated(string initializerExpr, int expected)
    {
        var s = Analyze(
            @$"CONST INT foo = {initializerExpr}"
        );

        AssertConst(s, "foo", IntType.Instance, expected);
    }

    [Theory]
    [InlineData("1.5", 1.5f)]
    [InlineData("1", 1.0f)]
    [InlineData("NULL", 0.0f)]
    [InlineData("1.0 + 2.0 * 3.0", 1.0f + 2.0f * 3.0f)]
    [InlineData("1 + 2 * 3", (float)(1 + 2 * 3))] // INT expression is promoted to FLOAT
    public void FloatInitializerExpressionIsEvaluated(string initializerExpr, float expected)
    {
        var s = Analyze(
            @$"CONST FLOAT foo = {initializerExpr}"
        );

        AssertConst(s, "foo", FloatType.Instance, expected);
    }

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("FALSE", false)]
    [InlineData("NULL", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("1+1", true)]
    [InlineData("TRUE AND FALSE", false)]
    [InlineData("1 == 0 OR TRUE AND 1", true)]
    public void BoolInitializerExpressionIsEvaluated(string initializerExpr, bool expected)
    {
        var s = Analyze(
            @$"CONST BOOL foo = {initializerExpr}"
        );

        AssertConst(s, "foo", BoolType.Instance, expected);
    }

    [Theory]
    [InlineData("'hello'", "hello")]
    [InlineData("''", "")]
    [InlineData("'a\\nb'", "a\nb")]
    [InlineData("NULL", null)]
    public void StringInitializerExpressionIsEvaluated(string initializerExpr, string expected)
    {
        var s = Analyze(
            @$"CONST STRING foo = {initializerExpr}"
        );

        AssertConst(s, "foo", StringType.Instance, expected);
    }

    [Theory]
    [InlineData("<<0.0,0.0,0.0>>", 0.0f, 0.0f, 0.0f)]
    [InlineData("<<0,0,0>>", 0.0f, 0.0f, 0.0f)]
    [InlineData("<<1.0,2.0,3.0>> + <<2.0,2.0,2.0>> * <<3.0,3.0,3.0>>", 7.0f, 8.0f, 9.0f)]
    [InlineData("<<1,2,3>> + <<2,2,2>> * <<3,3,3>>", 7.0f, 8.0f, 9.0f)]
    public void VectorInitializerExpressionIsEvaluated(string initializerExpr, float expectedX, float expectedY, float expectedZ)
    {
        var s = Analyze(
            @$"CONST VECTOR foo = {initializerExpr}"
        );

        AssertConstVec(s, "foo", expectedX, expectedY, expectedZ);
    }

    private static void AssertConst<T>(SemanticsAnalyzer s, string varName, TypeInfo expectedType, T expectedValue)
    {
        True(s.GetSymbolUnchecked(varName, out var declaration));
        True(declaration is VarDeclaration);
        if (declaration is VarDeclaration constVar)
        {
            Equal(VarKind.Constant, constVar.Kind);
            NotNull(constVar.Initializer);
            Equal(expectedType, constVar.Semantics.ValueType);
            NotNull(constVar.Semantics.ConstantValue);
            Equal(expectedType, constVar.Semantics.ConstantValue!.Type);
            switch (expectedValue)
            {
                case int v: Equal(v, constVar.Semantics.ConstantValue!.IntValue); break;
                case float v: Equal(v, constVar.Semantics.ConstantValue!.FloatValue); break;
                case bool v: Equal(v, constVar.Semantics.ConstantValue!.BoolValue); break;
                case string v: Equal(v, constVar.Semantics.ConstantValue!.StringValue); break;
                default: throw new NotImplementedException();
            }
        }
    }

    private static void AssertConstVec(SemanticsAnalyzer s, string varName, float expectedX, float expectedY, float expectedZ)
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
