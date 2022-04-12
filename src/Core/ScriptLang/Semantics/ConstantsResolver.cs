namespace ScTools.ScriptLang.Semantics
{
    using System.Collections.Generic;
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
    /// Calculates the values of enum members and CONST vars.
    /// Their initializers will get replaced by a <see cref="ILiteralExpression"/>.
    /// 
    /// Only INT, FLOAT, BOOL and STRING constants are supported.
    /// 
    /// Used by <see cref="TypeChecker"/>.
    /// </summary>
    internal sealed class ConstantsResolver
    {
        public DiagnosticsReport Diagnostics { get; }
        public GlobalSymbolTable Symbols { get; }

        private readonly ExpressionTypeChecker exprTypeChecker;

        public ConstantsResolver(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols, exprTypeChecker) = (diagnostics, symbols, new(diagnostics, symbols));

        public void Resolve()
        {
            var dependencyFinder = new DependencyFinder(Diagnostics, Symbols);

            var constants = Symbols.Values.Where(v => v is EnumMemberDeclaration or VarDeclaration { Kind: VarKind.Constant }).ToArray();
            var constantsDependencies = constants.ToDictionary(c => c, c => c.Accept(dependencyFinder, default).ToArray());
            var unsolved = new HashSet<IValueDeclaration>(constants);

            while (unsolved.Count > 0)
            {
                var origUnsolvedCount = unsolved.Count;

                foreach (var c in constants)
                {
                    var dependencies = constantsDependencies[c];
                    if (dependencies.All(d => !unsolved.Contains(d)) && unsolved.Contains(c))
                    {
                        Solve(c);
                        unsolved.Remove(c);
                    }

                }

                if (origUnsolvedCount == unsolved.Count)
                {
                    // no constants were resolved in this iteration, so there must be some dependency cycle in their initializers
                    unsolved.ForEach(c => Diagnostics.AddError($"The evaluation of the initializer for '{c.Name}' involves a circular dependency", c.Source));
                    break;
                }
            }
        }

        private void Solve(IValueDeclaration value)
        {
            switch (value)
            {
                case EnumMemberDeclaration d: SolveEnumMember(d); break;
                case VarDeclaration d: SolveConstant(d); break;
                default: Debug.Assert(false); break;
            }
        }

        private void SolveEnumMember(EnumMemberDeclaration enumMember)
        {
            if (enumMember.Initializer is null)
            {
                EnumMemberDeclaration? prevMember = GetPrevEnumMember(enumMember);
                enumMember.Value = prevMember is null ? 0 : prevMember.Value + 1;
            }
            else
            {
                var init = enumMember.Initializer!;
                init.Accept(exprTypeChecker, default);
                if (!init.IsConstant)
                {
                    enumMember.Initializer = new ErrorExpression(init.Source, Diagnostics, $"The expression assigned to '{enumMember.Name}' must be constant");
                    return;
                }

                if (init is IntLiteralExpression lit)
                {
                    // already a literal, don't need to simplify the expression
                    enumMember.Value = lit.Value;
                    return;
                }

                if (!enumMember.Type.CanAssign(init.Type!, init.IsLValue) &&
                    !BuiltInTypes.Int.CreateType(init.Source).CanAssign(init.Type!, init.IsLValue))
                {
                    enumMember.Type = new ErrorType(init.Source, Diagnostics, $"Cannot assign type '{init.Type}' to '{enumMember.Type}'");
                    return;
                }

                enumMember.Value = init.Type is IError ? 0 : ExpressionEvaluator.EvalInt(init, Symbols);
            }

            // simplify the initializer expression
            enumMember.Initializer = new IntLiteralExpression(Token.Integer(enumMember.Value, enumMember.Initializer?.Source ?? enumMember.Source));
        }

        private void SolveConstant(VarDeclaration constant)
        {
            if (constant.Type is not (IntType or FloatType or BoolType or StringType))
            {
                constant.Type = new ErrorType(constant.Type.Source, Diagnostics, $"The type of CONST variables must be 'INT', 'FLOAT', 'BOOL' or 'STRING', found '{constant.Type}'");
                return;
            }

            constant.Initializer!.Accept(exprTypeChecker, default);
            if (!constant.Initializer!.IsConstant)
            {
                constant.Initializer = new ErrorExpression(constant.Initializer!.Source, Diagnostics, $"The expression assigned to '{constant.Name}' must be constant");
                return;
            }

            if (!constant.Type.CanAssign(constant.Initializer.Type!, constant.Initializer.IsLValue))
            {
                constant.Type = new ErrorType(constant.Source, Diagnostics, $"Cannot assign type '{constant.Initializer.Type}' to '{constant.Type}'");
                return;
            }

            if (constant.Initializer is ILiteralExpression)
            {
                // already a literal, don't need to simplify the expression
                return;
            }

            switch (constant.Type)
            {
                case IntType:
                    var intValue = constant.Initializer.Type is IError ? 0 : ExpressionEvaluator.EvalInt(constant.Initializer, Symbols);

                    // simplify the initializer expression
                    constant.Initializer = new IntLiteralExpression(Token.Integer(intValue, constant.Initializer?.Source ?? constant.Source));
                    break;

                case FloatType:
                    var floatValue = constant.Initializer.Type is IError ? 0.0f : ExpressionEvaluator.EvalFloat(constant.Initializer, Symbols);

                    // simplify the initializer expression
                    constant.Initializer = new FloatLiteralExpression(Token.Float(floatValue, constant.Initializer?.Source ?? constant.Source));
                    break;

                case BoolType:
                    var boolValue = constant.Initializer.Type is IError ? false : ExpressionEvaluator.EvalBool(constant.Initializer, Symbols);

                    // simplify the initializer expression
                    constant.Initializer = new BoolLiteralExpression(Token.Bool(boolValue, constant.Initializer?.Source ?? constant.Source));
                    break;

                case StringType:
                    var strValue = constant.Initializer.Type is IError ? null : ExpressionEvaluator.EvalString(constant.Initializer, Symbols);

                    // simplify the initializer expression
                    constant.Initializer = new StringLiteralExpression((strValue is null ? Token.Null() : Token.String(strValue)) with { Location = constant.Initializer?.Source ?? constant.Source });
                    break;

                default: throw new System.NotImplementedException();
            }
        }

        private static EnumMemberDeclaration? GetPrevEnumMember(EnumMemberDeclaration enumMember)
        {
            var enumDecl = ((EnumType)enumMember.Type).Declaration;
            EnumMemberDeclaration? prevMember = null;
            foreach (var e in enumDecl.Members)
            {
                if (e == enumMember)
                {
                    break;
                }

                prevMember = e;
            }

            return prevMember;
        }


        private class DependencyFinder : EmptyVisitor<IEnumerable<IValueDeclaration>, Void>
        {
            public DiagnosticsReport Diagnostics { get; }
            public GlobalSymbolTable Symbols { get; }

            public DependencyFinder(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
                => (Diagnostics, Symbols) = (diagnostics, symbols);

            private IEnumerable<IValueDeclaration> None { get; } = Enumerable.Empty<IValueDeclaration>();

            public override IEnumerable<IValueDeclaration> Visit(VarDeclaration node, Void param)
                => node.Initializer?.Accept(this, param) ?? None;

            public override IEnumerable<IValueDeclaration> Visit(EnumMemberDeclaration node, Void param)
            {
                if (node.Initializer is null)
                {
                    // if the enum member doesn't have an initializer, it depends on the previous enum member
                    EnumMemberDeclaration? prevMember = GetPrevEnumMember(node);
                    return prevMember is null ? None : new[] { prevMember };
                }
                else
                {
                    return node.Initializer.Accept(this, param);
                }
            }

            public override IEnumerable<IValueDeclaration> Visit(BinaryExpression node, Void param)
                => node.LHS.Accept(this, param).Concat(node.RHS.Accept(this, param));

            public override IEnumerable<IValueDeclaration> Visit(BoolLiteralExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(FieldAccessExpression node, Void param)
                => node.SubExpression.Accept(this, param);

            public override IEnumerable<IValueDeclaration> Visit(FloatLiteralExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(IndexingExpression node, Void param)
                => node.Array.Accept(this, param).Concat(node.Index.Accept(this, param));

            public override IEnumerable<IValueDeclaration> Visit(IntLiteralExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(InvocationExpression node, Void param)
                => node.Callee.Accept(this, param).Concat(node.Arguments.SelectMany(arg => arg.Accept(this, param)));

            public override IEnumerable<IValueDeclaration> Visit(NullExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(SizeOfExpression node, Void param)
                => node.SubExpression.Accept(this, param);

            public override IEnumerable<IValueDeclaration> Visit(StringLiteralExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(UnaryExpression node, Void param)
                => node.SubExpression.Accept(this, param);

            public override IEnumerable<IValueDeclaration> Visit(DeclarationRefExpression node, Void param)
                => node.Declaration is IValueDeclaration valueDecl ? new[] { valueDecl } : None;

            public override IEnumerable<IValueDeclaration> Visit(VectorExpression node, Void param)
                => node.X.Accept(this, param).Concat(node.Y.Accept(this, param)).Concat(node.Z.Accept(this, param));
        }
    }
}
