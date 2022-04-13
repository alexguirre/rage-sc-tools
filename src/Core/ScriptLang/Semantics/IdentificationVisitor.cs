namespace ScTools.ScriptLang.Semantics
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.SymbolTables;

    /// <summary>
    /// Traverses the whole AST to identify symbols and associate them with their declarations, taking scopes into account.
    /// 
    /// <list type="bullet">
    /// <item>Resolves and removes <see cref="NamedType"/>s.</item>
    /// <item>Associates <see cref="ValueDeclRefExpression"/>s to their corresponding <see cref="IValueDeclaration"/>.</item>
    /// <item>Associates <see cref="BreakStatement"/>s to the closest enclosing <see cref="IBreakableStatement"/> (i.e. the enclosing loop or switch).</item>
    /// <item>Associates <see cref="ContinueStatement"/>s to the closest enclosing <see cref="ILoopStatement"/>.</item>
    /// <item>Associates <see cref="GotoStatement"/>s to their corresponding <see cref="LabelDeclaration"/>.</item>
    /// </list>
    /// </summary>
    public sealed class IdentificationVisitor : DFSVisitor
    {
        private readonly Stack<IBreakableStatement> breakableStatements = new();
        private readonly Stack<ILoopStatement> loopStatements = new();
        private LabelSymbolTable? currFuncLabels;

        public DiagnosticsReport Diagnostics { get; }
        public ScopeSymbolTable Symbols { get; }
        public NativeDB NativeDB { get; }

        private IdentificationVisitor(DiagnosticsReport diagnostics, GlobalSymbolTable symbols, NativeDB nativeDB)
            => (Diagnostics, Symbols, NativeDB) = (diagnostics, new(symbols), nativeDB);

        public override Void Visit(FuncDeclaration node, Void param)
        {
            if (node.Prototype.Kind is FuncKind.Native && NativeDB.FindOriginalHash(node.Name) == null)
            {
                Diagnostics.AddError($"Unknown native '{node.Name}'", node.Location);
            }

            currFuncLabels = LabelSymbolTableBuilder.Build(node, Diagnostics);
            Symbols.PushScope();
            base.Visit(node, param);
            Symbols.PopScope();
            currFuncLabels = null;

            return DefaultReturn;
        }

        public override Void Visit(FuncProtoDeclaration node, Void param)
        {
            if (!Symbols.HasScope)
            {
                // We are not inside a scope yet, so we are visiting a user-defined PROTO declaration
                // (not a compiler generated one from a FuncDeclaration).
                // Open a new scope to visit the parameters of the PROTO declaration.
                Symbols.PushScope();
                base.Visit(node, param);
                Symbols.PopScope();
            }
            else
            {
                base.Visit(node, param);
            }

            return DefaultReturn;
        }

        public override Void Visit(StructDeclaration node, Void param)
        {
            var fieldNames = new HashSet<string>(node.Fields.Count);
            foreach (var field in node.Fields)
            {
                if (!fieldNames.Add(field.Name))
                {
                    Diagnostics.AddError($"Struct field '{field.Name}' is already declared", field.Location);
                }

                field.Accept(this, param);
            }

            return DefaultReturn;
        }

        public override Void Visit(VarDeclaration node, Void param)
        {
            node.Type.Accept(this, param);
            node.Initializer?.Accept(this, param);

            if (node.Kind is VarKind.Local or VarKind.Parameter or VarKind.ScriptParameter)
            {
                if (!Symbols.AddValue(node))
                {
                    Diagnostics.AddError($"Symbol '{node.Name}' is already declared", node.Location);
                }
            }

            return DefaultReturn;
        }

        public override Void Visit(NameExpression node, Void param)
        {
            Debug.Assert(node.Semantics.Declaration is null); // verify we are not visiting the same node multiple times

            var decl = Symbols.FindValue(node.Name) ?? Symbols.GlobalSymbols.FindType(node.Name) as IDeclaration;
            node.Semantics = node.Semantics with
            {
                Declaration = decl ?? new ErrorDeclaration(node.Location, Diagnostics, $"Unknown symbol '{node.Name}'")
            };

            return DefaultReturn;
        }

        public override Void Visit(IfStatement node, Void param)
        {
            node.Condition.Accept(this, param);

            Symbols.PushScope();
            node.Then.ForEach(stmt => stmt.Accept(this, param));
            Symbols.PopScope();

            Symbols.PushScope();
            node.Else.ForEach(stmt => stmt.Accept(this, param));
            Symbols.PopScope();

            return DefaultReturn;
        }

        public override Void Visit(RepeatStatement node, Void param)
        {
            node.Limit.Accept(this, param);
            node.Counter.Accept(this, param);

            Symbols.PushScope();
            breakableStatements.Push(node);
            loopStatements.Push(node);
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            loopStatements.Pop();
            breakableStatements.Pop();
            Symbols.PopScope();

            return DefaultReturn;
        }

        public override Void Visit(SwitchStatement node, Void param)
        {
            node.Expression.Accept(this, param);

            Symbols.PushScope();
            breakableStatements.Push(node);
            node.Cases.ForEach(c => c.Accept(this, param));
            breakableStatements.Pop();
            Symbols.PopScope();

            return DefaultReturn;
        }

        public override Void Visit(WhileStatement node, Void param)
        {
            node.Condition.Accept(this, param);

            Symbols.PushScope();
            breakableStatements.Push(node);
            loopStatements.Push(node);
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            loopStatements.Pop();
            breakableStatements.Pop();
            Symbols.PopScope();

            return DefaultReturn;
        }

        public override Void Visit(BreakStatement node, Void param)
        {
            Debug.Assert(node.Semantics.EnclosingStatement is null);

            node.Semantics = new(EnclosingStatement: breakableStatements.Count != 0 ? breakableStatements.Peek() : new ErrorStatement(node.Location, Diagnostics, "BREAK statement not in loop or switch"));

            return DefaultReturn;
        }

        public override Void Visit(ContinueStatement node, Void param)
        {
            Debug.Assert(node.Semantics.EnclosingLoop is null);

            node.Semantics = new(EnclosingLoop: loopStatements.Count != 0 ? loopStatements.Peek() : new ErrorStatement(node.Location, Diagnostics, "CONTINUE statement not in loop"));

            return DefaultReturn;
        }

        public override Void Visit(GotoStatement node, Void param)
        {
            Debug.Assert(node.Label is null);

            var labeledStmt = currFuncLabels?.FindLabeledStatement(node.TargetLabel);
            node.Semantics = new(Target: labeledStmt ?? new ErrorStatement(node.Location, Diagnostics, $"Unknown label '{node.TargetLabel}'"));

            return DefaultReturn;
        }

        public override Void Visit(NamedType node, Void param)
        {
            Debug.Assert(node.ResolvedType is null); // verify we are not visiting the same node multiple times

            var typeDecl = Symbols.GlobalSymbols.FindType(node.Name);
            if (typeDecl is null)
            {
                node.ResolvedType = new ErrorType(node.Location, Diagnostics, $"Unknown type '{node.Name}'");
            }
            else
            {
                node.ResolvedType = typeDecl.CreateType(node.Location);
            }

            return DefaultReturn;
        }

        public static void Visit(Program root, DiagnosticsReport diagnostics, GlobalSymbolTable symbols, NativeDB nativeDB)
        {
            root.Accept(new IdentificationVisitor(diagnostics, symbols, nativeDB), default);
            root.Accept(new RemoveNamedTypesVisitor(), default);
        }
    }
}
