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
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello world'
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
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 123
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
                 TEXT_LABEL_31 tlSource = 'hello world'
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = tlSource
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
                 {tlName} tl = 'hello world'
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
                 TEXT_LABEL_31 tl = 'hello world'
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
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello'
                 tl += 'foobar'
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
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello'
                 tl += 123
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
                 TEXT_LABEL_31 tlSource = 'hello world'
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'foobar'
                 tl += tlSource
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
                 TEXT_LABEL_31 tl = 'hello world'
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'foobar'
                 tl += GET_MY_TEXT()
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTextLabelAppendNonAddressableTextLabel, (8, 24), (8, 36), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CannotAppendUnsupportedType(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'foobar'
                 tl += TRUE
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTextLabelAppendInvalidType, (3, 24), (3, 27), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CanAssignNonAddressableTextLabel(TextLabelType tl)
    {
        // Uses TEXT_LABEL_COPY, not TEXT_LABEL_ASSIGN_STRING, so the RHS doesn't need to be addressable, it is pushed to the stack.
        var s = Analyze(
            @$"FUNC TEXT_LABEL_31 GET_MY_TEXT()
                 TEXT_LABEL_31 tl = 'hello world'
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = GET_MY_TEXT()
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CanPassTextLabelAsStringArgument(TextLabelType tl)
    {
        var s = Analyze(
            @$"PROC FOO(STRING s)
               ENDPROC

               SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello'
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
               ENDPROC

               FUNC {TextLabelType.GetTypeNameForLength(tl.Length)} GET_MY_TEXT()
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello world'
                 RETURN tl
               ENDFUNC

               SCRIPT my_script
                 FOO(GET_MY_TEXT())
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArgCannotPassNonLValueToRefParam, (10, 22), (10, 34), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CannotUseBinaryAddOperator(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello world'
                 tl = tl + 'foobar'
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticBadBinaryOp, (3, 23), (3, 35), s.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes64Bit))]
    public void CannotUseOtherCompoundOperators(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'foobar'
                 tl -= 1
                 tl *= 1
                 tl /= 1
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTextLabelOnlyAppendSupported, (3, 21), (3, 22), s.Diagnostics);
        CheckError(ErrorCode.SemanticTextLabelOnlyAppendSupported, (4, 21), (4, 22), s.Diagnostics);
        CheckError(ErrorCode.SemanticTextLabelOnlyAppendSupported, (5, 21), (5, 22), s.Diagnostics);
    }
}
