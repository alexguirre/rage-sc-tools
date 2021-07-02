namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.CodeGen;

    public enum HandleKind
    {
        PlayerIndex,
        EntityIndex,
        PedIndex,
        VehicleIndex,
        ObjectIndex,
        CameraIndex,
        PickupIndex,
        BlipInfoId,
    }

    public sealed class HandleType : BaseType
    {
        public override int SizeOf => 1;
        public HandleKind Kind { get; set; }

        public HandleType(SourceRange source, HandleKind kind) : base(source) => Kind = kind;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is HandleType handleTy && handleTy.Kind == Kind;

        public override bool CanAssign(IType rhs, bool rhsIsLValue)
        {
            if (rhs is ErrorType || Equivalent(rhs))
            {
                return true;
            }

            // allow to assign NULL, e.g: PED_INDEX myPed = NULL
            if (rhs is NullType)
            {
                return true;
            }

            // allow to assign PED/VEHICLE/OBJECT_INDEX to ENTITY_INDEX (to allow native calls that expect ENTITY_INDEX but you have some other handle type)
            if (rhs is HandleType rhsHandle)
            {
                return IsValidConversion(to: Kind, from: rhsHandle.Kind);
            }

            return false;
        }

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (op is BinaryOperator.Equals or BinaryOperator.NotEquals)
            {
                if (rhs is NullType ||
                    (rhs is HandleType rhsHandle && (IsValidConversion(to: Kind, from: rhsHandle.Kind) || IsValidConversion(to: rhsHandle.Kind, from: Kind))))
                {
                    return new BoolType(source);
                }
            }

            return base.BinaryOperation(op, rhs, source, diagnostics);
        }

        public override void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr)
        {
            cg.EmitValue(expr.LHS);
            cg.EmitValue(expr.RHS);
            switch (expr.Operator)
            {
                case BinaryOperator.Equals: cg.Emit(Opcode.IEQ); break;
                case BinaryOperator.NotEquals: cg.Emit(Opcode.INE); break;

                default: throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets whether a handle of kind <paramref name="from"/> can be converted to handle of kind <paramref name="to"/>.
        /// <para>
        /// The only allowed conversion is PED_INDEX/VEHICLE_INDEX/OBJECT_INDEX to ENTITY_INDEX (to allow native calls that expect ENTITY_INDEX but you have some other handle type).
        /// </para>
        /// <para>
        /// If both <paramref name="from"/> and <paramref name="to"/> are the same handle kind, returns <c>true</c>.
        /// </para>
        /// </summary>
        public static bool IsValidConversion(HandleKind to, HandleKind from)
        {
            if (to is HandleKind.EntityIndex)
            {
                return from is HandleKind.EntityIndex or
                               HandleKind.PedIndex or
                               HandleKind.VehicleIndex or
                               HandleKind.ObjectIndex;
            }
            else
            {
                return to == from;
            }
        }

        public static string KindToTypeName(HandleKind kind)
            => kind switch
            {
                HandleKind.PlayerIndex => "PLAYER_INDEX",
                HandleKind.EntityIndex => "ENTITY_INDEX",
                HandleKind.PedIndex => "PED_INDEX",
                HandleKind.VehicleIndex => "VEHICLE_INDEX",
                HandleKind.ObjectIndex => "OBJECT_INDEX",
                HandleKind.CameraIndex => "CAMERA_INDEX",
                HandleKind.PickupIndex => "PICKUP_INDEX",
                HandleKind.BlipInfoId => "BLIP_INFO_ID",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
    }
}
