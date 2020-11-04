#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections.Generic;
    using System.Linq;

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

            // if the last statement is a return we can omit this epilogue
            if (!(Body.LastOrDefault() is BoundReturnStatement))
            {
                code.EmitEpilogue(this.Function);
            }
            code.EndFunction();
        }
    }
}
