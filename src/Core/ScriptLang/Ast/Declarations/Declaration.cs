namespace ScTools.ScriptLang.Ast.Declarations
{
    using ScTools.ScriptLang.Ast.Types;

    /// <summary>
    /// Represents a symbol declaration.
    /// </summary>
    public interface IDeclaration : INode
    {
        string Name { get; set; }
    }

    /// <summary>
    /// Represents a declaration of a type.
    /// </summary>
    public interface ITypeDeclaration : IDeclaration
    {
        IType CreateType(SourceRange source);
    }

    /// <summary>
    /// Represents a declaration of a variable, a procedure, a function or an enum member.
    /// </summary>
    public interface IValueDeclaration : IDeclaration
    {
        IType Type { get; set; }
    }

    public abstract class BaseTypeDeclaration : BaseNode, ITypeDeclaration
    {
        public string Name { get; set; }

        public BaseTypeDeclaration(SourceRange source, string name) : base(source)
            => Name = name;

        public abstract IType CreateType(SourceRange source);
    }

    public abstract class BaseValueDeclaration : BaseNode, IValueDeclaration
    {
        public string Name { get; set; }
        public IType Type { get; set; }

        public BaseValueDeclaration(string name, IType type, params Token[] tokens) : base(OfTokens(tokens), OfChildren())
            => (Name, Type) = (name, type);
        public BaseValueDeclaration(SourceRange source, string name, IType type) : base(source)
            => (Name, Type) = (name, type);
    }
}
