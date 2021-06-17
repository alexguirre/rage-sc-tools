namespace ScTools.ScriptLang.Ast
{
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;

    public interface IVisitor<TReturn, TParam>
    {
        TReturn Visit(Program node, TParam param);

        TReturn Visit(EnumDeclaration node, TParam param);
        TReturn Visit(EnumMemberDeclaration node, TParam param);
        TReturn Visit(FuncDeclaration node, TParam param);
        TReturn Visit(FuncProtoDeclaration node, TParam param);
        TReturn Visit(GlobalBlockDeclaration node, TParam param);
        TReturn Visit(LabelDeclaration node, TParam param);
        TReturn Visit(StructDeclaration node, TParam param);
        TReturn Visit(StructField node, TParam param);
        TReturn Visit(VarDeclaration node, TParam param);

        TReturn Visit(BinaryExpression node, TParam param);
        TReturn Visit(BoolLiteralExpression node, TParam param);
        TReturn Visit(FieldAccessExpression node, TParam param);
        TReturn Visit(FloatLiteralExpression node, TParam param);
        TReturn Visit(IndexingExpression node, TParam param);
        TReturn Visit(IntLiteralExpression node, TParam param);
        TReturn Visit(InvocationExpression node, TParam param);
        TReturn Visit(NullExpression node, TParam param);
        TReturn Visit(SizeOfExpression node, TParam param);
        TReturn Visit(StringLiteralExpression node, TParam param);
        TReturn Visit(UnaryExpression node, TParam param);
        TReturn Visit(ValueDeclRefExpression node, TParam param);
        TReturn Visit(VectorExpression node, TParam param);

        TReturn Visit(AssignmentStatement node, TParam param);
        TReturn Visit(BreakStatement node, TParam param);
        TReturn Visit(GotoStatement node, TParam param);
        TReturn Visit(IfStatement node, TParam param);
        TReturn Visit(RepeatStatement node, TParam param);
        TReturn Visit(ReturnStatement node, TParam param);
        TReturn Visit(SwitchStatement node, TParam param);
        TReturn Visit(ValueSwitchCase node, TParam param);
        TReturn Visit(DefaultSwitchCase node, TParam param);
        TReturn Visit(WhileStatement node, TParam param);

        TReturn Visit(AnyType node, TParam param);
        TReturn Visit(ArrayRefType node, TParam param);
        TReturn Visit(ArrayType node, TParam param);
        TReturn Visit(BoolType node, TParam param);
        TReturn Visit(EnumType node, TParam param);
        TReturn Visit(FloatType node, TParam param);
        TReturn Visit(FuncType node, TParam param);
        TReturn Visit(IntType node, TParam param);
        TReturn Visit(NamedType node, TParam param);
        TReturn Visit(NullType node, TParam param);
        TReturn Visit(RefType node, TParam param);
        TReturn Visit(StringType node, TParam param);
        TReturn Visit(StructType node, TParam param);
        TReturn Visit(TextLabelType node, TParam param);
    }

    /// <summary>
    /// Default implementation of <see cref="IVisitor{TReturn, TParam}"/> with depth-first search traversal.
    /// </summary>
    public abstract class DFSVisitor<TReturn, TParam> : IVisitor<TReturn, TParam>
    {
        public abstract TReturn DefaultReturn { get; }

        public virtual TReturn Visit(Program node, TParam param)
        {
            node.Declarations.ForEach(decl => decl.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(EnumDeclaration node, TParam param)
        {
            node.Members.ForEach(m => m.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(EnumMemberDeclaration node, TParam param)
        {
            node.Type.Accept(this, param);
            node.Initializer?.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(FuncDeclaration node, TParam param)
        {
            node.Prototype.Accept(this, param);
            node.Type.Accept(this, param);
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(FuncProtoDeclaration node, TParam param)
        {
            node.ReturnType?.Accept(this, param);
            node.Parameters.ForEach(p => p.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(GlobalBlockDeclaration node, TParam param)
        {
            node.Vars.ForEach(v => v.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(LabelDeclaration node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(StructDeclaration node, TParam param)
        {
            node.Fields.ForEach(f => f.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(StructField node, TParam param)
        {
            node.Type.Accept(this, param);
            node.Initializer?.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(VarDeclaration node, TParam param)
        {
            node.Type.Accept(this, param);
            node.Initializer?.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(BinaryExpression node, TParam param)
        {
            node.LHS.Accept(this, param);
            node.RHS.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(BoolLiteralExpression node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(FieldAccessExpression node, TParam param)
        {
            node.SubExpression.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(FloatLiteralExpression node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(IndexingExpression node, TParam param)
        {
            node.Array.Accept(this, param);
            node.Index.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(IntLiteralExpression node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(InvocationExpression node, TParam param)
        {
            node.Callee.Accept(this, param);
            node.Arguments.ForEach(arg => arg.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(NullExpression node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(SizeOfExpression node, TParam param)
        {
            node.SubExpression.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(StringLiteralExpression node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(UnaryExpression node, TParam param)
        {
            node.SubExpression.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(ValueDeclRefExpression node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(VectorExpression node, TParam param)
        {
            node.X.Accept(this, param);
            node.Y.Accept(this, param);
            node.Z.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(AssignmentStatement node, TParam param)
        {
            node.LHS.Accept(this, param);
            node.RHS.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(BreakStatement node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(GotoStatement node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(IfStatement node, TParam param)
        {
            node.Condition.Accept(this, param);
            node.Then.ForEach(stmt => stmt.Accept(this, param));
            node.Else.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(RepeatStatement node, TParam param)
        {
            node.Limit.Accept(this, param);
            node.Counter.Accept(this, param);
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(ReturnStatement node, TParam param)
        {
            node.Expression?.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(SwitchStatement node, TParam param)
        {
            node.Expression.Accept(this, param);
            node.Cases.ForEach(c => c.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(ValueSwitchCase node, TParam param)
        {
            node.Value.Accept(this, param);
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(DefaultSwitchCase node, TParam param)
        {
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(WhileStatement node, TParam param)
        {
            node.Condition.Accept(this, param);
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public virtual TReturn Visit(AnyType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(ArrayRefType node, TParam param)
        {
            node.ItemType.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(ArrayType node, TParam param)
        {
            node.ItemType.Accept(this, param);
            node.LengthExpression.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(BoolType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(EnumType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(FloatType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(FuncType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(IntType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(NamedType node, TParam param)
        {
            node.ResolvedType?.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(NullType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(RefType node, TParam param)
        {
            node.PointeeType.Accept(this, param);
            return DefaultReturn;
        }

        public virtual TReturn Visit(StringType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(StructType node, TParam param)
        {
            return DefaultReturn;
        }

        public virtual TReturn Visit(TextLabelType node, TParam param)
        {
            return DefaultReturn;
        }
    }

    /// <summary>
    /// Default implementation of <see cref="IVisitor{TReturn, TParam}"/> with depth-first search traversal and no return or parameter.
    /// </summary>
    public abstract class DFSVisitor : DFSVisitor<Void, Void>
    {
        public override Void DefaultReturn => default;
    }

    /// <summary>
    /// Empty struct used by <see cref="IVisitor{TReturn, TParam}"/> implementations that do not require <c>TReturn</c> or <c>TParam</c>.
    /// </summary>
    public struct Void { }
}
