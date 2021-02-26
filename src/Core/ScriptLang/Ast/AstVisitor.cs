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
        public virtual void VisitScriptHashStatement(ScriptHashStatement node) => DefaultVisit(node);
        public virtual void VisitUsingStatement(UsingStatement node) => DefaultVisit(node);
        public virtual void VisitProcedureStatement(ProcedureStatement node) => DefaultVisit(node);
        public virtual void VisitFunctionStatement(FunctionStatement node) => DefaultVisit(node);
        public virtual void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node) => DefaultVisit(node);
        public virtual void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node) => DefaultVisit(node);
        public virtual void VisitProcedureNativeStatement(ProcedureNativeStatement node) => DefaultVisit(node);
        public virtual void VisitFunctionNativeStatement(FunctionNativeStatement node) => DefaultVisit(node);
        public virtual void VisitStructStatement(StructStatement node) => DefaultVisit(node);
        public virtual void VisitStaticVariableStatement(StaticVariableStatement node) => DefaultVisit(node);
        public virtual void VisitConstantVariableStatement(ConstantVariableStatement node) => DefaultVisit(node);

        public virtual void VisitStatementBlock(StatementBlock node) => DefaultVisit(node);
        public virtual void VisitErrorStatement(ErrorStatement node) => DefaultVisit(node);
        public virtual void VisitVariableDeclarationStatement(VariableDeclarationStatement node) => DefaultVisit(node);
        public virtual void VisitAssignmentStatement(AssignmentStatement node) => DefaultVisit(node);
        public virtual void VisitIfStatement(IfStatement node) => DefaultVisit(node);
        public virtual void VisitWhileStatement(WhileStatement node) => DefaultVisit(node);
        public virtual void VisitRepeatStatement(RepeatStatement node) => DefaultVisit(node);
        public virtual void VisitSwitchStatement(SwitchStatement node) => DefaultVisit(node);
        public virtual void VisitValueSwitchCase(ValueSwitchCase node) => DefaultVisit(node);
        public virtual void VisitDefaultSwitchCase(DefaultSwitchCase node) => DefaultVisit(node);
        public virtual void VisitReturnStatement(ReturnStatement node) => DefaultVisit(node);
        public virtual void VisitInvocationStatement(InvocationStatement node) => DefaultVisit(node);

        public virtual void VisitErrorExpression(ErrorExpression node) => DefaultVisit(node);
        public virtual void VisitUnaryExpression(UnaryExpression node) => DefaultVisit(node);
        public virtual void VisitBinaryExpression(BinaryExpression node) => DefaultVisit(node);
        public virtual void VisitAggregateExpression(AggregateExpression node) => DefaultVisit(node);
        public virtual void VisitIdentifierExpression(IdentifierExpression node) => DefaultVisit(node);
        public virtual void VisitMemberAccessExpression(MemberAccessExpression node) => DefaultVisit(node);
        public virtual void VisitArrayAccessExpression(ArrayAccessExpression node) => DefaultVisit(node);
        public virtual void VisitInvocationExpression(InvocationExpression node) => DefaultVisit(node);
        public virtual void VisitLiteralExpression(LiteralExpression node) => DefaultVisit(node);

        public virtual void VisitRefDeclarator(RefDeclarator node) => DefaultVisit(node);
        public virtual void VisitSimpleDeclarator(SimpleDeclarator node) => DefaultVisit(node);
        public virtual void VisitArrayDeclarator(ArrayDeclarator node) => DefaultVisit(node);

        public virtual void VisitDeclaration(Declaration node) => DefaultVisit(node);
    }

    public abstract class AstVisitor<TResult>
    {
        [return: MaybeNull] public virtual TResult Visit(Node node) => node.Accept(this);
        [return: MaybeNull] public virtual TResult DefaultVisit(Node node) => default;

        [return: MaybeNull] public virtual TResult VisitRoot(Root node) => DefaultVisit(node);
        
        [return: MaybeNull] public virtual TResult VisitScriptNameStatement(ScriptNameStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitScriptHashStatement(ScriptHashStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitUsingStatement(UsingStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitProcedureStatement(ProcedureStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitFunctionStatement(FunctionStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitFunctionPrototypeStatement(FunctionPrototypeStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitProcedureNativeStatement(ProcedureNativeStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitFunctionNativeStatement(FunctionNativeStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitStructStatement(StructStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitStaticVariableStatement(StaticVariableStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitConstantVariableStatement(ConstantVariableStatement node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitStatementBlock(StatementBlock node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitErrorStatement(ErrorStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitVariableDeclarationStatement(VariableDeclarationStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitAssignmentStatement(AssignmentStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitIfStatement(IfStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitWhileStatement(WhileStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitRepeatStatement(RepeatStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitSwitchStatement(SwitchStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitValueSwitchCase(ValueSwitchCase node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitDefaultSwitchCase(DefaultSwitchCase node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitReturnStatement(ReturnStatement node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitInvocationStatement(InvocationStatement node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitErrorExpression(ErrorExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitUnaryExpression(UnaryExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitBinaryExpression(BinaryExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitAggregateExpression(AggregateExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitIdentifierExpression(IdentifierExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitMemberAccessExpression(MemberAccessExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitArrayAccessExpression(ArrayAccessExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitInvocationExpression(InvocationExpression node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitLiteralExpression(LiteralExpression node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitRefDeclarator(RefDeclarator node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitSimpleDeclarator(SimpleDeclarator node) => DefaultVisit(node);
        [return: MaybeNull] public virtual TResult VisitArrayDeclarator(ArrayDeclarator node) => DefaultVisit(node);

        [return: MaybeNull] public virtual TResult VisitDeclaration(Declaration node) => DefaultVisit(node);
    }
}
