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

    [Theory]
    [MemberData(nameof(GetAllHandleKinds))]
    public void DifferentInstancesOfSameHandleTypeMustBeEqual(HandleKind kind)
    {
        var handleA = new HandleType(kind);
        var handleB = new HandleType(kind);

        EqualAndSameHashCode(handleA, handleB);
    }
    public static IEnumerable<object[]> GetAllHandleKinds() => HandleType.All.Select(h => new object[] { h.Kind });

    [Fact]
    public void HandleTypesAssignmentAndInheritanceSemanticsAreCorrect()
    {
        var baseA = new HandleKind(BaseClass: 0, DerivedClass: 0);
        var baseB = new HandleKind(BaseClass: 1, DerivedClass: 0);
        var derivedA1 = baseA with { DerivedClass = 1 };
        var derivedA2 = baseA with { DerivedClass = 2 };
        var derivedB1 = baseB with { DerivedClass = 1 };
        var derivedB2 = baseB with { DerivedClass = 2 };

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
