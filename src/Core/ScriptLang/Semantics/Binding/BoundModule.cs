#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class BoundModule : BoundNode
    {
        public string? Name { get; set; }
        public int Hash { get; set; }
        public IList<VariableSymbol> Statics { get; } = new List<VariableSymbol>();
        public IList<BoundFunction> Functions { get; } = new List<BoundFunction>();

        public void Emit(ByteCodeBuilder code)
        {
            code.BeginModule(this);
            var main = Functions.SingleOrDefault(f => f.Function.IsMain);
            if (main != null)
            {
                // if we have MAIN, emit before any other function
                main.Emit(code);
            }

            foreach (var func in Functions.Where(f => f != main))
            {
                func.Emit(code);
            }
            code.EndModule();
        }
    }
}
