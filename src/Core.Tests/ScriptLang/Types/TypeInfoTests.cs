namespace ScTools.Tests.ScriptLang.Types;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

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

    [Fact]
    public void DifferentInstancesOfSameNativeTypeMustBeEqual()
    {
        var decl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("MY_TYPE"), null);
        var handleA = new NativeType(decl, null);
        var handleB = new NativeType(decl, null);

        EqualAndSameHashCode(handleA, handleB);
    }

    [Fact]
    public void NativeTypesAssignmentAndInheritanceSemanticsAreCorrect()
    {
        var baseADecl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("BASE_A"), null);
        var baseBDecl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("BASE_B"), null);
        var derivedA1Decl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("DERIVED_A1"), new(Token.Identifier("BASE_A")));
        var derivedA2Decl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("DERIVED_A2"), new(Token.Identifier("BASE_A")));
        var derivedB1Decl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("DERIVED_B1"), new(Token.Identifier("BASE_B")));
        var derivedB2Decl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("DERIVED_B2"), new(Token.Identifier("BASE_B")));
        var baseA = new NativeType(baseADecl, null);
        var baseB = new NativeType(baseBDecl, null);
        var derivedA1 = new NativeType(derivedA1Decl, baseA);
        var derivedA2 = new NativeType(derivedA2Decl, baseA);
        var derivedB1 = new NativeType(derivedB1Decl, baseB);
        var derivedB2 = new NativeType(derivedB2Decl, baseB);

        // is assignable from itself
        True(baseA.IsAssignableFrom(baseA));
        True(baseB.IsAssignableFrom(baseB));
        True(derivedA1.IsAssignableFrom(derivedA1));
        True(derivedA2.IsAssignableFrom(derivedA2));
        True(derivedB1.IsAssignableFrom(derivedB1));
        True(derivedB2.IsAssignableFrom(derivedB2));

        // base classes is assignable from derived classes
        True(baseA.IsAssignableFrom(derivedA1));
        True(baseA.IsAssignableFrom(derivedA2));
        True(baseB.IsAssignableFrom(derivedB1));
        True(baseB.IsAssignableFrom(derivedB2));

        // derived classes are not assignable from base classes
        False(derivedA1.IsAssignableFrom(baseA));
        False(derivedA2.IsAssignableFrom(baseA));
        False(derivedB1.IsAssignableFrom(baseB));
        False(derivedB2.IsAssignableFrom(baseB));

        // derived classes with same base class are assignable from each other
        False(derivedA1.IsAssignableFrom(derivedA2));
        False(derivedA2.IsAssignableFrom(derivedA1));
        False(derivedB1.IsAssignableFrom(derivedB2));
        False(derivedB2.IsAssignableFrom(derivedB1));

        // different base classes are not assignable from each other
        False(baseA.IsAssignableFrom(derivedB1));
        False(baseA.IsAssignableFrom(derivedB2));
        False(baseB.IsAssignableFrom(derivedA1));
        False(baseB.IsAssignableFrom(derivedA2));
    }

    [Theory]
    [MemberData(nameof(GetAllTextLabelLengths64Bit))]
    public void DifferentInstancesOfSameTextLabelTypeMustBeEqual(int length)
    {
        var tlA = new TextLabelType(length, 8);
        var tlB = new TextLabelType(length, 8);

        EqualAndSameHashCode(tlA, tlB);
    }
    public static IEnumerable<object[]> GetAllTextLabelLengths64Bit() => TextLabelType.All64.Select(tl => new object[] { tl.Length });

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
