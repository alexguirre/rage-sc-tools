﻿namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

public class EnumTests : SemanticsTestsBase
{
    [Fact]
    public void MembersWithoutInitializersGetAssignedIncrementingValues()
    {
        var s = Analyze(
            @"ENUM foo
                A, B, C
              ENDENUM"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        AssertEnum(s, "A", enumTy, 0);
        AssertEnum(s, "B", enumTy, 1);
        AssertEnum(s, "C", enumTy, 2);
    }

    [Fact]
    public void MembersWithoutInitializersContinueIncrementingPreviousValue()
    {
        var s = Analyze(
            @"ENUM foo
                A, B = 5, C,
                D = -1, E
              ENDENUM"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        AssertEnum(s, "A", enumTy, 0);
        AssertEnum(s, "B", enumTy, 5);
        AssertEnum(s, "C", enumTy, 6);
        AssertEnum(s, "D", enumTy, -1);
        AssertEnum(s, "E", enumTy, 0);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("NULL", 0)] // NULL is implicity converted to INT as 0, so it should be allowed in enum initializers as well
    [InlineData("HASH('foo') - 1", 0x238678DD - 1)]
    [InlineData("(1 + 2 * 3) & 0xFE", (1 + 2 * 3) & 0xFE)]
    [InlineData("123 ^ 456", 123 ^ 456)]
    [InlineData("123 | 456", 123 | 456)]
    public void MemberInitializerExpressionsAreEvaluated(string initializerExpr, int expected)
    {
        var s = Analyze(
           @$"ENUM foo
                A = {initializerExpr}
              ENDENUM"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        AssertEnum(s, "A", enumTy, expected);
    }

    [Theory]
    [InlineData("A", 1)]
    [InlineData("ENUM_TO_INT(A) + 3", 4)]
    [InlineData("A | B", 3)]
    [InlineData("A & B", 0)]
    public void MemberInitializerUsesPreviousMembers(string initializerExpr, int expectedValue)
    {
        var s = Analyze(
            @$"ENUM foo
                A = 1,
                B = 2,
                C = {initializerExpr}
              ENDENUM"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        AssertEnum(s, "A", enumTy, 1);
        AssertEnum(s, "B", enumTy, 2);
        AssertEnum(s, "C", enumTy, expectedValue);
    }

    [Fact]
    public void MemberInitializerCannotUseMemberDeclaredAfter()
    {
        var s = Analyze(
            @"ENUM foo
                A = 1 + B,
                B
              ENDENUM"
        );

        CheckError(ErrorCode.SemanticUndefinedSymbol, (2, 25), (2, 25), s.Diagnostics);

        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        AssertEnum(s, "A", enumTy, 0); // error occurred so default to sequential numbering
        AssertEnum(s, "B", enumTy, 1);
    }

    [Fact]
    public void MemberInitializerCannotBeRecursive()
    {
        var s = Analyze(
            @"ENUM foo
                A = A + 1
              ENDENUM"
        );

        CheckError(ErrorCode.SemanticUndefinedSymbol, (2, 21), (2, 21), s.Diagnostics);

        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        AssertEnum(s, "A", enumTy, 0);
    }

    [Fact]
    public void EnumNameAlreadyDefined()
    {
        var (s, ast) = AnalyzeAndAst(
            @"INT foo
              ENUM foo
                A
              ENDENUM"
        );

        CheckError(ErrorCode.SemanticSymbolAlreadyDefined, (2, 20), (2, 22), s.Diagnostics);

        False(s.GetTypeSymbolUnchecked("foo", out _));

        AssertEnum(s, "A", new EnumType(ast.FindFirstNodeOfType<EnumDeclaration>()), 0);
    }

    [Fact]
    public void MemberNameAlreadyDefinedOutsideEnum()
    {
        var s = Analyze(
            @"INT A
              ENUM foo
                A
              ENDENUM"
        );

        CheckError(ErrorCode.SemanticSymbolAlreadyDefined, (3, 17), (3, 17), s.Diagnostics);
    }

    [Fact]
    public void MemberNameAlreadyDefinedInsideEnum()
    {
        var s = Analyze(
            @"ENUM foo
                A,
                A
              ENDENUM"
        );

        CheckError(ErrorCode.SemanticSymbolAlreadyDefined, (3, 17), (3, 17), s.Diagnostics);
    }

    [Fact]
    public void NonConstantInitializerIsNotAllowed()
    {
        var s = Analyze(
            @"FUNC INT BAR()
                RETURN 1
              ENDFUNC

              ENUM foo
                A = BAR()
              ENDENUM"
        );

        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (6, 21), (6, 25), s.Diagnostics);
    }

    [Fact]
    public void HashEnumValuesAreHashes()
    {
        var s = Analyze(
            @"HASH_ENUM foo
                AAA, BBB, CCC
              ENDENUM"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        True(enumTy.IsHash);
        AssertEnum(s, "AAA", enumTy, unchecked((int)"AAA".ToLowercaseHash()));
        AssertEnum(s, "BBB", enumTy, unchecked((int)"BBB".ToLowercaseHash()));
        AssertEnum(s, "CCC", enumTy, unchecked((int)"CCC".ToLowercaseHash()));
    }

    [Fact]
    public void HashEnumValueWithInitializer()
    {
        var s = Analyze(
            @"HASH_ENUM foo
                AAA = 123, BBB, CCC
              ENDENUM"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        True(enumTy.IsHash);
        AssertEnum(s, "AAA", enumTy, 123);
        AssertEnum(s, "BBB", enumTy, unchecked((int)"BBB".ToLowercaseHash()));
        AssertEnum(s, "CCC", enumTy, unchecked((int)"CCC".ToLowercaseHash()));
    }

    // TODO: support STRICT_ENUM specific behaviour
    [Fact]
    public void StrictEnum()
    {
        var s = Analyze(
            @"STRICT_ENUM foo
                A, B, C
              ENDENUM"
        );

        False(s.Diagnostics.HasErrors);
        True(s.GetTypeSymbolUnchecked("foo", out var ty));
        var enumTy = (EnumType)ty!;
        True(enumTy.IsStrict);
        AssertEnum(s, "A", enumTy, 0);
        AssertEnum(s, "B", enumTy, 1);
        AssertEnum(s, "C", enumTy, 2);
    }

    [Theory]
    [InlineData(@"PROC myProc()
                    MY_ENUM v = 0
                  ENDPROC")]
    [InlineData(@"PROC myProc()
                    MY_ENUM v
                    v = 0
                  ENDPROC")]
    [InlineData(@"PROC myProc(MY_ENUM v = 0)
                  ENDPROC")]
    [InlineData(@"FUNC INT myFunc(MY_ENUM v = 0)
                    RETURN 0
                  ENDFUNC")]
    [InlineData(@"TYPEDEF PROC myProcTypeDef(MY_ENUM v = 0)")]
    [InlineData(@"TYPEDEF FUNC INT myFuncTypeDef(MY_ENUM v = 0)")]
    [InlineData(@"NATIVE PROC myProcNative(MY_ENUM v = 0)")]
    [InlineData(@"NATIVE FUNC INT myFuncNative(MY_ENUM v = 0)")]
    [InlineData(@"PROC myProc(MY_ENUM v = 1-1)
                  ENDPROC")]
    public void ZeroCanBeAssignedToEnums(string src)
    {
        var s = Analyze(
            @$"
                ENUM MY_ENUM
                    MY_ENUM_A = 1,
                    MY_ENUM_B
                ENDENUM

                {src}
            "
        );
        
        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void EnumsCanUseBinaryAddOperator()
    {
        var s = Analyze(
            @"ENUM MY_ENUM
                A, B
              ENDENUM

              PROC myProc()
                MY_ENUM v = A + B
              ENDPROC"
        );
        
        False(s.Diagnostics.HasErrors);
    }
}
