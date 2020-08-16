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
            => new ScriptNameStatement((Identifier)context.identifier().Accept(this), Source(context));

        public override Node VisitProcedureStatement([NotNull] ScLangParser.ProcedureStatementContext context)
            => new ProcedureStatement((Identifier)context.identifier().Accept(this),
                                      (ParameterList)context.parameterList().Accept(this),
                                      (StatementBlock)context.statementBlock().Accept(this),
                                      Source(context));

        public override Node VisitFunctionStatement([NotNull] ScLangParser.FunctionStatementContext context)
            => new FunctionStatement((Identifier)context.identifier().Accept(this),
                                     (Type)context.returnType.Accept(this),
                                     (ParameterList)context.parameterList().Accept(this),
                                     (StatementBlock)context.statementBlock().Accept(this),
                                     Source(context));

        public override Node VisitProcedurePrototypeStatement([NotNull] ScLangParser.ProcedurePrototypeStatementContext context)
            => new ProcedurePrototypeStatement((Identifier)context.identifier().Accept(this),
                                               (ParameterList)context.parameterList().Accept(this),
                                               Source(context));

        public override Node VisitFunctionPrototypeStatement([NotNull] ScLangParser.FunctionPrototypeStatementContext context)
            => new FunctionPrototypeStatement((Identifier)context.identifier().Accept(this),
                                              (Type)context.returnType.Accept(this),
                                              (ParameterList)context.parameterList().Accept(this),
                                              Source(context));

        public override Node VisitStructStatement([NotNull] ScLangParser.StructStatementContext context)
            => new StructStatement((Identifier)context.identifier().Accept(this),
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
            => new ParenthesizedExpression((Expression)context.expression().Accept(this), Source(context));
        
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
                _ => throw new NotImplementedException()
            },
            (Expression)context.left.Accept(this),
            (Expression)context.right.Accept(this),
            Source(context));

        public override Node VisitAggregateExpression([NotNull] ScLangParser.AggregateExpressionContext context)
            => new AggregateExpression(context.expression().Select(expr => expr.Accept(this)).Cast<Expression>(), Source(context));

        public override Node VisitIdentifierExpression([NotNull] ScLangParser.IdentifierExpressionContext context)
            => new IdentifierExpression((Identifier)context.identifier().Accept(this), Source(context));

        public override Node VisitMemberAccessExpression([NotNull] ScLangParser.MemberAccessExpressionContext context)
            => new MemberAccessExpression((Expression)context.expression().Accept(this),
                                          (Identifier)context.identifier().Accept(this),
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
                var c when c.numeric() != null => LiteralKind.Numeric,
                var c when c.@string() != null => LiteralKind.String,
                var c when c.@bool() != null => LiteralKind.Bool,
                _ => throw new NotImplementedException()
            },
            context.GetText(),
            Source(context));
        #endregion Expressions

        #region Types
        public override Node VisitType([NotNull] ScLangParser.TypeContext context)
            => new Type((Identifier)context.typeName().Accept(this),
                        context.isRef != null,
                        Source(context));
        #endregion

        #region Misc
        public override Node VisitStatementBlock([NotNull] ScLangParser.StatementBlockContext context)
            => new StatementBlock(context.statement().Select(stmt => stmt.Accept(this)).Cast<Statement>(), Source(context));

        public override Node VisitVariableDeclaration([NotNull] ScLangParser.VariableDeclarationContext context)
            => new VariableDeclaration((Type)context.type().Accept(this),
                                       (Identifier)context.identifier().Accept(this),
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

        public override Node VisitIdentifier([NotNull] ScLangParser.IdentifierContext context)
            => new Identifier(context.GetText(), Source(context));

        public override Node VisitTypeName([NotNull] ScLangParser.TypeNameContext context)
            => new Identifier(context.GetText(), Source(context));

        public override Node VisitArrayIndexer([NotNull] ScLangParser.ArrayIndexerContext context)
            => new ArrayIndexer((Expression)context.expression().Accept(this), Source(context));
        #endregion Misc
    }
}
