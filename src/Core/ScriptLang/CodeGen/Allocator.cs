namespace ScTools.ScriptLang.CodeGen
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;

    /// <summary>
    /// Assigns addresses to all variables.
    /// </summary>
    public static class Allocator
    {
        public static void AllocateVars(Program root, DiagnosticsReport diagnostics)
        {
            root.Accept(new AllocatorVisitor(diagnostics), new AllocatorContext());
        }

        private class AllocatorContext
        {
            public Program? Program { get; set; }
            public GlobalBlockDeclaration? GlobalBlock { get; set; }
            public StructDeclaration? Struct { get; set; }
            public int LocalsSize { get; set; }
        }

        private sealed class AllocatorVisitor : DFSVisitor<Void, AllocatorContext>
        {
            public override Void DefaultReturn => default;
            public DiagnosticsReport Diagnostics { get; }

            public AllocatorVisitor(DiagnosticsReport diagnostics) => Diagnostics = diagnostics;

            public override Void Visit(Program node, AllocatorContext ctx)
            {
                ctx.Program = node;
                base.Visit(node, ctx);

                if (ctx.Program.ArgVar is not null)
                {
                    ctx.Program.ArgVar.Address = ctx.Program.StaticsSize;
                    ctx.Program.ArgsSize = ctx.Program.ArgVar.Type.SizeOf;
                    ctx.Program.StaticsSize += ctx.Program.ArgsSize;
                    ctx.Program.Statics.Add(ctx.Program.ArgVar.Address, ctx.Program.ArgVar);
                }

                ctx.Program = null;
                return default;
            }

            public override Void Visit(GlobalBlockDeclaration node, AllocatorContext ctx)
            {
                Debug.Assert(ctx.Program is not null);
                if (Parser.CaseInsensitiveComparer.Equals(ctx.Program.ScriptName, node.Name))
                {
                    ctx.Program.GlobalBlock = node;
                }

                ctx.GlobalBlock = node;
                base.Visit(node, ctx);
                ctx.GlobalBlock = null;
                return default;
            }

            public override Void Visit(FuncDeclaration node, AllocatorContext ctx)
            {
                // allocate parameters
                node.Prototype.Accept(this, ctx);

                ctx.LocalsSize += 2; // additional space needed to store return address and caller frame offset

                // allocate locals
                node.Body.ForEach(stmt => stmt.Accept(this, ctx));
                return default;
            }

            public override Void Visit(FuncProtoDeclaration node, AllocatorContext ctx)
            {
                ctx.LocalsSize = 0;
                node.Parameters.ForEach(p => p.Accept(this, ctx));
                return default;
            }

            public override Void Visit(StructDeclaration node, AllocatorContext ctx)
            {
                var offset = 0;
                foreach (var field in node.Fields)
                {
                    field.Offset = offset;
                    offset += field.Type.SizeOf;
                }
                return default;
            }

            public override Void Visit(VarDeclaration node, AllocatorContext ctx)
            {
                switch (node.Kind)
                {
                    case VarKind.Constant: break; // nothing, constants don't have addresses

                    case VarKind.Global:
                        Debug.Assert(ctx.GlobalBlock is not null);
                        node.Address = ctx.GlobalBlock.Size | (ctx.GlobalBlock.BlockIndex << 18);

                        ctx.GlobalBlock.Size += node.Type.SizeOf;
                        if (ctx.GlobalBlock.Size > GlobalBlockDeclaration.MaxSize)
                        {
                            Diagnostics.AddError($"Global block size '{ctx.GlobalBlock.Size}' exceeds maximum size '{GlobalBlockDeclaration.MaxSize}'", node.Source);
                        }
                        break;

                    case VarKind.Static:
                        Debug.Assert(ctx.Program is not null);
                        node.Address = ctx.Program.StaticsSize;
                        ctx.Program.StaticsSize += node.Type.SizeOf;
                        ctx.Program.Statics.Add(node.Address, node);
                        break;

                    case VarKind.StaticArg:
                        Debug.Assert(ctx.Program is not null);
                        // ARG var is stored after all static vars, so save it to resolve its address later
                        ctx.Program.ArgVar = node;
                        break;

                    case VarKind.Local or VarKind.Parameter:
                        node.Address = ctx.LocalsSize;
                        ctx.LocalsSize += node.Type.SizeOf;
                        break;
                }

                return default;
            }
        }
    }
}
