namespace ScTools.Decompiler;

public class CallGraphNode
{
    public required Function Function { get; init; }
    public List<CallGraphNode> OutgoingEdges { get; } = new();
    public List<CallGraphNode> IncomingEdges { get; } = new();
}

public static class CallGraphBuilder
{
    public static CallGraphNode BuildFrom(Script script, Function function) => new Builder(script, function).Build();

    private class Builder
    {
        private readonly Script script;
        private readonly CallGraphNode root;
        private readonly Dictionary<Function, CallGraphNode> nodes = new();

        public Builder(Script script, Function root)
        {
            this.script = script;
            this.root = new() { Function = root };
            nodes.Add(root, this.root);
        }

        public CallGraphNode Build()
        {
            Explore(root);
            return root;
        }

        private void Explore(CallGraphNode root)
        {
            var nodesToExplore = new Queue<CallGraphNode>();
            nodesToExplore.Enqueue(root);

            var blocks = new Queue<CFGBlock>();
            var exploredBlocks = new HashSet<CFGBlock>();
            while (nodesToExplore.TryDequeue(out var node))
            {
                blocks.Clear();
                exploredBlocks.Clear();
                blocks.Enqueue(node.Function.RootBlock);
                while (blocks.TryDequeue(out var block))
                {
                    foreach (var inst in block)
                    {
                        if (inst is not IR.IRCall call)
                        {
                            continue;
                        }

                        var target = script.GetFunctionAt(call.CallAddress);
                        if (!nodes.ContainsKey(target))
                        {
                            var targetNode = new CallGraphNode { Function = target };
                            node.OutgoingEdges.Add(targetNode);
                            targetNode.IncomingEdges.Add(node);
                            nodes.Add(target, targetNode);
                            nodesToExplore.Enqueue(targetNode);
                        }
                        else
                        {
                            var targetNode = nodes[target];
                            if (!node.OutgoingEdges.Contains(targetNode))
                            {
                                node.OutgoingEdges.Add(targetNode);
                                targetNode.IncomingEdges.Add(node);
                            }
                        }
                    }

                    exploredBlocks.Add(block);
                    foreach (var edge in block.OutgoingEdges)
                    {
                        if (!exploredBlocks.Contains(edge.Target))
                        {
                            blocks.Enqueue(edge.Target);
                        }
                    }
                }
            }
        }
    }
}
