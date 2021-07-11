namespace ScTools.Decompilation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class ControlFlowGraph
    {
        public Function Function { get; }
        public CFGBlock[] Blocks { get; }

        public ControlFlowGraph(Function function, IEnumerable<CFGBlock> blocks)
        {
            Function = function;
            Blocks = blocks.OrderBy(b => b.StartAddress).ToArray();
        }

        public string ToDot()
        {
            using var s = new StringWriter();
            ToDot(s);
            return s.ToString();
        }

        public void ToDot(TextWriter w)
        {
            // header
            w.WriteLine("digraph {0} {{", Function.Name);
            w.WriteLine("    node[shape = box];");
            w.WriteLine();

            // nodes
            var blocksIds = new Dictionary<CFGBlock, int>();
            foreach (var block in Blocks)
            {
                var id = blocksIds.Count;
                blocksIds.Add(block, id);

                var contents = "<empty>";
                if (block.IsDelimited && !block.IsEmpty)
                {
                    contents = string.Join(Environment.NewLine, block.EnumerateInstructions().Select(i => i.Opcode.ToString()));
                }

                w.WriteLine("    b{0} [label=\"{1}\"];", id, contents.Escape());
            }
            w.WriteLine();

            // edges
            foreach (var from in Blocks)
            {
                foreach (var to in from.Successors)
                {
                    w.WriteLine("    b{0} -> b{1}", blocksIds[from], blocksIds[to]);
                }
            }

            // footer
            w.WriteLine("}");
        }
    }
}
