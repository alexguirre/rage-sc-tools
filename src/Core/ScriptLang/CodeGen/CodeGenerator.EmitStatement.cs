#nullable enable
namespace ScTools.ScriptLang.CodeGen
{
    using System;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Binding;

    public sealed partial class CodeGenerator
    {
        private void EmitStatemnet(BoundStatement stmt)
        {
            switch (stmt)
            {
                //case AssignmentStatement s: EmitAssignmentStatement(s); break;
                case BoundReturnStatement s: EmitReturnStatement(s); break;
                default: throw new NotImplementedException(stmt.GetType().Name);
            }
        }

        //private void EmitAssignmentStatement(AssignmentStatement stmt)
        //{
        //    EmitExpression(stmt.Right);
        //    //EmitExpression()
        //}

        private void EmitReturnStatement(BoundReturnStatement stmt)
        {
            Debug.Assert(func != null);

            if (stmt.Expression != null)
            {
                Debug.Assert(func.Type.ReturnType != null && func.Type.ReturnType == stmt.Expression.Type);
                EmitExpression(stmt.Expression);
            }

            EmitEpilogue();
        }
    }
}
