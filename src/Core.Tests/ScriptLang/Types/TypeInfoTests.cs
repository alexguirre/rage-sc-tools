namespace ScTools.Tests.ScriptLang.Types;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Xunit;

using static Xunit.Assert;

public class TypeInfoTests
{
    [Theory]
    [InlineData(typeof(ErrorType))]
    [InlineData(typeof(VoidType))]
    [InlineData(typeof(IntType))]
    [InlineData(typeof(FloatType))]
    [InlineData(typeof(BoolType))]
    [InlineData(typeof(StringType))]
    [InlineData(typeof(NullType))]
    [InlineData(typeof(AnyType))]
    [InlineData(typeof(VectorType))]
    public void DifferentInstancesOfSameTypeMustBeEqual(Type type)
    {
        var typeInfoA = (TypeInfo)Activator.CreateInstance(type)!;
        var typeInfoB = (TypeInfo)Activator.CreateInstance(type)!;

        EqualAndSameHashCode(typeInfoA, typeInfoB);
    }

    [Theory]
    [MemberData(nameof(GetAllHandleKinds))]
    public void DifferentInstancesOfSameHandleTypeMustBeEqual(HandleKind kind)
    {
        var handleA = new HandleType(kind);
        var handleB = new HandleType(kind);

        EqualAndSameHashCode(handleA, handleB);
    }
    public static IEnumerable<object[]> GetAllHandleKinds() => HandleType.All.Select(h => new object[] { h.Kind });

    [Theory]
    [MemberData(nameof(GetAllTextLabelLengths))]
    public void DifferentInstancesOfSameTextLabelTypeMustBeEqual(int length)
    {
        var tlA = new TextLabelType(length);
        var tlB = new TextLabelType(length);

        EqualAndSameHashCode(tlA, tlB);
    }
    public static IEnumerable<object[]> GetAllTextLabelLengths() => TextLabelType.All.Select(tl => new object[] { tl.Length });

    [Fact]
    public void DifferentInstancesOfSameEnumTypeMustBeEqual()
    {
        var enumDecl = new EnumDeclaration(TokenKind.ENUM.Create(), Token.Identifier("foo"), TokenKind.ENDENUM.Create(), Enumerable.Empty<EnumMemberDeclaration>());
        var enumTypeA = new EnumType(enumDecl);
        var enumTypeB = new EnumType(enumDecl);

        EqualAndSameHashCode(enumTypeA, enumTypeB);
    }

    [Fact]
    public void DifferentInstancesOfSameStructTypeMustBeEqual()
    {
        var structDecl = new StructDeclaration(TokenKind.STRUCT.Create(), Token.Identifier("foo"), TokenKind.ENDSTRUCT.Create(),
            new[] { new VarDeclaration(new TypeName(Token.Identifier("INT")), new VarDeclarator(Token.Identifier("a")), VarKind.Field) });
        var structTypeA = new StructType(structDecl, ImmutableArray.Create(new FieldInfo(new IntType(), "a", 0)));
        var structTypeB = new StructType(structDecl, ImmutableArray.Create(new FieldInfo(new IntType(), "a", 0)));

        EqualAndSameHashCode(structTypeA, structTypeB);
    }

    [Fact]
    public void DifferentInstancesOfSameFunctionTypeMustBeEqual()
    {
        var funcA = new FunctionType(new IntType(), ImmutableArray.Create(new ParameterInfo(new StringType(), true)));
        var funcB = new FunctionType(new IntType(), ImmutableArray.Create(new ParameterInfo(new StringType(), true)));

        EqualAndSameHashCode(funcA, funcB);
    }

    [Fact]
    public void DifferentInstancesOfSameArrayTypeMustBeEqual()
    {
        var arrA = new ArrayType(new ArrayType(new IntType(), 20), 5);
        var arrB = new ArrayType(new ArrayType(new IntType(), 20), 5);

        EqualAndSameHashCode(arrA, arrB);
    }

    [Fact]
    public void DifferentInstancesOfSameTypeNameTypeMustBeEqual()
    {
        var typeDecl = new EnumDeclaration(TokenKind.ENUM.Create(), Token.Identifier("foo"), TokenKind.ENDENUM.Create(), Enumerable.Empty<EnumMemberDeclaration>());
        var typeNameA = new TypeNameType(typeDecl);
        var typeNameB = new TypeNameType(typeDecl);

        EqualAndSameHashCode(typeNameA, typeNameB);
    }

    private static void EqualAndSameHashCode<T>(T a, T b)
    {
        Equal(a, b);
        Equal(a.GetHashCode(), b.GetHashCode());
    }
}
