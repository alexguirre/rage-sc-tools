namespace ScTools.ScriptLang.Ast.Declarations
{
    using ScTools.ScriptLang.Ast.Types;

    using System.Collections.Generic;
    using System.Diagnostics;

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


    /// <summary>
    /// Represents a symbol declaration.
    /// </summary>
    public interface IDeclaration_New : INode
    {
        string Name { get; }
    }

    /// <summary>
    /// Represents a declaration of a type.
    /// </summary>
    public interface ITypeDeclaration_New : IDeclaration_New
    {
    }

    /// <summary>
    /// Represents a declaration of a variable, a procedure, a function or an enum member.
    /// </summary>
    public interface IValueDeclaration_New : IDeclaration_New
    {
    }

    public abstract class BaseTypeDeclaration_New : BaseNode, ITypeDeclaration_New
    {
        public string Name => Tokens[0].Lexeme.ToString();

        public BaseTypeDeclaration_New(Token nameIdentifierToken)
            : base(OfTokens(nameIdentifierToken), OfChildren())
        {
            Debug.Assert(nameIdentifierToken.Kind is TokenKind.Identifier);
        }
    }

    public abstract class BaseValueDeclaration_New : BaseNode, IValueDeclaration_New
    {
        public abstract string Name { get; }

        public BaseValueDeclaration_New(IEnumerable<Token> tokens, IEnumerable<INode> children) : base(tokens, children)
        {
        }
    }
}
