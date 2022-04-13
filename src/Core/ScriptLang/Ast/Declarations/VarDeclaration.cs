namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Ast.Types;

public enum VarKind
{
    /// <summary>
    /// A static variable with the CONST modifier. Where used, it is replaced by its value at compile-time. Requires a non-null <see cref="VarDeclaration.Initializer"/>.
    /// </summary>
    Constant,
    /// <summary>
    /// A variable defined in a GLOBAL block, shared between script threads.
    /// </summary>
    Global,
    /// <summary>
    /// A variable defined outside any functions/procedures.
    /// </summary>
    Static,
    /// <summary>
    /// A static variable that represents a parameter of a SCRIPT declaration. Initialized by other script thread when starting
    /// this script with `START_NEW_SCRIPT_WITH_ARGS` or `START_NEW_SCRIPT_WITH_NAME_HASH_AND_ARGS`.
    /// </summary>
    ScriptParameter,
    /// <summary>
    /// A variable defined inside a function/procedure.
    /// </summary>
    Local,
    /// <summary>
    /// A local variable that represents a parameter of a function/procedure. <see cref="VarDeclaration.Initializer"/> must be null.
    /// </summary>
    Parameter,
}

public sealed class VarDeclaration : BaseValueDeclaration, IStatement
{
    public Label? Label { get; }
    public VarKind Kind { get; set; }
    /// <summary>
    /// Gets or sets whether this variable is a reference. Only valid with kind <see cref="VarKind.Parameter"/>.
    /// </summary>
    public bool IsReference { get; set; }
    public IExpression? Initializer { get; set; }
    public int Address { get; set; }

    public VarDeclaration(Token nameIdentifier, IType type, VarKind kind, bool isReference, Label? label = null) : base(nameIdentifier.Lexeme.ToString(), type, nameIdentifier) // TODO: pass children to BaseValueDeclaration
        => (Kind, IsReference, Label) = (kind, isReference, label);
    public VarDeclaration(SourceRange source, string name, IType type, VarKind kind, bool isReference) : base(source, name, type)
        => (Kind, IsReference) = (kind, isReference);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    internal VarDeclaration WithLabel(Label? label)
        => new(Tokens[0], Type, Kind, IsReference, label);
}
