﻿namespace ScTools.ScriptLang.Semantics;

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

public sealed class SemanticsAnalyzer : AstVisitor
{
    // helpers
    private readonly IUsingResolver? usingResolver;
    private readonly TypeFactory typeFactory;
    private readonly ExpressionTypeChecker exprTypeChecker = new();
    private readonly LabelGenerator labelGen = new();

    // context
    private readonly HashSet<CompilationUnit> imports = new();
    private readonly SymbolTable<ISymbol> symbols = new();
    private readonly SymbolTable<Label> labels = new();
    private readonly TypeRegistry typeRegistry = new();
    private bool hasScriptDeclaration = false;
    private EnumMemberDeclaration? previousEnumMember = null;
    private StructDeclaration? currentStructDeclaration = null;
    private TypeInfo? currentFunctionReturnType = null;
    private bool currentFunctionHasOptionalParameter = false;
    private SwitchStatement? currentSwitchStmt = null;
    private bool currentSwitchHasDefaultCase = false;
    private readonly HashSet<int> currentSwitchHandledCases = new();
    private readonly Stack<IBreakableStatement> breakableStatements = new();
    private readonly Stack<ILoopStatement> loopStatements = new();
    private readonly List<GotoStatement> gotosToResolve = new();

    public DiagnosticsReport Diagnostics { get; }

    public SemanticsAnalyzer(DiagnosticsReport diagnostics, IUsingResolver? usingResolver = null)
    {
        this.usingResolver = usingResolver;
        Diagnostics = diagnostics;
        typeFactory = new(this, exprTypeChecker);

        RegisterBuiltIns();
    }

    private void RegisterBuiltIns()
    {
        RegisterBuiltInTypes();
        RegisterIntrinsics();
    }

    private void RegisterBuiltInTypes()
    {
        AddSymbol(new BuiltInTypeSymbol("ANY", AnyType.Instance));
        AddSymbol(new BuiltInTypeSymbol("INT", IntType.Instance));
        AddSymbol(new BuiltInTypeSymbol("FLOAT", FloatType.Instance));
        AddSymbol(new BuiltInTypeSymbol("BOOL", BoolType.Instance));
        AddSymbol(new BuiltInTypeSymbol("STRING", StringType.Instance));
        AddSymbol(new BuiltInTypeSymbol("VECTOR", VectorType.Instance));

        TextLabelType.All64.ForEach(tl => AddSymbol(new BuiltInTypeSymbol(TextLabelType.GetTypeNameForLength(tl.Length), tl)));
        AddSymbol(new BuiltInTypeSymbol("TEXT_LABEL", TextLabelType.All64.First(tl => tl.Length == 32)));
    }

    private void RegisterIntrinsics()
    {
        Intrinsics.All.ForEach(AddSymbol);
    }

    public override void Visit(CompilationUnit node)
    {
        node.Usings.ForEach(@using => @using.Accept(this));
        hasScriptDeclaration = false;
        node.Declarations.ForEach(decl => decl.Accept(this));
    }

    public override void Visit(UsingDirective node)
    {
        node.Semantics = node.Semantics with { ImportedCompilationUnit = null };
        if (usingResolver is null)
        {
            return;
        }

        var resolveResult = usingResolver.ResolveUsingAsync(node).Result;
        switch (resolveResult.Status)
        {
            case UsingResolveStatus.Valid:
                Debug.Assert(resolveResult.Ast is not null);
                node.Semantics = node.Semantics with { ImportedCompilationUnit = resolveResult.Ast };
                Import(resolveResult.Ast);
                break;
            case UsingResolveStatus.NotFound:
                UsingNotFoundError(node);
                break;
            case UsingResolveStatus.CyclicDependency: // TODO: add error for UsingResolveStatus.CyclicDependency
                throw new NotImplementedException("UsingResolveStatus.CyclicDependency not handled");
        }
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
            if (!node.Initializer.ValueKind.Is(ValueKind.Constant))
            {
                InitializerExpressionIsNotConstantError(node);
            }
            else if (IntType.Instance.IsAssignableFrom(initializerType) ||
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
            // fallback if there is no inititalizer or it had an error
            var enumTy = (EnumType)node.Semantics.ValueType;
            if (enumTy.IsHash)
            {
                // name hashed
                value = ConstantValue.Int(unchecked((int)node.NameToken.Lexeme.ToLowercaseHash()));
            }
            else
            {
                // incremental numeration
                value = previousEnumMember is null ?
                    ConstantValue.Int(0) :
                    ConstantValue.Int(previousEnumMember.Semantics.ConstantValue!.IntValue + 1);
            }
        }

        node.Semantics = node.Semantics with { ConstantValue = value };
        previousEnumMember = node;

        AddSymbol(node);
    }

    public override void Visit(ScriptDeclaration node)
    {
        if (hasScriptDeclaration)
        {
            MultipleScriptDeclarationsError(node);
        }
        else
        {
            hasScriptDeclaration = true;
        }
        
        AddSymbol(node);

        currentFunctionReturnType = VoidType.Instance;
        currentFunctionHasOptionalParameter = false;
        EnterFunctionScope();
        node.Parameters.ForEach(p => p.Accept(this));
        VisitBody(node.Body);
        ExitFunctionScope();
        currentFunctionReturnType = null;
        currentFunctionHasOptionalParameter = false;
    }

    public override void Visit(FunctionDeclaration node)
    {
        AddSymbol(node);

        currentFunctionReturnType = ((FunctionType)node.Semantics.ValueType!).Return;
        currentFunctionHasOptionalParameter = false;
        EnterFunctionScope();
        node.Parameters.ForEach(p => p.Accept(this));
        VisitBody(node.Body);
        ExitFunctionScope();
        currentFunctionReturnType = null;
        currentFunctionHasOptionalParameter = false;
    }

    public override void Visit(FunctionTypeDefDeclaration node)
    {
        AddSymbol(node);

        currentFunctionHasOptionalParameter = false;
        EnterScope();
        node.Parameters.ForEach(p => p.Accept(this));
        ExitScope();
        currentFunctionHasOptionalParameter = false;
    }

    public override void Visit(NativeFunctionDeclaration node)
    {
        AddSymbol(node);

        currentFunctionHasOptionalParameter = false;
        EnterScope();
        node.Parameters.ForEach(p => p.Accept(this));
        ExitScope();
        currentFunctionHasOptionalParameter = false;
    }

    public override void Visit(NativeTypeDeclaration node)
    {
        AddSymbol(node);
    }

    public override void Visit(GlobalBlockDeclaration node)
    {
        node.Vars.ForEach(v => v.Accept(this));
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

    public override void Visit(ExpressionStatement node)
    {
        node.Expression.Accept(exprTypeChecker, this);
        node.Expression.Semantics = node.Expression.Semantics with { IsStatement = true };

        switch (node.Expression)
        {
            case InvocationExpression:
            case PostfixUnaryExpression { Operator: PostfixUnaryOperator.Increment or PostfixUnaryOperator.Decrement }:
                // nothing to do, these are valid expression statements
                break;
            default:
                InvalidExpressionStatementError(node);
                break;
        }
    }

    public override void Visit(VarDeclaration node)
    {
        var varType = typeFactory.GetFrom(node);

        if (node is { IsReference: true, Kind: not VarKind.Parameter })
        {
            ReferenceNotAllowedError(node);
        }

        switch (node.Kind)
        {
            case VarKind.Constant:
                if (!varType.IsError && varType is not (IntType or FloatType))
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
                    var value = node.Semantics.ConstantValue;
                    value ??= varType switch
                    {
                        // give default values to prevent null references in ConstantExpressionEvaluator
                        IntType => ConstantValue.Int(0),
                        FloatType => ConstantValue.Float(0),
                        _ => null,
                    };
                    Debug.Assert(value is not null);
                    node.Semantics = node.Semantics with { ConstantValue = value };
                }
                break;
            case VarKind.Global:
                CheckRuntimeInitializer(this, node);
                // TODO: check if global var types are safe to use
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
            case VarKind.Parameter:
                CheckConstantInitializer(this, node);
                
                if (currentFunctionHasOptionalParameter && node.Initializer is null)
                {
                    RequiredParameterAfterOptionalParameterError(node);
                }
                else if (node.Initializer is not null)
                {
                    currentFunctionHasOptionalParameter = true;
                }
                
                break;
            case VarKind.ScriptParameter:
                if (node.Initializer is not null)
                {
                    // TODO: disallow optional script parameters
                }
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
                else if (!s.IsZeroAssignedToEnum(varType, node.Initializer, out value))
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
            s.CheckRuntimeAssignment(varType, initializerType, node.Initializer);

            node.Semantics = node.Semantics with { ConstantValue = null };
        }
    }

    public override void Visit(AssignmentStatement node)
    {
        var lhsType = node.LHS.Accept(exprTypeChecker, this);
        var rhsType = node.RHS.Accept(exprTypeChecker, this);
        var rhsNode = node.RHS;

        if (node.CompoundOperator.HasValue)
        {
            var binOp = node.CompoundOperator.Value;
            
            if (lhsType is TextLabelType)
            {
                // special case for text labels which support `lhs += rhs` but not `lhs = lhs + rhs`
                if (binOp != BinaryOperator.Add)
                {
                    TextLabelOnlyAppendSupportedError(node);
                }
                else
                {
                    switch (rhsType)
                    {
                        case TextLabelType when !rhsNode.ValueKind.Is(ValueKind.Addressable):
                            TextLabelAppendNonAddressableTextLabelError(rhsNode);
                            break;
                        case TextLabelType:
                        case StringType:
                        case IntType:
                            // nothing to do, RHS has a valid type
                            break;
                        default:
                            if (!rhsType.IsError)
                            {
                                TextLabelAppendInvalidTypeError(rhsNode);
                            }
                            break;
                    }
                }
            }
            else
            {
                // synthesize `lhs binOp= rhs` as `lhs = lhs binOp rhs`
                var binExpr = new BinaryExpression(node.CompoundOperator.Value.ToToken().Create(), node.LHS, node.RHS);
                rhsType = binExpr.Accept(exprTypeChecker, this);
                rhsNode = binExpr;
            }
        }
        
        CheckRuntimeAssignment(lhsType, rhsType, rhsNode);
    }

    private void CheckRuntimeAssignment(TypeInfo lhsType, TypeInfo rhsType, INode? rhsNode)
    {
        if (!lhsType.IsError && !rhsType.IsError)
        {
            if (!lhsType.IsAssignableFrom(rhsType) && !IsZeroAssignedToEnum(lhsType, rhsNode, out _))
            {
                Debug.Assert(rhsNode is not null);
                CannotConvertTypeError(rhsType, lhsType, rhsNode.Location);
            }
        }
    }

    private bool IsZeroAssignedToEnum(TypeInfo lhsType, INode? rhsNode, [NotNullWhen(true)] out ConstantValue? value)
    {
        value = null;
        return lhsType is EnumType && rhsNode is IExpression { Type: IntType } rhsExpr &&
               rhsExpr.ValueKind.Is(ValueKind.Constant) &&
               (value = ConstantExpressionEvaluator.Eval(rhsExpr, this)) is { Type: IntType, IntValue: 0 };
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

        AddLabelsToIf(node);
        EnterScope();
        VisitBody(node.Then);
        ExitScope();
        EnterScope();
        VisitBody(node.Else);
        ExitScope();
    }

    public override void Visit(RepeatStatement node)
    {
        var limitType = node.Limit.Accept(exprTypeChecker, this);
        if (!limitType.IsError && !IntType.Instance.IsAssignableFrom(limitType))
        {
            CannotConvertTypeError(limitType, IntType.Instance, node.Limit.Location);
        }

        var counterType = node.Counter.Accept(exprTypeChecker, this);
        if (!counterType.IsError)
        {
            if (!IntType.Instance.IsAssignableFrom(counterType) || !counterType.IsAssignableFrom(IntType.Instance))
            {
                CannotConvertTypeError(counterType, IntType.Instance, node.Counter.Location);
            }
            else if (!node.Counter.ValueKind.Is(ValueKind.Assignable))
            {
                ExpressionIsNotAssignableError(node.Counter);
            }
        }
        
        AddLabelsToLoop(node);
        EnterLoop(node);
        VisitBody(node.Body);
        ExitLoop(node);
    }

    public override void Visit(ForStatement node)
    {
        var counterType = node.Counter.Accept(exprTypeChecker, this);
        if (!counterType.IsError)
        {
            if (!IntType.Instance.IsAssignableFrom(counterType) || !counterType.IsAssignableFrom(IntType.Instance))
            {
                CannotConvertTypeError(counterType, IntType.Instance, node.Counter.Location);
            }
            else if (!node.Counter.ValueKind.Is(ValueKind.Assignable))
            {
                ExpressionIsNotAssignableError(node.Counter);
            }
        }

        var initializerType = node.Initializer.Accept(exprTypeChecker, this);
        if (!initializerType.IsError && !counterType.IsError && !counterType.IsAssignableFrom(initializerType))
        {
            CannotConvertTypeError(initializerType, counterType, node.Initializer.Location);
        }

        var limitType = node.Limit.Accept(exprTypeChecker, this);
        if (!limitType.IsError && !IntType.Instance.IsAssignableFrom(limitType))
        {
            CannotConvertTypeError(limitType, IntType.Instance, node.Limit.Location);
        }

        AddLabelsToLoop(node);
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
        var exprType = node.Expression.Accept(exprTypeChecker, this);
        TypeInfo switchType = ErrorType.Instance;
        if (!exprType.IsError)
        {
            if (IntType.Instance.IsAssignableFrom(exprType))
            {
                switchType = IntType.Instance;
            }
            else if (exprType is EnumType enumType)
            {
                switchType = enumType;
            }
            else
            {
                TypeNotAllowedInSwitchError(node);
            }
        }

        node.Semantics = node.Semantics with { SwitchType = switchType };
        AddLabelsToBreakable(node);
        EnterBreakableStatement(node);
        currentSwitchHandledCases.Clear();
        currentSwitchHasDefaultCase = false;
        currentSwitchStmt = node;
        node.Cases.ForEach(c => c.Accept(this));
        currentSwitchStmt = null;
        ExitBreakableStatement(node);
    }

    public override void Visit(ValueSwitchCase node)
    {
        Debug.Assert(currentSwitchStmt is not null);

        var switchType = currentSwitchStmt.Semantics.SwitchType!;
        var valueType = node.Value.Accept(exprTypeChecker, this);
        if (!valueType.IsError && !switchType.IsError)
        {
            if (!node.Value.ValueKind.Is(ValueKind.Constant))
            {
                SwitchCaseValueIsNotConstantError(node);
            }
            else if (!switchType.IsAssignableFrom(valueType))
            {
                CannotConvertTypeError(valueType, switchType, node.Value.Location);
            }
            else
            {
                var value = ConstantExpressionEvaluator.Eval(node.Value, this).IntValue;
                if (!currentSwitchHandledCases.Add(value))
                {
                    DuplicateSwitchCaseError(node);
                }
                node.Semantics = node.Semantics with { Value = value };
            }
        }

        AddLabelsToSwitchCase(node);
        VisitBody(node.Body);
    }

    public override void Visit(DefaultSwitchCase node)
    {
        Debug.Assert(currentSwitchStmt is not null);

        if (currentSwitchHasDefaultCase)
        {
            DuplicateSwitchDefaultCaseError(node);
        }

        currentSwitchHasDefaultCase = true;
        AddLabelsToSwitchCase(node);
        VisitBody(node.Body);
    }

    public override void Visit(WhileStatement node)
    {
        var conditionType = node.Condition.Accept(exprTypeChecker, this);
        if (!conditionType.IsError && !BoolType.Instance.IsAssignableFrom(conditionType))
        {
            CannotConvertTypeError(conditionType, BoolType.Instance, node.Condition.Location);
        }

        AddLabelsToLoop(node);
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

    private void AddLabelsToLoop(ILoopStatement loopStmt)
    {
        var n = (ISemanticNode<LoopStatementSemantics>)loopStmt;
        n.Semantics = n.Semantics with
        {
            BeginLabel = labelGen.NextLabel(),
            ContinueLabel = labelGen.NextLabel(),
        };
        AddLabelsToBreakable(loopStmt);
    }

    private void AddLabelsToBreakable(IBreakableStatement breakableStmt)
    {
        breakableStmt.Semantics = breakableStmt.Semantics with
        {
            ExitLabel = labelGen.NextLabel(),
        };
    }

    private void AddLabelsToSwitchCase(SwitchCase @case)
    {
        @case.Semantics = @case.Semantics with
        {
            Label = labelGen.NextLabel(),
        };
    }

    private void AddLabelsToIf(IfStatement @if)
    {
        @if.Semantics = @if.Semantics with
        {
            ElseLabel = labelGen.NextLabel(),
            EndLabel = labelGen.NextLabel(),
        };
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

    public void AddSymbol(ISymbol symbol)
    {
        // resolve the declaration type
        // the TypeFactory takes care of setting TypeDeclarationSemantics.DeclaredType or ValueDeclarationSemantics.ValueType
        if (symbol is ITypeDeclaration typeDecl)
        {
            typeFactory.GetFrom(typeDecl);
        }
        else if (symbol is IValueDeclaration valueDecl)
        {
            typeFactory.GetFrom(valueDecl);
        }

        if (!typeRegistry.Find(symbol.Name, out _) && symbols.Add(symbol))
        {
            if (symbol is ITypeSymbol typeSymbol)
            {
                typeRegistry.Register(symbol.Name, typeSymbol.DeclaredType);
            }
        }
        else
        {
            if (symbol is IDeclaration decl)
            {
                SymbolAlreadyDefinedError(decl);
            }
            else
            {
                // if the symbol doesn't come from the AST, probably an issue with a built-in type
                throw new ArgumentException("Built-in symbol already defined", nameof(symbol));
            }
        }
    }

    /// <summary>
    /// Brings all symbols in the specified compilation unit into scope for this compilation unit.
    /// </summary>
    private void Import(CompilationUnit compilationUnit)
    {
        if (!imports.Add(compilationUnit))
        {
             return;
        }

        foreach (var decl in compilationUnit.Declarations)
        {
            switch (decl)
            {
                case EnumDeclaration enumDecl:
                    AddSymbol(enumDecl);
                    enumDecl.Members.ForEach(AddSymbol);
                    break;
                case GlobalBlockDeclaration globalsDecl:
                    globalsDecl.Vars.ForEach(AddSymbol);
                    break;
                default:
                    AddSymbol(decl);
                    break;
            }
        }
    }

    public bool GetSymbol(NameExpression expr, [MaybeNullWhen(false)] out ISymbol symbol) => GetSymbol(expr.Name, expr.Location, out symbol);
    public bool GetSymbol(Token identifier, [MaybeNullWhen(false)] out ISymbol symbol) => GetSymbol(identifier.Lexeme.ToString(), identifier.Location, out symbol);
    public bool GetSymbol(string name, SourceRange location, [MaybeNullWhen(false)] out ISymbol symbol)
    {
        if (GetSymbolUnchecked(name, out symbol))
        {
            return true;
        }
        else
        {
            UndefinedSymbolError(name, location);
            symbol = null;
            return false;
        }
    }
    public bool GetSymbolUnchecked(string name, [MaybeNullWhen(false)] out ISymbol symbol) => symbols.Find(name, out symbol);

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

        if (!labels.Add(stmt.Label))
        {
            LabelAlreadyDefinedError(stmt.Label);
        }
    }

    public bool GetLabel(Token identifier, [MaybeNullWhen(false)] out Label label) => GetLabel(identifier.Lexeme.ToString(), identifier.Location, out label);
    public bool GetLabel(string name, SourceRange location, [MaybeNullWhen(false)] out Label label)
    {
        if (GetLabelUnchecked(name, out label))
        {
            return true;
        }
        else
        {
            UndefinedLabelError(name, location);
            label = null;
            return false;
        }
    }
    public bool GetLabelUnchecked(string name, [MaybeNullWhen(false)] out Label label) => labels.Find(name, out label);

    #region Errors
    private void Error(ErrorCode code, string message, SourceRange location)
        => Diagnostics.Add((int)code, DiagnosticSeverity.Error, message, location);
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
    internal void ExpressionIsNotAssignableError(IExpression expr)
        => Error(ErrorCode.SemanticExpressionIsNotAssignable, $"Expression must be assignable", expr.Location);
    internal void ConstantWithoutInitializerError(VarDeclaration constVarDecl)
        => Error(ErrorCode.SemanticConstantWithoutInitializer, $"Constant '{constVarDecl.Name}' requires an initializer", constVarDecl.NameToken.Location);
    internal void InitializerExpressionIsNotConstantError(VarDeclaration constVarDecl)
        => Error(ErrorCode.SemanticInitializerExpressionIsNotConstant, $"Initializer expression of constant '{constVarDecl.Name}' must be constant", constVarDecl.Initializer!.Location);
    internal void InitializerExpressionIsNotConstantError(EnumMemberDeclaration enumMemberDecl)
        => Error(ErrorCode.SemanticInitializerExpressionIsNotConstant, $"Initializer expression of enum '{enumMemberDecl.Name}' must be constant", enumMemberDecl.Initializer!.Location);
    internal void ArrayLengthExpressionIsNotConstantError(VarDeclaration arrayDecl, IExpression expr)
        => Error(ErrorCode.SemanticArrayLengthExpressionIsNotConstant, $"Size expression of array '{arrayDecl.Name}' must be constant", expr.Location);

    internal void ArrayItemTypeIsIncompleteArrayError(VarDeclaration arrayDecl)
        => Error(ErrorCode.SemanticArrayItemTypeIsIncompleteArray, $"Arrays cannot have an incomplete array as item type", arrayDecl.Declarator.ArrayOpenBracketToken!.Value.Location.Merge(arrayDecl.Declarator.ArrayCloseBracketToken!.Value.Location));
    internal void IncompleteArrayNotAllowedError(VarDeclaration arrayDecl)
        => Error(ErrorCode.SemanticIncompleteArrayNotAllowed, $"Incomplete arrays are not allowed in this context", arrayDecl.Declarator.ArrayOpenBracketToken!.Value.Location.Merge(arrayDecl.Declarator.ArrayCloseBracketToken!.Value.Location));
    internal void IncompleteArrayNotByRefError(VarDeclaration arrayDecl)
        => Error(ErrorCode.SemanticIncompleteArrayNotByRef, $"Incomplete array parameters must be passed by reference", arrayDecl.Location);
    internal void TypeNotAllowedInConstantError(VarDeclaration constVarDecl, TypeInfo type)
        => Error(ErrorCode.SemanticTypeNotAllowedInConstant, $"Type '{type.ToPrettyString()}' is not allowed in constants", constVarDecl.Type.Location);
    internal void ExpectedValueInReturnError(TypeInfo returnType, ReturnStatement returnStmt)
        => Error(ErrorCode.SemanticExpectedValueInReturn, $"Expected value of type '{returnType.ToPrettyString()}' in RETURN", returnStmt.Location);
    internal void ValueReturnedFromProcedureError(ReturnStatement returnStmt)
        => Error(ErrorCode.SemanticValueReturnedFromProcedure, $"Cannot return values from procedures", returnStmt.Expression!.Location);
    internal void SwitchCaseValueIsNotConstantError(ValueSwitchCase @case)
        => Error(ErrorCode.SemanticSwitchCaseValueIsNotConstant, $"Switch case value expression must be constant", @case.Location);
    internal void DuplicateSwitchCaseError(ValueSwitchCase @case)
        => Error(ErrorCode.SemanticDuplicateSwitchCase, $"Duplicate switch case '{@case.Semantics.Value}'", @case.Value.Location);
    internal void DuplicateSwitchDefaultCaseError(DefaultSwitchCase @case)
        => Error(ErrorCode.SemanticDuplicateSwitchCase, $"Duplicate switch DEFAULT case", @case.Tokens[0].Location);
    internal void TypeNotAllowedInSwitchError(SwitchStatement switchStmt)
        => Error(ErrorCode.SemanticTypeNotAllowedInSwitch, $"Type '{switchStmt.Expression.Type!.ToPrettyString()}' is not allowed in SWITCH statement", switchStmt.Expression.Location);
    internal void UsingNotFoundError(UsingDirective usingDirective)
        => Error(ErrorCode.SemanticUsingNotFound, $"File '{usingDirective.Path.Escape()}' not found in USING directive", usingDirective.PathToken.Location);
    internal void ExpectedNativeTypeError(TypeName typeName)
        => Error(ErrorCode.SemanticExpectedNativeType, $"Expected a NATIVE type, but found '{typeName.Name}'", typeName.Location);
    internal void RequiredParameterAfterOptionalParameterError(VarDeclaration parameter)
        => Error(ErrorCode.SemanticRequiredParameterAfterOptionalParameter, $"Optional parameters must appear after all required parameters", parameter.Location);
    internal void TextLabelOnlyAppendSupportedError(AssignmentStatement stmt)
        => Error(ErrorCode.SemanticTextLabelOnlyAppendSupported, $"TEXT_LABEL type only supported '+='", stmt.Tokens[0].Location);
    internal void TextLabelAppendNonAddressableTextLabelError(IExpression rhs)
        => Error(ErrorCode.SemanticTextLabelAppendNonAddressableTextLabel, $"Cannot append non-addressable TEXT_LABEL", rhs.Location);
    internal void TextLabelAppendInvalidTypeError(IExpression rhs)
        => Error(ErrorCode.SemanticTextLabelAppendInvalidType, $"Cannot append type '{rhs.Type!.ToPrettyString()}' to TEXT_LABEL. Only INT, STRING or TEXT_LABEL are supported", rhs.Location);
    internal void ReferenceNotAllowedError(VarDeclaration varDecl)
        => Error(ErrorCode.SemanticReferenceNotAllowed, $"References are not allowed in this context", varDecl.Declarator.RefAmpersandToken!.Value.Location); 
    internal void InvalidExpressionStatementError(ExpressionStatement stmt)
        => Error(ErrorCode.SemanticInvalidExpressionStatement, $"Only invocation, increment, and decrement expressions can be used as a statement", stmt.Location);
    internal void MultipleScriptDeclarationsError(ScriptDeclaration scriptDecl)
        => Error(ErrorCode.SemanticMultipleScriptDeclarations, $"Multiple SCRIPT declarations are not allowed", scriptDecl.Location);
#endregion Errors
}
