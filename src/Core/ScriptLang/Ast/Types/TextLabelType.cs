namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.CodeGen;

    /// <summary>
    /// Represents an array type of constant size.
    /// </summary>
    public sealed class TextLabelType : BaseType
    {
        public const int MinLength = 8;
        public const int MaxLength = 248;

        private int length;
        public int Length
        {
            get => length;
            set => length = IsValidLength(value) ? value : throw new ArgumentException("Invalid length", nameof(value)); 
        }

        public override int SizeOf => Length / 8;

        public TextLabelType(SourceRange source, int length) : base(source)
            => Length = length;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is TextLabelType otherLbl && Length == otherLbl.Length;

        public override bool CanAssign(IType rhs, bool rhsIsLValue)
            => rhs.ByValue is TextLabelType or StringType or IntType or ErrorType;

        public override void CGAssign(CodeGenerator cg, AssignmentStatement stmt)
        {
            var rhsType = stmt.RHS.Type!.ByValue;
            if (rhsType is StringType)
            {
                cg.EmitValue(stmt.RHS);
                cg.EmitAddress(stmt.LHS);
                cg.Emit(Opcode.TEXT_LABEL_ASSIGN_STRING, Length);
            }
            else if (rhsType is IntType)
            {
                cg.EmitValue(stmt.RHS);
                cg.EmitAddress(stmt.LHS);
                cg.Emit(Opcode.TEXT_LABEL_ASSIGN_INT, Length);
            }
            else if (rhsType is TextLabelType { Length: var rhsTLLength })
            {
                if (rhsTLLength != Length)
                {
                    // if rhs is TEXT_LABEL_n but with different length, use TEXT_LABEL_COPY which allows to specify the size of src and dest
                    cg.EmitValue(stmt.RHS);                 // src
                    cg.EmitPushConstInt(rhsType.SizeOf);    // src size
                    cg.EmitPushConstInt(SizeOf);            // dest size
                    cg.EmitAddress(stmt.LHS);               // dest address
                    cg.Emit(Opcode.TEXT_LABEL_COPY);

                }
                else
                {
                    // if rhs is TEXT_LABEL_n of the same length, just use a normal copy by value
                    cg.EmitValue(stmt.RHS);
                    cg.EmitStoreAt(stmt.LHS);
                }
            }
            else
            {
                throw new NotSupportedException($"Codegen to assign '{stmt.RHS.Type}' to '{this}' is not supported");
            }
        }

        /// <returns><c>true</c> if <paramref name="length"/> is in the range [<see cref="MinLength"/>, <see cref="MaxLength"/>] and is a multiple of 8; otherwise, <c>false</c>.</returns>
        public static bool IsValidLength(int length)
            => length is >= MinLength and <= MaxLength && (length % 8) == 0;
    }
}
