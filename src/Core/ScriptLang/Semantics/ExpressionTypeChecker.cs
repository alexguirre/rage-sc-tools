namespace ScTools.ScriptLang.Semantics
{
    using System.Diagnostics;
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

            node.Semantics = new(Type: node.LHS.Type!.BinaryOperation(node.Operator, node.RHS.Type!, node.Location, Diagnostics),
                                 IsLValue: false,
                                 IsConstant: node.LHS.IsConstant && node.RHS.IsConstant);
            return default;
        }

        public override Void Visit(BoolLiteralExpression node, Void param)
        {
            node.Semantics = new(Type: BuiltInTypes.Bool.CreateType(node.Location),
                                 IsLValue: false,
                                 IsConstant: true);
            return default;
        }

        public override Void Visit(FieldAccessExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.Semantics = new(Type: node.SubExpression.Type!.FieldAccess(node.FieldName, node.Location, Diagnostics),
                                 IsLValue: node.SubExpression.IsLValue,
                                 IsConstant: false);
            return default;
        }

        public override Void Visit(FloatLiteralExpression node, Void param)
        {
            node.Semantics = new(Type: BuiltInTypes.Float.CreateType(node.Location),
                                 IsLValue: false,
                                 IsConstant: true);
            return default;
        }

        public override Void Visit(IndexingExpression node, Void param)
        {
            node.Array.Accept(this, param);
            node.Index.Accept(this, param);

            node.Semantics = new(Type: node.Array.Type!.Indexing(node.Index.Type!, node.Location, Diagnostics),
                                 IsLValue: true,
                                 IsConstant: false);
            return default;
        }

        public override Void Visit(IntLiteralExpression node, Void param)
        {
            node.Semantics = new(Type: BuiltInTypes.Int.CreateType(node.Location),
                                 IsLValue: false,
                                 IsConstant: true);
            return default;
        }

        public override Void Visit(InvocationExpression node, Void param)
        {
            node.Callee.Accept(this, param);
            node.Arguments.ForEach(arg => arg.Accept(this, param));

            var (ty, isConstant) = node.Callee.Type!.Invocation(node.Arguments.ToArray(), node.Location, Diagnostics);
            node.Semantics = new(Type: ty,
                                 IsLValue: false,
                                 IsConstant: isConstant);
            return default;
        }

        public override Void Visit(NullExpression node, Void param)
        {
            node.Semantics = new(Type: new NullType(node.Location),
                                 IsLValue: false,
                                 IsConstant: true);
            return default;
        }

        public override Void Visit(SizeOfExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.Semantics = new(Type: BuiltInTypes.Int.CreateType(node.Location),
                                 IsLValue: false,
                                 IsConstant: node.SubExpression.IsConstant);
            return default;
        }

        public override Void Visit(StringLiteralExpression node, Void param)
        {
            node.Semantics = new(Type: BuiltInTypes.String.CreateType(node.Location),
                                 IsLValue: false,
                                 IsConstant: true);
            return default;
        }

        public override Void Visit(UnaryExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.Semantics = new(Type: node.SubExpression.Type!.UnaryOperation(node.Operator, node.Location, Diagnostics),
                                 IsLValue: false,
                                 IsConstant: node.SubExpression.IsConstant);
            return default;
        }

        public override Void Visit(NameExpression node, Void param)
        {
            var sem = node.Semantics;
            var decl = sem.Declaration;
            if (decl is IValueDeclaration valueDecl)
            {
                node.Semantics = sem with
                {
                    Type = valueDecl.Type,
                    IsLValue = valueDecl is VarDeclaration { Kind: not VarKind.Constant },
                    // FIXME
                    IsConstant = false, // valueDecl is EnumMemberDeclaration or VarDeclaration { Kind: VarKind.Constant }
                };
            }
            else if (decl is ITypeDeclaration typeDecl)
            {
                node.Semantics = sem with
                {
                    Type = new TypeNameType(node.Location, typeDecl),
                    IsLValue = false,
                    IsConstant = true
                };
            }
            else if (decl is IError error)
            {
                node.Semantics = sem with
                {
                    Type = new ErrorType(error.Location, error.Diagnostic),
                    IsLValue = false,
                    IsConstant = false
                };
            }
            else
            {
                Debug.Assert(false);
            }
            return default;
        }

        public override Void Visit(VectorExpression node, Void param)
        {
            node.X.Accept(this, param);
            node.Y.Accept(this, param);
            node.Z.Accept(this, param);

            var vectorTy = BuiltInTypes.Vector.CreateType(node.Location);
            node.Semantics = new(Type: vectorTy,
                                 IsLValue: false,
                                 IsConstant: node.X.IsConstant && node.Y.IsConstant && node.Z.IsConstant);

            var floatTy = BuiltInTypes.Float.CreateType(node.Location);
            for (int i = 0; i < 3; i++)
            {
                var src = i switch { 0 => node.X, 1 => node.Y, 2 => node.Z, _ => throw new System.NotImplementedException() };
                if (!floatTy.CanAssign(src.Type!, src.IsLValue))
                {
                    var comp = i switch { 0 => "X", 1 => "Y", 2 => "Z", _ => throw new System.NotImplementedException() };
                    Diagnostics.AddError($"Vector component {comp} requires type '{floatTy}', found '{src.Type}'", src.Location);
                }
            }

            return default;
        }

        public override Void Visit(ErrorExpression node, Void param)
        {
            node.Semantics = new(Type: new ErrorType(node.Location, node.Diagnostic), IsLValue: false, IsConstant: false);
            return default;
        }
    }
}
