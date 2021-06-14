namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    public interface IType : INode
    {
        int SizeOf { get; }

        public static readonly IType Unknown = new UnknownType();

        private sealed class UnknownType : IType
        {
            public SourceRange Source { get => SourceRange.Unknown; set => throw new NotSupportedException(); }

            public TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
                => throw new NotSupportedException();
        }
    }

    public abstract class BaseType: BaseNode, IType
    {
        public abstract int SizeOf { get; }

        public BaseType(SourceRange source) : base(source) {}
    }
}
