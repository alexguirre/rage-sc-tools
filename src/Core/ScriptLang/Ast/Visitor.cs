namespace ScTools.ScriptLang.Ast
{
    using System;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;

    public interface IVisitor
    {
        void Visit(CompilationUnit node);

        void Visit(UsingDirective node);

        void Visit(EnumDeclaration node);
        void Visit(EnumMemberDeclaration node);
        void Visit(FunctionDeclaration node);
        void Visit(FunctionPointerDeclaration node);
        void Visit(NativeFunctionDeclaration node);
        void Visit(ScriptDeclaration node);
        void Visit(GlobalBlockDeclaration node);
        void Visit(StructDeclaration node);
        void Visit(VarDeclaration node);
        void Visit(VarDeclarator node);
        void Visit(VarRefDeclarator node);
        void Visit(VarArrayDeclarator node);

        void Visit(BinaryExpression node);
        void Visit(BoolLiteralExpression node);
        void Visit(FieldAccessExpression node);
        void Visit(FloatLiteralExpression node);
        void Visit(IndexingExpression node);
        void Visit(IntLiteralExpression node);
        void Visit(InvocationExpression node);
        void Visit(NullExpression node);
        void Visit(StringLiteralExpression node);
        void Visit(UnaryExpression node);
        void Visit(NameExpression node);
        void Visit(VectorExpression node);

        void Visit(Label node);
        void Visit(AssignmentStatement node);
        void Visit(BreakStatement node);
        void Visit(ContinueStatement node);
        void Visit(EmptyStatement node);
        void Visit(GotoStatement node);
        void Visit(IfStatement node);
        void Visit(RepeatStatement node);
        void Visit(ReturnStatement node);
        void Visit(SwitchStatement node);
        void Visit(ValueSwitchCase node);
        void Visit(DefaultSwitchCase node);
        void Visit(WhileStatement node);

        void Visit(TypeName node);

        void Visit(ErrorDeclaration node);
        void Visit(ErrorExpression node);
        void Visit(ErrorStatement node);
    }

    public interface IVisitor<TReturn, TParam>
    {
        TReturn Visit(CompilationUnit node, TParam param);

        TReturn Visit(UsingDirective node, TParam param);

        TReturn Visit(EnumDeclaration node, TParam param);
        TReturn Visit(EnumMemberDeclaration node, TParam param);
        TReturn Visit(FunctionDeclaration node, TParam param);
        TReturn Visit(FunctionPointerDeclaration node, TParam param);
        TReturn Visit(NativeFunctionDeclaration node, TParam param);
        TReturn Visit(ScriptDeclaration node, TParam param);
        TReturn Visit(GlobalBlockDeclaration node, TParam param);
        TReturn Visit(StructDeclaration node, TParam param);
        TReturn Visit(VarDeclaration node, TParam param);
        TReturn Visit(VarDeclarator node, TParam param);
        TReturn Visit(VarRefDeclarator node, TParam param);
        TReturn Visit(VarArrayDeclarator node, TParam param);

        TReturn Visit(BinaryExpression node, TParam param);
        TReturn Visit(BoolLiteralExpression node, TParam param);
        TReturn Visit(FieldAccessExpression node, TParam param);
        TReturn Visit(FloatLiteralExpression node, TParam param);
        TReturn Visit(IndexingExpression node, TParam param);
        TReturn Visit(IntLiteralExpression node, TParam param);
        TReturn Visit(InvocationExpression node, TParam param);
        TReturn Visit(NullExpression node, TParam param);
        TReturn Visit(StringLiteralExpression node, TParam param);
        TReturn Visit(UnaryExpression node, TParam param);
        TReturn Visit(NameExpression node, TParam param);
        TReturn Visit(VectorExpression node, TParam param);

        TReturn Visit(Label node, TParam param);
        TReturn Visit(AssignmentStatement node, TParam param);
        TReturn Visit(BreakStatement node, TParam param);
        TReturn Visit(ContinueStatement node, TParam param);
        TReturn Visit(EmptyStatement node, TParam param);
        TReturn Visit(GotoStatement node, TParam param);
        TReturn Visit(IfStatement node, TParam param);
        TReturn Visit(RepeatStatement node, TParam param);
        TReturn Visit(ReturnStatement node, TParam param);
        TReturn Visit(SwitchStatement node, TParam param);
        TReturn Visit(ValueSwitchCase node, TParam param);
        TReturn Visit(DefaultSwitchCase node, TParam param);
        TReturn Visit(WhileStatement node, TParam param);

        TReturn Visit(TypeName node, TParam param);

        TReturn Visit(ErrorDeclaration node, TParam param);
        TReturn Visit(ErrorExpression node, TParam param);
        TReturn Visit(ErrorStatement node, TParam param);
    }

    ///// <summary>
    ///// Default implementation of <see cref="IVisitor{TReturn, TParam}"/> with depth-first search traversal.
    ///// </summary>
    //public abstract class DFSVisitor<TReturn, TParam> : IVisitor<TReturn, TParam>
    //{
    //    public abstract TReturn DefaultReturn { get; }

    //    public virtual TReturn Visit(CompilationUnit node, TParam param)
    //    {
    //        node.Usings.ForEach(@using => @using.Accept(this, param));
    //        node.Declarations.ForEach(decl => decl.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(UsingDirective node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(EnumDeclaration node, TParam param)
    //    {
    //        node.Members.ForEach(m => m.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(EnumMemberDeclaration node, TParam param)
    //    {
    //        node.Initializer?.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(FunctionDeclaration node, TParam param)
    //    {
    //        node.ReturnType?.Accept(this, param);
    //        node.Parameters.ForEach(p => p.Accept(this, param));
    //        node.Body.ForEach(stmt => stmt.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(FunctionPointerDeclaration node, TParam param)
    //    {
    //        node.ReturnType?.Accept(this, param);
    //        node.Parameters.ForEach(p => p.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(NativeFunctionDeclaration node, TParam param)
    //    {
    //        node.ReturnType?.Accept(this, param);
    //        node.Parameters.ForEach(p => p.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(ScriptDeclaration node, TParam param)
    //    {
    //        node.Parameters.ForEach(p => p.Accept(this, param));
    //        node.Body.ForEach(stmt => stmt.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(GlobalBlockDeclaration node, TParam param)
    //    {
    //        node.Vars.ForEach(v => v.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(StructDeclaration node, TParam param)
    //    {
    //        node.Fields.ForEach(f => f.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(VarDeclaration node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.Type.Accept(this, param);
    //        node.Declarator.Accept(this, param);
    //        node.Initializer?.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(VarDeclarator node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(VarRefDeclarator node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(VarArrayDeclarator node, TParam param)
    //    {
    //        node.Lengths.ForEach(l => l?.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(BinaryExpression node, TParam param)
    //    {
    //        node.LHS.Accept(this, param);
    //        node.RHS.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(BoolLiteralExpression node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(FieldAccessExpression node, TParam param)
    //    {
    //        node.SubExpression.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(FloatLiteralExpression node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(IndexingExpression node, TParam param)
    //    {
    //        node.Array.Accept(this, param);
    //        node.Index.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(IntLiteralExpression node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(InvocationExpression node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.Callee.Accept(this, param);
    //        node.Arguments.ForEach(arg => arg.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(NullExpression node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(StringLiteralExpression node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(UnaryExpression node, TParam param)
    //    {
    //        node.SubExpression.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(NameExpression node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(VectorExpression node, TParam param)
    //    {
    //        node.X.Accept(this, param);
    //        node.Y.Accept(this, param);
    //        node.Z.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(Label node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(AssignmentStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.LHS.Accept(this, param);
    //        node.RHS.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(BreakStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(ContinueStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(EmptyStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(GotoStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(IfStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.Condition.Accept(this, param);
    //        node.Then.ForEach(stmt => stmt.Accept(this, param));
    //        node.Else.ForEach(stmt => stmt.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(RepeatStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.Limit.Accept(this, param);
    //        node.Counter.Accept(this, param);
    //        node.Body.ForEach(stmt => stmt.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(ReturnStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.Expression?.Accept(this, param);
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(SwitchStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.Expression.Accept(this, param);
    //        node.Cases.ForEach(c => c.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(ValueSwitchCase node, TParam param)
    //    {
    //        node.Value.Accept(this, param);
    //        node.Body.ForEach(stmt => stmt.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(DefaultSwitchCase node, TParam param)
    //    {
    //        node.Body.ForEach(stmt => stmt.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(WhileStatement node, TParam param)
    //    {
    //        node.Label?.Accept(this, param);
    //        node.Condition.Accept(this, param);
    //        node.Body.ForEach(stmt => stmt.Accept(this, param));
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(TypeName node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(ErrorDeclaration node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(ErrorExpression node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }

    //    public virtual TReturn Visit(ErrorStatement node, TParam param)
    //    {
    //        return DefaultReturn;
    //    }
    //}

    ///// <summary>
    ///// Default implementation of <see cref="IVisitor{TReturn, TParam}"/> with depth-first search traversal and no return or parameter.
    ///// </summary>
    //public abstract class DFSVisitor : DFSVisitor<Void, Void>
    //{
    //    public override Void DefaultReturn => default;
    //}

    /// <summary>
    /// Default implementation of <see cref="IVisitor{TReturn, TParam}"/> where all visit methods throw <see cref="NotImplementedException"/>.
    /// </summary>
    public abstract class EmptyVisitor<TReturn, TParam> : IVisitor<TReturn, TParam>
    {
        protected virtual TReturn DefaultVisit(INode node, TParam param) => throw new NotImplementedException();

        public virtual TReturn Visit(CompilationUnit node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(UsingDirective node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(EnumDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(EnumMemberDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(FunctionDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(FunctionPointerDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(NativeFunctionDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(ScriptDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(GlobalBlockDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(StructDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(VarDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(VarDeclarator node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(VarRefDeclarator node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(VarArrayDeclarator node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(BinaryExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(BoolLiteralExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(FieldAccessExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(FloatLiteralExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(IndexingExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(IntLiteralExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(InvocationExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(NullExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(StringLiteralExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(UnaryExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(NameExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(VectorExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(Label node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(AssignmentStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(BreakStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(ContinueStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(EmptyStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(GotoStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(IfStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(RepeatStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(ReturnStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(SwitchStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(ValueSwitchCase node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(DefaultSwitchCase node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(WhileStatement node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(TypeName node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(ErrorDeclaration node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(ErrorExpression node, TParam param) => DefaultVisit(node, param);
        public virtual TReturn Visit(ErrorStatement node, TParam param) => DefaultVisit(node, param);
    }

    /// <summary>
    /// Default implementation of <see cref="IVisitor"/> where all visit methods throw <see cref="NotImplementedException"/>.
    /// </summary>
    public abstract class Visitor : IVisitor
    {
        protected virtual void DefaultVisit(INode node) => throw new NotImplementedException();

        public virtual void Visit(CompilationUnit node) => DefaultVisit(node);
        public virtual void Visit(UsingDirective node) => DefaultVisit(node);
        public virtual void Visit(EnumDeclaration node) => DefaultVisit(node);
        public virtual void Visit(EnumMemberDeclaration node) => DefaultVisit(node);
        public virtual void Visit(FunctionDeclaration node) => DefaultVisit(node);
        public virtual void Visit(FunctionPointerDeclaration node) => DefaultVisit(node);
        public virtual void Visit(NativeFunctionDeclaration node) => DefaultVisit(node);
        public virtual void Visit(ScriptDeclaration node) => DefaultVisit(node);
        public virtual void Visit(GlobalBlockDeclaration node) => DefaultVisit(node);
        public virtual void Visit(StructDeclaration node) => DefaultVisit(node);
        public virtual void Visit(VarDeclaration node) => DefaultVisit(node);
        public virtual void Visit(VarDeclarator node) => DefaultVisit(node);
        public virtual void Visit(VarRefDeclarator node) => DefaultVisit(node);
        public virtual void Visit(VarArrayDeclarator node) => DefaultVisit(node);
        public virtual void Visit(BinaryExpression node) => DefaultVisit(node);
        public virtual void Visit(BoolLiteralExpression node) => DefaultVisit(node);
        public virtual void Visit(FieldAccessExpression node) => DefaultVisit(node);
        public virtual void Visit(FloatLiteralExpression node) => DefaultVisit(node);
        public virtual void Visit(IndexingExpression node) => DefaultVisit(node);
        public virtual void Visit(IntLiteralExpression node) => DefaultVisit(node);
        public virtual void Visit(InvocationExpression node) => DefaultVisit(node);
        public virtual void Visit(NullExpression node) => DefaultVisit(node);
        public virtual void Visit(StringLiteralExpression node) => DefaultVisit(node);
        public virtual void Visit(UnaryExpression node) => DefaultVisit(node);
        public virtual void Visit(NameExpression node) => DefaultVisit(node);
        public virtual void Visit(VectorExpression node) => DefaultVisit(node);
        public virtual void Visit(Label node) => DefaultVisit(node);
        public virtual void Visit(AssignmentStatement node) => DefaultVisit(node);
        public virtual void Visit(BreakStatement node) => DefaultVisit(node);
        public virtual void Visit(ContinueStatement node) => DefaultVisit(node);
        public virtual void Visit(EmptyStatement node) => DefaultVisit(node);
        public virtual void Visit(GotoStatement node) => DefaultVisit(node);
        public virtual void Visit(IfStatement node) => DefaultVisit(node);
        public virtual void Visit(RepeatStatement node) => DefaultVisit(node);
        public virtual void Visit(ReturnStatement node) => DefaultVisit(node);
        public virtual void Visit(SwitchStatement node) => DefaultVisit(node);
        public virtual void Visit(ValueSwitchCase node) => DefaultVisit(node);
        public virtual void Visit(DefaultSwitchCase node) => DefaultVisit(node);
        public virtual void Visit(WhileStatement node) => DefaultVisit(node);
        public virtual void Visit(TypeName node) => DefaultVisit(node);
        public virtual void Visit(ErrorDeclaration node) => DefaultVisit(node);
        public virtual void Visit(ErrorExpression node) => DefaultVisit(node);
        public virtual void Visit(ErrorStatement node) => DefaultVisit(node);
    }
}
