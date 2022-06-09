namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Types;

public class TextLabelTests : SemanticsTestsBase
{
    // Unclear what the syntax for text labels would be in the original language, so we are doing most operations through intrinsics.

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignString(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello world')
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignStringWithoutIntrinsics(TextLabelType tl)
    {
        var s = Analyze(
            @$"{TextLabelType.GetTypeNameForLength(tl.Length)} tlStatic = 'hello world'

               SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello world'
                 tl = 'hello world'
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }
    
    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignInt(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_INT(tl, 123)
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignTextLabel(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 TEXT_LABEL_31 tlSource
                 TEXT_LABEL_ASSIGN_STRING(tlSource, 'hello world')
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, tlSource)
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AssignSameTextLabelTypeWithoutIntrinsic(TextLabelType tl)
    {
        var tlName = TextLabelType.GetTypeNameForLength(tl.Length);
        var s = Analyze(
            @$"FUNC {tlName} GET_MY_TEXT()
                 {tlName} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello world')
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 {tlName} tl = GET_MY_TEXT()
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void AssignDifferentTextLabelTypesWithoutIntrinsic()
    {
        // TEXT_LABEL_COPY allows different lengths between source and destination text labels.
        var s = Analyze(
            @$"FUNC TEXT_LABEL_31 GET_MY_TEXT()
                 TEXT_LABEL_31 tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello world')
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 TEXT_LABEL_63 tl1 = GET_MY_TEXT()
                 TEXT_LABEL_15 tl2 = GET_MY_TEXT()
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AppendString(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello')
                 TEXT_LABEL_APPEND_STRING(tl, 'foobar')
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AppendInt(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello')
                 TEXT_LABEL_APPEND_INT(tl, 123)
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void AppendTextLabel(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 TEXT_LABEL_31 tlSource
                 TEXT_LABEL_ASSIGN_STRING(tlSource, 'hello world')
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'foobar')
                 TEXT_LABEL_APPEND_STRING(tl, tlSource)
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CannotAppendNonAddressableTextLabel(TextLabelType tl)
    {
        // TEXT_LABEL_APPEND_STRING requires an address to convert the TEXT_LABEL_n to STRING but return values are not addressable
        var s = Analyze(
            @$"FUNC TEXT_LABEL_31 GET_MY_TEXT()
                 TEXT_LABEL_31 tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello world')
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'foobar')
                 TEXT_LABEL_APPEND_STRING(tl, GET_MY_TEXT())
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArgCannotPassNonLValueToRefParam, (10, 47), (10, 59), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CannotAssignNonAddressableTextLabel(TextLabelType tl)
    {
        // TEXT_LABEL_ASSIGN_STRING requires an address to convert the TEXT_LABEL_n to STRING but return values are not addressable
        var s = Analyze(
            @$"FUNC TEXT_LABEL_31 GET_MY_TEXT()
                 TEXT_LABEL_31 tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello world')
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, GET_MY_TEXT())
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArgCannotPassNonLValueToRefParam, (9, 47), (9, 59), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CanPassTextLabelAsStringArgument(TextLabelType tl)
    {
        var s = Analyze(
            @$"PROC FOO(STRING s)
               ENDPROC

               SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello')
                 FOO(tl)
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CannotPassNonAddressableTextLabelAsStringArgument(TextLabelType tl)
    {
        var s = Analyze(
            @$"PROC FOO(STRING s)
               ENDFUNC

               FUNC {TextLabelType.GetTypeNameForLength(tl.Length)} GET_MY_TEXT()
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl
                 TEXT_LABEL_ASSIGN_STRING(tl, 'hello world')
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 FOO(GET_MY_TEXT())
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArgCannotPassNonLValueToRefParam, (11, 22), (11, 34), s.Diagnostics);
    }
}
