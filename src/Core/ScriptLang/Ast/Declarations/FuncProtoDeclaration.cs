namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Types;

    /// <summary>
    /// Represents a function or procedure prototype declaration.
    /// </summary>
    public sealed class FuncProtoDeclaration : BaseTypeDeclaration
    {
        public IType ReturnType { get; set; }
        public IList<VarDeclaration> Parameters { get; set; } = new List<VarDeclaration>();

        public bool IsProc => ReturnType is VoidType;

        public FuncProtoDeclaration(SourceRange source, string name, IType returnType) : base(source, name)
            => ReturnType = returnType;

        public override FuncType CreateType(SourceRange source) => new(source, this);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
