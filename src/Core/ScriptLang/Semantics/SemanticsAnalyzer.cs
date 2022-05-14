namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.BuiltIns;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

public sealed class SemanticsAnalyzer : Visitor
{
    // helpers
    private readonly TypeFactory typeFactory;
    private readonly ExpressionTypeChecker exprTypeChecker = new();

    // context
    private readonly SymbolTable<IDeclaration> symbols = new();
    private readonly SymbolTable<IStatement> labels = new();
    private readonly TypeRegistry typeRegistry = new();
    private EnumMemberDeclaration? previousEnumMember = null;
    private StructDeclaration? currentStructDeclaration = null;
    private TypeInfo? currentFunctionReturnType = null;
    private readonly Stack<IBreakableStatement> breakableStatements = new();
    private readonly Stack<ILoopStatement> loopStatements = new();
    private readonly List<GotoStatement> gotosToResolve = new();

    public DiagnosticsReport Diagnostics { get; }

    public SemanticsAnalyzer(DiagnosticsReport diagnostics)
    {
        Diagnostics = diagnostics;
        typeFactory = new(this);

        RegisterBuiltIns();
    }

    private void RegisterBuiltIns()
    {
        RegisterBuiltInTypes();
        RegisterIntrinsics();
    }

    private void RegisterBuiltInTypes()
    {
        AddSymbol(new BuiltInTypeDeclaration("ANY", AnyType.Instance));
        AddSymbol(new BuiltInTypeDeclaration("INT", IntType.Instance));
        AddSymbol(new BuiltInTypeDeclaration("FLOAT", FloatType.Instance));
        AddSymbol(new BuiltInTypeDeclaration("BOOL", BoolType.Instance));
        AddSymbol(new BuiltInTypeDeclaration("STRING", StringType.Instance));
        AddSymbol(new BuiltInTypeDeclaration("VECTOR", VectorType.Instance));

        HandleType.All.ForEach(h => AddSymbol(new BuiltInTypeDeclaration(HandleType.KindToTypeName(h.Kind), h)));

        TextLabelType.All64.ForEach(tl => AddSymbol(new BuiltInTypeDeclaration(TextLabelType.GetTypeNameForLength(tl.Length), tl)));
    }

    private void RegisterIntrinsics()
    {
        Intrinsics.All.ForEach(AddSymbol);
    }

    public override void Visit(CompilationUnit node)
    {
        node.Declarations.ForEach(decl => decl.Accept(this));
    }

    public override void Visit(EnumDeclaration node)
    {
        AddSymbol(node);

        node.Members.ForEach(m => m.Accept(this));
        previousEnumMember = null;
    }

    public override void Visit(EnumMemberDeclaration node)
    {
        Debug.Assert(node.Semantics.ValueType is not null); // Visit(EnumDeclaration) should have already set the type of its members

        var initializerType = node.Initializer?.Accept(exprTypeChecker, this) ?? ErrorType.Instance;

        ConstantValue? value = null;
        if (!initializerType.IsError)
        {
            Debug.Assert(node.Initializer is not null);
            if (IntType.Instance.IsAssignableFrom(initializerType) ||
                node.Semantics.ValueType.IsAssignableFrom(initializerType))
            {
                value = ConstantExpressionEvaluator.Eval(node.Initializer, this);
                value = ConstantValue.Int(value.IntValue); // force result type to INT (in case of NULL)
            }
            else
            {
                CannotConvertTypeError(initializerType, IntType.Instance, node.Initializer.Location);
            }
        }

        if (value is null)
        {
            // fallback to incremental numeration if there is no initializer or it had an error
            value = previousEnumMember is null ?
                ConstantValue.Int(0) :
                ConstantValue.Int(previousEnumMember.Semantics.ConstantValue!.IntValue + 1);
        }

        node.Semantics = node.Semantics with { ConstantValue = value };
        previousEnumMember = node;

        AddSymbol(node);
    }

    public override void Visit(ScriptDeclaration node)
    {
        AddSymbol(node);

        currentFunctionReturnType = VoidType.Instance;
        EnterFunctionScope();
        node.Parameters.ForEach(p => p.Accept(this));
        VisitBody(node.Body);
        ExitFunctionScope();
        currentFunctionReturnType = null;
    }

    public override void Visit(FunctionDeclaration node)
    {
        AddSymbol(node);

        currentFunctionReturnType = ((FunctionType)node.Semantics.ValueType!).Return;
        EnterFunctionScope();
        node.Parameters.ForEach(p => p.Accept(this));
        VisitBody(node.Body);
        ExitFunctionScope();
        currentFunctionReturnType = null;
    }

    public override void Visit(FunctionPointerTypeDeclaration node)
    {
        AddSymbol(node);

        EnterScope();
        node.Parameters.ForEach(p => p.Accept(this));
        ExitScope();
    }

    public override void Visit(NativeFunctionDeclaration node)
    {
        AddSymbol(node);

        EnterScope();
        node.Parameters.ForEach(p => p.Accept(this));
        ExitScope();
    }

    public override void Visit(GlobalBlockDeclaration node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(StructDeclaration node)
    {
        currentStructDeclaration = node;
        EnterScope();
        node.Fields.ForEach(f => f.Accept(this));
        ExitScope();
        currentStructDeclaration = null;

        AddSymbol(node);
    }

    public override void Visit(InvocationExpression node)
    {
        node.Accept(exprTypeChecker, this);
    }

    public override void Visit(VarDeclaration node)
    {
        var varType = typeFactory.GetFrom(node);

        // TODO: global variables

        switch (node.Kind)
        {
            case VarKind.Constant:
                if (!varType.IsError && varType is not (IntType or FloatType or BoolType or StringType or VectorType or EnumType))
                {
                    TypeNotAllowedInConstantError(node, varType);
                }
                else
                {
                    if (node.Initializer is null)
                    {
                        ConstantWithoutInitializerError(node);
                    }

                    CheckConstantInitializer(this, node);
                }
                break;
            case VarKind.Static:
                CheckRuntimeInitializer(this, node);
                break;
            case VarKind.Field:
                Debug.Assert(currentStructDeclaration is not null);
                CheckConstantInitializer(this, node);
                break;
            case VarKind.Local:
                CheckRuntimeInitializer(this, node);
                break;
            case VarKind.Parameter or VarKind.ScriptParameter:
                // parameters are not allowed initializers (error in parser phase), don't need to check the initializer
                // TODO: check if script parameter types are safe to use?
                break;
            default:
                throw new NotImplementedException($"Var kind '{node.Kind}' is not supported");
        }

        AddSymbol(node);


        static void CheckConstantInitializer(SemanticsAnalyzer s, VarDeclaration node)
        {
            var varType = node.Semantics.ValueType;
            Debug.Assert(varType is not null);
            var initializerType = node.Initializer?.Accept(s.exprTypeChecker, s) ?? ErrorType.Instance;

            ConstantValue? value = null;
            if (!varType.IsError && !initializerType.IsError)
            {
                Debug.Assert(node.Initializer is not null);
                if (!node.Initializer.ValueKind.Is(ValueKind.Constant))
                {
                    s.InitializerExpressionIsNotConstantError(node);
                }
                else if (varType.IsAssignableFrom(initializerType))
                {
                    value = ConstantExpressionEvaluator.Eval(node.Initializer, s);
                }
                else
                {
                    s.CannotConvertTypeError(initializerType, varType, node.Initializer.Location);
                }
            }

            node.Semantics = node.Semantics with { ConstantValue = value };
        }

        static void CheckRuntimeInitializer(SemanticsAnalyzer s, VarDeclaration node)
        {
            var varType = node.Semantics.ValueType;
            Debug.Assert(varType is not null);
            var initializerType = node.Initializer?.Accept(s.exprTypeChecker, s) ?? ErrorType.Instance;

            if (!varType.IsError && !initializerType.IsError)
            {
                Debug.Assert(node.Initializer is not null);
                if (!varType.IsAssignableFrom(initializerType))
                {
                    s.CannotConvertTypeError(initializerType, varType, node.Initializer.Location);
                }
            }

            node.Semantics = node.Semantics with { ConstantValue = null };
        }
    }

    public override void Visit(AssignmentStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(BreakStatement node)
    {
        if (breakableStatements.TryPeek(out var breakableStmt))
        {
            node.Semantics = node.Semantics with { EnclosingStatement = breakableStmt };
        }
        else
        {
            // TODO: report error "BREAK statement not in loop or switch"
        }
    }

    public override void Visit(ContinueStatement node)
    {
        if (loopStatements.TryPeek(out var loopStmt))
        {
            node.Semantics = node.Semantics with { EnclosingLoop = loopStmt };
        }
        else
        {
            // TODO: report error "CONTINUE statement not in loop"
        }
    }

    public override void Visit(EmptyStatement node)
    {
        // empty
    }

    public override void Visit(GotoStatement node)
    {
        // GOTOs targets will be resolved once we know all labels that exist in the current function before leaving its scope
        gotosToResolve.Add(node);
    }

    public override void Visit(IfStatement node)
    {
        var conditionType = node.Condition.Accept(exprTypeChecker, this);
        if (!conditionType.IsError && !BoolType.Instance.IsAssignableFrom(conditionType))
        {
            CannotConvertTypeError(conditionType, BoolType.Instance, node.Condition.Location);
        }

        EnterScope();
        VisitBody(node.Then);
        ExitScope();
        EnterScope();
        VisitBody(node.Else);
        ExitScope();
    }

    public override void Visit(RepeatStatement node)
    {
        throw new System.NotImplementedException();

        // TODO: type-check limit and counter
        EnterLoop(node);
        VisitBody(node.Body);
        ExitLoop(node);
    }

    public override void Visit(ReturnStatement node)
    {
        Debug.Assert(currentFunctionReturnType is not null);

        if (node.Expression is not null)
        {
            // returning some value

            if (currentFunctionReturnType is VoidType)
            {
                ValueReturnedFromProcedureError(node);
            }
            else
            {
                var exprTy = node.Expression.Accept(exprTypeChecker, this);
                if (!exprTy.IsError && !currentFunctionReturnType.IsAssignableFrom(exprTy))
                {
                    CannotConvertTypeError(exprTy, currentFunctionReturnType, node.Expression.Location);
                }
            }
        }
        else
        {
            // return without value

            if (currentFunctionReturnType is not VoidType)
            {
                ExpectedValueInReturnError(currentFunctionReturnType, node);
            }
        }
    }

    public override void Visit(SwitchStatement node)
    {
        throw new System.NotImplementedException();

        // TODO: type-check switch expression
        EnterBreakableStatement(node);
        node.Cases.ForEach(c => c.Accept(this));
        ExitBreakableStatement(node);
    }

    public override void Visit(ValueSwitchCase node)
    {
        throw new System.NotImplementedException();

        // TODO: type-check case value expression
        // TODO: ensure there the case value is not repeated
        VisitBody(node.Body);
    }

    public override void Visit(DefaultSwitchCase node)
    {
        throw new System.NotImplementedException();

        // TODO: ensure there is a single DEFAULT case
        VisitBody(node.Body);
    }

    public override void Visit(WhileStatement node)
    {
        var conditionType = node.Condition.Accept(exprTypeChecker, this);
        if (!conditionType.IsError && !BoolType.Instance.IsAssignableFrom(conditionType))
        {
            CannotConvertTypeError(conditionType, BoolType.Instance, node.Condition.Location);
        }

        EnterLoop(node);
        VisitBody(node.Body);
        ExitLoop(node);
    }

    public override void Visit(ErrorDeclaration node)
    {
        // empty
    }

    public override void Visit(ErrorStatement node)
    {
        // empty
    }

    private void EnterScope() => symbols.PushScope();
    private void ExitScope() => symbols.PopScope();

    private void EnterFunctionScope()
    {
        EnterScope();
        labels.PushScope();
        gotosToResolve.Clear();
    }

    private void ExitFunctionScope()
    {
        ResolveGotos();

        labels.PopScope();
        ExitScope();


        void ResolveGotos()
        {
            foreach (var @goto in gotosToResolve)
            {
                if (GetLabel(@goto.TargetLabelToken, out var targetLabel))
                {
                    @goto.Semantics = @goto.Semantics with { Target = targetLabel };
                }
            }
            gotosToResolve.Clear();
        }
    }

    private void EnterLoop(ILoopStatement loopStmt)
    {
        loopStatements.Push(loopStmt);
        EnterBreakableStatement(loopStmt);
    }

    private void ExitLoop(ILoopStatement loopStmt)
    {
        ExitBreakableStatement(loopStmt);
        var exitedLoop = loopStatements.Pop();
        Debug.Assert(exitedLoop == loopStmt);
    }

    private void EnterBreakableStatement(IBreakableStatement breakableStmt)
    {
        breakableStatements.Push(breakableStmt);
        EnterScope();
    }

    private void ExitBreakableStatement(IBreakableStatement breakableStmt)
    {
        ExitScope();
        var exitedStmt = breakableStatements.Pop();
        Debug.Assert(exitedStmt == breakableStmt);
    }

    private void VisitBody(ImmutableArray<IStatement> statements)
    {
        foreach (var stmt in statements)
        {
            AddLabel(stmt);
            stmt.Accept(this);
        }
    }

    private void AddSymbol(IDeclaration declaration)
    {
        // resolve the declaration type
        // the TypeFactory takes care of setting TypeDeclarationSemantics.DeclaredType or ValueDeclarationSemantics.ValueType
        TypeInfo? type = null;
        if (declaration is ITypeDeclaration typeDecl)
        {
            type = typeFactory.GetFrom(typeDecl);
        }
        else if (declaration is IValueDeclaration valueDecl)
        {
            type = typeFactory.GetFrom(valueDecl);
        }

        if (!typeRegistry.Find(declaration.Name, out _) && symbols.Add(declaration.Name, declaration))
        {
            if (declaration is ITypeDeclaration)
            {
                typeRegistry.Register(declaration.Name, type!);
            }
        }
        else
        {
            SymbolAlreadyDefinedError(declaration);
        }
    }

    public bool GetSymbol(NameExpression expr, [MaybeNullWhen(false)] out IDeclaration declaration) => GetSymbol(expr.Name, expr.Location, out declaration);
    public bool GetSymbol(Token identifier, [MaybeNullWhen(false)] out IDeclaration declaration) => GetSymbol(identifier.Lexeme.ToString(), identifier.Location, out declaration);
    public bool GetSymbol(string name, SourceRange location, [MaybeNullWhen(false)] out IDeclaration declaration)
    {
        if (GetSymbolUnchecked(name, out declaration))
        {
            return true;
        }
        else
        {
            UndefinedSymbolError(name, location);
            declaration = null;
            return false;
        }
    }
    public bool GetSymbolUnchecked(string name, [MaybeNullWhen(false)] out IDeclaration declaration) => symbols.Find(name, out declaration);

    public bool GetTypeSymbol(TypeName typeName, [MaybeNullWhen(false)] out TypeInfo type) => GetTypeSymbol(typeName.NameToken, out type);
    public bool GetTypeSymbol(Token identifier, [MaybeNullWhen(false)] out TypeInfo type) => GetTypeSymbol(identifier.Lexeme.ToString(), identifier.Location, out type);
    public bool GetTypeSymbol(string name, SourceRange location, [MaybeNullWhen(false)] out TypeInfo type)
    {
        if (GetTypeSymbolUnchecked(name, out type))
        {
            return true;
        }
        else
        {
            if (symbols.Find(name, out _))
            {
                ExpectedTypeSymbolError(name, location);
            }
            else
            {
                UndefinedSymbolError(name, location);
            }

            type = null;
            return false;
        }
    }
    public bool GetTypeSymbolUnchecked(string name, [MaybeNullWhen(false)] out TypeInfo type) => typeRegistry.Find(name, out type);

    private void AddLabel(IStatement stmt)
    {
        if (stmt.Label is null) { return; }

        if (!labels.Add(stmt.Label.Name, stmt))
        {
            LabelAlreadyDefinedError(stmt.Label);
        }
    }

    public bool GetLabel(Token identifier, [MaybeNullWhen(false)] out IStatement stmt) => GetLabel(identifier.Lexeme.ToString(), identifier.Location, out stmt);
    public bool GetLabel(string name, SourceRange location, [MaybeNullWhen(false)] out IStatement stmt)
    {
        if (GetLabelUnchecked(name, out stmt))
        {
            return true;
        }
        else
        {
            UndefinedLabelError(name, location);
            stmt = null;
            return false;
        }
    }
    public bool GetLabelUnchecked(string name, [MaybeNullWhen(false)] out IStatement stmt) => labels.Find(name, out stmt);

    #region Errors
    private void Error(ErrorCode code, string message, SourceRange location)
        => Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);
    internal void SymbolAlreadyDefinedError(IDeclaration declaration)
        => Error(ErrorCode.SemanticSymbolAlreadyDefined, $"Symbol '{declaration.Name}' is already defined", declaration.NameToken.Location);
    internal void UndefinedSymbolError(string name, SourceRange location)
        => Error(ErrorCode.SemanticUndefinedSymbol, $"Symbol '{name}' is undefined", location);
    internal void ExpectedTypeSymbolError(string name, SourceRange location)
        => Error(ErrorCode.SemanticExpectedTypeSymbol, $"Expected a type, but found '{name}'", location);
    internal void LabelAlreadyDefinedError(Label declaration)
        => Error(ErrorCode.SemanticLabelAlreadyDefined, $"Label '{declaration.Name}' is already defined", declaration.NameToken.Location);
    internal void UndefinedLabelError(string name, SourceRange location)
        => Error(ErrorCode.SemanticUndefinedLabel, $"Label '{name}' is undefined", location);
    internal void CannotConvertTypeError(TypeInfo source, TypeInfo destination, SourceRange location)
        => Error(ErrorCode.SemanticCannotConvertType, $"Cannot convert type '{source.ToPrettyString()}' to '{destination.ToPrettyString()}'", location);
    internal void ConstantWithoutInitializerError(VarDeclaration constVarDecl)
        => Error(ErrorCode.SemanticConstantWithoutInitializer, $"Constant '{constVarDecl.Name}' requires an initializer", constVarDecl.NameToken.Location);
    internal void InitializerExpressionIsNotConstantError(VarDeclaration constVarDecl)
        => Error(ErrorCode.SemanticInitializerExpressionIsNotConstant, $"Initializer expression of constant '{constVarDecl.Name}' must be constant", constVarDecl.Initializer!.Location);
    internal void TypeNotAllowedInConstantError(VarDeclaration constVarDecl, TypeInfo type)
    {
        var loc = constVarDecl.Declarator switch
        {
            VarRefDeclarator r => constVarDecl.Type.Location.Merge(r.AmpersandToken.Location),
            VarArrayDeclarator a => constVarDecl.Type.Location.Merge(a.Location),
            _ => constVarDecl.Type.Location,
        };
        Error(ErrorCode.SemanticTypeNotAllowedInConstant, $"Type '{type.ToPrettyString()}' is not allowed in constants", loc);
    }
    internal void ExpectedValueInReturnError(TypeInfo returnType, ReturnStatement returnStmt)
        => Error(ErrorCode.SemanticExpectedValueInReturn, $"Expected value of type '{returnType.ToPrettyString()}' in RETURN", returnStmt.Location);
    internal void ValueReturnedFromProcedureError(ReturnStatement returnStmt)
        => Error(ErrorCode.SemanticValueReturnedFromProcedure, $"Cannot return values from procedures", returnStmt.Expression!.Location);
    #endregion Errors
}
