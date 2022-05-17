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
    [Fact]
    public void F2V()
    {
        CompileScript(
        scriptSource: @"
                VECTOR v = F2V(1.0)
            ",
        expectedAssembly: @"
                ENTER 0, 5

                PUSH_CONST_F1
                F2V
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                LEAVE 0, 0
            ");
    }

    [Fact]
    public void F2I()
    {
        CompileScript(
        scriptSource: @"
                INT i = F2I(1.0)
            ",
        expectedAssembly: @"
                ENTER 0, 3

                PUSH_CONST_F1
                F2I
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
    }

    [Fact]
    public void I2F()
    {
        CompileScript(
        scriptSource: @"
                FLOAT f = I2F(1)
            ",
        expectedAssembly: @"
                ENTER 0, 3

                PUSH_CONST_1
                I2F
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
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
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
    public void CountOfWithArrayOfKnownSize(string lengthExpr, int expectedLength)
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

    [Fact]
    public void CountOfWithArrayReferences()
    {
        CompileScript(
        scriptSource: @"
                INT array[10]
                INT i = COUNT_OF(array)
                TEST1(array)
                TEST2(array)
            ",
        declarationsSource: @"
                PROC TEST1(INT array[])
                    INT i = COUNT_OF(array)
                ENDPROC
                PROC TEST2(INT array[10])
                    INT i = COUNT_OF(array)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 14
                ; array initializer
                LOCAL_U8 2
                PUSH_CONST_U8 10
                STORE_REV
                DROP

                ; INT i = COUNT_OF(array)
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 13

                LOCAL_U8 2
                CALL TEST1

                LOCAL_U8 2
                CALL TEST2
                LEAVE 0, 0

            TEST1:
                ENTER 1, 4

                ; INT i = COUNT_OF(array)
                LOCAL_U8_LOAD 0
                LOAD
                LOCAL_U8_STORE 3

                LEAVE 1, 0

            TEST2:
                ENTER 1, 4

                ; INT i = COUNT_OF(array)
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 3

                LEAVE 1, 0
            ");
    }

    [Fact]
    public void EnumToString()
    {
        CompileScript(
        scriptSource: @"
                MYENUM e = B
                STRING s = ENUM_TO_STRING(e)
            ",
        declarationsSource: @"
                ENUM MYENUM
                    A, B, C
                ENDENUM
            ",
        expectedAssembly: @"
                ENTER 0, 4

                ; MYENUM e = B
                PUSH_CONST_1
                LOCAL_U8_STORE 2
                ; STRING s = ENUM_TO_STRING(e)
                LOCAL_U8_LOAD 2
                CALL __MYENUM_ENUM_TO_STRING
                LOCAL_U8_STORE 3

                LEAVE 0, 0

            __MYENUM_ENUM_TO_STRING:
                ENTER 1, 3

                LOCAL_U8_LOAD 0
                SWITCH 0:case0, 1:case1, 2:case2
                J default
            case0:
                PUSH_CONST_0 ; strMYENUM_A
                STRING
                LEAVE 1, 1
            case1:
                PUSH_CONST_2 ; strMYENUM_B
                STRING
                LEAVE 1, 1
            case2:
                PUSH_CONST_4 ; strMYENUM_C
                STRING
                LEAVE 1, 1
            default:
                PUSH_CONST_6 ; strENUM_NOT_FOUND
                STRING
                LEAVE 1, 1


                .string
            strMYENUM_A:        .str 'A'
            strMYENUM_B:        .str 'B'
            strMYENUM_C:        .str 'C'
            strENUM_NOT_FOUND:  .str 'ENUM_NOT_FOUND'
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllHandleTypes))]
    public void NativeToInt(HandleType handleType)
    {
        CompileScript(
        scriptSource: @$"
            {HandleType.KindToTypeName(handleType.Kind)} handle
            INT foo = NATIVE_TO_INT(handle)
        ",
        expectedAssembly: @$"
            ENTER 0, 4
            LOCAL_U8_LOAD 2
            LOCAL_U8_STORE 3
            LEAVE 0, 0
        ");
    }
}
