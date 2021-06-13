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
        public AstOld.Node AstNode { get; }
        public IList<VariableSymbol> Locals { get; } = new List<VariableSymbol>();
        public IList<VariableSymbol> LocalArgs { get; } = new List<VariableSymbol>();

        public bool IsProcedure => Type.ReturnType == null;
        public bool IsMain => IsProcedure && Type.ParameterCount == 0 && SymbolTable.CaseInsensitiveComparer.Equals(Name, MainName);
        public AstOld.StatementBlock? AstBlock => AstNode switch
        {
            AstOld.ProcedureStatement s => s.Block,
            AstOld.FunctionStatement s => s.Block,
            _ => null,
        };

        public DefinedFunctionSymbol(AstOld.ProcedureStatement node, ExplicitFunctionType type) : base(node.Name, node.Source)
            => (AstNode, Type) = (node, type);

        public DefinedFunctionSymbol(AstOld.FunctionStatement node, ExplicitFunctionType type) : base(node.Name, node.Source)
            => (AstNode, Type) = (node, type);
    }
}
