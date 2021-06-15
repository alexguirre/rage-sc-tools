namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    public sealed class UnresolvedNamedType : BaseType
    {
        public string Name { get; set; }
        public override int SizeOf => throw new NotSupportedException($"Cannot get size of unresolved type '{Name}'");

        public UnresolvedNamedType(SourceRange source, string name) : base(source)
            => Name = name;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
