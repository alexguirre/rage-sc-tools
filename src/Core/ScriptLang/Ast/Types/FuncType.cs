namespace ScTools.ScriptLang.Ast.Types
{
    using System.Collections.Generic;

    public sealed class FuncType : BaseType
    {
        public override int SizeOf => 1;
        public IType? ReturnType { get; set; } = null;
        public IList<FuncTypeParameter> Parameters { get; set; } = new List<FuncTypeParameter>();

        public bool IsProc => ReturnType is null;

        public FuncType(SourceRange source) : base(source) {}

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }

    public sealed class FuncTypeParameter : BaseNode
    {
        public string Name { get; set; }
        public IType Type { get; set; }

        public FuncTypeParameter(SourceRange source, string name, IType type) : base(source)
            => (Name, Type) = (name, type);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
