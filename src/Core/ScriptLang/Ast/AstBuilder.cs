#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;
    using Antlr4.Runtime.Tree;

    using ScTools.ScriptLang.Grammar;

    public static class AstBuilder
    {
        private static SourceRange Source(ParserRuleContext context) => SourceRange.FromTokens(context.Start, context.Stop);

        public static Root Build(ScLangParser.ScriptContext context)
            => new Root(context.topLevelStatement().SelectMany(stmt => Build(stmt)), Source(context));

        private static IEnumerable<TopLevelStatement> Build(ScLangParser.TopLevelStatementContext context)
        {
            switch (context)
            {
                case ScLangParser.StaticVariableStatementContext c:
                    foreach (var decl in Build(c.declaration()))
                    {
                        yield return new StaticVariableStatement(decl, Source(c));
                    }
                    break;
                case ScLangParser.ConstantVariableStatementContext c:
                    foreach (var decl in Build(c.declaration()))
                    {
                        yield return new ConstantVariableStatement(decl, Source(c));
                    }
                    break;
                default:
                    yield return BuildOne(context);
                    break;
            }
        }

        /// <summary>
        /// Builds TopLevelStatements that have a one-to-one mapping between parse tree and AST.
        /// </summary>
        private static TopLevelStatement BuildOne(ScLangParser.TopLevelStatementContext context)
            => context switch
            {
                ScLangParser.ScriptNameStatementContext c => new ScriptNameStatement(c.identifier().GetText(), Source(c)),
                ScLangParser.ScriptHashStatementContext c => new ScriptHashStatement(c.integer().GetText().ParseAsInt(), Source(c)),
                ScLangParser.UsingStatementContext c => new UsingStatement(c.@string().GetText(), Source(c)),
                ScLangParser.ProcedureStatementContext c => new ProcedureStatement(c.identifier().GetText(),
                                                                                   Build(c.parameterList()),
                                                                                   Build(c.statementBlock()),
                                                                                   Source(c)),
                ScLangParser.FunctionStatementContext c => new FunctionStatement(c.identifier(1).GetText(),
                                                                                 c.returnType.GetText(),
                                                                                 Build(c.parameterList()),
                                                                                 Build(c.statementBlock()),
                                                                                 Source(c)),
                ScLangParser.ProcedurePrototypeStatementContext c => new ProcedurePrototypeStatement(c.identifier().GetText(),
                                                                                                     Build(c.parameterList()),
                                                                                                     Source(c)),
                ScLangParser.FunctionPrototypeStatementContext c => new FunctionPrototypeStatement(c.identifier(1).GetText(),
                                                                                                   c.returnType.GetText(),
                                                                                                   Build(c.parameterList()),
                                                                                                   Source(c)),
                ScLangParser.ProcedureNativeStatementContext c => new ProcedureNativeStatement(c.identifier().GetText(),
                                                                                               Build(c.parameterList()),
                                                                                               Source(c)),
                ScLangParser.FunctionNativeStatementContext c => new FunctionNativeStatement(c.identifier(1).GetText(),
                                                                                             c.returnType.GetText(),
                                                                                             Build(c.parameterList()),
                                                                                             Source(c)),
                ScLangParser.StructStatementContext c => new StructStatement(c.identifier().GetText(),
                                                                             c.structFieldList().declarationNoInit().SelectMany(Build),
                                                                             Source(c)),
                _ => throw new NotSupportedException(),
            };

        private static IEnumerable<Declaration> Build(ScLangParser.ParameterListContext context)
            => context.singleDeclarationNoInit().Select(Build);

        private static IEnumerable<Declaration> Build(ScLangParser.DeclarationContext context)
        {
            var type = context.type.GetText();
            return context.initDeclaratorList().initDeclarator().Select(d => new Declaration(type,
                                                                                             Build(d.declarator()),
                                                                                             BuildOrNull(d.initializer),
                                                                                             Source(context)));
        }

        private static IEnumerable<Declaration> Build(ScLangParser.DeclarationNoInitContext context)
        {
            var type = context.type.GetText();
            return context.declaratorList().declarator().Select(d => new Declaration(type,
                                                                                     Build(d),
                                                                                     null,
                                                                                     Source(context)));
        }

        private static Declaration Build(ScLangParser.SingleDeclarationNoInitContext context)
            => new Declaration(context.type.GetText(),
                               Build(context.declarator()),
                               null,
                               Source(context));

        private static Declarator Build(ScLangParser.DeclaratorContext context)
            => context.refDeclarator() != null ? Build(context.refDeclarator()) : Build(context.noRefDeclarator());

        private static Declarator Build(ScLangParser.RefDeclaratorContext context)
            => new RefDeclarator(Build(context.noRefDeclarator()), Source(context));

        private static Declarator Build(ScLangParser.NoRefDeclaratorContext context)
            => context switch
            {
                ScLangParser.SimpleDeclaratorContext c => new SimpleDeclarator(c.identifier().GetText(), Source(c)),
                ScLangParser.ArrayDeclaratorContext c => new ArrayDeclarator(Build(c.noRefDeclarator()), Build(c.expression()), Source(c)),
                ScLangParser.ParenthesizedRefDeclaratorContext c => Build(c.refDeclarator()),
                _ => throw new NotSupportedException(),
            };

        private static StatementBlock? BuildOrNull(ScLangParser.StatementBlockContext? context)
            => context != null ? Build(context) : null;

        private static StatementBlock Build(ScLangParser.StatementBlockContext context)
            => new StatementBlock(context.statement().SelectMany(Build), Source(context));

        private static IEnumerable<Statement> Build(ScLangParser.StatementContext context)
        {
            switch (context)
            {
                case ScLangParser.VariableDeclarationStatementContext c:
                    foreach (var decl in Build(c.declaration()))
                    {
                        yield return new VariableDeclarationStatement(decl, Source(c));
                    }
                    break;
                default:
                    yield return BuildOne(context);
                    break;
            }
        }

        /// <summary>
        /// Builds Statements that have a one-to-one mapping between parse tree and AST.
        /// </summary>
        private static Statement BuildOne(ScLangParser.StatementContext context)
            => context switch
            {
                ScLangParser.AssignmentStatementContext c => new AssignmentStatement(c.op.Type switch
                                                                                     {
                                                                                         ScLangLexer.OP_ASSIGN => null,
                                                                                         ScLangLexer.OP_ASSIGN_ADD => BinaryOperator.Add,
                                                                                         ScLangLexer.OP_ASSIGN_SUBTRACT => BinaryOperator.Subtract,
                                                                                         ScLangLexer.OP_ASSIGN_MULTIPLY => BinaryOperator.Multiply,
                                                                                         ScLangLexer.OP_ASSIGN_DIVIDE => BinaryOperator.Divide,
                                                                                         ScLangLexer.OP_ASSIGN_MODULO => BinaryOperator.Modulo,
                                                                                         ScLangLexer.OP_ASSIGN_OR => BinaryOperator.Or,
                                                                                         ScLangLexer.OP_ASSIGN_AND => BinaryOperator.And,
                                                                                         ScLangLexer.OP_ASSIGN_XOR => BinaryOperator.Xor,
                                                                                         _ => throw new NotImplementedException()
                                                                                     },
                                                                                     Build(c.left),
                                                                                     Build(c.right),
                                                                                     Source(c)),
                ScLangParser.IfStatementContext c => new IfStatement(Build(c.condition),
                                                                     Build(c.thenBlock),
                                                                     BuildOrNull(c.elseBlock),
                                                                     Source(c)),
                ScLangParser.WhileStatementContext c => new WhileStatement(Build(c.condition),
                                                                           Build(c.statementBlock()),
                                                                           Source(c)),
                ScLangParser.RepeatStatementContext c => new RepeatStatement(Build(c.limit),
                                                                             Build(c.counter),
                                                                             Build(c.statementBlock()),
                                                                             Source(c)),
                ScLangParser.SwitchStatementContext c => new SwitchStatement(Build(c.expression()),
                                                                             c.switchCase().Select(Build),
                                                                             Source(c)),
                ScLangParser.ReturnStatementContext c => new ReturnStatement(BuildOrNull(c.expression()),
                                                                             Source(c)),
                ScLangParser.InvocationStatementContext c => new InvocationStatement(Build(c.expression()),
                                                                                     c.argumentList().expression().Select(Build),
                                                                                     Source(c)),
                ScLangParser.StatementContext c => new ErrorStatement(c.GetText(), Source(c)),
                _ => throw new NotSupportedException(),
            };

        private static SwitchCase Build(ScLangParser.SwitchCaseContext context)
            => context switch
            {
                ScLangParser.ValueSwitchCaseContext c => new ValueSwitchCase(Build(c.value), Build(c.statementBlock()), Source(c)),
                ScLangParser.DefaultSwitchCaseContext c => new DefaultSwitchCase(Build(c.statementBlock()), Source(c)),
                _ => throw new NotSupportedException(),
            };

        private static Expression? BuildOrNull(ScLangParser.ExpressionContext? context)
            => context != null ? Build(context) : null;

        private static Expression Build(ScLangParser.ExpressionContext context)
            => context switch
            {
                ScLangParser.ParenthesizedExpressionContext c => Build(c.expression()),
                ScLangParser.UnaryExpressionContext c => new UnaryExpression(c.op.Type switch
                                                                             {
                                                                                 ScLangLexer.K_NOT => UnaryOperator.Not,
                                                                                 ScLangLexer.OP_SUBTRACT => UnaryOperator.Negate,
                                                                                 _ => throw new NotImplementedException()
                                                                             },
                                                                             Build(c.expression()),
                                                                             Source(c)),
                ScLangParser.BinaryExpressionContext c => new BinaryExpression(c.op.Type switch
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
                                                                               Build(c.left),
                                                                               Build(c.right),
                                                                               Source(c)),
                ScLangParser.AggregateExpressionContext c => new AggregateExpression(c.expression().Select(Build), Source(c)),
                ScLangParser.IdentifierExpressionContext c => new IdentifierExpression(c.identifier().GetText(), Source(c)),
                ScLangParser.MemberAccessExpressionContext c => new MemberAccessExpression(Build(c.expression()), 
                                                                                           c.identifier().GetText(),
                                                                                           Source(c)),
                ScLangParser.ArrayAccessExpressionContext c => new ArrayAccessExpression(Build(c.expression()),
                                                                                         Build(c.arrayIndexer().expression()),
                                                                                         Source(c)),
                ScLangParser.InvocationExpressionContext c => new InvocationExpression(Build(c.expression()),
                                                                                       c.argumentList().expression().Select(Build),
                                                                                       Source(c)),
                ScLangParser.LiteralExpressionContext c => new LiteralExpression(c switch
                                                                                 {
                                                                                     _ when c.integer() != null => LiteralKind.Int,
                                                                                     _ when c.@float() != null => LiteralKind.Float,
                                                                                     _ when c.@string() != null => LiteralKind.String,
                                                                                     _ when c.@bool() != null => LiteralKind.Bool,
                                                                                     _ => throw new NotImplementedException()
                                                                                 },
                                                                                 c.GetText(),
                                                                                 Source(c)),
                ScLangParser.ExpressionContext c => new ErrorExpression(c.GetText(), Source(c)),
                _ => throw new NotSupportedException(),
            };
    }
}
