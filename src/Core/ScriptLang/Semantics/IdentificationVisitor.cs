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
    /// <item>Associates <see cref="GotoStatement"/>s to their corresponding <see cref="LabelDeclaration"/>.</item>
    /// </list>
    /// </summary>
    public sealed class IdentificationVisitor : DFSVisitor
    {
        private readonly Stack<IBreakableStatement> breakableStatements = new();
        private LabelSymbolTable? currFuncLabels;

        public DiagnosticsReport Diagnostics { get; }
        public ScopeSymbolTable Symbols { get; }

        private IdentificationVisitor(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols) = (diagnostics, new(symbols));

        public override Void Visit(FuncDeclaration node, Void param)
        {
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
                    Diagnostics.AddError($"Struct field '{field.Name}' is already declared", field.Source);
                }

                field.Accept(this, param);
            }

            return DefaultReturn;
        }

        public override Void Visit(VarDeclaration node, Void param)
        {
            node.Type.Accept(this, param);
            node.Initializer?.Accept(this, param);

            if (node.Kind is VarKind.Local or VarKind.Parameter)
            {
                if (!Symbols.AddValue(node))
                {
                    Diagnostics.AddError($"Symbol '{node.Name}' is already declared", node.Source);
                }
            }

            return DefaultReturn;
        }

        public override Void Visit(ValueDeclRefExpression node, Void param)
        {
            Debug.Assert(node.Declaration is null); // verify we are not visiting the same node multiple times

            var valueDecl = Symbols.FindValue(node.Name);
            if (valueDecl is null)
            {
                node.Declaration = new ErrorDeclaration(node.Source, Diagnostics, $"Unknown symbol '{node.Name}'");
            }
            else
            {
                node.Declaration = valueDecl;
            }

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
            node.Body.ForEach(stmt => stmt.Accept(this, param));
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
            node.Body.ForEach(stmt => stmt.Accept(this, param));
            breakableStatements.Pop();
            Symbols.PopScope();

            return DefaultReturn;
        }

        public override Void Visit(BreakStatement node, Void param)
        {
            Debug.Assert(node.EnclosingStatement is null);

            if (breakableStatements.Count == 0)
            {
                node.EnclosingStatement = new ErrorStatement(node.Source, Diagnostics, "No enclosing loop or switch out of which to break");
            }
            else
            {
                node.EnclosingStatement = breakableStatements.Peek();
            }

            return DefaultReturn;
        }

        public override Void Visit(GotoStatement node, Void param)
        {
            Debug.Assert(node.Label is null);

            var label = currFuncLabels?.FindLabel(node.LabelName);
            if (label is null)
            {
                node.Label = new ErrorDeclaration(node.Source, Diagnostics, $"Unknown label '{node.LabelName}'");
            }
            else
            {
                node.Label = label;
            }

            return DefaultReturn;
        }

        public override Void Visit(NamedType node, Void param)
        {
            Debug.Assert(node.ResolvedType is null); // verify we are not visiting the same node multiple times

            var typeDecl = Symbols.GlobalSymbols.FindType(node.Name);
            if (typeDecl is null)
            {
                node.ResolvedType = new ErrorType(node.Source, Diagnostics, $"Unknown type '{node.Name}'");
            }
            else
            {
                node.ResolvedType = typeDecl.CreateType(node.Source);
            }

            return DefaultReturn;
        }

        public static void Visit(Program root, DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
        {
            root.Accept(new IdentificationVisitor(diagnostics, symbols), default);
            root.Accept(new RemoveNamedTypesVisitor(), default);
        }
    }
}
