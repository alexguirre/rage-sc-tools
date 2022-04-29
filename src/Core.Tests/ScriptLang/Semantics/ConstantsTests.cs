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

        CheckError(ErrorCode.SemanticConstantWithoutInitializer, (1, 11), (1, 13), s.Diagnostics);
    }

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
            @$"CONST INT foo = {initializerExpr}"
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
            @$"CONST FLOAT foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
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
    [InlineData("'hello' == NULL", false)]
    [InlineData("NULL <> 'hello'", true)]
    [InlineData("123 > 122", true)]
    [InlineData("121 >= 122", false)]
    [InlineData("123 < 122", false)]
    [InlineData("123 <= 123", true)]
    [InlineData("123.25 > 122.5", true)]
    [InlineData("121.75 >= 122.5", false)]
    [InlineData("123.25 < 122.5", false)]
    [InlineData("123.5 <= 123.5", true)]
    [InlineData("TRUE == TRUE", true)]
    [InlineData("TRUE <> FALSE", true)]
    [InlineData("IS_BIT_SET(1, 0)", true)]
    [InlineData("IS_BIT_SET(2, 1)", true)]
    [InlineData("IS_BIT_SET(2, 0)", false)]
    public void BoolInitializerExpressionIsEvaluated(string initializerExpr, bool expected)
    {
        var s = Analyze(
            @$"CONST BOOL foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
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

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", StringType.Instance, expected);
    }

    [Theory]
    [InlineData("<<0.0,0.0,0.0>>", 0.0f, 0.0f, 0.0f)]
    [InlineData("<<0,0,0>>", 0.0f, 0.0f, 0.0f)]
    [InlineData("<<1.0,2.0,3.0>> + <<2.0,2.0,2.0>> * <<3.0,3.0,3.0>>", 7.0f, 8.0f, 9.0f)]
    [InlineData("<<1,2,3>> + <<2,2,2>> * <<3,3,3>>", 7.0f, 8.0f, 9.0f)]
    [InlineData("F2V(1.0)", 1.0f, 1.0f, 1.0f)]
    public void VectorInitializerExpressionIsEvaluated(string initializerExpr, float expectedX, float expectedY, float expectedZ)
    {
        var s = Analyze(
            @$"CONST VECTOR foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertConstVec(s, "foo", expectedX, expectedY, expectedZ);
    }

    [Fact]
    public void CannotInitializeToFunctionInvocation()
    {
        var s = Analyze(
            @$"FUNC INT foo()
                RETURN 1
               ENDFUNC

               CONST INT bar = foo()"
        );

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (5, 32), (5, 36), s.Diagnostics);
    }

    [Fact]
    public void CannotInitializeToStaticVariable()
    {
        var s = Analyze(
            @$"INT foo = 1
               CONST INT bar = foo"
        );

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (2, 32), (2, 34), s.Diagnostics);
    }

    [Fact]
    public void CanInitializeToConstant()
    {
        var s = Analyze(
            @$"CONST INT foo = 1
               CONST INT bar = foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, 1);
        AssertConst(s, "bar", IntType.Instance, 1);
    }

    [Fact]
    public void CannotInitializeToConstantDefinedAfter()
    {
        var s = Analyze(
            @$"CONST INT bar = foo
               CONST INT foo = 1"
        );

        AssertConst(s, "foo", IntType.Instance, 1);

        CheckError(ErrorCode.SemanticUndefinedSymbol, (1, 17), (1, 19), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllHandleTypes))]
    public void HandleTypesAreNotAllowed(HandleType handleType)
    {
        var handleTypeName = HandleType.KindToTypeName(handleType.Kind);
        var s = Analyze(
            @$"CONST {handleTypeName} foo = NULL"
        );

        CheckError(ErrorCode.SemanticTypeNotAllowedInConstant, (1, 7), (1, 7 + handleTypeName.Length - 1), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void TextLabelTypesAreNotAllowed(TextLabelType tlType)
    {
        var tlTypeName = TextLabelType.GetTypeNameForLength(tlType.Length);
        var s = Analyze(
            @$"CONST {tlTypeName} foo = ''"
        );

        CheckError(ErrorCode.SemanticTypeNotAllowedInConstant, (1, 7), (1, 7 + tlTypeName.Length - 1), s.Diagnostics);
    }

    [Fact]
    public void StructTypesAreNotAllowed()
    {
        var s = Analyze(
            @$"STRUCT MYDATA
                INT a = 1
               ENDSTRUCT

               CONST MYDATA foo"
        );

        CheckError(ErrorCode.SemanticTypeNotAllowedInConstant, (5, 22), (5, 27), s.Diagnostics);
    }

    [Fact]
    public void EnumTypesAreAllowed()
    {
        var s = Analyze(
            @$"ENUM MYENUM
                A = 1
               ENDENUM

               CONST MYENUM foo = A"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("MYENUM", out var enumTy));
        AssertConst(s, "foo", enumTy!, 1);
    }

    [Fact]
    public void FunctionPointerTypesAreNotAllowed()
    {
        var s = Analyze(
            @$"PROCPTR FOOHANDLER()

               PROC CUSTOM_HANDLER()
               ENDPROC

               CONST FOOHANDLER foo = CUSTOM_HANDLER"
        );

        CheckError(ErrorCode.SemanticTypeNotAllowedInConstant, (6, 22), (6, 31), s.Diagnostics);
    }

    [Fact]
    public void ReferencesAreNotAllowed()
    {
        // This is now detected in the parser phase
        var s = Analyze(
            @$"CONST INT& foo"
        );

        CheckError(ErrorCode.ParserReferenceNotAllowed, (1, 10), (1, 10), s.Diagnostics);
    }

    [Fact]
    public void AnyTypeIsNotAllowed()
    {
        var s = Analyze(
            @$"CONST ANY foo = 1"
        );

        CheckError(ErrorCode.SemanticTypeNotAllowedInConstant, (1, 7), (1, 9), s.Diagnostics);
    }

    [Theory]
    [InlineData("[10]")]
    [InlineData("[10][20]")]
    [InlineData("[]")]
    public void ArrayTypesAreNotAllowed(string arraySize)
    {
        var s = Analyze(
            @$"CONST INT foo{arraySize}"
        );

        CheckError(ErrorCode.SemanticTypeNotAllowedInConstant, (1, 7), (1, 13 + arraySize.Length), s.Diagnostics);
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
