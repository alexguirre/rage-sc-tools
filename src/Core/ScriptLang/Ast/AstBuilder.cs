#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System;
    using System.Linq;

    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;
    using Antlr4.Runtime.Tree;

    using ScTools.ScriptLang.Grammar;

    public sealed class AstBuilder : ScLangBaseVisitor<Node>
    {
        private sealed class PlaceholderStatement : Statement
        {
            public PlaceholderStatement(SourceRange source) : base(source) { }
            public override string ToString() => "PlaceholderStmt";
        }

        private sealed class PlaceholderExpression : Expression
        {
            public PlaceholderExpression(SourceRange source) : base(source) { }
            public override string ToString() => "PlaceholderExpr";
        }

        private static SourceRange Source(ParserRuleContext context) => SourceRange.FromTokens(context.Start, context.Stop);
        private static Node PlaceholderStmt(ScLangParser.StatementContext context) => new PlaceholderStatement(Source(context));
        private static Node PlaceholderExpr(ScLangParser.ExpressionContext context) => new PlaceholderExpression(Source(context));

        public override Node VisitScript([NotNull] ScLangParser.ScriptContext context)
            => new Root(context.topLevelStatement().Select(stmt => Visit(stmt)).Cast<TopLevelStatement>(), Source(context));

        #region Top Level Statements
        public override Node VisitScriptNameStatement([NotNull] ScLangParser.ScriptNameStatementContext context)
            => new ScriptNameStatement(context.identifier().GetText(), Source(context));

        public override Node VisitUsingStatement([NotNull] ScLangParser.UsingStatementContext context)
            => new UsingStatement(context.@string().GetText(), Source(context));

        public override Node VisitProcedureStatement([NotNull] ScLangParser.ProcedureStatementContext context)
            => new ProcedureStatement(context.identifier().GetText(),
                                      (ParameterList)Visit(context.parameterList()),
                                      (StatementBlock)Visit(context.statementBlock()),
                                      Source(context));

        public override Node VisitFunctionStatement([NotNull] ScLangParser.FunctionStatementContext context)
            => new FunctionStatement(context.identifier(1).GetText(),
                                     context.returnType.GetText(),
                                     (ParameterList)Visit(context.parameterList()),
                                     (StatementBlock)Visit(context.statementBlock()),
                                     Source(context));

        public override Node VisitProcedurePrototypeStatement([NotNull] ScLangParser.ProcedurePrototypeStatementContext context)
            => new ProcedurePrototypeStatement(context.identifier().GetText(),
                                               (ParameterList)Visit(context.parameterList()),
                                               Source(context));

        public override Node VisitFunctionPrototypeStatement([NotNull] ScLangParser.FunctionPrototypeStatementContext context)
            => new FunctionPrototypeStatement(context.identifier(1).GetText(),
                                              context.returnType.GetText(),
                                              (ParameterList)Visit(context.parameterList()),
                                              Source(context));

        public override Node VisitProcedureNativeStatement([NotNull] ScLangParser.ProcedureNativeStatementContext context)
            => new ProcedureNativeStatement(context.identifier().GetText(),
                                            (ParameterList)Visit(context.parameterList()),
                                            Source(context));

        public override Node VisitFunctionNativeStatement([NotNull] ScLangParser.FunctionNativeStatementContext context)
            => new FunctionNativeStatement(context.identifier(1).GetText(),
                                           context.returnType.GetText(),
                                           (ParameterList)Visit(context.parameterList()),
                                           Source(context));

        public override Node VisitStructStatement([NotNull] ScLangParser.StructStatementContext context)
            => new StructStatement(context.identifier().GetText(),
                                   (StructFieldList)Visit(context.structFieldList()),
                                   Source(context));

        public override Node VisitStaticVariableStatement([NotNull] ScLangParser.StaticVariableStatementContext context)
            => new StaticVariableStatement((VariableDeclarationWithInitializer)Visit(context.variableDeclarationWithInitializer()),
                                           Source(context));
        #endregion Top Level Statements


        #region Statements
        public override Node VisitVariableDeclarationStatement([NotNull] ScLangParser.VariableDeclarationStatementContext context)
            => new VariableDeclarationStatement((VariableDeclarationWithInitializer)Visit(context.variableDeclarationWithInitializer()),
                                                Source(context));

        public override Node VisitAssignmentStatement([NotNull] ScLangParser.AssignmentStatementContext context)
            => new AssignmentStatement((Expression)Visit(context.left),
                                       (Expression)Visit(context.right),
                                       Source(context));

        public override Node VisitIfStatement([NotNull] ScLangParser.IfStatementContext context)
            => new IfStatement((Expression)Visit(context.condition),
                               (StatementBlock)Visit(context.thenBlock),
                               (StatementBlock?)Visit(context.elseBlock),
                               Source(context));

        public override Node VisitWhileStatement([NotNull] ScLangParser.WhileStatementContext context)
            => new WhileStatement((Expression)Visit(context.condition),
                                  (StatementBlock)Visit(context.statementBlock()),
                                  Source(context));

        public override Node VisitReturnStatement([NotNull] ScLangParser.ReturnStatementContext context)
            => new ReturnStatement((Expression?)Visit(context.expression()), Source(context));

        public override Node VisitInvocationStatement([NotNull] ScLangParser.InvocationStatementContext context)
            => new InvocationStatement((Expression)Visit(context.expression()),
                                       (ArgumentList)Visit(context.argumentList()),
                                       Source(context));
        #endregion Statements

        #region Expressions
        public override Node VisitParenthesizedExpression([NotNull] ScLangParser.ParenthesizedExpressionContext context)
            => Visit(context.expression());
        
        public override Node VisitUnaryExpression([NotNull] ScLangParser.UnaryExpressionContext context)
            => new UnaryExpression(context.op.Type switch
            {
                ScLangLexer.K_NOT => UnaryOperator.Not,
                ScLangLexer.OP_SUBTRACT => UnaryOperator.Negate,
                _ => throw new NotImplementedException()
            },
            (Expression)Visit(context.expression()),
            Source(context));

        public override Node VisitBinaryExpression([NotNull] ScLangParser.BinaryExpressionContext context)
            => new BinaryExpression(context.op.Type switch
            {
                ScLangLexer.OP_ADD => BinaryOperator.Add,
                ScLangLexer.OP_SUBTRACT => BinaryOperator.Subtract,
                ScLangLexer.OP_MULTIPLY => BinaryOperator.Multiply,
                ScLangLexer.OP_DIVIDE => BinaryOperator.Divide,
                ScLangLexer.OP_MODULO => BinaryOperator.Modulo,
                ScLangLexer.OP_OR => BinaryOperator.Or,
                ScLangLexer.OP_AND => BinaryOperator.And,
                ScLangLexer.OP_XOR => BinaryOperator.Xor,
                ScLangLexer.OP_EQUAL => BinaryOperator.Equal,
                ScLangLexer.OP_NOT_EQUAL => BinaryOperator.NotEqual,
                ScLangLexer.OP_GREATER => BinaryOperator.Greater,
                ScLangLexer.OP_GREATER_OR_EQUAL => BinaryOperator.GreaterOrEqual,
                ScLangLexer.OP_LESS => BinaryOperator.Less,
                ScLangLexer.OP_LESS_OR_EQUAL => BinaryOperator.LessOrEqual,
                ScLangLexer.K_AND => BinaryOperator.LogicalAnd,
                ScLangLexer.K_OR => BinaryOperator.LogicalOr,
                _ => throw new NotImplementedException()
            },
            (Expression)Visit(context.left),
            (Expression)Visit(context.right),
            Source(context));

        public override Node VisitAggregateExpression([NotNull] ScLangParser.AggregateExpressionContext context)
            => new AggregateExpression(context.expression().Select(Visit).Cast<Expression>(), Source(context));

        public override Node VisitIdentifierExpression([NotNull] ScLangParser.IdentifierExpressionContext context)
            => new IdentifierExpression(context.identifier().GetText(), Source(context));

        public override Node VisitMemberAccessExpression([NotNull] ScLangParser.MemberAccessExpressionContext context)
            => new MemberAccessExpression((Expression)Visit(context.expression()),
                                          context.identifier().GetText(),
                                          Source(context));

        public override Node VisitArrayAccessExpression([NotNull] ScLangParser.ArrayAccessExpressionContext context)
            => new ArrayAccessExpression((Expression)Visit(context.expression()),
                                         (ArrayIndexer)Visit(context.arrayIndexer()),
                                         Source(context));

        public override Node VisitInvocationExpression([NotNull] ScLangParser.InvocationExpressionContext context)
            => new InvocationExpression((Expression)Visit(context.expression()),
                                        (ArgumentList)Visit(context.argumentList()),
                                        Source(context));

        public override Node VisitLiteralExpression([NotNull] ScLangParser.LiteralExpressionContext context)
            => new LiteralExpression(context switch
            {
                var c when c.integer() != null => LiteralKind.Int,
                var c when c.@float() != null => LiteralKind.Float,
                var c when c.@string() != null => LiteralKind.String,
                var c when c.@bool() != null => LiteralKind.Bool,
                _ => throw new NotImplementedException()
            },
            context.GetText(),
            Source(context));
        #endregion Expressions

        #region Declarators
        public override Node VisitSimpleDeclarator([NotNull] ScLangParser.SimpleDeclaratorContext context)
            => new SimpleDeclarator(context.identifier().GetText(), Source(context));

        public override Node VisitArrayDeclarator([NotNull] ScLangParser.ArrayDeclaratorContext context)
            => new ArrayDeclarator((Declarator)Visit(context.noRefDeclarator()),
                                   (Expression)Visit(context.expression()),
                                   Source(context));

        public override Node VisitParenthesizedRefDeclarator([NotNull] ScLangParser.ParenthesizedRefDeclaratorContext context)
            => (RefDeclarator)Visit(context.refDeclarator());

        public override Node VisitRefDeclarator([NotNull] ScLangParser.RefDeclaratorContext context)
            => new RefDeclarator((Declarator)Visit(context.noRefDeclarator()), Source(context));
        #endregion

        #region Misc
        public override Node VisitStatementBlock([NotNull] ScLangParser.StatementBlockContext context)
            => new StatementBlock(context.statement().Select(Visit).Cast<Statement>(), Source(context));

        public override Node VisitVariableDeclaration([NotNull] ScLangParser.VariableDeclarationContext context)
            => new VariableDeclaration(context.type.GetText(),
                                       (Declarator)Visit(context.declarator()),
                                       Source(context));

        public override Node VisitVariableDeclarationWithInitializer([NotNull] ScLangParser.VariableDeclarationWithInitializerContext context)
            => new VariableDeclarationWithInitializer((VariableDeclaration)Visit(context.decl),
                                                      (Expression?)Visit(context.initializer),
                                                      Source(context));

        public override Node VisitParameterList([NotNull] ScLangParser.ParameterListContext context)
            => new ParameterList(context.variableDeclaration().Select(Visit).Cast<VariableDeclaration>(), Source(context));

        public override Node VisitArgumentList([NotNull] ScLangParser.ArgumentListContext context)
            => new ArgumentList(context.expression().Select(Visit).Cast<Expression>(),
                                Source(context));

        public override Node VisitStructFieldList([NotNull] ScLangParser.StructFieldListContext context)
            => new StructFieldList(context.variableDeclarationWithInitializer().Select(Visit).Cast<VariableDeclarationWithInitializer>(),
                                   Source(context));

        public override Node VisitArrayIndexer([NotNull] ScLangParser.ArrayIndexerContext context)
            => new ArrayIndexer((Expression)Visit(context.expression()), Source(context));

        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("tree")]
        public override Node? Visit(IParseTree? tree)
        {
            return tree switch
            {
                null => null,
                ScLangParser.StatementContext stmt when !stmt.GetType().IsSubclassOf(typeof(ScLangParser.StatementContext))
                    => new ErrorStatement(stmt.GetText(), Source(stmt)),
                ScLangParser.ExpressionContext expr when !expr.GetType().IsSubclassOf(typeof(ScLangParser.ExpressionContext))
                    => new ErrorExpression(expr.GetText(), Source(expr)),
                _ => base.Visit(tree),
            };
        }
        #endregion Misc
    }
}
