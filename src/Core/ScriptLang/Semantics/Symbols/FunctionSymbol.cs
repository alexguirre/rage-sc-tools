#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Collections.Generic;

    public class FunctionSymbol : ISymbol
    {
        public const string MainName = "MAIN";

        public string Name { get; }
        public SourceRange Source { get; }
        public Ast.Node AstNode { get; }
        public FunctionType Type { get; }
        public bool IsNative { get; }
        public IList<VariableSymbol> Locals { get; } = new List<VariableSymbol>();
        public IList<VariableSymbol> LocalArgs { get; } = new List<VariableSymbol>();

        public bool IsProcedure => Type.ReturnType == null;
        public bool IsMain => IsProcedure && Type.Parameters.Count == 0 && SymbolTable.CaseInsensitiveComparer.Equals(Name, MainName);
        public Ast.StatementBlock? AstBlock => AstNode switch
        {
            Ast.ProcedureStatement s => s.Block,
            Ast.FunctionStatement s => s.Block,
            _ => null,
        };

        public FunctionSymbol(Ast.ProcedureStatement node, FunctionType type)
            => (Name, Source, AstNode, Type, IsNative) = (node.Name, node.Source, node, type, false);

        public FunctionSymbol(Ast.FunctionStatement node, FunctionType type)
            => (Name, Source, AstNode, Type, IsNative) = (node.Name, node.Source, node, type, false);

        public FunctionSymbol(Ast.ProcedureNativeStatement node, FunctionType type)
            => (Name, Source, AstNode, Type, IsNative) = (node.Name, node.Source, node, type, true);

        public FunctionSymbol(Ast.FunctionNativeStatement node, FunctionType type)
            => (Name, Source, AstNode, Type, IsNative) = (node.Name, node.Source, node, type, true);
    }
}
