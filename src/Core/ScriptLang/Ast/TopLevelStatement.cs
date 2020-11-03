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
        public string Name { get; }

        public ScriptNameStatement(string name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"SCRIPT_NAME {Name}";
    }

    public sealed class ProcedureStatement : TopLevelStatement
    {
        public string Name { get; }
        public ParameterList ParameterList { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return ParameterList; yield return Block; } }

        public ProcedureStatement(string name, ParameterList parameterList, StatementBlock block, SourceRange source) : base(source)
            => (Name, ParameterList, Block) = (name, parameterList, block);

        public override string ToString() => $"PROC {Name}{ParameterList}\n{Block}\nENDPROC";
    }

    public sealed class ProcedurePrototypeStatement : TopLevelStatement
    {
        public string Name { get; }
        public ParameterList ParameterList { get; }

        public override IEnumerable<Node> Children { get { yield return ParameterList; } }

        public ProcedurePrototypeStatement(string name, ParameterList parameterList, SourceRange source) : base(source)
            => (Name, ParameterList) = (name, parameterList);

        public override string ToString() => $"PROTO PROC {Name}{ParameterList}";
    }

    public sealed class ProcedureNativeStatement : TopLevelStatement
    {
        public string Name { get; }
        public ParameterList ParameterList { get; }

        public override IEnumerable<Node> Children { get { yield return ParameterList; } }

        public ProcedureNativeStatement(string name, ParameterList parameterList, SourceRange source) : base(source)
            => (Name, ParameterList) = (name, parameterList);

        public override string ToString() => $"NATIVE PROC {Name}{ParameterList}";
    }

    public sealed class FunctionStatement : TopLevelStatement
    {
        public string Name { get; }
        public Type ReturnType { get; }
        public ParameterList ParameterList { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return ReturnType; yield return ParameterList; yield return Block; } }

        public FunctionStatement(string name, Type returnType, ParameterList parameterList, StatementBlock block, SourceRange source) : base(source)
            => (Name, ReturnType, ParameterList, Block) = (name, returnType, parameterList, block);

        public override string ToString() => $"FUNC {ReturnType} {Name}{ParameterList}\n{Block}\nENDFUNC";
    }

    public sealed class FunctionPrototypeStatement : TopLevelStatement
    {
        public string Name { get; }
        public Type ReturnType { get; }
        public ParameterList ParameterList { get; }

        public override IEnumerable<Node> Children { get { yield return ReturnType; yield return ParameterList; } }

        public FunctionPrototypeStatement(string name, Type returnType, ParameterList parameterList, SourceRange source) : base(source)
            => (Name, ReturnType, ParameterList) = (name, returnType, parameterList);

        public override string ToString() => $"PROTO FUNC {ReturnType} {Name}{ParameterList}";
    }

    public sealed class FunctionNativeStatement : TopLevelStatement
    {
        public string Name { get; }
        public Type ReturnType { get; }
        public ParameterList ParameterList { get; }

        public override IEnumerable<Node> Children { get { yield return ReturnType; yield return ParameterList; } }

        public FunctionNativeStatement(string name, Type returnType, ParameterList parameterList, SourceRange source) : base(source)
            => (Name, ReturnType, ParameterList) = (name, returnType, parameterList);

        public override string ToString() => $"NATIVE FUNC {ReturnType} {Name}{ParameterList}";
    }

    public sealed class StructStatement : TopLevelStatement
    {
        public string Name { get; }
        public StructFieldList FieldList { get; }

        public override IEnumerable<Node> Children { get { yield return FieldList; } }

        public StructStatement(string name, StructFieldList fieldList, SourceRange source) : base(source)
            => (Name, FieldList) = (name, fieldList);

        public override string ToString() => $"STRUCT {Name}\n{FieldList}\nENDSTRUCT";
    }

    public sealed class StaticVariableStatement : TopLevelStatement
    {
        public VariableDeclarationWithInitializer Variable { get; }

        public override IEnumerable<Node> Children { get { yield return Variable; } }

        public StaticVariableStatement(VariableDeclarationWithInitializer variable, SourceRange source) : base(source)
            => Variable = variable;

        public override string ToString() => $"{Variable}";
    }
}
