namespace ScTools.ScriptLang.Ast
{
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    public interface IVisitor<TReturn, TParam>
    {
        TReturn Visit(Program node, TParam param);

        TReturn Visit(EnumDeclaration node, TParam param);
        TReturn Visit(EnumMemberDeclaration node, TParam param);

        TReturn Visit(EnumType node, TParam param);
    }
}
