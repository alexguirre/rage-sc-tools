namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;

    public enum FuncKind
    {
        /// <summary>
        /// A function defined in the script source code.
        /// </summary>
        UserDefined,
        /// <summary>
        /// A function defined in the script source code that references a game native function.
        /// With this kind, <see cref="FuncDeclaration.Body"/> is empty.
        /// </summary>
        Native,
        /// <summary>
        /// A function defined by the compiler.
        /// With this kind, <see cref="FuncDeclaration.Body"/> is empty.
        /// </summary>
        Intrinsic,
    }

    /// <summary>
    /// Represents a function or procedure declaration.
    /// </summary>
    public sealed class FuncDeclaration : BaseValueDeclaration
    {
        public FuncKind Kind { get; set; }
        public FuncProtoDeclaration Prototype { get; set; }
        public IList<IStatement> Body { get; set; } = new List<IStatement>();

        public FuncDeclaration(SourceRange source, string name, FuncKind kind, FuncProtoDeclaration prototype) : base(source, name)
            => (Kind, Prototype, Type) = (kind, prototype, new FuncType(source, Prototype));

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
