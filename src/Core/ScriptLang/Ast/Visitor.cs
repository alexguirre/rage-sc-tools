namespace ScTools.ScriptLang.Ast
{
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;

    public interface IVisitor<TReturn, TParam>
    {
        TReturn Visit(Program node, TParam param);

        TReturn Visit(EnumDeclaration node, TParam param);
        TReturn Visit(EnumMemberDeclaration node, TParam param);
        TReturn Visit(FuncDeclaration node, TParam param);
        TReturn Visit(FuncProtoDeclaration node, TParam param);
        TReturn Visit(LabelDeclaration node, TParam param);
        TReturn Visit(VarDeclaration node, TParam param);

        TReturn Visit(IfStatement node, TParam param);

        TReturn Visit(EnumType node, TParam param);
        TReturn Visit(FuncType node, TParam param);
        TReturn Visit(FuncTypeParameter node, TParam param);
    }
}
