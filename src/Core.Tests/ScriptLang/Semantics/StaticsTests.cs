namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

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
    [InlineData("1")]
    [InlineData("NULL")]
    [InlineData("HASH('foo') - 1")]
    [InlineData("(1 + 2 * 3) & 0xFE")]
    public void IntInitializerExpressionIsEvaluated(string initializerExpr)
    {
        var s = Analyze(
            @$"INT foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", IntType.Instance, hasInitializer: true);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("1")]
    [InlineData("NULL")]
    [InlineData("1.0 + 2.0 * 3.0")]
    [InlineData("1 + 2 * 3")] // INT expression is promoted to FLOAT
    public void FloatInitializerExpressionIsEvaluated(string initializerExpr)
    {
        var s = Analyze(
            @$"FLOAT foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", FloatType.Instance, hasInitializer: true);
    }

    [Theory]
    [InlineData("TRUE")]
    [InlineData("FALSE")]
    [InlineData("NULL")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("1+1")]
    [InlineData("TRUE AND FALSE")]
    [InlineData("1 == 0 OR TRUE AND 1")]
    public void BoolInitializerExpressionIsEvaluated(string initializerExpr)
    {
        var s = Analyze(
            @$"BOOL foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", BoolType.Instance, hasInitializer: true);
    }

    [Theory]
    [InlineData("'hello'")]
    [InlineData("''")]
    [InlineData("'a\\nb'")]
    [InlineData("NULL")]
    public void StringInitializerExpressionIsEvaluated(string initializerExpr)
    {
        var s = Analyze(
            @$"STRING foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", StringType.Instance, hasInitializer: true);
    }

    [Theory]
    [InlineData("<<0.0,0.0,0.0>>")]
    [InlineData("<<0,0,0>>")]
    [InlineData("<<1.0,2.0,3.0>> + <<2.0,2.0,2.0>> * <<3.0,3.0,3.0>>")]
    [InlineData("<<1,2,3>> + <<2,2,2>> * <<3,3,3>>")]
    public void VectorInitializerExpressionIsEvaluated(string initializerExpr)
    {
        var s = Analyze(
            @$"VECTOR foo = {initializerExpr}"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", VectorType.Instance, hasInitializer: true);
    }

    [Fact]
    public void NativeTypesWithoutInitializerAreAllowed()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE
               MY_TYPE foo"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("MY_TYPE", out var ty));
        var nativeTy = (NativeType)ty!;
        AssertStatic(s, "foo", nativeTy);
    }

    [Fact]
    public void NativeTypesWithNullInitializerAreAllowed()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE
               MY_TYPE foo = NULL"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("MY_TYPE", out var ty));
        var nativeTy = (NativeType)ty!;
        AssertStatic(s, "foo", nativeTy, hasInitializer: true);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void TextLabelTypesWithInitializerAreAllowed(TextLabelType tlType)
    {
        var s = Analyze(
            @$"{TextLabelType.GetTypeNameForLength(tlType.Length)} foo = 'hello'"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", tlType, hasInitializer: true);
    }

    [Fact]
    public void CanInitializeToFunctionInvocation()
    {
        var s = Analyze(
            @$"FUNC INT foo()
                RETURN 1
               ENDFUNC

               INT bar = foo()"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "bar", IntType.Instance, hasInitializer: true);
    }

    [Fact]
    public void CanInitializeToOtherStaticVariable()
    {
        var s = Analyze(
            @$"INT foo = 1
               INT bar = foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", IntType.Instance, hasInitializer: true);
        AssertStatic(s, "bar", IntType.Instance, hasInitializer: true);
    }

    [Fact]
    public void CanInitializeToConstant()
    {
        var s = Analyze(
            @$"CONST_INT foo 1
               INT bar = foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "bar", IntType.Instance, hasInitializer: true);
    }

    [Fact]
    public void CanInitializeTextLabelToString()
    {
        var s = Analyze(
            @$"TEXT_LABEL_63 bar = 'hello'"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "bar", new TextLabelType(64, 8), hasInitializer: true);
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
        AssertStatic(s, "foo", enumTy!, hasInitializer: true);
    }

    [Fact]
    public void FunctionTypeDefsAreAllowed()
    {
        var s = Analyze(
            @$"TYPEDEF PROC FOOHANDLER()

               FOOHANDLER foo"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", new FunctionType(VoidType.Instance, ImmutableArray<ParameterInfo>.Empty));
    }

    [Fact]
    public void FunctionTypeDefsWithInitializerAreAllowed()
    {
        var s = Analyze(
            @$"TYPEDEF PROC FOOHANDLER()

               PROC CUSTOM_HANDLER()
               ENDPROC

               FOOHANDLER foo = CUSTOM_HANDLER"
        );

        False(s.Diagnostics.HasErrors);
        AssertStatic(s, "foo", new FunctionType(VoidType.Instance, ImmutableArray<ParameterInfo>.Empty), hasInitializer: true);
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
        AssertStatic(s, "foo", AnyType.Instance, hasInitializer: true);
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

    // TODO: IncompleteArrayTypesAreNotAllowed
    [Fact(Skip = "Incomplete array type is not yet supported")]
    public void IncompleteArrayTypesAreNotAllowed()
    {
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
}
