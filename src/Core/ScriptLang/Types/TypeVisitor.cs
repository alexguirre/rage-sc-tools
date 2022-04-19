namespace ScTools.ScriptLang.Types;

public interface ITypeVisitor<TReturn>
{
    TReturn Visit(ErrorType type);
    TReturn Visit(VoidType type);
    TReturn Visit(RefType type);
    TReturn Visit(ArrayType type);
    TReturn Visit(IntType type);
    TReturn Visit(FloatType type);
    TReturn Visit(BoolType type);
    TReturn Visit(StringType type);
    TReturn Visit(AnyType type);
    TReturn Visit(NullType type);
    TReturn Visit(VectorType type);
    TReturn Visit(EnumType type);
    TReturn Visit(StructType type);
    TReturn Visit(HandleType type);
    TReturn Visit(TextLabelType type);
    TReturn Visit(FunctionType type);
    TReturn Visit(TypeNameType type);
}
