namespace ScTools.Tests.ScriptLang.CodeGen;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Types;

using Xunit;

using static Xunit.Assert;

public class TextLabelTests : CodeGenTestsBase
{
    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignString(TextLabelType tl)
    {
        CompileScript(
        scriptSource: @$"
                {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello world'
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl.SizeOf}

                PUSH_CONST_0
                STRING
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                LEAVE 0, 0

                .string
                .str 'hello world'
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignStringStatically(TextLabelType tl)
    {
        CompileScript(
        scriptSource: @$"
                {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello world'
                tl = 'hello world'
            ",
        declarationsSource: @$"
                {TextLabelType.GetTypeNameForLength(tl.Length)} tlStatic = 'hello world'
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl.SizeOf}

                ; tlStatic = 'hello world'
                PUSH_CONST_0
                STRING
                STATIC_U8 0
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; tl = 'hello world' for var inititalizer
                PUSH_CONST_0
                STRING
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; tl = 'hello world' for assignment
                PUSH_CONST_0
                STRING
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                LEAVE 0, 0

                .string
                .str 'hello world'

                .static
                .int {tl.SizeOf} dup (0)
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignInt(TextLabelType tl)
    {
        CompileScript(
        scriptSource: @$"
                {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 123
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl.SizeOf}

                PUSH_CONST_U8 123
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_INT {tl.Length}

                LEAVE 0, 0
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignSameTextLabelType(TextLabelType tl)
    {
        // same TEXT_LABEL_n type can just use LOAD_N/STORE_N
        var tlName = TextLabelType.GetTypeNameForLength(tl.Length);
        CompileScript(
        scriptSource: @$"
                {tlName} tl = GET_MY_TEXT()
            ",
        declarationsSource: @$"
               FUNC {tlName} GET_MY_TEXT()
                 {tlName} tl = 'hello world'
                 RETURN tl
               ENDFUNC
            ",
        expectedAssembly: tl.SizeOf > 1 ? @$"
                ENTER 0, {2 + tl.SizeOf}

                ; tl = GET_MY_TEXT()
                CALL GET_MY_TEXT
                {IntToPushInst(tl.SizeOf)}
                LOCAL_U8 {2}
                STORE_N

                LEAVE 0, 0

            GET_MY_TEXT:
                ENTER 0, {2 + tl.SizeOf}

                ; tl = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 {2}
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; RETURN tl
                {IntToPushInst(tl.SizeOf)}
                LOCAL_U8 {2}
                LOAD_N
                LEAVE 0, {tl.SizeOf}
                .string
                .str 'hello world'
            "
            // special case for size 1 labels (TEXT_LABEL_7)
            : @$"
                ENTER 0, {2 + tl.SizeOf}

                ; tl = GET_MY_TEXT()
                CALL GET_MY_TEXT
                LOCAL_U8_STORE {2}

                LEAVE 0, 0

            GET_MY_TEXT:
                ENTER 0, {2 + tl.SizeOf}

                ; tl = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 {2}
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; RETURN tl
                LOCAL_U8_LOAD {2}
                LEAVE 0, {tl.SizeOf}
                .string
                .str 'hello world'
            ");
    }

    [Fact]
    public void AssignDifferentTextLabelTypes()
    {
        // TEXT_LABEL_COPY allows different lengths between source and destination text labels.
        var tl15 = new TextLabelType(16, 8);
        var tl31 = new TextLabelType(32, 8);
        var tl63 = new TextLabelType(64, 8);
        CompileScript(
        scriptSource: @$"
                 TEXT_LABEL_63 tl1 = GET_MY_TEXT()
                 TEXT_LABEL_15 tl2 = GET_MY_TEXT()
            ",
        declarationsSource: @$"
               FUNC TEXT_LABEL_31 GET_MY_TEXT()
                 TEXT_LABEL_31 tl = 'hello world'
                 RETURN tl
               ENDFUNC
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl63.SizeOf + tl15.SizeOf}

                ; TEXT_LABEL_63 tl1 = GET_MY_TEXT()
                CALL GET_MY_TEXT
                {IntToPushInst(tl31.SizeOf)}
                {IntToPushInst(tl63.SizeOf)}
                LOCAL_U8 {2}
                TEXT_LABEL_COPY

                ; TEXT_LABEL_15 tl2 = GET_MY_TEXT()
                CALL GET_MY_TEXT
                {IntToPushInst(tl31.SizeOf)}
                {IntToPushInst(tl15.SizeOf)}
                LOCAL_U8 {2 + tl63.SizeOf}
                TEXT_LABEL_COPY

                LEAVE 0, 0

            GET_MY_TEXT:
                ENTER 0, {2 + tl31.SizeOf}

                ; tl = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 {2}
                TEXT_LABEL_ASSIGN_STRING {tl31.Length}

                ; RETURN tl
                {IntToPushInst(tl31.SizeOf)}
                LOCAL_U8 {2}
                LOAD_N
                LEAVE 0, {tl31.SizeOf}
                .string
                .str 'hello world'
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AppendString(TextLabelType tl)
    {
        CompileScript(
        scriptSource: @$"
                {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello'
                tl += 'foobar'
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl.SizeOf}

                ; tl = 'hello'
                PUSH_CONST_0
                STRING
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; tl += 'foobar'
                PUSH_CONST_6
                STRING
                LOCAL_U8 2
                TEXT_LABEL_APPEND_STRING {tl.Length}

                LEAVE 0, 0

                .string
                .str 'hello'
                .str 'foobar'
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AppendInt(TextLabelType tl)
    {
        CompileScript(
        scriptSource: @$"
                {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello'
                tl += 123
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl.SizeOf}

                ; tl = 'hello'
                PUSH_CONST_0
                STRING
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; tl += 123
                PUSH_CONST_U8 123
                LOCAL_U8 2
                TEXT_LABEL_APPEND_INT {tl.Length}

                LEAVE 0, 0

                .string
                .str 'hello'
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AppendTextLabel(TextLabelType tl)
    {
        var tl31 = new TextLabelType(32, 8);
        CompileScript(
        scriptSource: @$"
                TEXT_LABEL_31 tlSource = 'hello world'
                {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'foobar'
                tl += tlSource
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl31.SizeOf + tl.SizeOf}

                ; tlSource = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 {2}
                TEXT_LABEL_ASSIGN_STRING {tl31.Length}

                ; tl = 'foobar'
                PUSH_CONST_U8 sFoo
                STRING
                LOCAL_U8 {2 + tl31.SizeOf}
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; tl += tlSource
                LOCAL_U8 {2}
                LOCAL_U8 {2 + tl31.SizeOf}
                TEXT_LABEL_APPEND_STRING {tl.Length}

                LEAVE 0, 0

                .string
                .str 'hello world'
           sFoo:.str 'foobar'
            ");
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CanPassTextLabelAsStringArgument(TextLabelType tl)
    {
        CompileScript(
        scriptSource: @$"
                {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello'
                FOO(tl)
            ",
        declarationsSource: @$"
                PROC FOO(STRING s)
                ENDPROC
            ",
        expectedAssembly: @$"
                ENTER 0, {2 + tl.SizeOf}

                ; tl = 'hello'
                PUSH_CONST_0
                STRING
                LOCAL_U8 {2}
                TEXT_LABEL_ASSIGN_STRING {tl.Length}

                ; FOO(tl)
                LOCAL_U8 {2}
                CALL FOO

                LEAVE 0, 0

            FOO:
                ENTER 1, 3
                LEAVE 1, 0

                .string
                .str 'hello'
            ");
    }
}
