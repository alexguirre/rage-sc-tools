#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Text;

    // based on: https://github.com/rspivak/lsbasi/blob/master/part7/python/genastdot.py
    public sealed class AstDotGenerator
    {
        public static string Generate(Node root)
        {
            StringBuilder sb = new StringBuilder(Header);
            int idCounter = 0;
            var ids = new Dictionary<Node, int>();
            Print(sb, ref idCounter, ids, root);
            sb.AppendLine(Footer);
            return sb.ToString();
        }

        private static void Print(StringBuilder sb, ref int idCounter, Dictionary<Node, int> ids, Node node)
        {
            int id = idCounter++;
            ids.Add(node, id);
            sb.AppendLine($"    node{id} [label=\"{node.GetType().Name}\"]");

            foreach (var n in node.Children)
            {
                Print(sb, ref idCounter, ids, n);
            }

            foreach (var n in node.Children)
            {
                sb.AppendLine($"    node{id} -> node{ids[n]}");
            }
        }

        private const string Header = @"
digraph astgraph {
    node [shape=circle, fontsize=12, fontname=""Courier"", height=.1];
    ranksep=.3;
    edge[arrowsize = .5]
";

        private const string Footer = "}";
    }
}
