namespace ScTools.ScriptLang.Types;

using System.Linq;

internal static class Rules
{
    /// <summary>
    /// Gets whether <paramref name="type"/> can be passed between script threads safely.
    /// <para>
    /// Unsafe types:
    /// <list type="bullet">
    /// <item>
    ///     <term>Function Pointers</term>
    ///     <description>
    ///         Function pointers are stored as an address specific to the script program,
    ///         so in script threads with different program they will point to completely
    ///         different code.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term>STRING</term>
    ///     <description>
    ///         STRINGs are stored as a native memory address, normally from memory owned by
    ///         the script program. When the owning script program is unloaded, the memory
    ///         will be freed.
    ///         Instead, TEXT_LABELs can be used.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term>Structs or arrays containing any other unsafe type</term>
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    public static bool IsCrossScriptThreadSafe(TypeInfo type)
        => type switch
        {
            FunctionType or StringType => false,
            StructType structTy => structTy.Fields.All(f => IsCrossScriptThreadSafe(f.Type)),
            ArrayType arrayTy => IsCrossScriptThreadSafe(arrayTy.Item),
            _ => true,
        };

    public static bool IsDefaultInitialized(this TypeInfo type)
        => type switch
        {
            StructType structTy => structTy.Declaration.Fields.Zip(structTy.Fields)
                                    .Any(t => t.First.Initializer is not null || IsDefaultInitialized(t.Second.Type)),
            ArrayType => true,
            _ => false,
        };

    public static bool IsPromotableTo(this TypeInfo source, TypeInfo destination)
        => source == destination || source switch
        {
            NullType => destination is IntType or FloatType or StringType or BoolType or AnyType or HandleType,
            IntType => destination is FloatType or BoolType,
            HandleType h => destination is HandleType destH && destH.Kind.IsAssignableFrom(destH.Kind),

            _ => false,
        };

    public static bool IsRefAssignableFrom(this TypeInfo destination, TypeInfo source)
        => source is ErrorType || destination == source || destination is AnyType;
    public static bool IsAssignableFrom(this TypeInfo destination, TypeInfo source)
        => source is ErrorType || destination.Accept(new IsAssignableFromVisitor(source));
    public static bool IsAssignableTo(this TypeInfo source, TypeInfo destination)
        => destination.IsAssignableFrom(source);

    private sealed class IsAssignableFromVisitor : ITypeVisitor<bool>
    {
        public TypeInfo Source { get; }

        public IsAssignableFromVisitor(TypeInfo source) => Source = source;

        public bool Visit(ErrorType destination) => true;
        public bool Visit(VoidType destination) => false;
        public bool Visit(ArrayType destination) => false;
        // INT <- INT | NULL
        public bool Visit(IntType destination) => Source is IntType or NullType;
        // FLOAT <- FLOAT | INT | NULL
        public bool Visit(FloatType destination) => Source is FloatType or IntType or NullType;
        // BOOL <- BOOL | INT | NULL
        public bool Visit(BoolType destination) => Source is BoolType or IntType or NullType;
        // STRING <- STRING | NULL
        public bool Visit(StringType destination) => Source is StringType or NullType; // TODO: should be able to assign TEXT_LABELs vars to STRING types
        // ANY <- any type with size 1
        public bool Visit(AnyType destination) => Source.SizeOf == 1;
        public bool Visit(NullType destination) => false;
        // VECTOR <- VECTOR
        public bool Visit(VectorType destination) => Source is VectorType;
        // ENUM <- ENUM
        public bool Visit(EnumType destination) => destination == Source;
        // STRUCT <- STRUCT
        public bool Visit(StructType destination) => destination == Source;
        // ENTITY_INDEX <- ENTITY_INDEX | PED_INDEX | VEHICLE_INDEX | OBJECT_INDEX | NULL
        // HANDLE <- HANDLE | NULL
        public bool Visit(HandleType destination)
        {
            if (Source is not HandleType srcHandle)
            {
                return Source is NullType;
            }

            return destination.Kind.IsAssignableFrom(srcHandle.Kind);
        }
        // TEXT_LABEL_n <- TEXT_LABEL_n | STRING
        public bool Visit(TextLabelType destination) => Source is TextLabelType or StringType;
        // FUNCPTR <- FUNCPTR
        public bool Visit(FunctionType destination) => destination == Source;
        public bool Visit(TypeNameType destination) => false;
    }
}
