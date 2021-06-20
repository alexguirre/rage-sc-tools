namespace ScTools.ScriptLang.Semantics
{
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.BuiltIns;
    using ScTools.ScriptLang.SymbolTables;

    /// <summary>
    /// Handles type-checking of <see cref="IExpression"/>s.
    /// Only visit methods for expression nodes are implemented, the other methods throw <see cref="System.NotImplementedException"/>.
    /// </summary>
    public sealed class ExpressionTypeChecker : EmptyVisitor
    {
        public DiagnosticsReport Diagnostics { get; }
        public GlobalSymbolTable Symbols { get; }

        public ExpressionTypeChecker(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols) = (diagnostics, symbols);

        public override Void Visit(BinaryExpression node, Void param)
        {
            node.LHS.Accept(this, param);
            node.RHS.Accept(this, param);

            node.IsLValue = false;
            node.IsConstant = node.LHS.IsConstant && node.RHS.IsConstant;
            node.Type = node.LHS.Type!.BinaryOperation(node.Operator, node.RHS.Type!, node.Source, Diagnostics);
            return default;
        }

        public override Void Visit(BoolLiteralExpression node, Void param)
        {
            node.IsLValue = false;
            node.IsConstant = true;
            node.Type = BuiltInTypes.Bool.CreateType(node.Source);
            return default;
        }

        public override Void Visit(FieldAccessExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.IsConstant = false;
            (node.Type, node.IsLValue) = node.SubExpression.Type!.FieldAccess(node.FieldName, node.Source, Diagnostics);
            return default;
        }

        public override Void Visit(FloatLiteralExpression node, Void param)
        {
            node.IsLValue = false;
            node.IsConstant = true;
            node.Type = BuiltInTypes.Float.CreateType(node.Source);
            return default;
        }

        public override Void Visit(IndexingExpression node, Void param)
        {
            node.Array.Accept(this, param);
            node.Index.Accept(this, param);

            node.IsLValue = true;
            node.IsConstant = false;
            node.Type = node.Array.Type!.Indexing(node.Index.Type!, node.Source, Diagnostics);
            return default;
        }

        public override Void Visit(IntLiteralExpression node, Void param)
        {
            node.IsLValue = false;
            node.IsConstant = true;
            node.Type = BuiltInTypes.Int.CreateType(node.Source);
            return default;
        }

        public override Void Visit(InvocationExpression node, Void param)
        {
            node.Callee.Accept(this, param);
            node.Arguments.ForEach(arg => arg.Accept(this, param));

            node.IsLValue = false;
            node.IsConstant = false;
            node.Type = node.Callee.Type!.Invocation(node.Arguments.Select(arg => (arg.Type!, arg.IsLValue, arg.Source)).ToArray(), node.Source, Diagnostics);
            return default;
        }

        public override Void Visit(NullExpression node, Void param)
        {
            node.IsLValue = false;
            node.IsConstant = true;
            node.Type = new NullType(node.Source);
            return default;
        }

        public override Void Visit(SizeOfExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.IsLValue = false;
            node.IsConstant = node.SubExpression.IsConstant;
            node.Type = BuiltInTypes.Int.CreateType(node.Source);
            return default;
        }

        public override Void Visit(StringLiteralExpression node, Void param)
        {
            node.IsLValue = false;
            node.IsConstant = true;
            node.Type = BuiltInTypes.String.CreateType(node.Source);
            return default;
        }

        public override Void Visit(UnaryExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.IsLValue = false;
            node.IsConstant = node.SubExpression.IsConstant;
            node.Type = node.SubExpression.Type!.UnaryOperation(node.Operator, node.Source, Diagnostics);
            return default;
        }

        public override Void Visit(ValueDeclRefExpression node, Void param)
        {
            node.IsLValue = node.Declaration is VarDeclaration { Kind: not VarKind.Constant };
            node.IsConstant = node.Declaration is EnumMemberDeclaration or FuncDeclaration or VarDeclaration { Kind: VarKind.Constant };
            node.Type = node.Declaration!.Type;
            return default;
        }

        public override Void Visit(VectorExpression node, Void param)
        {
            node.X.Accept(this, param);
            node.Y.Accept(this, param);
            node.Z.Accept(this, param);

            var vectorTy = BuiltInTypes.Vector.CreateType(node.Source);
            node.IsLValue = false;
            node.IsConstant = node.X.IsConstant && node.Y.IsConstant && node.Z.IsConstant;
            node.Type = vectorTy;

            for (int i = 0; i < vectorTy.Declaration.Fields.Count; i++)
            {
                var src = i switch { 0 => node.X, 1 => node.Y, 2 => node.Z, _ => throw new System.NotImplementedException() };
                var dest = vectorTy.Declaration.Fields[i];
                if (!dest.Type.CanAssignInit(src.Type!, src.IsLValue))
                {
                    Diagnostics.AddError($"Vector component {dest.Name.ToUpperInvariant()} requires type '{dest.Type}', found '{src.Type}'", src.Source);
                }
            }

            return default;
        }

        public override Void Visit(ErrorExpression node, Void param)
        {
            node.IsLValue = false;
            node.IsConstant = false;
            node.Type = new ErrorType(node.Source, node.Diagnostic);
            return default;
        }
    }
}
