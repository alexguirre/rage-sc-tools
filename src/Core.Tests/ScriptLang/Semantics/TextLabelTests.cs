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

public class TextLabelTests : SemanticsTestsBase
{
    //TEXT_LABEL_ASSIGN_STRING = 0x65,
    //TEXT_LABEL_ASSIGN_INT = 0x66,
    //TEXT_LABEL_APPEND_STRING = 0x67,
    //TEXT_LABEL_APPEND_INT = 0x68,
    //TEXT_LABEL_COPY = 0x69,
    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void AssignString(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 'hello world'
                 tl = 'foobar'
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void AssignInt(TextLabelType tl)
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 {TextLabelType.GetTypeNameForLength(tl.Length)} tl = 123
                 tl = 456
               ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelTypes))]
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
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void AssignNonAddressableTextLabel(TextLabelType tl)
    {
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
    [MemberData(nameof(GetAllTextLabelTypes))]
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
    [MemberData(nameof(GetAllTextLabelTypes))]
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
    [MemberData(nameof(GetAllTextLabelTypes))]
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
    [MemberData(nameof(GetAllTextLabelTypes))]
    public void CannotAppendNonAddressableTextLabel(TextLabelType tl)
    {
        // TEXT_LABEL_APPEND_STRING requires an address but return values are not addressable
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
    }

    [Fact]
    public void BinaryAddOperatorIsNotSupported()
    {
        var s = Analyze(
            @$"SCRIPT my_script
                 TEXT_LABEL_31 tlA = 'hello'
                 TEXT_LABEL_31 tlB = 'world'
                 TEXT_LABEL_31 tl = tlA + tlB
               ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticBadBinaryOp, (4, 41), (4, 41), s.Diagnostics);
    }
}
