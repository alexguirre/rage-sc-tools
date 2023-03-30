namespace ScTools.Decompiler;

using System;
using System.Collections.Generic;
using System.IO;

public static class CallGraphGraphViz
{
    public static string ToDot(CallGraphNode root)
    {
        using var s = new StringWriter();
        ToDot(s, root);
        return s.ToString();
    }

    public static void ToDot(TextWriter w, CallGraphNode root)
    {
        // header
        w.WriteLine($@"digraph ""Call Graph"" {{");
        w.WriteLine($@"    label=""Call Graph""");
        w.WriteLine($@"    labelloc=""t""");
        w.WriteLine($@"    graph[splines = ortho, fontname = ""Consolas""];");
        w.WriteLine($@"    edge[fontname = ""Consolas""];");
        w.WriteLine($@"    node[shape = box, fontname = ""Consolas""];");
        w.WriteLine();

        // nodes
        var nodes = new List<CallGraphNode>();
        var nodeQueue = new Queue<CallGraphNode>();
        nodeQueue.Enqueue(root);
        var nodesIds = new Dictionary<CallGraphNode, int>();
        while (nodeQueue.TryDequeue(out var node))
        {
            if (nodesIds.ContainsKey(node))
            {
                continue;
            }

            nodes.Add(node);
            var id = nodesIds.Count;
            nodesIds.Add(node, id);

            w.WriteLine("    n{0} [label=\"{1}\"];", id, $"func_{node.Function.StartAddress:000000}");

            foreach (var edgeTarget in node.OutgoingEdges)
            {
                nodeQueue.Enqueue(edgeTarget);
            }
        }
        w.WriteLine();

        // edges
        foreach (var source in nodes)
        {
            foreach (var edgeTarget in source.OutgoingEdges)
            {
                w.WriteLine("    n{0} -> n{1} [color = {2}, xlabel = \"{3}\", fontcolor = {2}]", nodesIds[source], nodesIds[edgeTarget], EdgeToColor(), EdgeToLabel());
            }
        }

        // footer
        w.WriteLine("}");

        static string EdgeToColor() => "black";

        static string EdgeToLabel() => "";
    }
}
