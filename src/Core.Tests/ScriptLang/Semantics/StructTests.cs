namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

public class StructTests : SemanticsTestsBase
{
    [Fact]
    public void FieldWithoutInitializer()
    {
        var s = Analyze(
            @"STRUCT foo
                INT a
              ENDSTRUCT"
        );

        False(s.Diagnostics.HasErrors);

        True(s.GetTypeSymbolUnchecked("foo", out _));
        True(s.GetSymbolUnchecked("foo", out var structDecl));
        AssertField((StructDeclaration)structDecl!, "a", IntType.Instance);
    }

    [Fact]
    public void FieldWithInitializer()
    {
        var s = Analyze(
            @"STRUCT foo
                INT a = 123
              ENDSTRUCT"
        );

        False(s.Diagnostics.HasErrors);

        True(s.GetTypeSymbolUnchecked("foo", out _));
        True(s.GetSymbolUnchecked("foo", out var structDecl));
        AssertField((StructDeclaration)structDecl!, "a", IntType.Instance, ConstantValue.Int(123));
    }

    [Fact]
    public void FieldWithNonConstantInitializerNotAllowed()
    {
        var s = Analyze(
            @"FUNC INT BAR()
                RETURN 123
              ENDFUNC

              STRUCT foo
                INT a = BAR()
              ENDSTRUCT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticInitializerExpressionIsNotConstant, (6, 25), (6, 29), s.Diagnostics);
    }

    [Fact]
    public void CannotRepeatFieldName()
    {
        var s = Analyze(
            @"STRUCT foo
                INT a
                INT a
              ENDSTRUCT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticSymbolAlreadyDefined, (3, 21), (3, 21), s.Diagnostics);
    }

    [Fact]
    public void StructCannotContainItself()
    {
        // `foo` is not yet defined inside the field declarations
        var s = Analyze(
            @"STRUCT foo
                foo a
              ENDSTRUCT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticUndefinedSymbol, (2, 17), (2, 19), s.Diagnostics);
    }

    [Fact]
    public void StructNameAlreadyDefined()
    {
        var s = Analyze(
            @"INT foo
              STRUCT foo
                INT a
              ENDSTRUCT"
        );

        CheckError(ErrorCode.SemanticSymbolAlreadyDefined, (2, 22), (2, 24), s.Diagnostics);

        False(s.GetTypeSymbolUnchecked("foo", out _));
    }

    private static void AssertField(StructDeclaration structDecl, string fieldName, TypeInfo expectedFieldType, ConstantValue? expectedInitializerValue = null)
    {
        var field = Single(structDecl.Fields, f => Parser.CaseInsensitiveComparer.Equals(f.Name, fieldName));
        Equal(VarKind.Field, field.Kind);
        Equal(expectedFieldType, field.Semantics.ValueType);
        Equal(expectedInitializerValue, field.Semantics.ConstantValue);
    }
}
