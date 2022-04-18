namespace ScTools.ScriptLang.Types;

internal static class Rules
{
    //public static TypeInfo PromoteBinaryOperands(TypeInfo lhs, TypeInfo rhs)
    //{
    //    if (lhs is FloatType || rhs is FloatType)
    //    {
    //        return FloatType.Instance;
    //    }
    //}

    //public TecsypeInfo TryPromote

    /*
     * -> indicates the type of the expression
     * :- indicates that a promotion occurred

        Add,
        Subtract,
        Multiply,
        Divide,
            INT    op INT                        -> INT
            FLOAT  op FLOAT                      -> FLOAT
            INT    op FLOAT  :- FLOAT op FLOAT   -> FLOAT
            FLOAT  op INT    :- FLOAT op FLOAT   -> FLOAT
            VECTOR op VECTOR                     -> VECTOR

        Modulo,
            INT    op INT                       -> INT
            FLOAT  op FLOAT                     -> FLOAT
            INT    op FLOAT  :- FLOAT op FLOAT  -> FLOAT
            FLOAT  op INT    :- FLOAT op FLOAT  -> FLOAT

        And,
        Xor,
        Or,
            INT op INT  -> INT

        Equals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
            INT op INT                        -> BOOL
            FLOAT op FLOAT                    -> BOOL
            INT op FLOAT   :- FLOAT op FLOAT  -> BOOL
            FLOAT op INT   :- FLOAT op FLOAT  -> BOOL

        LogicalAnd,
        LogicalOr,
            INT op INT                        -> BOOL
            BOOL op BOOL                      -> BOOL
            INT op BOOL                       -> BOOL
            BOOL op INT                       -> BOOL
     */


    public static bool IsAssignableFrom(this TypeInfo type, TypeInfo from, ValueKind fromValueKind)
        => from is ErrorType || type.Accept(new IsAssignableFromVisitor(from, fromValueKind));
    public static bool IsAssignableTo(this TypeInfo type, TypeInfo to, ValueKind thisValueKind)
        => to.IsAssignableFrom(type, thisValueKind);

    private sealed class IsAssignableFromVisitor : ITypeVisitor<bool>
    {
        public TypeInfo Source { get; }
        public ValueKind SourceKind { get; }

        public IsAssignableFromVisitor(TypeInfo source, ValueKind sourceKind) => (Source, SourceKind) = (source, sourceKind);

        public bool Visit(ErrorType destination) => true;
        public bool Visit(VoidType destination) => false;
        // TYPE& <- TYPE&
        // TYPE& <- TYPE if lvalue
        // ANY& <- any if lvalue
        public bool Visit(RefType destination)
            => destination == Source || 
                SourceKind.Is(ValueKind.Addressable) && (destination.Pointee is AnyType || destination.Pointee == Source);
        public bool Visit(ArrayType destination) => false;
        // INT <- INT | NULL
        public bool Visit(IntType destination) => Source is IntType or NullType;
        // FLOAT <- FLOAT | NULL
        public bool Visit(FloatType destination) => Source is FloatType or NullType;
        // BOOL <- BOOL | INT | NULL
        public bool Visit(BoolType destination) => Source is BoolType or IntType or NullType;
        // STRING <- STRING | NULL | TEXT_LABEL if lvalue
        public bool Visit(StringType destination) => Source is StringType or NullType || (Source is TextLabelType && SourceKind.Is(ValueKind.Addressable));
        // ANY <- any type with size 1
        public bool Visit(AnyType destination) => Source.SizeOf == 1;
        public bool Visit(NullType destination) => false;
        // VECTOR <- VECTOR
        public bool Visit(VectorType destination) => Source is VectorType;
        // ENUM <- ENUM
        public bool Visit(EnumType destination) => destination == Source;
        // STRUCT <- STRUCT
        public bool Visit(StructType destination) => destination == Source;
        // ENTITY_INDEX <- ENTITY_INDEX | PED_INDEX | VEHICLE_INDEX | OBJECT_INDEX
        // HANDLE <- HANDLE
        public bool Visit(HandleType destination)
        {
            if (Source is not HandleType srcHandle)
            {
                return false;
            }

            if (destination.Kind is HandleKind.EntityIndex)
            {
                return srcHandle.Kind is HandleKind.EntityIndex or
                                            HandleKind.PedIndex or
                                            HandleKind.VehicleIndex or
                                            HandleKind.ObjectIndex;
            }
            else
            {
                return destination.Kind == srcHandle.Kind;
            }
        }
        // TEXT_LABEL_n <- TEXT_LABEL_* | STRING
        public bool Visit(TextLabelType destination) => Source is TextLabelType or StringType;
        // FUNCPTR <- FUNCPTR
        public bool Visit(FunctionType destination) => destination == Source;
    }
}
