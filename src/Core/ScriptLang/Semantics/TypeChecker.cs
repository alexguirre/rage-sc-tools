namespace ScTools.ScriptLang.Semantics
{
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.SymbolTables;

    public sealed class TypeChecker : DFSVisitor
    {
        public DiagnosticsReport Diagnostics { get; }
        public GlobalSymbolTable Symbols { get; }

        public TypeChecker(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols) = (diagnostics, symbols);

        public override Void Visit(BinaryExpression node, Void param)
        {
            // TODO: BinaryExpression
            return base.Visit(node, param);
        }

        public override Void Visit(BoolLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.FindType("BOOL")!.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(FieldAccessExpression node, Void param)
        {
            // TODO: FieldAccessExpression
            return base.Visit(node, param);
        }

        public override Void Visit(FloatLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.FindType("FLOAT")!.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(IndexingExpression node, Void param)
        {
            // TODO: IndexingExpression
            return base.Visit(node, param);
        }

        public override Void Visit(IntLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.FindType("INT")!.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(InvocationExpression node, Void param)
        {
            // TODO: InvocationExpression
            return base.Visit(node, param);
        }

        public override Void Visit(NullExpression node, Void param)
        {
            node.LValue = false;
            node.Type = new NullType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(SizeOfExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.LValue = false;
            node.Type = Symbols.FindType("INT")!.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(StringLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.FindType("STRING")!.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(UnaryExpression node, Void param)
        {
            // TODO: UnaryExpression
            return base.Visit(node, param);
        }

        public override Void Visit(ValueDeclRefExpression node, Void param)
        {
            node.LValue = node.Declaration is VarDeclaration { Kind: not VarKind.Constant };
            node.Type = node.Declaration!.Type;
            return DefaultReturn;
        }

        public override Void Visit(VectorExpression node, Void param)
        {
            node.X.Accept(this, param);
            node.Y.Accept(this, param);
            node.Z.Accept(this, param);

            var vectorTy = (StructType)Symbols.FindType("VECTOR")!.CreateType(node.Source);
            node.LValue = false;
            node.Type = vectorTy;

            for (int i = 0; i < vectorTy.Declaration.Fields.Count; i++)
            {
                var src = i switch { 0 => node.X, 1 => node.Y, 2 => node.Z, _ => throw new System.NotImplementedException() };
                var dest = vectorTy.Declaration.Fields[i];
                if (!dest.Type.CanAssign(src.Type!))
                {
                    Diagnostics.AddError($"Mismatched type of component {dest.Name.ToUpperInvariant()}. Expected FLOAT, found {src.Type}", src.Source);
                }
            }

            return DefaultReturn;
        }
    }
}
