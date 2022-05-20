namespace ScTools.ScriptLang.Ast.Statements;

public sealed partial class EmptyStatement : BaseStatement
{
    public EmptyStatement(Label? label) : base(OfTokens(), OfChildren(), label) { }
}
