namespace ScTools.ScriptLang.Ast
{
    public interface INode
    {
        SourceRange Source { get; set; }

        TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);
    }

    public abstract class BaseNode : INode
    {
        public SourceRange Source { get; set; }

        public BaseNode(SourceRange source)
            => Source = source;

        public abstract TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param);
    }
}
