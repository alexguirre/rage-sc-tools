#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;

    public abstract class TopLevelStatement : Node
    {
        public TopLevelStatement(SourceRange source) : base(source)
        {
        }
    }

    public sealed class ScriptNameStatement : TopLevelStatement
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public ScriptNameStatement(Identifier name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"SCRIPT_NAME {Name}";
    }

    public sealed class ProcedureStatement : TopLevelStatement
    {
        public Identifier Name { get; }
        public ParameterList ParameterList { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Name; yield return ParameterList; yield return Block; } }

        public ProcedureStatement(Identifier name, ParameterList parameterList, StatementBlock block, SourceRange source) : base(source)
            => (Name, ParameterList, Block) = (name, parameterList, block);

        public override string ToString() => $"PROC {Name}{ParameterList}\n{Block}\nENDPROC";
    }

    public sealed class FunctionStatement : TopLevelStatement
    {
        public Identifier Name { get; }
        public Type ReturnType { get; }
        public ParameterList ParameterList { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Name; yield return ReturnType; yield return ParameterList; yield return Block; } }

        public FunctionStatement(Identifier name, Type returnType, ParameterList parameterList, StatementBlock block, SourceRange source) : base(source)
            => (Name, ReturnType, ParameterList, Block) = (name, returnType, parameterList, block);

        public override string ToString() => $"FUNC {ReturnType} {Name}{ParameterList}\n{Block}\nENDFUNC";
    }

    public sealed class StructStatement : TopLevelStatement
    {
        public Identifier Name { get; }
        // TODO: StructStatement fields

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public StructStatement(Identifier name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"STRUCT {Name}\nENDSTRUCT";
    }

    // TODO: StaticFieldStatement
    public sealed class StaticFieldStatement : TopLevelStatement
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public StaticFieldStatement(Identifier name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"{Name}";
    }
}
