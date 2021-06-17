namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Types;


    /// <summary>
    /// Represents a function or procedure prototype declaration.
    /// </summary>
    public sealed class FuncProtoDeclaration : BaseTypeDeclaration
    {
        public IType? ReturnType { get; set; } = null;
        public IList<FuncParameter> Parameters { get; set; } = new List<FuncParameter>();

        public bool IsProc => ReturnType is null;

        public FuncProtoDeclaration(SourceRange source, string name) : base(source, name) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }

    public sealed class FuncParameter : BaseNode
    {
        public string Name { get; set; }
        public IType Type { get; set; }

        public FuncParameter(SourceRange source, string name, IType type) : base(source)
            => (Name, Type) = (name, type);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
