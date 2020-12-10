#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class BoundFunction : BoundNode
    {
        private readonly Dictionary<VariableSymbol, int /*location*/> allocatedLocals = new();
        private int localsSize = 0;
        private int localArgsSize = 0;

        public FunctionSymbol Function { get; }
        public IList<BoundStatement> Body { get; } = new List<BoundStatement>();

        public BoundFunction(FunctionSymbol function)
        {
            Function = function;
        }

        public void Emit(ByteCodeBuilder code)
        {
            AllocateLocals();

            code.BeginFunction(this);
            code.EmitPrologue(localArgsSize, localsSize);

            foreach (var stmt in Body)
            {
                stmt.Emit(code, this);
            }

            // if the last statement is a return we can omit this epilogue
            if (!(Body.LastOrDefault() is BoundReturnStatement))
            {
                EmitEpilogue(code);
            }
            code.EndFunction();
        }

        public void EmitEpilogue(ByteCodeBuilder code) => code.EmitEpilogue(localArgsSize, Function.Type.ReturnType);

        public int? GetLocalLocation(VariableSymbol var)
            => allocatedLocals.TryGetValue(var, out int loc) ? loc : null;

        private void AllocateLocals()
        {
            allocatedLocals.Clear();
            localArgsSize = 0;
            localsSize = 0;

            int location = 0;
            foreach (var l in Function.LocalArgs)
            {
                allocatedLocals.Add(l, location);
                int size = l.Type.SizeOf;
                location += size;
                localArgsSize += size;
            }
            location += 2; // space required by the game
            foreach (var l in Function.Locals)
            {
                allocatedLocals.Add(l, location);
                int size = l.Type.SizeOf;
                location += size;
                localsSize += size;
            }
        }
    }
}
