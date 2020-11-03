#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class BoundFunction : BoundNode
    {
        public FunctionSymbol Function { get; }
        public IList<BoundStatement> Body { get; } = new List<BoundStatement>();

        public BoundFunction(FunctionSymbol function)
        {
            Function = function;
        }

        public void Emit(ByteCodeBuilder code)
        {
            code.BeginFunction(Function.Name);
            code.EmitPrologue(this.Function);

            foreach (var stmt in Body)
            {
                stmt.Emit(code, this);
            }

            code.EmitEpilogue(this.Function);
            code.EndFunction();
        }
    }
}
