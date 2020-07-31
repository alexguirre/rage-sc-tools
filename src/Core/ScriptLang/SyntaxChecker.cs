#nullable enable
namespace ScTools.ScriptLang
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;

    public static class SyntaxChecker
    {
        public static Diagnostics Check(Root root, string filePath)
        {
            Visitor v = new Visitor(root, filePath);
            root.Accept(v);
            return v.Diagnostics;
        }

        private sealed class Visitor : AstVisitor
        {
            public Root Root { get; }
            public string FilePath { get; }
            public Diagnostics Diagnostics { get; }

            private bool InProcedure { get; set; }
            private bool InFunction { get; set; }
            private bool FoundScriptName { get; set; }

            public Visitor(Root root, string filePath)
                => (Root, FilePath, Diagnostics) = (root, filePath, new Diagnostics());

            private void Error(string message, Node node) => Diagnostics.AddError(FilePath, message, node.Source);

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

            public override void VisitVariableDeclarationWithInitializer(VariableDeclarationWithInitializer node)
            {
                if (node.Declaration.ArrayRank != null && node.Initializer != null)
                {
                    Error("Array variable must not have an initializer.", node);
                }

                DefaultVisit(node);
            }

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
