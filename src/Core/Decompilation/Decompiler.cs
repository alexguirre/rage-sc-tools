namespace ScTools.Decompilation
{
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;

    public class Decompiler
    {
        private readonly DecompiledScript[] scripts;
        private readonly GlobalBlock[] globalBlocks;

        public Decompiler(IEnumerable<Script> scripts)
        {
            this.scripts = scripts.AsParallel().Select(sc => new DecompiledScript(sc)).ToArray();
            globalBlocks = this.scripts.Where(dec => dec.Script.GlobalsLengthAndBlock != 0).Select(dec => new GlobalBlock(dec)).ToArray();
        }
    }
}
