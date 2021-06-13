namespace ScTools.ScriptLang.Ast.Directives
{
    public interface IDirective
    {
    }

    public abstract class BaseDirective : BaseNode, IDirective
    {
        public BaseDirective(SourceRange source) : base(source) {}
    }
}
