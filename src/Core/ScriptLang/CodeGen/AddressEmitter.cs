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

        public override Void Visit(FieldAccessExpression node, Void param) => default;

        public override Void Visit(IndexingExpression node, Void param)
        {
            CG.EmitValue(node.Index);
            node.Array.Accept(this, param);

            var itemSize = node.Type!.SizeOf;
            switch (itemSize)
            {
                case >= 0 and <= 0x000000FF:
                    CG.Emit(Opcode.ARRAY_U8, itemSize);
                    break;

                case >= 0 and <= 0x0000FFFF:
                    CG.Emit(Opcode.ARRAY_U16, itemSize);
                    break;

                default: Debug.Assert(false, "Array item size too big"); break;
            }

            return default;
        }

        public override Void Visit(ValueDeclRefExpression node, Void param)
        {
            var varDecl = (VarDeclaration)node.Declaration!; // VarDeclaration are the only declarations that can be lvalues

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

                case VarKind.Static or VarKind.StaticArg:
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

                case VarKind.Local or VarKind.Parameter:
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
