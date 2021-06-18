namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    public interface IType : INode
    {
        int SizeOf { get; }

        /// <summary>
        /// Gets whether the type <paramref name="rhs"/> can be assigned to this type.
        /// </summary>
        bool CanAssign(IType rhs);

        // Semantic Checks

        /// <summary>
        /// Checks if this type supports the binary operation <paramref name="op"/> with the type <paramref name="rhs"/> and returns the resulting type.
        /// </summary>
        IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if this type supports the unary operation <paramref name="op"/> and returns the resulting type.
        /// </summary>
        IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if this type has the field <paramref name="fieldName"/> and returns its type and whether it is an lvalue.
        /// </summary>
        (IType Type, bool LValue) FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if this type supports indexing with <paramref name="index"/> and returns the resulting item type.
        /// </summary>
        IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if this type supports invocation with the specified <paramref name="args"/> and returns the returned type.
        /// </summary>
        IType Invocation(IType[] args, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if the type <paramref name="rhs"/> can be assigned to this type.
        /// </summary>
        void Assign(IType rhs, SourceRange source, DiagnosticsReport diagnostics);
    }

    public abstract class BaseType: BaseNode, IType
    {
        public abstract int SizeOf { get; }

        public BaseType(SourceRange source) : base(source) {}

        public override string ToString() => TypePrinter.ToString(this, null);

        public virtual bool CanAssign(IType rhs) => false;

        public virtual IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
            => new ErrorType(source, diagnostics, $"Binary operator '{op.ToToken()}' is not supported with operands of type '{this}' and '{rhs}'");

        public virtual IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics)
            => new ErrorType(source, diagnostics, $"Unary operator '{op.ToToken()}' is not supported by type '{this}'");

        public virtual (IType Type, bool LValue) FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
            => (new ErrorType(source, diagnostics, $"Field access is not supported by type '{this}'"), false);

        public virtual IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics)
            => new ErrorType(source, diagnostics, $"Indexing is not supported by type '{this}'");

        public virtual IType Invocation(IType[] args, SourceRange source, DiagnosticsReport diagnostics)
            => new ErrorType(source, diagnostics, $"Invocation is not supported by type '{this}'");

        public virtual void Assign(IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (CanAssign(rhs))
            {
                return;
            }

            diagnostics.AddError($"Cannot assign type '{rhs}' to '{this}'", source);
        }
    }
}
