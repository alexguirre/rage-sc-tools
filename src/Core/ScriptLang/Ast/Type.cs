#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;

    public abstract class Type : Node
    {
        public Type(SourceRange source) : base(source) { }
    }

    public sealed class BasicType : Type
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public BasicType(Identifier name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"{Name}";
    }

    public sealed class RefType : Type
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public RefType(Identifier name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"{Name}&";
    }

    public sealed class ProcedureRefType : Type
    {
        public ParameterList ParameterList { get; }

        public override IEnumerable<Node> Children { get { yield return ParameterList; } }

        public ProcedureRefType(ParameterList parameterList, SourceRange source) : base(source)
            => ParameterList = parameterList;

        public override string ToString() => $"PROC& {ParameterList}";
    }

    public sealed class FunctionRefType : Type
    {
        public Type ReturnType { get; }
        public ParameterList ParameterList { get; }

        public override IEnumerable<Node> Children { get { yield return ParameterList; } }

        public FunctionRefType(Type returnType, ParameterList parameterList, SourceRange source) : base(source)
            => (ReturnType, ParameterList) = (returnType, parameterList);

        public override string ToString() => $"FUNC& {ReturnType}{ParameterList}";
    }
}
