namespace ScTools.Decompiler;

using System;
using System.Collections.Generic;
using System.IO;

public static class CFGGraphViz
{
    public static string ToDot(CFGBlock[] blocks)
    {
        using var s = new StringWriter();
        ToDot(s, blocks);
        return s.ToString();
    }

    public static void ToDot(TextWriter w, CFGBlock[] blocks)
    {
        // header
        w.WriteLine($@"digraph ""CFG"" {{");
        w.WriteLine($@"    label=""CFG""");
        w.WriteLine($@"    labelloc=""t""");
        w.WriteLine($@"    graph[splines = ortho, fontname = ""Consolas""];");
        w.WriteLine($@"    edge[fontname = ""Consolas""];");
        w.WriteLine($@"    node[shape = box, fontname = ""Consolas""];");
        w.WriteLine();

        // nodes
        var blocksIds = new Dictionary<CFGBlock, int>();
        foreach (var block in blocks)
        {
            var id = blocksIds.Count;
            blocksIds.Add(block, id);

            w.Write("    b{0} [label=\"", id);
            //if (block.IsDelimited && !block.IsEmpty)
            //{
            w.Write("{0:D8}:\\l", block.Start.Address);
            var inst = block.Start;
            while (inst is not null && inst != block.End)
            {
                w.Write("  ");
                IR.IRPrinter.Print(inst, w);
                w.Write("\\l");  // \l left-aligns the line
                inst = inst.Next;
            }
            //}
            //else
            //{
            //    w.Write("<empty>");
            //}
            w.WriteLine("\"];");
        }
        w.WriteLine();

        // edges
        foreach (var source in blocks)
        {
            foreach (var edge in source.OutgoingEdges)
            {
                var target = edge.Target;
                w.WriteLine("    b{0} -> b{1} [color = {2}, xlabel = \"{3}\", fontcolor = {2}]", blocksIds[source], blocksIds[target], EdgeToColor(edge), EdgeToLabel(edge));
            }
        }

        // footer
        w.WriteLine("}");

        static string EdgeToColor(CFGEdge edge)
            => edge.Kind switch
            {
                CFGEdgeKind.Unconditional => "black",
                CFGEdgeKind.IfFalse => "red",
                CFGEdgeKind.IfTrue => "green",
                CFGEdgeKind.SwitchCase => "blue",
                _ => throw new NotSupportedException(),
            };

        static string EdgeToLabel(CFGEdge edge)
            => edge.Kind switch
            {
                CFGEdgeKind.Unconditional => "",
                CFGEdgeKind.IfFalse => "if false",
                CFGEdgeKind.IfTrue => "if true",
                CFGEdgeKind.SwitchCase => edge.SwitchCaseValue is null ? "case default" : $"case {edge.SwitchCaseValue.Value}",
                _ => throw new NotSupportedException(),
            };
    }
}
