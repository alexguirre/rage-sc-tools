namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Statements;

    /// <summary>
    /// Represents a function or procedure declaration.
    /// </summary>
    public sealed class FuncDeclaration : BaseValueDeclaration
    {
        public FuncProtoDeclaration Prototype { get; set; }
        public IList<IStatement> Body { get; set; } = new List<IStatement>();

        public FuncDeclaration(SourceRange source, string name, FuncProtoDeclaration prototype) : base(source, name, prototype.CreateType(source))
            => Prototype = prototype;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
