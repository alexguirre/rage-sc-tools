#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Collections.Generic;

    /// <summary>
    /// Refers to a function defined in the script program source code.
    /// </summary>
    public class DefinedFunctionSymbol : FunctionSymbol
    {
        public const string MainName = "MAIN";

        public override ExplicitFunctionType Type { get; }
        public Ast.Node AstNode { get; }
        public IList<VariableSymbol> Locals { get; } = new List<VariableSymbol>();
        public IList<VariableSymbol> LocalArgs { get; } = new List<VariableSymbol>();

        public bool IsProcedure => Type.ReturnType == null;
        public bool IsMain => IsProcedure && Type.ParameterCount == 0 && SymbolTable.CaseInsensitiveComparer.Equals(Name, MainName);
        public Ast.StatementBlock? AstBlock => AstNode switch
        {
            Ast.ProcedureStatement s => s.Block,
            Ast.FunctionStatement s => s.Block,
            _ => null,
        };

        public DefinedFunctionSymbol(Ast.ProcedureStatement node, ExplicitFunctionType type) : base(node.Name, node.Source)
            => (AstNode, Type) = (node, type);

        public DefinedFunctionSymbol(Ast.FunctionStatement node, ExplicitFunctionType type) : base(node.Name, node.Source)
            => (AstNode, Type) = (node, type);
    }
}
