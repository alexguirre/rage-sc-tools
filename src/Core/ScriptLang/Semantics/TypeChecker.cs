namespace ScTools.ScriptLang.Semantics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.BuiltIns;
    using ScTools.ScriptLang.SymbolTables;

    public readonly struct TypeCheckerContext
    {
        public bool SecondPass { get; init; }
        public StructDeclaration? Struct { get; init; }
        public FuncDeclaration? Function { get; init; }
        public HashSet<int?> SwitchHandledCases { get; init; }

        public TypeCheckerContext BeginSecondPass()
            => new() { SecondPass = true, Struct = Struct, Function = Function, SwitchHandledCases = SwitchHandledCases };

        public TypeCheckerContext Enter(StructDeclaration structDecl)
            => new() { SecondPass = SecondPass, Struct = structDecl, Function = Function, SwitchHandledCases = SwitchHandledCases };

        public TypeCheckerContext Enter(FuncDeclaration funcDecl)
            => new() { SecondPass = SecondPass, Struct = Struct, Function = funcDecl, SwitchHandledCases = SwitchHandledCases };

        public TypeCheckerContext Enter(SwitchStatement switchStmt)
            => new() { SecondPass = SecondPass, Struct = Struct, Function = Function, SwitchHandledCases = new(switchStmt.Cases.Count) };
    }

    public sealed class TypeChecker : DFSVisitor<Void, TypeCheckerContext>
    {
        public override Void DefaultReturn => default;

        public DiagnosticsReport Diagnostics { get; }
        public GlobalSymbolTable Symbols { get; }

        private readonly ExpressionTypeChecker exprTypeChecker;

        private TypeChecker(DiagnosticsReport diagnostics, GlobalSymbolTable symbols)
            => (Diagnostics, Symbols, exprTypeChecker) = (diagnostics, symbols, new(diagnostics, symbols));

        public override Void Visit(Program node, TypeCheckerContext param)
        {
            // visit all declarations to ensure that all global/static variables/function types are type-checked
            // before they are used inside functions
            node.Declarations.ForEach(decl => decl.Accept(this, param));

            // now visit the body of all functions
            param = param.BeginSecondPass();
            node.Declarations.Where(d => d is FuncDeclaration).ForEach(decl => decl.Accept(this, param));
            return DefaultReturn;
        }

        #region Expressions
        public override Void Visit(BinaryExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(BoolLiteralExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(FieldAccessExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(FloatLiteralExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(IndexingExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(IntLiteralExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(InvocationExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(NullExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(SizeOfExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(StringLiteralExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(UnaryExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(ValueDeclRefExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        public override Void Visit(VectorExpression node, TypeCheckerContext param) => node.Accept(exprTypeChecker, default);
        #endregion Expressions

        #region Types
        public override Void Visit(ArrayType node, TypeCheckerContext param)
        {
            node.ItemType.Accept(this, param);
            node.LengthExpression.Accept(this, param);

            if (!node.LengthExpression.IsConstant)
            {
                node.LengthExpression = new ErrorExpression(node.LengthExpression.Source, Diagnostics, $"Array size must be a constant expression");
            }
            else if (!BuiltInTypes.Int.CreateType(node.LengthExpression.Source).CanAssign(node.LengthExpression.Type!, node.LengthExpression.IsLValue))
            {
                Diagnostics.AddError($"Array size requires type '{BuiltInTypes.Int.Name}', found '{node.LengthExpression.Type}'", node.LengthExpression.Source);
            }
            else
            {
                node.Length = ExpressionEvaluator.EvalInt(node.LengthExpression, Symbols);
            }

            return DefaultReturn;
        }
        #endregion Types

        #region Declarations
        public override Void Visit(FuncDeclaration node, TypeCheckerContext param)
        {
            if (!param.SecondPass)
            {
                node.Prototype.Accept(this, param);
                node.Type.Accept(this, param);
            }
            else
            {
                param = param.Enter(node);
                node.Body.ForEach(stmt => stmt.Accept(this, param));
            }
            return DefaultReturn;
        }

        public override Void Visit(EnumMemberDeclaration node, TypeCheckerContext param)
        {
            // type-check for enum members is done by ConstantsResolver before TypeChecker is executed
            return DefaultReturn;
        }

        public override Void Visit(StructDeclaration node, TypeCheckerContext param)
            => base.Visit(node, param.Enter(node));

        public override Void Visit(StructField node, TypeCheckerContext param)
        {
            Debug.Assert(param.Struct is not null);

            node.Type.Accept(this, param);

            if (TypeHelper.IsOrContainsStruct(node.Type, param.Struct))
            {
                Diagnostics.AddError($"Struct field '{node.Name}' causes a cycle in the layout of '{param.Struct.Name}'", node.Source);
            }

            if (node.Initializer is not null)
            {
                node.Initializer.Accept(this, param);

                if (!node.Initializer.IsConstant)
                {
                    node.Initializer = new ErrorExpression(node.Initializer.Source, Diagnostics, $"Default initializer of the struct field '{node.Name}' must be a constant expression");
                }
                else
                {
                    node.Type.Assign(node.Initializer.Type!, node.Initializer.IsLValue, node.Initializer.Source, Diagnostics);
                }
            }

            return DefaultReturn;
        }

        public override Void Visit(VarDeclaration node, TypeCheckerContext param)
        {
            if (node.Kind is VarKind.Constant)
            {
                // type-check for CONST vars is done by ConstantsResolver before TypeChecker is executed
                return DefaultReturn;
            }

            node.Type.Accept(this, param);

            if (node.Kind is VarKind.Global or VarKind.ScriptParameter && !TypeHelper.IsCrossScriptThreadSafe(node.Type))
            {
                Diagnostics.AddError($"Type '{node.Type}' of {KindToString(node.Kind)} '{node.Name}' cannot be shared between script threads safely", node.Source);
            }

            if (node.Initializer is not null)
            {
                node.Initializer.Accept(this, param);

                if (node.Kind is VarKind.Global or VarKind.Static && !node.Initializer.IsConstant)
                {
                    node.Initializer = new ErrorExpression(node.Initializer.Source, Diagnostics, $"Initializer of {KindToString(node.Kind)} variable '{node.Name}' must be a constant expression");
                }
                else
                {
                    node.Type.Assign(node.Initializer.Type!, node.Initializer.IsLValue, node.Initializer.Source, Diagnostics);
                }
            }

            return DefaultReturn;

            static string KindToString(VarKind kind)
                => kind switch { VarKind.Global => "global variable", VarKind.Static => "static variable", VarKind.ScriptParameter => "SCRIPT parameter", _ => throw new System.NotImplementedException() };
    }
        #endregion Declarations

        #region Statements
        public override Void Visit(AssignmentStatement node, TypeCheckerContext param)
        {
            node.LHS.Accept(this, param);
            node.RHS.Accept(this, param);

            if (!node.LHS.IsLValue)
            {
                Diagnostics.AddError("The left-hand side of an assignment must be an lvalue", node.Source);
            }
            else
            {
                node.LHS.Type!.Assign(node.RHS.Type!, node.RHS.IsLValue, node.Source, Diagnostics);
            }

            return DefaultReturn;
        }

        public override Void Visit(IfStatement node, TypeCheckerContext param)
        {
            node.Condition.Accept(this, param);

            if (!BuiltInTypes.Bool.CreateType(node.Condition.Source).CanAssign(node.Condition.Type!, node.Condition.IsLValue))
            {
                Diagnostics.AddError($"IF condition requires type '{BuiltInTypes.Bool.Name}', found '{node.Condition.Type}'", node.Condition.Source);
            }

            node.Then.ForEach(stmt => stmt.Accept(this, param));
            node.Else.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(RepeatStatement node, TypeCheckerContext param)
        {
            node.Limit.Accept(this, param);
            node.Counter.Accept(this, param);

            var intTy = BuiltInTypes.Int.CreateType(node.Limit.Source);
            if (!intTy.CanAssign(node.Limit.Type!, node.Limit.IsLValue))
            {
                Diagnostics.AddError($"REPEAT limit requires type '{intTy}', found '{node.Limit.Type}'", node.Limit.Source);
            }

            if (!node.Counter.IsLValue)
            {
                Diagnostics.AddError($"REPEAT counter requires an lvalue reference of type '{intTy}', found non-lvalue '{node.Counter.Type}'", node.Counter.Source);
            }
            else if (!intTy.CanAssign(node.Counter.Type!, node.Counter.IsLValue))
            {
                Diagnostics.AddError($"REPEAT counter requires type '{intTy}', found '{node.Counter.Type}'", node.Counter.Source);
            }

            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(ReturnStatement node, TypeCheckerContext param)
        {
            Debug.Assert(param.Function is not null);

            node.Expression?.Accept(this, param);

            if (param.Function.Prototype.IsProc)
            {
                if (node.Expression is not null)
                {
                    Diagnostics.AddError($"No expression expected in RETURN statement inside procedure", node.Source);
                }
            }
            else
            {
                var returnTy = param.Function.Prototype.ReturnType;
                if (node.Expression is not null)
                {
                    if (!returnTy.CanAssign(node.Expression.Type!, node.Expression.IsLValue))
                    {
                        Diagnostics.AddError($"Function returns '{returnTy}', found '{node.Expression.Type}' in RETURN statement", node.Expression.Source);
                    }
                }
                else
                {
                    Diagnostics.AddError($"Expected expression in RETURN statement inside function returning '{returnTy}'", node.Source);
                }
            }

            return DefaultReturn;
        }

        public override Void Visit(SwitchStatement node, TypeCheckerContext param)
        {
            node.Expression.Accept(this, param);

            if (!BuiltInTypes.Int.CreateType(node.Expression.Source).CanAssign(node.Expression.Type!, node.Expression.IsLValue))
            {
                Diagnostics.AddError($"SWITCH expression requires type '{BuiltInTypes.Int.Name}', found '{node.Expression.Type}'", node.Expression.Source);
            }

            param = param.Enter(node);
            node.Cases.ForEach(c => c.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(ValueSwitchCase node, TypeCheckerContext param)
        {
            node.Value.Accept(this, param);

            if (!node.Value.IsConstant)
            {
                node.Value = new ErrorExpression(node.Value.Source, Diagnostics, $"CASE value must be a constant expression");
            }
            else if (!BuiltInTypes.Int.CreateType(node.Value.Source).CanAssign(node.Value.Type!, node.Value.IsLValue))
            {
                Diagnostics.AddError($"CASE value requires type '{BuiltInTypes.Int.Name}', found '{node.Value.Type}'", node.Value.Source);
            }
            else
            {
                var caseValue = ExpressionEvaluator.EvalInt(node.Value, Symbols);
                if (!param.SwitchHandledCases.Add(caseValue))
                {
                    Diagnostics.AddError($"CASE value '{caseValue}' is already handled", node.Value.Source);
                }
            }

            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(DefaultSwitchCase node, TypeCheckerContext param)
        {
            if (!param.SwitchHandledCases.Add(null))
            {
                Diagnostics.AddError($"More than one DEFAULT case", node.Source);
            }

            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }

        public override Void Visit(WhileStatement node, TypeCheckerContext param)
        {
            node.Condition.Accept(this, param);

            if (!BuiltInTypes.Bool.CreateType(node.Condition.Source).CanAssign(node.Condition.Type!, node.Condition.IsLValue))
            {
                Diagnostics.AddError($"WHILE condition requires type '{BuiltInTypes.Bool.Name}', found '{node.Condition.Type}'", node.Condition.Source);
            }

            node.Body.ForEach(stmt => stmt.Accept(this, param));
            return DefaultReturn;
        }
        #endregion Statements

        public static void Check(Program root, DiagnosticsReport diagnostics, GlobalSymbolTable globalSymbols)
        {
            new ConstantsResolver(diagnostics, globalSymbols).Resolve();

            root.Accept(new TypeChecker(diagnostics, globalSymbols), default);
        }
    }
}
