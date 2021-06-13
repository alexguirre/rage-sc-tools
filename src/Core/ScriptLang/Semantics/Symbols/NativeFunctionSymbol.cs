#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    public class NativeFunctionSymbol : FunctionSymbol
    {
        public override ExplicitFunctionType Type { get; }

        public NativeFunctionSymbol(AstOld.ProcedureNativeStatement node, ExplicitFunctionType type) : base(node.Name, node.Source)
            => Type = type;

        public NativeFunctionSymbol(AstOld.FunctionNativeStatement node, ExplicitFunctionType type) : base(node.Name, node.Source)
            => Type = type;
    }
}
