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

        private readonly ExpressionTypeChecker exprTypeChecker;

        private TypeChecker(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols, exprTypeChecker) = (diagnostics, symbols, new(diagnostics, symbols));

        #region Expressions
        public override Void Visit(BinaryExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(BoolLiteralExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(FieldAccessExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(FloatLiteralExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(IndexingExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(IntLiteralExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(InvocationExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(NullExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(SizeOfExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(StringLiteralExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(UnaryExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(ValueDeclRefExpression node, Void param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(VectorExpression node, Void param) => node.Accept(exprTypeChecker, default);
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

        public override Void Visit(IfStatement node, Void param)
        {
            node.Condition.Accept(this, param);

            var boolTy = Symbols.Bool.CreateType(node.Condition.Source);
            if (!boolTy.CanAssign(node.Condition.Type!))
            {
                Diagnostics.AddError($"IF condition requires type '{boolTy}', found '{node.Condition.Type}'", node.Condition.Source);
            }

            node.Then.ForEach(stmt => stmt.Accept(this, param));
            node.Else.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(RepeatStatement node, Void param)
        {
            node.Limit.Accept(this, param);
            node.Counter.Accept(this, param);

            var intTy = Symbols.Int.CreateType(node.Limit.Source);
            if (!intTy.CanAssign(node.Limit.Type!))
            {
                Diagnostics.AddError($"REPEAT limit requires type '{intTy}', found '{node.Limit.Type}'", node.Limit.Source);
            }

            if (!node.Counter.IsLValue)
            {
                Diagnostics.AddError($"REPEAT counter requires an lvalue reference of type '{intTy}', found non-lvalue '{node.Counter.Type}'", node.Counter.Source);
            }
            else if (!intTy.CanAssign(node.Counter.Type!))
            {
                Diagnostics.AddError($"REPEAT counter requires type '{intTy}', found '{node.Counter.Type}'", node.Counter.Source);
            }

            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(ReturnStatement node, Void param)
        {
            node.Expression?.Accept(this, param);

            // TODO: check if return expression matches function return type

            return DefaultReturn;
        }

        public override Void Visit(SwitchStatement node, Void param)
        {
            node.Expression.Accept(this, param);

            var intTy = Symbols.Int.CreateType(node.Expression.Source);
            if (!intTy.CanAssign(node.Expression.Type!))
            {
                Diagnostics.AddError($"SWITCH expression requires type '{intTy}', found '{node.Expression.Type}'", node.Expression.Source);
            }

            node.Cases.ForEach(c => c.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(ValueSwitchCase node, Void param)
        {
            node.Value.Accept(this, param);

            var intTy = Symbols.Int.CreateType(node.Value.Source);
            if (!intTy.CanAssign(node.Value.Type!))
            {
                Diagnostics.AddError($"CASE value requires type '{intTy}', found '{node.Value.Type}'", node.Value.Source);
            }

            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(WhileStatement node, Void param)
        {
            node.Condition.Accept(this, param);

            var boolTy = Symbols.Bool.CreateType(node.Condition.Source);
            if (!boolTy.CanAssign(node.Condition.Type!))
            {
                Diagnostics.AddError($"WHILE condition requires type '{boolTy}', found '{node.Condition.Type}'", node.Condition.Source);
            }

            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }
        #endregion Statements

        public static void Check(Program root, DiagnosticsReport diagnostics, GlobalSymbolTable globalSymbols)
        {
            root.Accept(new TypeChecker(diagnostics, globalSymbols), default);
        }
    }
}
