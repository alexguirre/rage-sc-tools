#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static class SemanticAnalysis
    {
        public static (DiagnosticsReport, SymbolTable) Visit(Root root)
        {
            var diagnostics = new DiagnosticsReport();
            var symbols = new SymbolTable();
            AddBuiltIns(symbols);
            var pass1 = new FirstPass(diagnostics, symbols);
            root.Accept(pass1);
            bool allTypesInGlobalScopeResolved = pass1.ResolveTypes();

            var pass2 = new SecondPass(diagnostics, symbols);
            root.Accept(pass2);

            return (diagnostics, symbols);
        }

        private static void AddBuiltIns(SymbolTable symbols)
        {
            var fl = new BasicType(BasicTypeCode.Float);
            symbols.Add(new TypeSymbol("INT", new BasicType(BasicTypeCode.Int)));
            symbols.Add(new TypeSymbol("FLOAT", fl));
            symbols.Add(new TypeSymbol("BOOL", new BasicType(BasicTypeCode.Bool)));
            symbols.Add(new TypeSymbol("STRING", new BasicType(BasicTypeCode.String)));
            symbols.Add(new TypeSymbol("VEC3", new StructType("VEC3",
                                                              new Field(fl, "x"),
                                                              new Field(fl, "y"),
                                                              new Field(fl, "z"))));
        }

        /// <summary>
        /// Register global symbols (structs, static variable, procedures and functions)
        /// </summary>
        private sealed class FirstPass : AstVisitor
        {
            public DiagnosticsReport Diagnostics { get; set; }
            public SymbolTable Symbols { get; set; }

            public FirstPass(DiagnosticsReport diagnostics, SymbolTable symbols)
                => (Diagnostics, Symbols) = (diagnostics, symbols);

            // returns whether all types where resolved
            public bool ResolveTypes()
            {
                bool anyUnresolved = false;

                foreach (var symbol in Symbols.Symbols)
                {
                    switch (symbol)
                    {
                        case VariableSymbol s: s.Type = Resolve(s.Type); break;
                        case FunctionSymbol s: ResolveFunc(s.Type); break;
                        case TypeSymbol s when s.Type is StructType struc: ResolveStruct(struc); break;
                        case TypeSymbol s when s.Type is FunctionType func: ResolveFunc(func); break;
                    }
                }

                return !anyUnresolved;

                void ResolveStruct(StructType struc)
                {
                    for (int i = 0; i < struc.Fields.Count; i++)
                    {
                        var f = struc.Fields[i];
                        var newType = Resolve(f.Type);
                        if (IsCyclic(newType, struc))
                        {
                            Diagnostics.AddError("REPLACEME.sc", $"Circular type reference in '{struc.Name}'", SourceRange.Unknown);
                            anyUnresolved |= true;
                        }
                        else
                        {
                            struc.Fields[i] = new Field(newType, f.Name);
                        }
                    }

                    static bool IsCyclic(Type t, StructType orig)
                    {
                        if (t == orig)
                        {
                            return true;
                        }
                        else if (t is StructType s)
                        {
                            return s.Fields.Any(f => IsCyclic(f.Type, orig));
                        }

                        return false;
                    }
                }

                void ResolveFunc(FunctionType func)
                {
                    if (func.ReturnType != null)
                    {
                        func.ReturnType = Resolve(func.ReturnType);
                    }
 
                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        func.Parameters[i] = Resolve(func.Parameters[i]);
                    }
                }

                Type Resolve(Type t)
                {
                    if (t is UnresolvedType u)
                    {
                        var newType = u.Resolve(Symbols);
                        if (newType == null)
                        {
                            // TODO: include file path and source range in unknown type error
                            Diagnostics.AddError("REPLACEME.sc", $"Unknown type '{u.TypeName}'", SourceRange.Unknown);
                            anyUnresolved |= true;
                        }
                        else
                        {
                            return newType;
                        }
                    }

                    return t;
                }
            }

            private FunctionType CreateUnresolvedFunctionType(Ast.Type? returnType, IEnumerable<VariableDeclaration> parameters)
            {
                var r = returnType != null ? new UnresolvedType(returnType.Name.Name) : null;
                return new FunctionType(r, parameters.Select(p => new UnresolvedType(p.Type.Name.Name)));
            }

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                Symbols.Add(new FunctionSymbol(node.Name.Name, CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters)));
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                Symbols.Add(new FunctionSymbol(node.Name.Name, CreateUnresolvedFunctionType(null, node.ParameterList.Parameters)));
            }

            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters);

                Symbols.Add(new TypeSymbol(node.Name.Name, func));
            }

            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(null, node.ParameterList.Parameters);

                Symbols.Add(new TypeSymbol(node.Name.Name, func));
            }

            public override void VisitStaticVariableStatement(StaticVariableStatement node)
            {
                // TODO: allocate static variables
                Symbols.Add(new VariableSymbol(node.Variable.Declaration.Name.Name,
                                               new UnresolvedType(node.Variable.Declaration.Type.Name.Name),
                                               VariableKind.Static));
            }

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructType(node.Name.Name, node.FieldList.Fields.Select(f => new Field(new UnresolvedType(f.Declaration.Type.Name.Name), f.Declaration.Name.Name)));

                Symbols.Add(new TypeSymbol(node.Name.Name, struc));
            }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    n.Accept(this);
                }
            }
        }

        /// <summary>
        /// Register local symbols inside procedures/functions.
        /// </summary>
        private sealed class SecondPass : AstVisitor
        {
            public DiagnosticsReport Diagnostics { get; set; }
            public SymbolTable Symbols { get; set; }

            private int funcLocalsSize = 0;
            private int funcLocalArgsSize = 0; 
            private int funcAllocLocation = 0;

            public SecondPass(DiagnosticsReport diagnostics, SymbolTable symbols)
                => (Diagnostics, Symbols) = (diagnostics, symbols);

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                var funcSymbol = Symbols.Lookup(node.Name.Name) as FunctionSymbol;
                Debug.Assert(funcSymbol != null);

                VisitFunc(funcSymbol, node.ParameterList, node.Block);
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                var funcSymbol = Symbols.Lookup(node.Name.Name) as FunctionSymbol;
                Debug.Assert(funcSymbol != null);

                VisitFunc(funcSymbol, node.ParameterList, node.Block);
            }

            private void VisitFunc(FunctionSymbol func, ParameterList parameters, StatementBlock block)
            {
                funcLocalsSize = 0;
                funcLocalArgsSize = 0;
                funcAllocLocation = 0;

                Symbols = Symbols.EnterScope(block);
                parameters.Accept(this);
                block.Accept(this);
                Symbols = Symbols.ExitScope();

                func.LocalArgsSize = funcLocalArgsSize;
                func.LocalsSize = funcLocalsSize;
            }

            private Type TypeOf(string typeName)
            {
                var unresolved = new UnresolvedType(typeName);
                var resolved = unresolved.Resolve(Symbols);
                if (resolved == null)
                {
                    // TODO: include file path and source range in unknown type error
                    Diagnostics.AddError("REPLACEME.sc", $"Unknown type '{typeName}'", SourceRange.Unknown);
                }

                return resolved ?? unresolved;
            }

            public override void VisitParameterList(ParameterList node)
            {
                foreach (var p in node.Parameters)
                {
                    var v = new VariableSymbol(p.Name.Name,
                                               TypeOf(p.Type.Name.Name),
                                               VariableKind.LocalArgument)
                            {
                                Location = funcAllocLocation,
                            };
                    int size = v.Type.SizeOf;
                    funcAllocLocation += size;
                    funcLocalArgsSize += size;
                    Symbols.Add(v);
                }
                funcAllocLocation += 2; // space required by the game
            }

            public override void VisitVariableDeclarationStatement(VariableDeclarationStatement node)
            {
                var v = new VariableSymbol(node.Variable.Declaration.Name.Name,
                                           TypeOf(node.Variable.Declaration.Type.Name.Name),
                                           VariableKind.Local)
                {
                    Location = funcAllocLocation,
                };
                int size = v.Type.SizeOf;
                funcAllocLocation += size;
                funcLocalsSize += size;
                Symbols.Add(v);
            }

            public override void VisitIfStatement(IfStatement node)
            {
                node.Condition.Accept(this);

                Symbols = Symbols.EnterScope(node.ThenBlock);
                node.ThenBlock.Accept(this);
                Symbols = Symbols.ExitScope();

                if (node.ElseBlock != null)
                {
                    Symbols = Symbols.EnterScope(node.ElseBlock);
                    node.ElseBlock.Accept(this);
                    Symbols = Symbols.ExitScope();
                }
            }

            public override void VisitWhileStatement(WhileStatement node)
            {
                node.Condition.Accept(this);

                Symbols = Symbols.EnterScope(node.Block);
                node.Block.Accept(this);
                Symbols = Symbols.ExitScope();
            }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    n.Accept(this);
                }
            }
        }
    }
}
