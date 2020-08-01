#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class ScopeBuilder
    {
        public static (Scope RootScope, Diagnostics Diagnostics) Explore(Root root, string filePath)
        {
            var d = new Diagnostics();
            var v1 = new FirstPass(root, filePath, d);
            root.Accept(v1);
            var v2 = new SecondPass(root, filePath, v1.RootScope, d);
            root.Accept(v2);
            return (v1.RootScope, d);
        }

        private abstract class Pass : AstVisitor
        {
            public Root Root { get; }
            public string FilePath { get; }
            public Scope RootScope { get; }
            public Diagnostics Diagnostics { get; }
            protected Scope Scope { get; set; }

            public Pass(Root root, string filePath, Scope rootScope, Diagnostics diagnostics)
                => (Root, FilePath, RootScope, Scope, Diagnostics) = (root, filePath, rootScope, rootScope, diagnostics);

            protected void Error(string message, Node node) => Diagnostics.AddError(FilePath, message, node.Source);
            protected void RepeatedSymbol(string name, Node node) => Error($"Symbol '{name}' already exists", node);
            protected void UnknownSymbol(string name, Node node) => Error($"Unknown symbol '{name}'", node);
            protected void UnknownType(string name, Node node) => Error($"Unknown type '{name}'", node);

            protected void AddIfNotRepeated(ISymbol symbol, Node node)
            {
                if (Scope.Exists(symbol.Name))
                {
                    RepeatedSymbol(symbol.Name, node);
                }
                else
                {
                    Scope.Add(symbol);
                }
            }
        }

        /// <summary>
        /// Explore symbols in global scope.
        /// </summary>
        private sealed class FirstPass : Pass
        {
            public FirstPass(Root root, string filePath, Diagnostics diagnostics) : base(root, filePath, Scope.CreateRoot(), diagnostics) { }

            public override void VisitProcedureStatement(ProcedureStatement node)
                => AddIfNotRepeated(new ProcedureSymbol(node, Scope.CreateNested($"PROC#{node.Name.Name}")), node);

            public override void VisitFunctionStatement(FunctionStatement node)
                => AddIfNotRepeated(new FunctionSymbol(node, Scope.CreateNested($"FUNC#{node.Name.Name}")), node);

            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node)
                => AddIfNotRepeated(new ProcedurePrototypeSymbol(node, Scope.CreateNested($"PROTO-PROC#{node.Name.Name}")), node);

            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node)
                => AddIfNotRepeated(new FunctionPrototypeSymbol(node, Scope.CreateNested($"PROTO-FUNC#{node.Name.Name}")), node);

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructSymbol(node, Scope.CreateNested($"STRUCT#{node.Name.Name}"));
                AddIfNotRepeated(struc, node);

                Scope = struc.Scope;
                DefaultVisit(node);
                Scope = Scope.Parent!;
            }

            public override void VisitStaticFieldStatement(StaticFieldStatement node)
                => AddIfNotRepeated(new StaticFieldSymbol(node.Variable), node);

            public override void VisitStructFieldList(StructFieldList node)
            {
                foreach (var pair in node.Fields.Select(p => (Node: p, Symbol: new StructFieldSymbol(p))))
                {
                    AddIfNotRepeated(pair.Symbol, pair.Node);
                }
            }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    Visit(n);
                }
            }
        }

        /// <summary>
        /// Explore procedures and funcitons statement blocks.
        /// </summary>
        private sealed class SecondPass : Pass
        {
            private int ifCounter = 0;
            private int whileCounter = 0;

            public SecondPass(Root root, string filePath, Scope rootScope, Diagnostics diagnostics) : base(root, filePath, rootScope, diagnostics) { }

            private void ResetIfWhileCounters() => (ifCounter, whileCounter) = (0, 0);

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                bool found = Scope.TryFind(node.Name.Name, out var procSymbol);
                Debug.Assert(found);

                if (procSymbol is ProcedureSymbol proc) // may be null if there is a symbol with the same name as this procedure
                {
                    ResetIfWhileCounters();
                    Scope = proc.Scope;
                    DefaultVisit(node);
                    Scope = Scope.Parent!;
                }
            }


            public override void VisitFunctionStatement(FunctionStatement node)
            {
                bool found = Scope.TryFind(node.Name.Name, out var funcSymbol);
                Debug.Assert(found);

                if (funcSymbol is FunctionSymbol func) // may be null if there is a symbol with the same name as this function
                {
                    ResetIfWhileCounters();
                    Scope = func.Scope;
                    DefaultVisit(node);
                    Scope = Scope.Parent!;
                }
            }

            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node)
            {
                bool found = Scope.TryFind(node.Name.Name, out var protoSymbol);
                Debug.Assert(found);

                if (protoSymbol is ProcedurePrototypeSymbol proto)
                {
                    Scope = proto.Scope;
                    DefaultVisit(node);
                    Scope = Scope.Parent!;
                }
            }

            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node)
            {
                bool found = Scope.TryFind(node.Name.Name, out var protoSymbol);
                Debug.Assert(found);

                if (protoSymbol is FunctionPrototypeSymbol proto)
                {
                    Scope = proto.Scope;
                    DefaultVisit(node);
                    Scope = Scope.Parent!;
                }
            }

            public override void VisitParameterList(ParameterList node)
            {
                foreach (var n in node.Parameters)
                {
                    Visit(n);
                }

                foreach (var pair in node.Parameters.Select(p => (Node: p, Symbol: new ParameterSymbol(p))))
                {
                    AddIfNotRepeated(pair.Symbol, pair.Node);
                }
            }

            public override void VisitVariableDeclarationStatement(VariableDeclarationStatement node)
            {
                Visit(node.Variable.Declaration);

                if (node.Variable.Initializer != null)
                {
                    Visit(node.Variable.Initializer);
                }

                var symbol = new LocalSymbol(node.Variable);
                AddIfNotRepeated(symbol, node);
            }

            public override void VisitIfStatement(IfStatement node)
            {
                Visit(node.Condition);

                ifCounter++;
                Scope = Scope.CreateNested($"IF#{ifCounter}-THEN");
                Visit(node.ThenBlock);
                Scope = Scope.Parent!;

                if (node.ElseBlock != null)
                {
                    Scope = Scope.CreateNested($"IF#{ifCounter}-ELSE");
                    Visit(node.ElseBlock);
                    Scope = Scope.Parent!;
                }
            }

            public override void VisitWhileStatement(WhileStatement node)
            {
                Visit(node.Condition);

                whileCounter++;
                Scope = Scope.CreateNested($"WHILE#{whileCounter}");
                Visit(node.Block);
                Scope = Scope.Parent!;
            }

            public override void VisitIdentifierExpression(IdentifierExpression node)
            {
                if (!Scope.TryFind(node.Identifier.Name, out _))
                {
                    UnknownSymbol(node.Identifier.Name, node);
                }
            }

            public override void VisitType(Ast.Type node)
            {
                if (!Scope.TryFind(node.Name.Name, out _))
                {
                    UnknownType(node.Name.Name, node);
                }
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
