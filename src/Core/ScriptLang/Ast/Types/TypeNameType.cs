namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.CodeGen;

    /// <summary>
    /// Used to pass type names to intrinsic functions.
    /// For example: <code>COUNT_OF(MY_ENUM)</code><code>INT_TO_ENUM(MY_ENUM, 0)</code>
    /// In these cases, <c>MY_ENUM</c> is a <see cref="Expressions.NameExpression"/> with type <see cref="TypeNameType"/>.
    /// </summary>
    public sealed class TypeNameType : BaseType
    {
        public override int SizeOf => throw new NotImplementedException();

        public ITypeDeclaration TypeDeclaration { get; set; }

        public TypeNameType(SourceRange source, ITypeDeclaration typeDecl) : base(source)
            => TypeDeclaration = typeDecl;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is TypeNameType otherTy && otherTy.TypeDeclaration == TypeDeclaration;
        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType || Equivalent(rhs);

        public override void CGAssign(CodeGenerator cg, AssignmentStatement stmt) => throw new NotImplementedException();
    }
}
