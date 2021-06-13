#nullable enable
namespace ScTools.ScriptLang
{
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.AstOld;

    public static class SyntaxChecker
    {
        public static void Check(Root root, DiagnosticsReport diagnostics)
            => root.Accept(new Visitor(root, diagnostics));

        private sealed class Visitor : AstVisitor
        {
            public Root Root { get; }
            public DiagnosticsReport Diagnostics { get; }

            private bool InProcedure { get; set; }
            private bool InFunction { get; set; }
            private bool FoundScriptName { get; set; }
            private bool FoundScriptHash { get; set; }

            public Visitor(Root root, DiagnosticsReport diagnostics)
                => (Root, Diagnostics) = (root, diagnostics);

            private void Error(string message, Node node) => Diagnostics.AddError(message, node.Source);

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                // TODO: check that all function code paths return a value
                InFunction = true;
                DefaultVisit(node);
                InFunction = false;
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                InProcedure = true;
                DefaultVisit(node);
                InProcedure = false;
            }

            public override void VisitReturnStatement(ReturnStatement node)
            {
                if (InFunction)
                {
                    if (node.Expression == null)
                    {
                        Error("Return statement in function is missing the return value.", node);
                    }
                }
                else if (InProcedure)
                {
                    if (node.Expression != null)
                    {
                        Error("Return statement in procedure must not return value.", node);
                    }
                }
                else
                {
                    Debug.Assert(false, $"{nameof(ReturnStatement)} outside a procedure or function");
                }

                DefaultVisit(node);
            }

            public override void VisitScriptNameStatement(ScriptNameStatement node)
            {
                if (FoundScriptName)
                {
                    Error("SCRIPT_NAME statement is repeated", node);
                }
                else
                {
                    FoundScriptName = true;
                }
            }

            public override void VisitScriptHashStatement(ScriptHashStatement node)
            {
                if (FoundScriptHash)
                {
                    Error("SCRIPT_HASH statement is repeated", node);
                }
                else
                {
                    FoundScriptHash = true;
                }
            }

            public override void VisitConstantVariableStatement(ConstantVariableStatement node)
            {
                if (node.Declaration.Initializer == null)
                {
                    Error($"A constant requires an initializer", node);
                }

                DefaultVisit(node);
            }

            public override void VisitErrorStatement(ErrorStatement node)
                => Error($"Expected statement, found '{node.Text}'", node);

            public override void VisitErrorExpression(ErrorExpression node)
                => Error($"Expected expression, found '{node.Text}'", node);

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    Visit(n);
                }
            }
        }
    }
}
