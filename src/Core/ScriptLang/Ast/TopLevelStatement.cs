#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

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

        public override void Accept(AstVisitor visitor) => visitor.VisitScriptNameStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitScriptNameStatement(this);
    }

    public sealed class ScriptHashStatement : TopLevelStatement
    {
        public int Hash { get; }

        public ScriptHashStatement(int hash, SourceRange source) : base(source)
            => Hash = hash;

        public override string ToString() => $"SCRIPT_HASH 0x{Hash:X8}";

        public override void Accept(AstVisitor visitor) => visitor.VisitScriptHashStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitScriptHashStatement(this);
    }

    public sealed class UsingStatement : TopLevelStatement
    {
        public string PathRaw { get; }
        public string Path { get; }

        public UsingStatement(string pathRaw, SourceRange source) : base(source)
            => (PathRaw, Path) = (pathRaw, pathRaw[1..^1].Unescape());

        public override string ToString() => $"USING {PathRaw}";

        public override void Accept(AstVisitor visitor) => visitor.VisitUsingStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitUsingStatement(this);
    }

    public sealed class ProcedureStatement : TopLevelStatement
    {
        public string Name { get; }
        public ImmutableArray<Declaration> Parameters { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children => Parameters.Cast<Node>().Append(Block);

        public ProcedureStatement(string name, IEnumerable<Declaration> parameters, StatementBlock block, SourceRange source) : base(source)
            => (Name, Parameters, Block) = (name, parameters.ToImmutableArray(), block);

        public override string ToString() => $"PROC {Name}({string.Join(", ", Parameters)})\n{Block}\nENDPROC";

        public override void Accept(AstVisitor visitor) => visitor.VisitProcedureStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitProcedureStatement(this);
    }

    public sealed class ProcedurePrototypeStatement : TopLevelStatement
    {
        public string Name { get; }
        public ImmutableArray<Declaration> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public ProcedurePrototypeStatement(string name, IEnumerable<Declaration> parameters, SourceRange source) : base(source)
            => (Name, Parameters) = (name, parameters.ToImmutableArray());

        public override string ToString() => $"PROTO PROC {Name}({string.Join(", ", Parameters)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitProcedurePrototypeStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitProcedurePrototypeStatement(this);
    }

    public sealed class ProcedureNativeStatement : TopLevelStatement
    {
        public string Name { get; }
        public ImmutableArray<Declaration> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public ProcedureNativeStatement(string name, IEnumerable<Declaration> parameters, SourceRange source) : base(source)
            => (Name, Parameters) = (name, parameters.ToImmutableArray());

        public override string ToString() => $"NATIVE PROC {Name}({string.Join(", ", Parameters)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitProcedureNativeStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitProcedureNativeStatement(this);
    }

    public sealed class FunctionStatement : TopLevelStatement
    {
        public string Name { get; }
        public string ReturnType { get; }
        public ImmutableArray<Declaration> Parameters { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children => Parameters.Cast<Node>().Append(Block);

        public FunctionStatement(string name, string returnType, IEnumerable<Declaration> parameters, StatementBlock block, SourceRange source) : base(source)
            => (Name, ReturnType, Parameters, Block) = (name, returnType, parameters.ToImmutableArray(), block);

        public override string ToString() => $"FUNC {ReturnType} {Name}({string.Join(", ", Parameters)})\n{Block}\nENDFUNC";

        public override void Accept(AstVisitor visitor) => visitor.VisitFunctionStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitFunctionStatement(this);
    }

    public sealed class FunctionPrototypeStatement : TopLevelStatement
    {
        public string Name { get; }
        public string ReturnType { get; }
        public ImmutableArray<Declaration> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public FunctionPrototypeStatement(string name, string returnType, IEnumerable<Declaration> parameters, SourceRange source) : base(source)
            => (Name, ReturnType, Parameters) = (name, returnType, parameters.ToImmutableArray());

        public override string ToString() => $"PROTO FUNC {ReturnType} {Name}({string.Join(", ", Parameters)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitFunctionPrototypeStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitFunctionPrototypeStatement(this);
    }

    public sealed class FunctionNativeStatement : TopLevelStatement
    {
        public string Name { get; }
        public string ReturnType { get; }
        public ImmutableArray<Declaration> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public FunctionNativeStatement(string name, string returnType, IEnumerable<Declaration> parameters, SourceRange source) : base(source)
            => (Name, ReturnType, Parameters) = (name, returnType, parameters.ToImmutableArray());

        public override string ToString() => $"NATIVE FUNC {ReturnType} {Name}({string.Join(", ", Parameters)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitFunctionNativeStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitFunctionNativeStatement(this);
    }

    public sealed class StructStatement : TopLevelStatement
    {
        public string Name { get; }
        public ImmutableArray<Declaration> Fields { get; }

        public override IEnumerable<Node> Children => Fields;

        public StructStatement(string name, IEnumerable<Declaration> fields, SourceRange source) : base(source)
            => (Name, Fields) = (name, fields.ToImmutableArray());

        public override string ToString() => $"STRUCT {Name}\n{string.Join('\n', Fields)}\nENDSTRUCT";

        public override void Accept(AstVisitor visitor) => visitor.VisitStructStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitStructStatement(this);
    }

    public sealed class StaticVariableStatement : TopLevelStatement
    {
        public Declaration Declaration { get; }

        public override IEnumerable<Node> Children { get { yield return Declaration; } }

        public StaticVariableStatement(Declaration declaration, SourceRange source) : base(source)
            => Declaration = declaration;

        public override string ToString() => $"{Declaration}";

        public override void Accept(AstVisitor visitor) => visitor.VisitStaticVariableStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitStaticVariableStatement(this);
    }

    public sealed class ConstantVariableStatement : TopLevelStatement
    {
        public Declaration Declaration { get; }

        public override IEnumerable<Node> Children { get { yield return Declaration; } }

        public ConstantVariableStatement(Declaration declaration, SourceRange source) : base(source)
            => Declaration = declaration;

        public override string ToString() => $"CONST {Declaration}";

        public override void Accept(AstVisitor visitor) => visitor.VisitConstantVariableStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitConstantVariableStatement(this);
    }
}
