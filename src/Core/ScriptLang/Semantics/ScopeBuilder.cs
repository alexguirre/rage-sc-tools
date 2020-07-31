#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class ScopeBuilder
    {
        public static (Scope RootScope, Diagnostics Diagnostics) Explore(Root root, string filePath)
        {
            Visitor v = new Visitor(root, filePath);
            root.Accept(v);
            return (v.RootScope, v.Diagnostics);
        }

        private sealed class Visitor : AstVisitor
        {
            public Root Root { get; }
            public string FilePath { get; }
            public Scope RootScope { get; }
            public Diagnostics Diagnostics { get; }

            private Scope scope;
            private int ifCounter = 0;
            private int whileCounter = 0;

            public Visitor(Root root, string filePath)
            {
                Root = root;
                FilePath = filePath;
                RootScope = scope = Scope.CreateRoot();
                Diagnostics = new Diagnostics();
            }

            private void Error(string message, Node node) => Diagnostics.AddError(FilePath, message, node.Source);
            private void RepeatedSymbol(string name, Node node) => Error($"Symbol with name '{name}' already exists", node);

            private void AddIfNotRepeated(ISymbol symbol, Node node)
            {
                if (scope.Exists(symbol.Name))
                {
                    RepeatedSymbol(symbol.Name, node);
                }
                else
                {
                    scope.Add(symbol);
                }
            }

            private void ResetIfWhileCounters() => (ifCounter, whileCounter) = (0, 0);

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                var proc = new ProcedureSymbol(node, scope.CreateNested($"PROC#{node.Name.Name}"));
                AddIfNotRepeated(proc, node);

                ResetIfWhileCounters();
                scope = proc.Scope;
                DefaultVisit(node);
                scope = scope.Parent!;
            }


            public override void VisitFunctionStatement(FunctionStatement node)
            {
                var func = new FunctionSymbol(node, scope.CreateNested($"FUNC#{node.Name.Name}"));
                AddIfNotRepeated(func, node);

                ResetIfWhileCounters();
                scope = func.Scope;
                DefaultVisit(node);
                scope = scope.Parent!;
            }

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructSymbol(node, scope.CreateNested($"STRUCT#{node.Name.Name}"));
                AddIfNotRepeated(struc, node);

                scope = struc.Scope;
                DefaultVisit(node);
                scope = scope.Parent!;
            }

            public override void VisitStaticFieldStatement(StaticFieldStatement node)
            {
                var sf = new StaticFieldSymbol(node.Variable);
                AddIfNotRepeated(sf, node);
            }

            public override void VisitStructFieldList(StructFieldList node)
            {
                foreach (var pair in node.Fields.Select(p => (Node: p, Symbol: new StructFieldSymbol(p))))
                {
                    AddIfNotRepeated(pair.Symbol, pair.Node);
                }
            }

            public override void VisitParameterList(ParameterList node)
            {
                foreach (var pair in node.Parameters.Select(p => (Node: p, Symbol: new ParameterSymbol(p))))
                {
                    AddIfNotRepeated(pair.Symbol, pair.Node);
                }
            }

            public override void VisitVariableDeclarationStatement(VariableDeclarationStatement node)
            {
                var symbol = new LocalSymbol(node.Variable);
                AddIfNotRepeated(symbol, node);
            }

            public override void VisitIfStatement(IfStatement node)
            {
                Visit(node.Condition);

                ifCounter++;
                scope = scope.CreateNested($"IF#{ifCounter}-THEN");
                Visit(node.ThenBlock);
                scope = scope.Parent!;

                if (node.ElseBlock != null)
                {
                    scope = scope.CreateNested($"IF#{ifCounter}-ELSE");
                    Visit(node.ElseBlock);
                    scope = scope.Parent!;
                }
            }

            public override void VisitWhileStatement(WhileStatement node)
            {
                Visit(node.Condition);

                whileCounter++;
                scope = scope.CreateNested($"WHILE#{whileCounter}");
                Visit(node.Block);
                scope = scope.Parent!;
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
