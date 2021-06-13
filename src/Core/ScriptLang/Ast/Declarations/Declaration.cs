namespace ScTools.ScriptLang.Ast.Declarations
{
    public interface IDeclaration
    {
        string Name { get; }
    }

    public abstract class BaseDeclaration : BaseNode, IDeclaration
    {
        public string Name { get; }

        public BaseDeclaration(SourceRange source, string name) : base(source)
            => Name = name;
    }
}
