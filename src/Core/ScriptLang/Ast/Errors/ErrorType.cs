namespace ScTools.ScriptLang.Ast.Errors
{
    using System;

    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.CodeGen;

    public sealed class ErrorType : BaseError, IType
    {
        public int SizeOf => 0;
        public IType ByValue => this;

        public ErrorType(SourceRange source, Diagnostic diagnostic) : base(source, diagnostic) { }
        public ErrorType(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public bool Equivalent(IType other) => other is ErrorType;
        public bool CanBindRefTo(IType other) => true;
        public bool CanAssign(IType rhs, bool rhsIsLValue) => true;
        public bool CanAssignInit(IType rhs, bool rhsIsLValue) => true;

        // do nothing and return itself in the semantic checks to prevent reduntant errors
        public IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics) => this;
        public IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics) => this;
        public (IType Type, bool LValue) FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics) => (this, false);
        public IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics) => this;
        public IType Invocation((IType Type, bool IsLValue, SourceRange Source)[] args, SourceRange source, DiagnosticsReport diagnostics) => this;
        public void Assign(IType rhs, bool rhsIsLValue, SourceRange source, DiagnosticsReport diagnostics) { }
        public void AssignInit(IType rhs, bool rhsIsLValue, SourceRange source, DiagnosticsReport diagnostics) { }
        public void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr) => throw new NotImplementedException();
        public void CGUnaryOperation(CodeGenerator cg, UnaryExpression expr) => throw new NotImplementedException();
        public void CGFieldAddress(CodeGenerator cg, FieldAccessExpression expr) => throw new NotImplementedException();
    }
}
