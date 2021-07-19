namespace ScTools.Decompilation
{
    using System;
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
                foreach (var edge in from.Successors)
                {
                    var to = edge.To;
                    w.WriteLine("    b{0} -> b{1} [color = {2}, xlabel = \"{3}\", fontcolor = {2}]", blocksIds[from], blocksIds[to], EdgeToColor(edge), EdgeToLabel(edge));
                }
            }

            // footer
            w.WriteLine("}");

            static string EdgeToColor(ControlFlowEdge edge)
                => edge.Kind switch
                {
                    ControlFlowEdgeKind.Unconditional => "black",
                    ControlFlowEdgeKind.IfFalse => "red",
                    ControlFlowEdgeKind.IfTrue => "green",
                    ControlFlowEdgeKind.SwitchCase => "blue",
                    _ => throw new NotSupportedException(),
                };

            static string EdgeToLabel(ControlFlowEdge edge)
                => edge.Kind switch
                {
                    ControlFlowEdgeKind.Unconditional => "",
                    ControlFlowEdgeKind.IfFalse => "if false",
                    ControlFlowEdgeKind.IfTrue => "if true",
                    ControlFlowEdgeKind.SwitchCase => edge.SwitchCaseValue is null ? "case default" : $"case {edge.SwitchCaseValue.Value}",
                    _ => throw new NotSupportedException(),
                };
        }
    }
}
