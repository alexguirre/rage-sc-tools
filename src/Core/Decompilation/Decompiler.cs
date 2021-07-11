namespace ScTools.Decompilation
{
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.GameFiles;

    public class Decompiler
    {
        public DecompiledScript[] Scripts { get; }
        public GlobalBlock[] GlobalBlocks { get; }

        public Decompiler(IEnumerable<Script> scripts)
        {
            Scripts = scripts.AsParallel().Select(sc => new DecompiledScript(sc)).ToArray();
            GlobalBlocks = Scripts.Where(dec => dec.Script.GlobalsLengthAndBlock != 0).Select(dec => new GlobalBlock(dec)).ToArray();
        }
    }
}
