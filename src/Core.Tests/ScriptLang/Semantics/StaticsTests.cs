namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;

using Xunit;

using static Xunit.Assert;

public class StaticsTests : SemanticsTestsBase
{
    [Theory]
    [InlineData("INT", typeof(IntType))]
    [InlineData("FLOAT", typeof(FloatType))]
    [InlineData("BOOL", typeof(BoolType))]
    [InlineData("STRING", typeof(StringType))]
    [InlineData("VECTOR", typeof(VectorType))]
    public void PrimitiveTypesWithoutInitializerAreAllowed(string typeStr, Type expectedTypeType)
    {
        var expectedType = (TypeInfo)Activator.CreateInstance(expectedTypeType)!;

        var s = Analyze(
            @$"{typeStr} foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", expectedType);
    }

    [Theory]
    [InlineData("1", 1, typeof(IntType))]
    [InlineData("NULL", 0, typeof(NullType))]
    [InlineData("`foo` - 1", 0x238678DD - 1, typeof(IntType))]
    [InlineData("(1 + 2 * 3) & 0xFE", (1 + 2 * 3) & 0xFE, typeof(IntType))]
    public void IntInitializerExpressionIsEvaluated(string initializerExpr, int expected, Type expectedConstantType)
    {
        var s = Analyze(
            @$"INT foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "foo", IntType.Instance, expected, (TypeInfo)Activator.CreateInstance(expectedConstantType)!);
    }

    [Theory]
    [InlineData("1.5", 1.5f, typeof(FloatType))]
    [InlineData("1", 1.0f, typeof(IntType))]
    [InlineData("NULL", 0.0f, typeof(NullType))]
    [InlineData("1.0 + 2.0 * 3.0", 1.0f + 2.0f * 3.0f, typeof(FloatType))]
    [InlineData("1 + 2 * 3", (float)(1 + 2 * 3), typeof(IntType))] // INT expression is promoted to FLOAT
    public void FloatInitializerExpressionIsEvaluated(string initializerExpr, float expected, Type expectedConstantType)
    {
        var s = Analyze(
            @$"FLOAT foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "foo", FloatType.Instance, expected, (TypeInfo)Activator.CreateInstance(expectedConstantType)!);
    }

    [Theory]
    [InlineData("TRUE", true, typeof(BoolType))]
    [InlineData("FALSE", false, typeof(BoolType))]
    [InlineData("NULL", false, typeof(NullType))]
    [InlineData("1", true, typeof(IntType))]
    [InlineData("0", false, typeof(IntType))]
    [InlineData("1+1", true, typeof(IntType))]
    [InlineData("TRUE AND FALSE", false, typeof(BoolType))]
    [InlineData("1 == 0 OR TRUE AND 1", true, typeof(BoolType))]
    public void BoolInitializerExpressionIsEvaluated(string initializerExpr, bool expected, Type expectedConstantType)
    {
        var s = Analyze(
            @$"BOOL foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "foo", BoolType.Instance, expected, (TypeInfo)Activator.CreateInstance(expectedConstantType)!);
    }

    [Theory]
    [InlineData("'hello'", "hello", typeof(StringType))]
    [InlineData("''", "", typeof(StringType))]
    [InlineData("'a\\nb'", "a\nb", typeof(StringType))]
    [InlineData("NULL", null, typeof(NullType))]
    public void StringInitializerExpressionIsEvaluated(string initializerExpr, string? expected, Type expectedConstantType)
    {
        var s = Analyze(
            @$"STRING foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "foo", StringType.Instance, expected, (TypeInfo)Activator.CreateInstance(expectedConstantType)!);
    }

    [Theory]
    [InlineData("<<0.0,0.0,0.0>>", 0.0f, 0.0f, 0.0f)]
    [InlineData("<<0,0,0>>", 0.0f, 0.0f, 0.0f)]
    [InlineData("<<1.0,2.0,3.0>> + <<2.0,2.0,2.0>> * <<3.0,3.0,3.0>>", 7.0f, 8.0f, 9.0f)]
    [InlineData("<<1,2,3>> + <<2,2,2>> * <<3,3,3>>", 7.0f, 8.0f, 9.0f)]
    public void VectorInitializerExpressionIsEvaluated(string initializerExpr, float expectedX, float expectedY, float expectedZ)
    {
        var s = Analyze(
            @$"VECTOR foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializerVec(s, "foo", expectedX, expectedY, expectedZ);
    }

    [Theory]
    [MemberData(nameof(GetAllHandleTypes))]
    public void HandleTypesWithoutInitializerAreAllowed(HandleType handleType)
    {
        var s = Analyze(
            @$"{HandleType.KindToTypeName(handleType.Kind)} foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", handleType);
    }

    [Theory]
    [MemberData(nameof(GetAllHandleTypes))]
    public void HandleTypesWithNullInitializerAreAllowed(HandleType handleType)
    {
        var s = Analyze(
            @$"{HandleType.KindToTypeName(handleType.Kind)} foo = NULL"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "foo", handleType, 0, NullType.Instance);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void TextLabelTypesWithInitializerAreAllowed(TextLabelType tlType)
    {
        var s = Analyze(
            @$"{TextLabelType.GetTypeNameForLength(tlType.Length)} foo = 'hello'"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "foo", tlType, "hello", StringType.Instance);
    }

    [Fact]
    public void CannotInitializeToFunctionInvocation()
    {
        var s = Analyze(
            @$"FUNC INT foo()
                RETURN 1
               ENDFUNC

               INT bar = foo()"
        );

        AssertStatic(s, "bar", IntType.Instance, hasInitializer: true);

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (5, 26), (5, 30), s.Diagnostics);
    }

    [Fact]
    public void CannotInitializeToOtherStaticVariable()
    {
        var s = Analyze(
            @$"INT foo = 1
               INT bar = foo"
        );

        AssertStaticWithInitializer(s, "foo", IntType.Instance, 1);
        AssertStatic(s, "bar", IntType.Instance, hasInitializer: true);

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (2, 26), (2, 28), s.Diagnostics);
    }

    [Fact]
    public void CanInitializeToConstant()
    {
        var s = Analyze(
            @$"CONST INT foo = 1
               INT bar = foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "bar", IntType.Instance, 1);
    }

    [Fact]
    public void CanInitializeTextLabelToString()
    {
        var s = Analyze(
            @$"CONST STRING foo = 'hello'
               TEXT_LABEL_63 bar = foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "bar", new TextLabelType(64), "hello", StringType.Instance);
    }

    [Fact]
    public void StructTypesAreAllowed()
    {
        var s = Analyze(
            @$"STRUCT MYDATA
                INT a = 1
               ENDSTRUCT

               MYDATA foo"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("MYDATA", out var structTy));
        AssertStatic(s, "foo", structTy!);
    }

    [Fact]
    public void EnumTypesAreAllowed()
    {
        var s = Analyze(
            @$"ENUM MYENUM
                A = 1
               ENDENUM

               MYENUM foo = A"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("MYENUM", out var enumTy));
        AssertStaticWithInitializer(s, "foo", enumTy!, 1, IntType.Instance);
    }

    [Fact]
    public void FunctionPointerTypesAreAllowed()
    {
        var s = Analyze(
            @$"PROCPTR FOOHANDLER()

               FOOHANDLER foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", new FunctionType(VoidType.Instance, ImmutableArray<ParameterInfo>.Empty));
    }

    [Fact]
    public void CannotInitializeFunctionPointer()
    {
        // TODO: should this be allowed? function addresses are constant after all
        var s = Analyze(
            @$"PROCPTR FOOHANDLER()

               PROC CUSTOM_HANDLER()
               ENDPROC

               FOOHANDLER foo = CUSTOM_HANDLER"
        );

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (6, 33), (6, 46), s.Diagnostics);
    }

    [Fact]
    public void ReferencesAreNotAllowed()
    {
        // This is now detected in the parser phase
        var s = Analyze(
            @$"INT& foo"
        );

        CheckError(ErrorCode.ParserReferenceNotAllowed, (1, 4), (1, 4), s.Diagnostics);
    }

    [Fact]
    public void AnyTypeIsAllowed()
    {
        var s = Analyze(
            @$"ANY foo = 1"
        );

        False(s.Diagnostics.HasErrors);
        AssertStaticWithInitializer(s, "foo", AnyType.Instance, 1, IntType.Instance);
    }

    [Fact]
    public void ArrayTypesAreAllowed()
    {
        var s = Analyze(
            @"INT foo1[10]
              INT foo2[10][20]
              INT foo3[10][20][30]"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo1", new ArrayType(IntType.Instance, 10));
        AssertStatic(s, "foo2", new ArrayType(new ArrayType(IntType.Instance, 20), 10));
        AssertStatic(s, "foo3", new ArrayType(new ArrayType(new ArrayType(IntType.Instance, 30), 20), 10));
    }

    [Fact]
    public void IncompleteArrayTypesAreNotAllowed()
    {
        // TODO: represent 'incomplete' arrays in type systems
        var s = Analyze(
            @"INT foo[]"
        );

        True(s.Diagnostics.HasErrors);
        //CheckError
    }

    private static void AssertStatic(SemanticsAnalyzer s, string varName, TypeInfo expectedType, bool hasInitializer = false)
    {
        True(s.GetSymbolUnchecked(varName, out var declaration));
        True(declaration is VarDeclaration);
        if (declaration is VarDeclaration constVar)
        {
            Equal(VarKind.Static, constVar.Kind);
            if (hasInitializer)
            {
                NotNull(constVar.Initializer);
            }
            else
            {
                Null(constVar.Initializer);
            }
            Equal(expectedType, constVar.Semantics.ValueType);
            Null(constVar.Semantics.ConstantValue);
        }
    }

    private static void AssertStaticWithInitializer<T>(SemanticsAnalyzer s, string varName, TypeInfo expectedType, T expectedValue, TypeInfo? constantExpectedType = null)
    {
        True(s.GetSymbolUnchecked(varName, out var declaration));
        True(declaration is VarDeclaration);
        if (declaration is VarDeclaration constVar)
        {
            Equal(VarKind.Static, constVar.Kind);
            NotNull(constVar.Initializer);
            Equal(expectedType, constVar.Semantics.ValueType);
            NotNull(constVar.Semantics.ConstantValue);
            Equal(constantExpectedType ?? expectedType, constVar.Semantics.ConstantValue!.Type);
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

    private static void AssertStaticWithInitializerVec(SemanticsAnalyzer s, string varName, float expectedX, float expectedY, float expectedZ)
    {
        True(s.GetSymbolUnchecked(varName, out var declaration));
        True(declaration is VarDeclaration);
        if (declaration is VarDeclaration constVar)
        {
            Equal(VarKind.Static, constVar.Kind);
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
