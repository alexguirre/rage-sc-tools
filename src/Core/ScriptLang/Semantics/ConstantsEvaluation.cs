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
    using ScTools.ScriptLang.SymbolTables;

    using Console = System.Console;

    /// <summary>
    /// Calculates the values of enum members and CONST vars.
    /// TODO: Their initializers will get replaced by a literal expression.
    /// 
    /// Only INT, FLOAT, BOOL and STRING constants are supported.
    /// </summary>
    public sealed class ConstantsEvaluation
    {
        public DiagnosticsReport Diagnostics { get; }
        public GlobalSymbolTable Symbols { get; }

        private readonly ExpressionTypeChecker exprTypeChecker;

        public ConstantsEvaluation(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols, exprTypeChecker) = (diagnostics, symbols, new(diagnostics, symbols));

        public void Evaluate()
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
                    // TODO: error
                    Console.WriteLine($"There is some cycle. Unsolved ({unsolved.Count}): ");
                    unsolved.ForEach(c => Console.WriteLine($"\t{c.Name}"));
                    break;
                }
            }
        }

        private void Solve(IValueDeclaration value)
        {
            Console.Write($"solving: {value.Name}");
            switch (value)
            {
                case EnumMemberDeclaration d: SolveEnumMember(d); Console.Write($" = {d.Value}"); break;
                case VarDeclaration d: SolveConstant(d); Console.Write($" = {((IntLiteralExpression)d.Initializer!).Value}"); break;
                default: Debug.Assert(false); break;
            }
            Console.WriteLine();
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
                enumMember.Initializer.Accept(exprTypeChecker, default);
                if (!enumMember.Initializer!.IsConstant)
                {
                    enumMember.Initializer = new ErrorExpression(enumMember.Initializer!.Source, Diagnostics, $"The expression assigned to '{enumMember.Name}' must be constant");
                    return;
                }

                if (enumMember.Initializer is IntLiteralExpression lit)
                {
                    // already a literal, don't need to simplify the expression
                    enumMember.Value = lit.Value;
                    return;
                }

                if (!enumMember.Type.CanAssign(enumMember.Initializer.Type!))
                {
                    // note: the error diagnostic will be added later in the TypeChecker visitor
                    return;
                }

                enumMember.Value = enumMember.Initializer.Type is IError ? 0 : ExpressionEvaluator.EvalInt(enumMember.Initializer, Symbols);
            }

            // simplify the initializer expression
            enumMember.Initializer = new IntLiteralExpression(enumMember.Initializer?.Source ?? enumMember.Source, enumMember.Value);
        }

        private void SolveConstant(VarDeclaration constant)
        {
            constant.Initializer!.Accept(exprTypeChecker, default);
            if (!constant.Initializer!.IsConstant)
            {
                constant.Initializer = new ErrorExpression(constant.Initializer!.Source, Diagnostics, $"The expression assigned to '{constant.Name}' must be constant");
                return;
            }

            if (constant.Initializer is IntLiteralExpression or FloatLiteralExpression or BoolLiteralExpression or StringLiteralExpression)
            {
                // already a literal, don't need to simplify the expression
                return;
            }

            if (constant.Type is StringType)
            {
                Debug.Assert(constant.Initializer is StringLiteralExpression, "STRING constants should only have string literals as initializers, as they are the only constant expression");
                return;
            }

            if (constant.Type is IntType)
            {
                if (!constant.Type.CanAssign(constant.Initializer.Type!))
                {
                    // note: the error diagnostic will be added later in the TypeChecker visitor
                    return;
                }

                int value = constant.Initializer.Type is IError ? 0 : ExpressionEvaluator.EvalInt(constant.Initializer, Symbols);

                // simplify the initializer expression
                constant.Initializer = new IntLiteralExpression(constant.Initializer?.Source ?? constant.Source, value);
                Console.Write($" = {value}");
            }
            else
            {
                Debug.Assert(false, "Only INT constants are supported for now!");
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
            {
                Debug.Assert(node.Kind is VarKind.Constant);
                Debug.Assert(node.Initializer is not null);

                return node.Initializer.Accept(this, param);
            }

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
            {
                Debug.Assert(false, $"{nameof(FieldAccessExpression)} is not supported");
                return None;
            }

            public override IEnumerable<IValueDeclaration> Visit(FloatLiteralExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(IndexingExpression node, Void param)
            {
                Debug.Assert(false, $"{nameof(IndexingExpression)} is not supported");
                return None;
            }

            public override IEnumerable<IValueDeclaration> Visit(IntLiteralExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(InvocationExpression node, Void param)
            {
                Debug.Assert(false, $"{nameof(InvocationExpression)} is not supported");
                return None;
            }

            public override IEnumerable<IValueDeclaration> Visit(NullExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(SizeOfExpression node, Void param)
                => node.SubExpression.Accept(this, param);

            public override IEnumerable<IValueDeclaration> Visit(StringLiteralExpression node, Void param)
                => None;

            public override IEnumerable<IValueDeclaration> Visit(UnaryExpression node, Void param)
                => node.SubExpression.Accept(this, param);

            public override IEnumerable<IValueDeclaration> Visit(ValueDeclRefExpression node, Void param)
                => new[] { node.Declaration! };

            public override IEnumerable<IValueDeclaration> Visit(VectorExpression node, Void param)
            {
                Debug.Assert(false, $"{nameof(VectorExpression)} is not supported");
                return None;
            }
        }
    }
}
