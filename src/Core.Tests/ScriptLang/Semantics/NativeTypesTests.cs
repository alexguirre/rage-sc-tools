namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

public class NativeTypesTests : SemanticsTestsBase
{
    [Fact]
    public void CanDeclareNativeType()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE"
        );

        False(s.Diagnostics.HasErrors);

        True(s.GetTypeSymbolUnchecked("MY_TYPE", out _));
        True(s.GetSymbolUnchecked("MY_TYPE", out _));
    }

    [Fact]
    public void CanDeclareNativeTypeWithBaseType()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE
               NATIVE MY_DERIVED_TYPE : MY_TYPE"
        );

        False(s.Diagnostics.HasErrors);

        True(s.GetTypeSymbolUnchecked("MY_TYPE", out var nativeTy));
        True(s.GetSymbolUnchecked("MY_TYPE", out _));
        
        True(s.GetTypeSymbolUnchecked("MY_DERIVED_TYPE", out var nativeTyDerived));
        True(s.GetSymbolUnchecked("MY_DERIVED_TYPE", out _));

        Equal(nativeTy, ((NativeType)nativeTyDerived!).Base);
    }

    [Fact]
    public void DerivedNativeTypesAreImplicitlyConvertibleToBaseType()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE
               NATIVE MY_DERIVED_TYPE_1 : MY_TYPE
               NATIVE MY_DERIVED_TYPE_2 : MY_TYPE
               NATIVE MY_DERIVED_DERIVED_TYPE : MY_DERIVED_TYPE_2

               SCRIPT my_script
                    MY_DERIVED_TYPE_1 d1 = NULL
                    MY_DERIVED_TYPE_2 d2 = NULL
                    MY_DERIVED_DERIVED_TYPE dd = NULL
                    MY_TYPE t = NULL

                    t = d1
                    t = d2
                    d2 = dd
                    t = dd
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CannotImplicitlyConvertNativeTypes()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE_1
               NATIVE MY_TYPE_2

               SCRIPT my_script
                    MY_TYPE_1 t1 = NULL
                    MY_TYPE_2 t2 = NULL

                    t1 = t2
                    t2 = t1
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (8, 26), (8, 27), s.Diagnostics);
        CheckError(ErrorCode.SemanticCannotConvertType, (9, 26), (9, 27), s.Diagnostics);
    }

    [Theory]
    [InlineData("INT")]
    [InlineData("STRING")]
    [InlineData("VECTOR")]
    public void BaseTypeMustBeNativeType(string baseType)
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE : {baseType}"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticExpectedNativeType, (1, 18), (1, 18 + baseType.Length - 1), s.Diagnostics);
    }

    [Fact]
    public void BaseTypeMustBeDeclared()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE : MY_UNKNOWN_TYPE"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticUndefinedSymbol, (1, 18), (1, 32), s.Diagnostics);
    }
}
