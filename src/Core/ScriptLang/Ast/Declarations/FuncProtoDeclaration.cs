namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Types;

    public enum FuncKind
    {
        /// <summary>
        /// A entrypoint function. It cannot be invoked anywhere in the script and its parameters are <see cref="VarKind.ScriptParameter"/>s.
        /// </summary>
        Script,
        /// <summary>
        /// A function defined in the script source code.
        /// </summary>
        UserDefined,
        /// <summary>
        /// A function defined in the script source code that references a game native function.
        /// With this kind, <see cref="FuncDeclaration.Body"/> is empty.
        /// </summary>
        Native,
    }

    /// <summary>
    /// Represents a declaration of function or procedure type.
    /// </summary>
    public sealed class FuncProtoDeclaration : BaseTypeDeclaration
    {
        public FuncKind Kind { get; set; }
        public IType ReturnType { get; set; }
        public IList<VarDeclaration> Parameters { get; set; } = new List<VarDeclaration>();
        public int ParametersSize { get; set; }

        public bool IsProc => ReturnType is VoidType;

        public FuncProtoDeclaration(SourceRange source, string name, FuncKind kind, IType returnType) : base(source, name)
            => (Kind, ReturnType) = (kind, returnType);

        public override FuncType CreateType(SourceRange source) => new(source, this);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
