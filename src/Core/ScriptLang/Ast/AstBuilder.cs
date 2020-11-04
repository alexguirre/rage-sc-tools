#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System;
    using System.Linq;

    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;

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
            => new Root(context.topLevelStatement().Select(stmt => stmt.Accept(this)).Cast<TopLevelStatement>(), Source(context));

        #region Top Level Statements
        public override Node VisitScriptNameStatement([NotNull] ScLangParser.ScriptNameStatementContext context)
            => new ScriptNameStatement(context.identifier().GetText(), Source(context));

        public override Node VisitProcedureStatement([NotNull] ScLangParser.ProcedureStatementContext context)
            => new ProcedureStatement(context.identifier().GetText(),
                                      (ParameterList)context.parameterList().Accept(this),
                                      (StatementBlock)context.statementBlock().Accept(this),
                                      Source(context));

        public override Node VisitFunctionStatement([NotNull] ScLangParser.FunctionStatementContext context)
            => new FunctionStatement(context.identifier().GetText(),
                                     (Type)context.returnType.Accept(this),
                                     (ParameterList)context.parameterList().Accept(this),
                                     (StatementBlock)context.statementBlock().Accept(this),
                                     Source(context));

        public override Node VisitProcedurePrototypeStatement([NotNull] ScLangParser.ProcedurePrototypeStatementContext context)
            => new ProcedurePrototypeStatement(context.identifier().GetText(),
                                               (ParameterList)context.parameterList().Accept(this),
                                               Source(context));

        public override Node VisitFunctionPrototypeStatement([NotNull] ScLangParser.FunctionPrototypeStatementContext context)
            => new FunctionPrototypeStatement(context.identifier().GetText(),
                                              (Type)context.returnType.Accept(this),
                                              (ParameterList)context.parameterList().Accept(this),
                                              Source(context));

        public override Node VisitProcedureNativeStatement([NotNull] ScLangParser.ProcedureNativeStatementContext context)
            => new ProcedureNativeStatement(context.identifier().GetText(),
                                            (ParameterList)context.parameterList().Accept(this),
                                            Source(context));

        public override Node VisitFunctionNativeStatement([NotNull] ScLangParser.FunctionNativeStatementContext context)
            => new FunctionNativeStatement(context.identifier().GetText(),
                                           (Type)context.returnType.Accept(this),
                                           (ParameterList)context.parameterList().Accept(this),
                                           Source(context));

        public override Node VisitStructStatement([NotNull] ScLangParser.StructStatementContext context)
            => new StructStatement(context.identifier().GetText(),
                                   (StructFieldList)context.structFieldList().Accept(this),
                                   Source(context));

        public override Node VisitStaticVariableStatement([NotNull] ScLangParser.StaticVariableStatementContext context)
            => new StaticVariableStatement((VariableDeclarationWithInitializer)context.variableDeclarationWithInitializer().Accept(this),
                                           Source(context));
        #endregion Top Level Statements

        #region Statements
        public override Node VisitVariableDeclarationStatement([NotNull] ScLangParser.VariableDeclarationStatementContext context)
            => new VariableDeclarationStatement((VariableDeclarationWithInitializer)context.variableDeclarationWithInitializer().Accept(this),
                                                Source(context));

        public override Node VisitAssignmentStatement([NotNull] ScLangParser.AssignmentStatementContext context)
            => new AssignmentStatement((Expression)context.left.Accept(this),
                                       (Expression)context.right.Accept(this),
                                       Source(context));

        public override Node VisitIfStatement([NotNull] ScLangParser.IfStatementContext context)
            => new IfStatement((Expression)context.condition.Accept(this),
                               (StatementBlock)context.thenBlock.Accept(this),
                               (StatementBlock?)context.elseBlock?.Accept(this),
                               Source(context));

        public override Node VisitWhileStatement([NotNull] ScLangParser.WhileStatementContext context)
            => new WhileStatement((Expression)context.condition.Accept(this),
                                  (StatementBlock)context.statementBlock().Accept(this),
                                  Source(context));

        public override Node VisitReturnStatement([NotNull] ScLangParser.ReturnStatementContext context)
            => new ReturnStatement((Expression?)context.expression()?.Accept(this), Source(context));

        public override Node VisitInvocationStatement([NotNull] ScLangParser.InvocationStatementContext context)
            => new InvocationStatement((Expression)context.expression().Accept(this),
                                       (ArgumentList)context.argumentList().Accept(this),
                                       Source(context));
        #endregion Statements

        #region Expressions
        public override Node VisitParenthesizedExpression([NotNull] ScLangParser.ParenthesizedExpressionContext context)
            => context.expression().Accept(this);
        
        public override Node VisitUnaryExpression([NotNull] ScLangParser.UnaryExpressionContext context)
            => new UnaryExpression(context.op.Type switch
            {
                ScLangLexer.K_NOT => UnaryOperator.Not,
                ScLangLexer.OP_SUBTRACT => UnaryOperator.Negate,
                _ => throw new NotImplementedException()
            },
            (Expression)context.expression().Accept(this),
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
            (Expression)context.left.Accept(this),
            (Expression)context.right.Accept(this),
            Source(context));

        public override Node VisitAggregateExpression([NotNull] ScLangParser.AggregateExpressionContext context)
            => new AggregateExpression(context.expression().Select(expr => expr.Accept(this)).Cast<Expression>(), Source(context));

        public override Node VisitIdentifierExpression([NotNull] ScLangParser.IdentifierExpressionContext context)
            => new IdentifierExpression(context.identifier().GetText(), Source(context));

        public override Node VisitMemberAccessExpression([NotNull] ScLangParser.MemberAccessExpressionContext context)
            => new MemberAccessExpression((Expression)context.expression().Accept(this),
                                          context.identifier().GetText(),
                                          Source(context));

        public override Node VisitArrayAccessExpression([NotNull] ScLangParser.ArrayAccessExpressionContext context)
            => new ArrayAccessExpression((Expression)context.expression().Accept(this),
                                         (ArrayIndexer)context.arrayIndexer().Accept(this),
                                         Source(context));

        public override Node VisitInvocationExpression([NotNull] ScLangParser.InvocationExpressionContext context)
            => new InvocationExpression((Expression)context.expression().Accept(this),
                                        (ArgumentList)context.argumentList().Accept(this),
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

        #region Types
        public override Node VisitType([NotNull] ScLangParser.TypeContext context)
            => new Type(context.typeName().GetText(),
                        context.isRef != null,
                        Source(context));
        #endregion

        #region Misc
        public override Node VisitStatementBlock([NotNull] ScLangParser.StatementBlockContext context)
            => new StatementBlock(context.statement().Select(stmt => stmt.Accept(this)).Cast<Statement>(), Source(context));

        public override Node VisitVariableDeclaration([NotNull] ScLangParser.VariableDeclarationContext context)
            => new VariableDeclaration((Type)context.type().Accept(this),
                                       context.identifier().GetText(),
                                       (ArrayIndexer?)context.arrayIndexer()?.Accept(this),
                                       Source(context));

        public override Node VisitVariableDeclarationWithInitializer([NotNull] ScLangParser.VariableDeclarationWithInitializerContext context)
            => new VariableDeclarationWithInitializer((VariableDeclaration)context.decl.Accept(this),
                                                      (Expression?)context.initializer?.Accept(this),
                                                      Source(context));

        public override Node VisitParameterList([NotNull] ScLangParser.ParameterListContext context)
            => new ParameterList(context.variableDeclaration().Select(v => v.Accept(this)).Cast<VariableDeclaration>(), Source(context));

        public override Node VisitArgumentList([NotNull] ScLangParser.ArgumentListContext context)
            => new ArgumentList(context.expression().Select(expr => expr.Accept(this)).Cast<Expression>(),
                                Source(context));

        public override Node VisitStructFieldList([NotNull] ScLangParser.StructFieldListContext context)
            => new StructFieldList(context.variableDeclarationWithInitializer().Select(v => v.Accept(this)).Cast<VariableDeclarationWithInitializer>(),
                                   Source(context));

        public override Node VisitArrayIndexer([NotNull] ScLangParser.ArrayIndexerContext context)
            => new ArrayIndexer((Expression)context.expression().Accept(this), Source(context));
        #endregion Misc
    }
}
