#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;

    public abstract class TopLevelStatement : Node
    {
        public TopLevelStatement(SourceLocation location) : base(location)
        {
        }
    }

    public sealed class ScriptNameStatement : TopLevelStatement
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public ScriptNameStatement(Identifier name, SourceLocation location) : base(location)
            => Name = name;

        public override string ToString() => $"SCRIPT_NAME {Name}";
    }

    public sealed class ProcedureStatement : TopLevelStatement
    {
        public Identifier Name { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Name; yield return Block; } }

        public ProcedureStatement(Identifier name, StatementBlock block, SourceLocation location) : base(location)
            => (Name, Block) = (name, block);

        public override string ToString() => $"PROC {Name}()\n{Block}\nENDPROC";
    }

    public sealed class StructStatement : TopLevelStatement
    {
        public Identifier Name { get; }
        // TODO: StructStatement fields

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public StructStatement(Identifier name, SourceLocation location) : base(location)
            => Name = name;

        public override string ToString() => $"STRUCT {Name}\nENDSTRUCT";
    }

    // TODO: StaticFieldStatement
    public sealed class StaticFieldStatement : TopLevelStatement
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public StaticFieldStatement(Identifier name, SourceLocation location) : base(location)
            => Name = name;

        public override string ToString() => $"{Name}";
    }
}
