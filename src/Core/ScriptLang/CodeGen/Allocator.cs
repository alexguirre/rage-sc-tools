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
            public FuncDeclaration? Func { get; set; }
            public FuncProtoDeclaration? FuncProto { get; set; }
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
                ctx.Func = node;
                ctx.Func.LocalsSize = 0;
                // allocate parameters
                node.Prototype.Accept(this, ctx);

                // allocate locals
                node.Body.ForEach(stmt => stmt.Accept(this, ctx));
                ctx.Func = null;
                return default;
            }

            public override Void Visit(FuncProtoDeclaration node, AllocatorContext ctx)
            {
                ctx.FuncProto = node;
                ctx.FuncProto.ParametersSize = 0;
                node.Parameters.ForEach(p => p.Accept(this, ctx));
                ctx.FuncProto = null;
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

                    case VarKind.Parameter:
                        Debug.Assert(ctx.FuncProto is not null);
                        node.Address = ctx.FuncProto.ParametersSize;
                        ctx.FuncProto.ParametersSize += node.Type.SizeOf;
                        break;

                    case VarKind.Local:
                        Debug.Assert(ctx.Func is not null);
                        node.Address = ctx.Func.FrameSize;
                        ctx.Func.LocalsSize += node.Type.SizeOf;
                        break;
                }

                return default;
            }
        }
    }
}
