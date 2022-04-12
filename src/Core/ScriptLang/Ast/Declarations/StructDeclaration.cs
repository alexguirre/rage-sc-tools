namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;

    public sealed class StructDeclaration : BaseTypeDeclaration
    {
        public IList<StructField> Fields { get; set; } = new List<StructField>();

        public StructDeclaration(SourceRange source, string name) : base(source, name) { }

        public override StructType CreateType(SourceRange source) => new(source, this);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public StructField? FindField(string name) => Fields.SingleOrDefault(f => ParserNew.CaseInsensitiveComparer.Equals(f.Name, name));
    }

    public sealed class StructField : BaseNode
    {
        public string Name { get; set; }
        public IType Type { get; set; }
        public IExpression? Initializer { get; set; }
        public int Offset { get; set; }

        public StructField(SourceRange source, string name, IType type) : base(source)
            => (Name, Type) = (name, type);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
