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
        public static (DiagnosticsReport, SymbolTable) Visit(Root root, string filePath)
        {
            var diagnostics = new DiagnosticsReport();
            var symbols = new SymbolTable();
            AddBuiltIns(symbols);
            new FirstPass(diagnostics, filePath, symbols).Run(root);
            new SecondPass(diagnostics, filePath, symbols).Run(root);

            return (diagnostics, symbols);
        }

        private static void AddBuiltIns(SymbolTable symbols)
        {
            var fl = new BasicType(BasicTypeCode.Float);
            symbols.Add(new TypeSymbol("INT", SourceRange.Unknown, new BasicType(BasicTypeCode.Int)));
            symbols.Add(new TypeSymbol("FLOAT", SourceRange.Unknown, fl));
            symbols.Add(new TypeSymbol("BOOL", SourceRange.Unknown, new BasicType(BasicTypeCode.Bool)));
            symbols.Add(new TypeSymbol("STRING", SourceRange.Unknown, new BasicType(BasicTypeCode.String)));
            symbols.Add(new TypeSymbol("VEC3", SourceRange.Unknown, new StructType("VEC3",
                                                                                    new Field(fl, "x"),
                                                                                    new Field(fl, "y"),
                                                                                    new Field(fl, "z"))));
        }

        private abstract class Pass : AstVisitor
        {
            public DiagnosticsReport Diagnostics { get; set; }
            public string FilePath { get; set; }
            public SymbolTable Symbols { get; set; }

            public Pass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                => (Diagnostics, FilePath, Symbols) = (diagnostics, filePath, symbols);

            public void Run(Root root)
            {
                root.Accept(this);
                OnEnd();
            }

            protected virtual void OnEnd() { }

            protected Type TryResolveType(string typeName, SourceRange source)
            {
                var unresolved = new UnresolvedType(typeName);
                var resolved = unresolved.Resolve(Symbols);
                if (resolved == null)
                {
                    Diagnostics.AddError(FilePath, $"Unknown type '{typeName}'", source);
                }

                return resolved ?? unresolved;
            }

            protected Type? TypeOf(Expression expr) => expr.Accept(new TypeOf(Diagnostics, FilePath, Symbols));
        }

        /// <summary>
        /// Register global symbols (structs, static variable, procedures and functions)
        /// </summary>
        private sealed class FirstPass : Pass
        {
            public FirstPass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                : base(diagnostics, filePath, symbols)
            { }

            protected override void OnEnd()
            {
                ResolveTypes();
            }

            // returns whether all types where resolved
            private bool ResolveTypes()
            {
                bool anyUnresolved = false;

                foreach (var symbol in Symbols.Symbols)
                {
                    switch (symbol)
                    {
                        case VariableSymbol s: s.Type = Resolve(s.Type, s.Source); break;
                        case FunctionSymbol s: ResolveFunc(s.Type, s.Source); break;
                        case TypeSymbol s when s.Type is StructType struc: ResolveStruct(struc, s.Source); break;
                        case TypeSymbol s when s.Type is FunctionType func: ResolveFunc(func, s.Source); break;
                    }
                }

                return !anyUnresolved;

                void ResolveStruct(StructType struc, SourceRange source)
                {
                    // TODO: be more specific with SourceRange for structs fields
                    for (int i = 0; i < struc.Fields.Count; i++)
                    {
                        var f = struc.Fields[i];
                        var newType = Resolve(f.Type, source);
                        if (IsCyclic(newType, struc))
                        {
                            Diagnostics.AddError(FilePath, $"Circular type reference in '{struc.Name}'", source);
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

                void ResolveFunc(FunctionType func, SourceRange source)
                {
                    // TODO: be more specific with SourceRange for funcs return type and parameters
                    if (func.ReturnType != null)
                    {
                        func.ReturnType = Resolve(func.ReturnType, source);
                    }
 
                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        func.Parameters[i] = Resolve(func.Parameters[i], source);
                    }
                }

                Type Resolve(Type t, SourceRange source)
                {
                    if (t is UnresolvedType u)
                    {
                        var newType = u.Resolve(Symbols);
                        if (newType == null)
                        {
                            Diagnostics.AddError(FilePath, $"Unknown type '{u.TypeName}'", source);
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
                var r = returnType != null ? new UnresolvedType(returnType.Name) : null;
                return new FunctionType(r, parameters.Select(p => new UnresolvedType(p.Type.Name)));
            }

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                Symbols.Add(new FunctionSymbol(node.Name, node.Source, CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters)));
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                Symbols.Add(new FunctionSymbol(node.Name, node.Source, CreateUnresolvedFunctionType(null, node.ParameterList.Parameters)));
            }

            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters);

                Symbols.Add(new TypeSymbol(node.Name, node.Source, func));
            }

            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(null, node.ParameterList.Parameters);

                Symbols.Add(new TypeSymbol(node.Name, node.Source, func));
            }

            public override void VisitStaticVariableStatement(StaticVariableStatement node)
            {
                Debug.Assert(node.Variable.Initializer == null, "Static initializers are not supported");

                // TODO: allocate static variables
                Symbols.Add(new VariableSymbol(node.Variable.Declaration.Name,
                                               node.Source,
                                               new UnresolvedType(node.Variable.Declaration.Type.Name),
                                               VariableKind.Static));
            }

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructType(node.Name, node.FieldList.Fields.Select(f => new Field(new UnresolvedType(f.Declaration.Type.Name), f.Declaration.Name)));

                Symbols.Add(new TypeSymbol(node.Name, node.Source, struc));
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
        /// Register local symbols inside procedures/functions and check that expressions types are correct.
        /// </summary>
        private sealed class SecondPass : Pass
        {
            private FunctionSymbol? func = null;
            private int funcLocalsSize = 0;
            private int funcLocalArgsSize = 0; 
            private int funcAllocLocation = 0;

            public SecondPass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                : base(diagnostics, filePath, symbols)
            { }

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                var funcSymbol = Symbols.Lookup(node.Name) as FunctionSymbol;
                Debug.Assert(funcSymbol != null);

                VisitFunc(funcSymbol, node.ParameterList, node.Block);
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                var funcSymbol = Symbols.Lookup(node.Name) as FunctionSymbol;
                Debug.Assert(funcSymbol != null);

                VisitFunc(funcSymbol, node.ParameterList, node.Block);
            }

            private void VisitFunc(FunctionSymbol func, ParameterList parameters, StatementBlock block)
            {
                this.func = func;
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

            public override void VisitParameterList(ParameterList node)
            {
                foreach (var p in node.Parameters)
                {
                    var v = new VariableSymbol(p.Name,
                                               p.Source,
                                               TryResolveType(p.Type.Name, p.Type.Source),
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
                var v = new VariableSymbol(node.Variable.Declaration.Name,
                                           node.Source,
                                           TryResolveType(node.Variable.Declaration.Type.Name, node.Variable.Declaration.Type.Source),
                                           VariableKind.Local)
                {
                    Location = funcAllocLocation,
                };

                if (node.Variable.Initializer != null)
                {
                    var initializerType = TypeOf(node.Variable.Initializer);
                    if (initializerType != null && initializerType != v.Type)
                    {
                        Diagnostics.AddError(FilePath, $"Mismatched initializer type and type of variable '{v.Name}'", node.Variable.Initializer.Source);
                    }
                }

                int size = v.Type.SizeOf;
                funcAllocLocation += size;
                funcLocalsSize += size;
                Symbols.Add(v);
            }

            public override void VisitAssignmentStatement(AssignmentStatement node)
            {
                var destType = TypeOf(node.Left);
                var srcType = TypeOf(node.Right);
            
                if (destType == null || srcType == null)
                {
                    return;
                }

                if (destType != srcType)
                {
                    Diagnostics.AddError(FilePath, "Mismatched types in assigment", node.Source);
                }
            }

            public override void VisitIfStatement(IfStatement node)
            {
                var conditionType = TypeOf(node.Condition);
                if (!(conditionType is BasicType { TypeCode: BasicTypeCode.Bool }))
                {
                    Diagnostics.AddError(FilePath, $"IF statement condition requires BOOL type", node.Condition.Source);
                }

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
                var conditionType = TypeOf(node.Condition);
                if (!(conditionType is BasicType { TypeCode: BasicTypeCode.Bool }))
                {
                    Diagnostics.AddError(FilePath, $"WHILE statement condition requires BOOL type", node.Condition.Source);
                }

                Symbols = Symbols.EnterScope(node.Block);
                node.Block.Accept(this);
                Symbols = Symbols.ExitScope();
            }

            public override void VisitReturnStatement(ReturnStatement node)
            {
                Debug.Assert(func != null);

                if (node.Expression == null)
                {
                    // if this is a function, the missing expression should have been reported by SyntaxChecker
                    return;
                }

                var returnType = TypeOf(node.Expression);
                if (returnType != func.Type.ReturnType)
                {
                    Diagnostics.AddError(FilePath, $"Returned type does not match the specified function return type", node.Expression.Source);
                }
            }

            public override void VisitInvocationStatement(InvocationStatement node)
            {
                // TODO: very similar to TypeOf.VisitInvocationExpression, refactor
                var callableType = TypeOf(node.Expression);
                if (!(callableType is FunctionType f))
                {
                    if (callableType != null)
                    {
                        Diagnostics.AddError(FilePath, $"Cannot call '{node.Expression}', it is not a procedure or a function", node.Expression.Source);
                    }
                    return;
                }

                int expected = f.Parameters.Count;
                int found = node.ArgumentList.Arguments.Length;
                if (found != expected)
                {
                    Diagnostics.AddError(FilePath, $"Mismatched number of arguments. Expected {expected}, found {found}", node.ArgumentList.Source);
                }

                int argCount = Math.Min(expected, found);
                for (int i = 0; i < argCount; i++)
                {
                    var expectedType = f.Parameters[i];
                    var foundType = TypeOf(node.ArgumentList.Arguments[i]);

                    if (expectedType != foundType)
                    {
                        Diagnostics.AddError(FilePath, $"Mismatched type of argument #{i}", node.ArgumentList.Arguments[i].Source);
                    }
                }
            }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    n.Accept(this);
                }
            }
        }

        private sealed class TypeOf : AstVisitor<Type?>
        {
            private readonly DiagnosticsReport diagnostics;
            private readonly string filePath;
            private readonly SymbolTable symbols;

            public TypeOf(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                => (this.diagnostics, this.filePath, this.symbols) = (diagnostics, filePath, symbols);

            public override Type? VisitIdentifierExpression(IdentifierExpression node)
            {
                var symbol = symbols.Lookup(node.Identifier);
                if (symbol == null)
                {
                    diagnostics.AddError(filePath, $"Unknown symbol '{node.Identifier}'", node.Source);
                    return null;
                }

                if (symbol is VariableSymbol v)
                {
                    return v.Type;
                }
                else if (symbol is FunctionSymbol f)
                {
                    return f.Type;
                }

                diagnostics.AddError(filePath, $"Identifier '{node.Identifier}' must refer to a variable, procedure or function", node.Source);
                return null;
            }

            public override Type? VisitLiteralExpression(LiteralExpression node)
                => node.Kind switch
                {
                    LiteralKind.Int => (symbols.Lookup("INT") as TypeSymbol)!.Type,
                    LiteralKind.Float => (symbols.Lookup("FLOAT") as TypeSymbol)!.Type,
                    LiteralKind.Bool => (symbols.Lookup("BOOL") as TypeSymbol)!.Type,
                    LiteralKind.String => (symbols.Lookup("STRING") as TypeSymbol)!.Type,
                    _ => null,
                };

            public override Type? VisitUnaryExpression(UnaryExpression node)
                => node.Operand.Accept(this);

            public override Type? VisitBinaryExpression(BinaryExpression node)
            {
                var left = node.Left.Accept(this);
                var right = node.Right.Accept(this);

                if (left == null)
                {
                    return null;
                }

                if (right == null)
                {
                    return null;
                }

                // TODO: allow some conversions (e.g. INT -> FLOAT, aggregate -> struct)
                if (left != right)
                {
                    diagnostics.AddError(filePath, $"Mismatched type in binary operation '{BinaryExpression.OpToString(node.Op)}'", node.Source);
                    return null;
                }

                return left;
            }

            public override Type? VisitInvocationExpression(InvocationExpression node)
            {
                var callableType = node.Expression.Accept(this);
                if (!(callableType is FunctionType f))
                {
                    if (callableType != null)
                    {
                        diagnostics.AddError(filePath, $"Cannot call '{node.Expression}', it is not a procedure or a function", node.Expression.Source);
                    }
                    return null;
                }

                int expected = f.Parameters.Count;
                int found = node.ArgumentList.Arguments.Length;
                if (found != expected)
                {
                    diagnostics.AddError(filePath, $"Mismatched number of arguments. Expected {expected}, found {found}", node.ArgumentList.Source);
                }

                int argCount = Math.Min(expected, found);
                for (int i = 0; i < argCount; i++)
                {
                    var expectedType = f.Parameters[i];
                    var foundType = node.ArgumentList.Arguments[i].Accept(this);

                    if (expectedType != foundType)
                    {
                        diagnostics.AddError(filePath, $"Mismatched type of argument #{i}", node.ArgumentList.Arguments[i].Source);
                    }
                }

                return f.ReturnType;
            }

            public override Type? VisitMemberAccessExpression(MemberAccessExpression node)
            {
                var type = node.Expression.Accept(this);
                if (!(type is StructType struc))
                {
                    if (type != null)
                    {
                        diagnostics.AddError(filePath, "Only structs have members", node.Expression.Source);
                    }
                    return null;
                }

                var field = struc.Fields.SingleOrDefault(f => f.Name == node.Member);
                if (field == default)
                {
                    diagnostics.AddError(filePath, $"Unknown field '{struc.Name}.{node.Member}'", node.Source);
                    return null;
                }

                return field.Type;
            }

            // TODO: VisitAggregateExpression
            // TODO: VisitArrayAccessExpression

            public override Type? DefaultVisit(Node node) => throw new InvalidOperationException($"Unsupported AST node {node.GetType().Name}");
        }
    }
}
