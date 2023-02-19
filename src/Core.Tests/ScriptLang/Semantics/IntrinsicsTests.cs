namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Types;

public class IntrinsicsTests : SemanticsTestsBase
{
    [Theory]
    [InlineData("INT", 1)]
    [InlineData("FLOAT", 1)]
    [InlineData("BOOL", 1)]
    [InlineData("STRING", 1)]
    [InlineData("ANY", 1)]
    [InlineData("VECTOR", 3)]
    public void SizeOfWithPrimitiveTypes(string typeName, int expectedSize)
    {
        var s = Analyze(
            @$"CONST_INT foo SIZE_OF({typeName})"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedSize);
    }

    [Theory]
    [InlineData("MYSTRUCT", "STRUCT MYSTRUCT\nENDSTRUCT", 0)]
    [InlineData("MYSTRUCT", "STRUCT MYSTRUCT\nINT a, b\nVECTOR c\nENDSTRUCT", 5)]
    [InlineData("MYENUM", "ENUM MYENUM\nENDENUM", 1)]
    [InlineData("MYENUM", "ENUM MYENUM\nA,B\nENDENUM", 1)]
    [InlineData("MYFUNCPTR", "TYPEDEF PROC MYFUNCPTR()", 1)]
    public void SizeOfWithUserTypes(string typeName, string typeDecl, int expectedSize)
    {
        var s = Analyze(
            @$"{typeDecl}
               CONST_INT foo SIZE_OF({typeName})"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedSize);
    }

    [Fact]
    public void SizeOfWithNativeTypes()
    {
        var s = Analyze(
            @$"NATIVE MY_TYPE
               CONST_INT foo SIZE_OF(MY_TYPE)"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedValue: 1);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void SizeOfWithTextLabelTypes(TextLabelType tlType)
    {
        var s = Analyze(
            @$"CONST_INT foo SIZE_OF({TextLabelType.GetTypeNameForLength(tlType.Length)})"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedValue: tlType.SizeOf);
    }

    [Theory]
    [InlineData("INT", 1)]
    [InlineData("FLOAT", 1)]
    [InlineData("BOOL", 1)]
    [InlineData("STRING", 1)]
    [InlineData("ANY", 1)]
    [InlineData("VECTOR", 3)]
    public void SizeOfWithExpression(string staticVarType, int expectedSize)
    {
        var s = Analyze(
            @$"{staticVarType} staticVar
               {staticVarType} staticArray[15]
               CONST_INT foo SIZE_OF(staticVar)
               CONST_INT baz SIZE_OF(staticArray)"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedSize);
        AssertConst(s, "baz", IntType.Instance, 1 + expectedSize * 15);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("8", 8)]
    [InlineData("1+2", 3)]
    [InlineData("1+2*3", 7)]
    public void CountOf(string lengthExpr, int expectedLength)
    {
        var s = Analyze(
            @$"INT staticArray[{lengthExpr}]
               CONST_INT foo COUNT_OF(staticArray)"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedLength);
    }

    [Fact]
    public void NativeToInt()
    {
        var s = Analyze(@$"
            NATIVE MY_TYPE
            SCRIPT test_script
                MY_TYPE handle
                INT foo = NATIVE_TO_INT(handle)
            ENDSCRIPT
        ");

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void IntToNative()
    {
        var s = Analyze(@$"
            NATIVE MY_TYPE
            SCRIPT test_script
                MY_TYPE handle = INT_TO_NATIVE(MY_TYPE, 1)
            ENDSCRIPT
        ");

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [InlineData("INT")]
    [InlineData("FLOAT")]
    [InlineData("BOOL")]
    [InlineData("STRING")]
    [InlineData("ANY")]
    [InlineData("VECTOR")]
    [InlineData("TEXT_LABEL_63")]
    public void NativeToIntOnlyAllowsHandleTypes(string typeName)
    {
        var s = Analyze(@$"
            SCRIPT test_script
                {typeName} v
                INT foo = NATIVE_TO_INT(v)
            ENDSCRIPT
        ");

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArgNotANativeTypeValue, (4, 41), (4, 41), s.Diagnostics);
    }

    [Fact]
    public void Hash()
    {
        var s = Analyze(@$"
            SCRIPT test_script
                INT foo = HASH('Test')
            ENDSCRIPT
        ");

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void HashWorksInEnums()
    {
        var s = Analyze(@$"
            ENUM MY_ENUM
                MY_A = HASH('A'),
                MY_B = HASH('B'),
                MY_C = HASH('C')
            ENDENUM
        ");
        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("MY_ENUM", out var ty));
        var enumTy = (EnumType)ty!;
        AssertEnum(s, "MY_A", enumTy, unchecked((int)"A".ToLowercaseHash()));
        AssertEnum(s, "MY_B", enumTy, unchecked((int)"B".ToLowercaseHash()));
        AssertEnum(s, "MY_C", enumTy, unchecked((int)"C".ToLowercaseHash()));
    }
}
