namespace ScTools.ScriptLang.Ast.Types
{
    using System;
    using System.Linq;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.CodeGen;

    public sealed class StructType : BaseType
    {
        public override int SizeOf => Declaration.Fields.Sum(f => f.Type.SizeOf);
        public StructDeclaration Declaration { get; set; }

        public StructType(SourceRange source, StructDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is StructType otherStruct && otherStruct.Declaration == Declaration;

        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType || Equivalent(rhs);

        public override IType FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
        {
            var field = Declaration.FindField(fieldName);
            if (field is null)
            {
                return new ErrorType(source, diagnostics, $"Unknown field '{fieldName}'");
            }
            else
            {
                return field.Type;
            }
        }

        public override void CGFieldAddress(CodeGenerator cg, FieldAccessExpression expr)
        {
            var field = Declaration.FindField(expr.FieldName);
            if (field is null)
            {
                throw new ArgumentException($"Unknown field '{expr.FieldName}'", nameof(expr));
            }

            cg.EmitAddress(expr.SubExpression);
            cg.EmitOffset(field.Offset);
        }
    }
}
