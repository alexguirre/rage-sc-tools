namespace ScTools.ScriptLang.Ast
{
    using ScTools.ScriptLang.Ast.Directives;

    public interface IVisitor<TReturn, TParam>
    {
        TReturn Visit(Program node, TParam param);

        TReturn Visit(ScriptHashDirective node, TParam param);
        TReturn Visit(ScriptNameDirective node, TParam param);
        TReturn Visit(UsingDirective node, TParam param);
    }
}
