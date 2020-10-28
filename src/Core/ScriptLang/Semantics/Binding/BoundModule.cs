#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections.Generic;

    public sealed class BoundModule : BoundNode
    {
        public IList<BoundFunction> Functions { get; } = new List<BoundFunction>();
    }
}
