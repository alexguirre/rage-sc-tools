namespace ScTools.ScriptLang.CodeGen
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;

    /// <summary>
    /// Assigns addresses to all variables and labels to statements that need them.
    /// </summary>
    public static class Allocator
    {
        public static void Allocate(Program root, DiagnosticsReport diagnostics)
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
            public uint LastLabelID { get; set; }

            public string NextLabel() => $"__lbl{LastLabelID++}";
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

                // fix up script parameters, they need to be placed after the static vars
                foreach (var scriptParam in ctx.Program.Script!.Prototype.Parameters)
                {
                    scriptParam.Address += ctx.Program.StaticsSize;
                    ctx.Program.Statics.Add(scriptParam.Address, scriptParam);
                }
                ctx.Program.StaticsSize += ctx.Program.ScriptParametersSize;

                ctx.Program = null;
                return default;
            }

            public override Void Visit(GlobalBlockDeclaration node, AllocatorContext ctx)
            {
                Debug.Assert(ctx.Program is not null);
                if (Parser.CaseInsensitiveComparer.Equals(ctx.Program.Script!.Name, node.Name))
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

                // allocate locals and labels
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

                    case VarKind.Parameter or VarKind.ScriptParameter:
                        Debug.Assert(ctx.FuncProto is not null);
                        node.Address = ctx.FuncProto.ParametersSize;
                        ctx.FuncProto.ParametersSize += node.IsReference ? 1 : node.Type.SizeOf;
                        break;

                    case VarKind.Local:
                        Debug.Assert(ctx.Func is not null);
                        node.Address = ctx.Func.FrameSize;
                        ctx.Func.LocalsSize += node.Type.SizeOf;
                        break;
                }

                return base.Visit(node, ctx);
            }

            public override Void Visit(IfStatement node, AllocatorContext ctx)
            {
                node.ElseLabel = ctx.NextLabel();
                node.EndLabel = ctx.NextLabel();
                return base.Visit(node, ctx);
            }

            public override Void Visit(RepeatStatement node, AllocatorContext ctx)
            {
                node.BeginLabel = ctx.NextLabel();
                node.ContinueLabel = ctx.NextLabel();
                node.ExitLabel = ctx.NextLabel();
                return base.Visit(node, ctx);
            }

            public override Void Visit(WhileStatement node, AllocatorContext ctx)
            {
                node.BeginLabel = ctx.NextLabel();
                node.ExitLabel = ctx.NextLabel();
                return base.Visit(node, ctx);
            }

            public override Void Visit(SwitchStatement node, AllocatorContext ctx)
            {
                node.ExitLabel = ctx.NextLabel();
                return base.Visit(node, ctx);
            }

            public override Void Visit(ValueSwitchCase node, AllocatorContext ctx)
            {
                node.Label = ctx.NextLabel();
                return base.Visit(node, ctx);
            }

            public override Void Visit(DefaultSwitchCase node, AllocatorContext ctx)
            {
                node.Label = ctx.NextLabel();
                return base.Visit(node, ctx);
            }

            public override Void Visit(BinaryExpression node, AllocatorContext ctx)
            {
                if (node.Operator is BinaryOperator.LogicalAnd or BinaryOperator.LogicalOr)
                {
                    node.ShortCircuitLabel = ctx.NextLabel();
                }
                return base.Visit(node, ctx);
            }
        }
    }
}
