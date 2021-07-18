namespace ScTools.Decompilation
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using ScTools.Decompilation.Intermediate;

    public class ControlFlowGraph
    {
        public Function Function { get; }
        public ControlFlowBlock[] Blocks { get; }

        public ControlFlowGraph(Function function, IEnumerable<ControlFlowBlock> blocks)
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
            var escapedFunctionName = Function.Name.Escape();
            w.WriteLine("digraph \"{0}\" {{", escapedFunctionName);
            w.WriteLine("    label=\"{0}\"", escapedFunctionName);
            w.WriteLine("    labelloc=\"t\"");
            w.WriteLine("    graph[splines = ortho, fontname = \"Consolas\"];");
            w.WriteLine("    edge[fontname = \"Consolas\"];");
            w.WriteLine("    node[shape = box, fontname = \"Consolas\"];");
            w.WriteLine();

            // nodes
            var blocksIds = new Dictionary<ControlFlowBlock, int>();
            foreach (var block in Blocks)
            {
                var id = blocksIds.Count;
                blocksIds.Add(block, id);

                w.Write("    b{0} [label=\"", id);
                if (block.IsDelimited && !block.IsEmpty)
                {
                    w.Write("{0:D8}:\\l", block.StartAddress);
                    foreach (var inst in block.EnumerateInstructions())
                    {
                        w.Write("  ");
                        InstructionFormatter.Format(w, inst);
                        w.Write("\\l");  // \l left-aligns the line
                    }
                }
                else
                {
                    w.Write("<empty>");
                }
                w.WriteLine("\"];");
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
