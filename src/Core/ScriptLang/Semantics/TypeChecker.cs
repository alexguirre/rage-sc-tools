namespace ScTools.ScriptLang.Semantics
{
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.SymbolTables;

    public sealed class TypeChecker : DFSVisitor
    {
        public DiagnosticsReport Diagnostics { get; }
        public GlobalSymbolTable Symbols { get; }

        private TypeChecker(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols) = (diagnostics, symbols);

        #region Expressions
        public override Void Visit(BinaryExpression node, Void param)
        {
            node.LHS.Accept(this, param);
            node.RHS.Accept(this, param);

            node.LValue = false;
            node.Type = node.LHS.Type!.BinaryOperation(node.Operator, node.RHS.Type!, node.Source, Diagnostics);
            return DefaultReturn;
        }

        public override Void Visit(BoolLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.Bool.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(FieldAccessExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            (node.Type, node.LValue) = node.SubExpression.Type!.FieldAccess(node.FieldName, node.Source, Diagnostics);
            return DefaultReturn;
        }

        public override Void Visit(FloatLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.Float.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(IndexingExpression node, Void param)
        {
            node.Array.Accept(this, param);
            node.Index.Accept(this, param);

            node.LValue = true;
            node.Type = node.Array.Type!.Indexing(node.Index.Type!, node.Source, Diagnostics);
            return DefaultReturn;
        }

        public override Void Visit(IntLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.Int.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(InvocationExpression node, Void param)
        {
            node.Callee.Accept(this, param);
            node.Arguments.ForEach(arg => arg.Accept(this, param));

            node.LValue = false;
            node.Type = node.Callee.Type!.Invocation(node.Arguments.Select(arg => arg.Type!).ToArray(), node.Source, Diagnostics);
            return DefaultReturn;
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
            node.Type = Symbols.Int.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(StringLiteralExpression node, Void param)
        {
            node.LValue = false;
            node.Type = Symbols.String.CreateType(node.Source);
            return DefaultReturn;
        }

        public override Void Visit(UnaryExpression node, Void param)
        {
            node.SubExpression.Accept(this, param);

            node.LValue = false;
            node.Type = node.SubExpression.Type!.UnaryOperation(node.Operator, node.Source, Diagnostics);
            return DefaultReturn;
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

            var vectorTy = Symbols.Vector.CreateType(node.Source);
            node.LValue = false;
            node.Type = vectorTy;

            for (int i = 0; i < vectorTy.Declaration.Fields.Count; i++)
            {
                var src = i switch { 0 => node.X, 1 => node.Y, 2 => node.Z, _ => throw new System.NotImplementedException() };
                var dest = vectorTy.Declaration.Fields[i];
                if (!dest.Type.CanAssign(src.Type!))
                {
                    Diagnostics.AddError($"Vector component {dest.Name.ToUpperInvariant()} requires type '{dest.Type}', found '{src.Type}'", src.Source);
                }
            }

            return DefaultReturn;
        }
        #endregion Expressions

        #region Types
        public override Void Visit(ArrayType node, Void param)
        {
            node.ItemType.Accept(this, param);
            node.LengthExpression.Accept(this, param);

            var arrayLengthTy = Symbols.Int.CreateType(node.LengthExpression.Source);
            if (!arrayLengthTy.CanAssign(node.LengthExpression.Type!))
            {
                Diagnostics.AddError($"Array size requires type '{arrayLengthTy}', found '{node.LengthExpression.Type}'", node.LengthExpression.Source);
            }

            return DefaultReturn;
        }
        #endregion Types

        #region Declarations
        public override Void Visit(EnumMemberDeclaration node, Void param)
        {
            node.Type.Accept(this, param);
            if (node.Initializer is not null)
            {
                node.Initializer.Accept(this, param);

                node.Type.Assign(node.Initializer.Type!, node.Initializer.Source, Diagnostics);
            }

            return DefaultReturn;
        }

        public override Void Visit(StructField node, Void param)
        {
            node.Type.Accept(this, param);
            if (node.Initializer is not null)
            {
                node.Initializer.Accept(this, param);

                node.Type.Assign(node.Initializer.Type!, node.Initializer.Source, Diagnostics);
            }

            return DefaultReturn;
        }

        public override Void Visit(VarDeclaration node, Void param)
        {
            node.Type.Accept(this, param);
            if (node.Initializer is not null)
            {
                node.Initializer.Accept(this, param);

                node.Type.Assign(node.Initializer.Type!, node.Initializer.Source, Diagnostics);
            }

            return DefaultReturn;
        }
        #endregion Declarations

        #region Statements
        public override Void Visit(AssignmentStatement node, Void param)
        {
            node.LHS.Accept(this, param);
            node.RHS.Accept(this, param);

            node.LHS.Type!.Assign(node.RHS.Type!, node.Source, Diagnostics);
            return DefaultReturn;
        }
        #endregion Statements

        public static void Check(Program root, DiagnosticsReport diagnostics, GlobalSymbolTable globalSymbols)
        {
            root.Accept(new TypeChecker(diagnostics, globalSymbols), default);
        }
    }
}
