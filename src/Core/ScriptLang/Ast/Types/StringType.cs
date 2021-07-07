namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.CodeGen;

    public sealed class StringType : BaseType
    {
        public override int SizeOf => 1;

        public StringType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is StringType;

        public override bool CanAssign(IType rhs, bool rhsIsLValue)
            => rhs is StringType or NullType or ErrorType ||
               (rhs is TextLabelType && rhsIsLValue);

        public override void CGAssign(CodeGenerator cg, AssignmentStatement stmt)
        {
            if (stmt.RHS.Type is TextLabelType && stmt.RHS.IsLValue)
            {
                cg.EmitAddress(stmt.RHS);
            }
            else
            {
                cg.EmitValue(stmt.RHS);
            }
            cg.EmitStoreAt(stmt.LHS);
        }
    }
}
