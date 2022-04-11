namespace ScTools.ScriptLang.Ast.Types
{
    using System;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.CodeGen;

    public interface IType : INode
    {
        int SizeOf { get; }

        /// <summary>
        /// Gets whether this and <paramref name="other"/> represent the same type.
        /// </summary>
        bool Equivalent(IType other);

        /// <summary>
        /// Gets whether a reference of this type can be binded to a lvalue of type <paramref name="other"/>.
        /// </summary>
        bool CanBindRefTo(IType other);

        /// <summary>
        /// Gets whether the type <paramref name="rhs"/> can be assigned to this type.
        /// </summary>
        bool CanAssign(IType rhs, bool rhsIsLValue);

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
        /// Checks if this type has the field <paramref name="fieldName"/> and returns its type.
        /// </summary>
        IType FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if this type supports indexing with <paramref name="index"/> and returns the resulting item type.
        /// </summary>
        IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if this type supports invocation with the specified <paramref name="args"/> and returns the returned type.
        /// </summary>
        (IType ReturnType, bool IsConstant) Invocation(IExpression[] args, SourceRange source, DiagnosticsReport diagnostics);

        /// <summary>
        /// Checks if the type <paramref name="rhs"/> can be assigned to this type.
        /// </summary>
        void Assign(IType rhs, bool rhsIsLValue, SourceRange source, DiagnosticsReport diagnostics);

        // CodeGen
        void CGAssign(CodeGenerator cg, AssignmentStatement stmt);
        void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr);
        void CGUnaryOperation(CodeGenerator cg, UnaryExpression expr);
        void CGFieldAddress(CodeGenerator cg, FieldAccessExpression expr);
        void CGArrayItemAddress(CodeGenerator cg, IndexingExpression expr);
        void CGInvocation(CodeGenerator cg, InvocationExpression expr);
    }

    public interface IArrayType : IType
    {
        IType ItemType { get; set; }
    }

    public abstract class BaseType: BaseNode, IType
    {
        public abstract int SizeOf { get; }

        public BaseType(SourceRange source) : base(source) {}

        public override string ToString() => TypePrinter.ToString(this, null, false);

        public abstract bool Equivalent(IType other);
        public virtual bool CanBindRefTo(IType other) => Equivalent(other);
        public virtual bool CanAssign(IType rhs, bool rhsIsLValue) => false;

        public virtual IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
            => rhs is ErrorType ? rhs : new ErrorType(source, diagnostics, $"Binary operator '{op.ToToken()}' is not supported with operands of type '{this}' and '{rhs}'");

        public virtual IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics)
            => new ErrorType(source, diagnostics, $"Unary operator '{op.ToHumanString()}' is not supported by type '{this}'");

        public virtual IType FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
            => new ErrorType(source, diagnostics, $"Field access is not supported by type '{this}'");

        public virtual IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics)
            => index is ErrorType ? index : new ErrorType(source, diagnostics, $"Indexing is not supported by type '{this}'");

        public virtual (IType ReturnType, bool IsConstant) Invocation(IExpression[] args, SourceRange source, DiagnosticsReport diagnostics)
            => (new ErrorType(source, diagnostics, $"Invocation is not supported by type '{this}'"), false);

        public virtual void Assign(IType rhs, bool rhsIsLValue, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (CanAssign(rhs, rhsIsLValue))
            {
                return;
            }

            diagnostics.AddError($"Cannot assign type '{rhs}' to '{this}'", source);
        }

        public virtual void CGAssign(CodeGenerator cg, AssignmentStatement stmt)
        {
            // by default just copy rhs to lhs by value
            cg.EmitValue(stmt.RHS);
            cg.EmitStoreAt(stmt.LHS);
        }

        public virtual void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr) => throw new NotImplementedException();
        public virtual void CGUnaryOperation(CodeGenerator cg, UnaryExpression expr) => throw new NotImplementedException();
        public virtual void CGFieldAddress(CodeGenerator cg, FieldAccessExpression expr) => throw new NotImplementedException();
        public virtual void CGArrayItemAddress(CodeGenerator cg, IndexingExpression expr) => throw new NotImplementedException();
        public virtual void CGInvocation(CodeGenerator cg, InvocationExpression expr) => throw new NotImplementedException();
    }

    public abstract class BaseArrayType : BaseType, IArrayType
    {
        public IType ItemType { get; set; }

        public BaseArrayType(SourceRange source, IType itemType) : base(source)
            => ItemType = itemType;

        public override IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics)
        {
            var expectedIndexTy = new IntType(source);
            if (!expectedIndexTy.CanAssign(index, rhsIsLValue: false))
            {
                return new ErrorType(source, diagnostics, $"Expected type '{expectedIndexTy}' as array index, found '{index}'");
            }

            return ItemType;
        }

        public override void CGArrayItemAddress(CodeGenerator cg, IndexingExpression expr)
        {
            cg.EmitValue(expr.Index);
            cg.EmitAddress(expr.Array);

            var itemSize = expr.Type!.SizeOf;
            switch (itemSize)
            {
                case >= byte.MinValue and <= byte.MaxValue:
                    cg.Emit(Opcode.ARRAY_U8, itemSize);
                    break;

                case >= ushort.MinValue and <= ushort.MaxValue:
                    cg.Emit(Opcode.ARRAY_U16, itemSize);
                    break;

                default: Debug.Assert(false, "Array item size too big"); break;
            }
        }
    }
}
