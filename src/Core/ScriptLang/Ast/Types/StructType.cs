namespace ScTools.ScriptLang.Ast.Types
{
    using System.Linq;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class StructType : BaseType
    {
        public override int SizeOf => Declaration.Fields.Sum(f => f.Type.SizeOf);
        public StructDeclaration Declaration { get; set; }

        public StructType(SourceRange source, StructDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is StructType otherStruct && otherStruct.Declaration == Declaration;

        public override bool CanAssign(IType rhs)
        {
            rhs = rhs.ByValue;
            if (rhs is ErrorType || Equivalent(rhs))
            {
                return true;
            }

            if (IsBuiltInHandleType(this))
            {
                // special case for built-in handle-like structs (e.g. ENTITY_INDEX, PED_INDEX, VEHICLE_INDEX, ...)

                // allow to assign NULL, e.g: PED_INDEX myPed = NULL
                if (rhs is NullType)
                {
                    return true;
                }

                // allow to assign PED/VEHICLE/OBJECT_INDEX to ENTITY_INDEX (to simplify native calls that expect ENTITY_INDEX but you have some other handle type)
                if (Parser.CaseInsensitiveComparer.Equals(Declaration.Name, "ENTITY_INDEX") && rhs is StructType rhsTy && IsBuiltInHandleType(rhsTy))
                {
                    return Parser.CaseInsensitiveComparer.Equals(rhsTy.Declaration.Name, "PED_INDEX") ||
                           Parser.CaseInsensitiveComparer.Equals(rhsTy.Declaration.Name, "VEHICLE_INDEX") ||
                           Parser.CaseInsensitiveComparer.Equals(rhsTy.Declaration.Name, "OBJECT_INDEX");
                }
            }

            return false;

            // Gets whether the StructType is a built-in handle-like struct. See GlobalSymbolTable for how they are created.
            static bool IsBuiltInHandleType(StructType ty)
                => ty.Declaration.Source.IsUnknown && ty.Declaration.Fields.Count == 1 && ty.Declaration.Fields[0].Type is IntType;
        }

        public override (IType Type, bool LValue) FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
        {
            var field = Declaration.FindField(fieldName);
            if (field is null)
            {
                return (new ErrorType(source, diagnostics, $"Unknown field '{fieldName}'"), true);
            }
            else
            {
                return (field.Type, true);
            }
        }

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (rhs is ErrorType)
            {
                return rhs;
            }

            if (op is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide && 
                rhs is StructType rhsStructType &&
                IsBuiltInVectorType(this) && IsBuiltInVectorType(rhsStructType))
            {
                // special case to allow +-*/ operations for VECTOR type
                return new StructType(source, Declaration);
            }

            return base.BinaryOperation(op, rhs, source, diagnostics);
        }

        public override IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (op is UnaryOperator.Negate && IsBuiltInVectorType(this))
            {
                // special case to allow negation for VECTOR type
                return new StructType(source, Declaration);
            }

            return base.UnaryOperation(op, source, diagnostics);
        }

        private static bool IsBuiltInVectorType(StructType structType)
        {
            var decl = structType.Declaration;
            return decl.Source.IsUnknown && decl.Fields.Count == 3 && Parser.CaseInsensitiveComparer.Equals(decl.Name, "VECTOR");
        }
    }
}
