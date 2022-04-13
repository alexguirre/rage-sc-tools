namespace ScTools.ScriptLang.CodeGen
{
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Emits code to push the address of lvalue expressions.
    /// </summary>
    public sealed class AddressEmitter : EmptyVisitor
    {
        public CodeGenerator CG { get; }

        public AddressEmitter(CodeGenerator cg) => CG = cg;

        public override Void Visit(FieldAccessExpression node, Void param)
        {
            node.SubExpression.Type!.CGFieldAddress(CG, node);
            return default;
        }

        public override Void Visit(IndexingExpression node, Void param)
        {
            node.Array.Type!.CGArrayItemAddress(CG, node);
            return default;
        }

        public override Void Visit(NameExpression node, Void param)
        {
            var varDecl = (VarDeclaration)node.Semantics.Declaration!; // VarDeclaration are the only declarations that can be lvalues

            switch (varDecl.Kind)
            {
                case VarKind.Constant: Debug.Assert(false, "Cannot get address of constant var"); break;

                case VarKind.Global:
                    switch (varDecl.Address)
                    {
                        case >= 0 and <= 0x0000FFFF:
                            CG.Emit(Opcode.GLOBAL_U16, varDecl.Address);
                            break;

                        case >= 0 and <= 0x00FFFFFF:
                            CG.Emit(Opcode.GLOBAL_U24, varDecl.Address);
                            break;

                        default: Debug.Assert(false, "Global var address too big"); break;
                    }
                    break;

                case VarKind.Static or VarKind.ScriptParameter:
                    switch (varDecl.Address)
                    {
                        case >= 0 and <= 0x000000FF:
                            CG.Emit(Opcode.STATIC_U8, varDecl.Address);
                            break;

                        case >= 0 and <= 0x0000FFFF:
                            CG.Emit(Opcode.STATIC_U16, varDecl.Address);
                            break;

                        default: Debug.Assert(false, "Static var address too big"); break;
                    }
                    break;

                case VarKind.Parameter:
                    if (varDecl.IsReference)
                    {
                        // parameter passed by reference, the address is its value
                        switch (varDecl.Address)
                        {
                            case >= 0 and <= 0x000000FF:
                                CG.Emit(Opcode.LOCAL_U8_LOAD, varDecl.Address);
                                break;

                            case >= 0 and <= 0x0000FFFF:
                                CG.Emit(Opcode.LOCAL_U8_LOAD, varDecl.Address);
                                break;

                            default: Debug.Assert(false, "Local var address too big"); break;
                        }
                    }
                    else
                    {
                        // parameter passed by value, treat it as a local variable
                        goto case VarKind.Local;
                    }
                    break;
                case VarKind.Local:
                    switch (varDecl.Address)
                    {
                        case >= 0 and <= 0x000000FF:
                            CG.Emit(Opcode.LOCAL_U8, varDecl.Address);
                            break;

                        case >= 0 and <= 0x0000FFFF:
                            CG.Emit(Opcode.LOCAL_U16, varDecl.Address);
                            break;

                        default: Debug.Assert(false, "Local var address too big"); break;
                    }
                    break;
            }

            return default;
        }
    }
}
