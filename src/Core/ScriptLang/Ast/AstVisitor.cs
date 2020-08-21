#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    public abstract class AstVisitor
    {
        public virtual void Visit(Node node) => node.Accept(this);
        public virtual void DefaultVisit(Node node) { }

        public virtual void VisitRoot(Root node) => DefaultVisit(node);

        public virtual void VisitScriptNameStatement(ScriptNameStatement node) => DefaultVisit(node);
        public virtual void VisitProcedureStatement(ProcedureStatement node) => DefaultVisit(node);
        public virtual void VisitFunctionStatement(FunctionStatement node) => DefaultVisit(node);
        public virtual void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node) => DefaultVisit(node);
        public virtual void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node) => DefaultVisit(node);
        public virtual void VisitStructStatement(StructStatement node) => DefaultVisit(node);
        public virtual void VisitStaticVariableStatement(StaticVariableStatement node) => DefaultVisit(node);

        public virtual void VisitStatementBlock(StatementBlock node) => DefaultVisit(node);
        public virtual void VisitVariableDeclarationStatement(VariableDeclarationStatement node) => DefaultVisit(node);
        public virtual void VisitAssignmentStatement(AssignmentStatement node) => DefaultVisit(node);
        public virtual void VisitIfStatement(IfStatement node) => DefaultVisit(node);
        public virtual void VisitWhileStatement(WhileStatement node) => DefaultVisit(node);
        public virtual void VisitReturnStatement(ReturnStatement node) => DefaultVisit(node);
        public virtual void VisitInvocationStatement(InvocationStatement node) => DefaultVisit(node);

        public virtual void VisitUnaryExpression(UnaryExpression node) => DefaultVisit(node);
        public virtual void VisitBinaryExpression(BinaryExpression node) => DefaultVisit(node);
        public virtual void VisitAggregateExpression(AggregateExpression node) => DefaultVisit(node);
        public virtual void VisitIdentifierExpression(IdentifierExpression node) => DefaultVisit(node);
        public virtual void VisitMemberAccessExpression(MemberAccessExpression node) => DefaultVisit(node);
        public virtual void VisitArrayAccessExpression(ArrayAccessExpression node) => DefaultVisit(node);
        public virtual void VisitInvocationExpression(InvocationExpression node) => DefaultVisit(node);
        public virtual void VisitLiteralExpression(LiteralExpression node) => DefaultVisit(node);

        public virtual void VisitType(Type node) => DefaultVisit(node);

        public virtual void VisitIdentifier(Identifier node) => DefaultVisit(node);
        public virtual void VisitArrayIndexer(ArrayIndexer node) => DefaultVisit(node);
        public virtual void VisitVariableDeclaration(VariableDeclaration node) => DefaultVisit(node);
        public virtual void VisitVariableDeclarationWithInitializer(VariableDeclarationWithInitializer node) => DefaultVisit(node);
        public virtual void VisitParameterList(ParameterList node) => DefaultVisit(node);
        public virtual void VisitArgumentList(ArgumentList node) => DefaultVisit(node);
        public virtual void VisitStructFieldList(StructFieldList node) => DefaultVisit(node);
    }

    public abstract class AstVisitor<TResult>
    {
        [return: MaybeNull] public virtual TResult Visit(Node node) => node.Accept(this);
        [return: MaybeNull] public virtual TResult DefaultVisit(Node node) => default;

        [return: MaybeNull] public virtual TResult VisitRoot(Root node) => DefaultVisit(node);
        
        [return: MaybeNull] public virtual TResult VisitScriptNameStatement(ScriptNameStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitProcedureStatement(ProcedureStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitFunctionStatement(FunctionStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitFunctionPrototypeStatement(FunctionPrototypeStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitStructStatement(StructStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitStaticVariableStatement(StaticVariableStatement node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitStatementBlock(StatementBlock node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitVariableDeclarationStatement(VariableDeclarationStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitAssignmentStatement(AssignmentStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitIfStatement(IfStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitWhileStatement(WhileStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitReturnStatement(ReturnStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitInvocationStatement(InvocationStatement node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitUnaryExpression(UnaryExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitBinaryExpression(BinaryExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitAggregateExpression(AggregateExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitIdentifierExpression(IdentifierExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitMemberAccessExpression(MemberAccessExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitArrayAccessExpression(ArrayAccessExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitInvocationExpression(InvocationExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitLiteralExpression(LiteralExpression node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitType(Type node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitIdentifier(Identifier node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitArrayIndexer(ArrayIndexer node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitVariableDeclaration(VariableDeclaration node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitVariableDeclarationWithInitializer(VariableDeclarationWithInitializer node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitParameterList(ParameterList node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitArgumentList(ArgumentList node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitStructFieldList(StructFieldList node) => DefaultVisit(node);
    }

    public static class NodeVisitorExtensions
    {
        public static void Accept(this Node node, AstVisitor visitor)
        {
            switch (node)
            {
                case Root n: visitor.VisitRoot(n); break;

                case ScriptNameStatement n: visitor.VisitScriptNameStatement(n); break;
                case ProcedureStatement n: visitor.VisitProcedureStatement(n); break;
                case FunctionStatement n: visitor.VisitFunctionStatement(n); break;
                case ProcedurePrototypeStatement n: visitor.VisitProcedurePrototypeStatement(n); break;
                case FunctionPrototypeStatement n: visitor.VisitFunctionPrototypeStatement(n); break;
                case StructStatement n: visitor.VisitStructStatement(n); break;
                case StaticVariableStatement n: visitor.VisitStaticVariableStatement(n); break;

                case StatementBlock n: visitor.VisitStatementBlock(n); break;
                case VariableDeclarationStatement n: visitor.VisitVariableDeclarationStatement(n); break;
                case AssignmentStatement n: visitor.VisitAssignmentStatement(n); break;
                case IfStatement n: visitor.VisitIfStatement(n); break;
                case WhileStatement n: visitor.VisitWhileStatement(n); break;
                case ReturnStatement n: visitor.VisitReturnStatement(n); break;
                case InvocationStatement n: visitor.VisitInvocationStatement(n); break;

                case UnaryExpression n: visitor.VisitUnaryExpression(n); break;
                case BinaryExpression n: visitor.VisitBinaryExpression(n); break;
                case AggregateExpression n: visitor.VisitAggregateExpression(n); break;
                case IdentifierExpression n: visitor.VisitIdentifierExpression(n); break;
                case MemberAccessExpression n: visitor.VisitMemberAccessExpression(n); break;
                case ArrayAccessExpression n: visitor.VisitArrayAccessExpression(n); break;
                case InvocationExpression n: visitor.VisitInvocationExpression(n); break;
                case LiteralExpression n: visitor.VisitLiteralExpression(n); break;

                case Type n: visitor.VisitType(n); break;

                case Identifier n: visitor.VisitIdentifier(n); break;
                case ArrayIndexer n: visitor.VisitArrayIndexer(n); break;
                case VariableDeclaration n: visitor.VisitVariableDeclaration(n); break;
                case VariableDeclarationWithInitializer n: visitor.VisitVariableDeclarationWithInitializer(n); break;
                case ParameterList n: visitor.VisitParameterList(n); break;
                case ArgumentList n: visitor.VisitArgumentList(n); break;
                case StructFieldList n: visitor.VisitStructFieldList(n); break;

                default: throw new NotImplementedException();
            }
        }

        [return: MaybeNull]
        public static TResult Accept<TResult>(this Node node, AstVisitor<TResult> visitor) => node switch
        {
            Root n => visitor.VisitRoot(n),

            ScriptNameStatement n => visitor.VisitScriptNameStatement(n),
            ProcedureStatement n => visitor.VisitProcedureStatement(n),
            FunctionStatement n => visitor.VisitFunctionStatement(n),
            ProcedurePrototypeStatement n => visitor.VisitProcedurePrototypeStatement(n),
            FunctionPrototypeStatement n => visitor.VisitFunctionPrototypeStatement(n),
            StructStatement n => visitor.VisitStructStatement(n),
            StaticVariableStatement n => visitor.VisitStaticVariableStatement(n),

            StatementBlock n => visitor.VisitStatementBlock(n),
            VariableDeclarationStatement n => visitor.VisitVariableDeclarationStatement(n),
            AssignmentStatement n => visitor.VisitAssignmentStatement(n),
            IfStatement n => visitor.VisitIfStatement(n),
            WhileStatement n => visitor.VisitWhileStatement(n),
            ReturnStatement n => visitor.VisitReturnStatement(n),
            InvocationStatement n => visitor.VisitInvocationStatement(n),

            UnaryExpression n => visitor.VisitUnaryExpression(n),
            BinaryExpression n => visitor.VisitBinaryExpression(n),
            AggregateExpression n => visitor.VisitAggregateExpression(n),
            IdentifierExpression n => visitor.VisitIdentifierExpression(n),
            MemberAccessExpression n => visitor.VisitMemberAccessExpression(n),
            ArrayAccessExpression n => visitor.VisitArrayAccessExpression(n),
            InvocationExpression n => visitor.VisitInvocationExpression(n),
            LiteralExpression n => visitor.VisitLiteralExpression(n),

            Type n => visitor.VisitType(n),

            Identifier n => visitor.VisitIdentifier(n),
            ArrayIndexer n => visitor.VisitArrayIndexer(n),
            VariableDeclaration n => visitor.VisitVariableDeclaration(n),
            VariableDeclarationWithInitializer n => visitor.VisitVariableDeclarationWithInitializer(n),
            ParameterList n => visitor.VisitParameterList(n),
            ArgumentList n => visitor.VisitArgumentList(n),
            StructFieldList n => visitor.VisitStructFieldList(n),

            _ => throw new NotImplementedException()
        };
    }
}
