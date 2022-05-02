namespace ScTools.Tests.ScriptLang.CodeGen;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;

using Xunit;

using static Xunit.Assert;

public class IntrinsicsTests : CodeGenTestsBase
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
        CompileScript(
        scriptSource: @$"
            INT foo = SIZE_OF({typeName})
        ",
        expectedAssembly: @$"
            ENTER 0, 3
            {IntToPushInst(expectedSize)}
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ");
    }

    [Theory]
    [InlineData("MYSTRUCT", "STRUCT MYSTRUCT\nENDSTRUCT", 0)]
    [InlineData("MYSTRUCT", "STRUCT MYSTRUCT\nINT a, b\nVECTOR c\nENDSTRUCT", 5)]
    [InlineData("MYENUM", "ENUM MYENUM\nENDENUM", 1)]
    [InlineData("MYENUM", "ENUM MYENUM\nA,B\nENDENUM", 1)]
    [InlineData("MYFUNCPTR", "PROCPTR MYFUNCPTR()", 1)]
    public void SizeOfWithUserTypes(string typeName, string typeDecl, int expectedSize)
    {
        CompileScript(
        scriptSource: @$"
            INT foo = SIZE_OF({typeName})
        ",
        declarationsSource: typeDecl,
        expectedAssembly: @$"
            ENTER 0, 3
            {IntToPushInst(expectedSize)}
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ");
    }

    [Theory]
    [MemberData(nameof(GetAllHandleTypes))]
    public void SizeOfWithHandleTypes(HandleType handleType)
    {
        CompileScript(
        scriptSource: @$"
            INT foo = SIZE_OF({HandleType.KindToTypeName(handleType.Kind)})
        ",
        expectedAssembly: @$"
            ENTER 0, 3
            {IntToPushInst(handleType.SizeOf)}
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void SizeOfWithTextLabelTypes(TextLabelType tlType)
    {
        CompileScript(
        scriptSource: @$"
            INT foo = SIZE_OF({TextLabelType.GetTypeNameForLength(tlType.Length)})
        ",
        expectedAssembly: @$"
            ENTER 0, 3
            {IntToPushInst(tlType.SizeOf)}
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ");
    }

    [Theory]
    [InlineData("INT", 1)]
    [InlineData("FLOAT", 1)]
    [InlineData("BOOL", 1)]
    [InlineData("STRING", 1)]
    [InlineData("ANY", 1)]
    [InlineData("VECTOR", 3)]
    public void SizeOfWithExpression(string varType, int expectedSize)
    {
        CompileScript(
        scriptSource: @$"
            {varType} baz
            INT foo = SIZE_OF(baz)
        ",
        expectedAssembly: @$"
            ENTER 0, {2 + expectedSize + 1}
            {IntToPushInst(expectedSize)}
            LOCAL_U8_STORE {2 + expectedSize}
            LEAVE 0, 0
        ");
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("8", 8)]
    [InlineData("1+2", 3)]
    [InlineData("1+2*3", 7)]
    public void CountOf(string lengthExpr, int expectedLength)
    {
        CompileScript(
        scriptSource: @$"
            INT arr[{lengthExpr}]
            INT foo = COUNT_OF(arr)
        ",
        expectedAssembly: @$"
            ENTER 0, {2 + 1 + expectedLength + 1}

            ; array initializer
            LOCAL_U8 2
            {IntToPushInst(expectedLength)}
            STORE_REV
            DROP

            ; foo = COUNT_OF(arr)
            {IntToPushInst(expectedLength)}
            LOCAL_U8_STORE {2 + 1 + expectedLength}
            LEAVE 0, 0
        ");
    }
}
