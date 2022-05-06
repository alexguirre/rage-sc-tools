namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;

using Xunit;

using static Xunit.Assert;

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
            @$"CONST INT foo = SIZE_OF({typeName})"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedSize);
    }

    [Theory]
    [InlineData("MYSTRUCT", "STRUCT MYSTRUCT\nENDSTRUCT", 0)]
    [InlineData("MYSTRUCT", "STRUCT MYSTRUCT\nINT a, b\nVECTOR c\nENDSTRUCT", 5)]
    [InlineData("MYENUM", "ENUM MYENUM\nENDENUM", 1)]
    [InlineData("MYENUM", "ENUM MYENUM\nA,B\nENDENUM", 1)]
    [InlineData("MYFUNCPTR", "PROCPTR MYFUNCPTR()", 1)]
    public void SizeOfWithUserTypes(string typeName, string typeDecl, int expectedSize)
    {
        var s = Analyze(
            @$"{typeDecl}
               CONST INT foo = SIZE_OF({typeName})"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedSize);
    }

    [Theory]
    [MemberData(nameof(GetAllHandleTypes))]
    public void SizeOfWithHandleTypes(HandleType handleType)
    {
        var s = Analyze(
            @$"CONST INT foo = SIZE_OF({HandleType.KindToTypeName(handleType.Kind)})"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedValue: handleType.SizeOf);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void SizeOfWithTextLabelTypes(TextLabelType tlType)
    {
        var s = Analyze(
            @$"CONST INT foo = SIZE_OF({TextLabelType.GetTypeNameForLength(tlType.Length)})"
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
               CONST INT foo = SIZE_OF(staticVar)
               CONST INT baz = SIZE_OF(staticArray)"
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
               CONST INT foo = COUNT_OF(staticArray)"
        );

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "foo", IntType.Instance, expectedLength);
    }

    [Theory]
    [InlineData("ENUM_TO_STRING(A)", "A")]
    [InlineData("ENUM_TO_STRING(B)", "B")]
    [InlineData("ENUM_TO_STRING(C)", "C")]
    [InlineData("ENUM_TO_STRING(INT_TO_ENUM(MYENUM, -1))", "ENUM_NOT_FOUND")]
    public void EnumToStringConstant(string initializer, string expectedString)
    {
        var s = Analyze(@$"
                ENUM MYENUM
                    A, B, C
                ENDENUM

                CONST STRING MYSTR = {initializer}
            ");

        False(s.Diagnostics.HasErrors);
        AssertConst(s, "MYSTR", StringType.Instance, expectedString);
    }
}
