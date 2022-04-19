namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Types;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

public sealed class SemanticsAnalyzer : Visitor
{
    private readonly SymbolTable<IDeclaration> symbols = new();
    private readonly SymbolTable<Label> labels = new();
    private readonly TypeRegistry typeRegistry = new();
    private readonly TypeFactory typeFactory;
    private readonly ExpressionTypeChecker exprTypeChecker = new();

    private EnumMemberDeclaration? previousEnumMember = null;

    public DiagnosticsReport Diagnostics { get; }

    public SemanticsAnalyzer(DiagnosticsReport diagnostics)
    {
        Diagnostics = diagnostics;
        typeFactory = new(this);
    }

    public override void Visit(CompilationUnit node)
    {
        // TODO: what to do with the USINGs

        node.Declarations.ForEach(decl => decl.Accept(this));
    }

    public override void Visit(UsingDirective node) => throw new InvalidOperationException();

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

    public override void Visit(FunctionDeclaration node)
    {
        AddSymbol(node);

        symbols.PushScope();
        labels.PushScope();

        node.Parameters.ForEach(p => p.Accept(this));
        node.Body.ForEach(stmt => stmt.Accept(this));

        labels.PopScope();
        symbols.PopScope();
    }

    public override void Visit(FunctionPointerDeclaration node)
    {
        AddSymbol(node);

        symbols.PushScope();
        node.Parameters.ForEach(p => p.Accept(this));
        symbols.PopScope();
    }

    public override void Visit(NativeFunctionDeclaration node)
    {
        AddSymbol(node);

        symbols.PushScope();
        node.Parameters.ForEach(p => p.Accept(this));
        symbols.PopScope();
    }

    public override void Visit(ScriptDeclaration node)
    {
        AddSymbol(node);

        symbols.PushScope();
        labels.PushScope();

        node.Parameters.ForEach(p => p.Accept(this));
        node.Body.ForEach(stmt => stmt.Accept(this));

        labels.PopScope();
        symbols.PopScope();
    }

    public override void Visit(GlobalBlockDeclaration node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(StructDeclaration node)
    {
        AddSymbol(node);
    }

    public override void Visit(VarDeclaration node)
    {
        var varType = typeFactory.GetFrom(node);

        if (node.Kind is VarKind.Constant)
        {
            if (varType is not (IntType or FloatType or BoolType or StringType or VectorType or EnumType))
            {
                TypeNotAllowedInConstantError(node, varType);
            }
            else
            {
                if (node.Initializer is null)
                {
                    ConstantWithoutInitializerError(node);
                }

                var initializerType = node.Initializer?.Accept(exprTypeChecker, this) ?? ErrorType.Instance;

                ConstantValue? value = null;
                if (!varType.IsError && !initializerType.IsError)
                {
                    Debug.Assert(node.Initializer is not null);
                    if (!node.Initializer.ValueKind.Is(ValueKind.Constant))
                    {
                        InitializerExpressionIsNotConstantError(node);
                    }
                    else if (varType.IsAssignableFrom(initializerType))
                    {
                        value = ConstantExpressionEvaluator.Eval(node.Initializer, this);
                    }
                    else
                    {
                        CannotConvertTypeError(initializerType, varType, node.Initializer.Location);
                    }
                }

                node.Semantics = node.Semantics with { ConstantValue = value };
            }
        }


        AddSymbol(node);
    }

    public override void Visit(Label node)
    {
        AddLabel(node);
    }

    public override void Visit(AssignmentStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(BreakStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(ContinueStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(EmptyStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(GotoStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(IfStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(RepeatStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(ReturnStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(SwitchStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(ValueSwitchCase node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(DefaultSwitchCase node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(WhileStatement node)
    {
        throw new System.NotImplementedException();
    }

    public override void Visit(TypeName node)
    {
        // empty
    }

    public override void Visit(ErrorDeclaration node)
    {
        // empty
    }

    public override void Visit(ErrorStatement node)
    {
        // empty
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

    private void AddLabel(Label label)
    {
        if (!labels.Add(label.Name, label))
        {
            LabelAlreadyDefinedError(label);
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


    private void Error(ErrorCode code, string message, SourceRange location)
        => Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);
    private void SymbolAlreadyDefinedError(IDeclaration declaration)
        => Error(ErrorCode.SemanticSymbolAlreadyDefined, $"Symbol '{declaration.Name}' is already defined", declaration.NameToken.Location);
    private void UndefinedSymbolError(string name, SourceRange location)
        => Error(ErrorCode.SemanticUndefinedSymbol, $"Symbol '{name}' is undefined", location);
    private void ExpectedTypeSymbolError(string name, SourceRange location)
        => Error(ErrorCode.SemanticExpectedTypeSymbol, $"Expected a type, but found '{name}'", location);
    private void LabelAlreadyDefinedError(Label declaration)
        => Error(ErrorCode.SemanticLabelAlreadyDefined, $"Label '{declaration.Name}' is already defined", declaration.NameToken.Location);
    private void UndefinedLabelError(string name, SourceRange location)
        => Error(ErrorCode.SemanticUndefinedLabel, $"Label '{name}' is undefined", location);
    private void ExpectedLabelError(string name, SourceRange location)
            => Error(ErrorCode.SemanticExpectedLabel, $"Expected a label, but found '{name}'", location);
    public void CannotConvertTypeError(TypeInfo source, TypeInfo destination, SourceRange location)
        => Error(ErrorCode.SemanticCannotConvertType, $"Cannot convert type '{source.GetType().Name}' to '{destination.GetType().Name}'", location);
    private void ConstantWithoutInitializerError(VarDeclaration constVarDecl)
        => Error(ErrorCode.SemanticConstantWithoutInitializer, $"Constant '{constVarDecl.Name}' requires an initializer", constVarDecl.NameToken.Location);
    private void InitializerExpressionIsNotConstantError(VarDeclaration constVarDecl)
        => Error(ErrorCode.SemanticInitializerExpressionIsNotConstant, $"Initializer expression of constant '{constVarDecl.Name}' must be constant", constVarDecl.Initializer!.Location);
    private void TypeNotAllowedInConstantError(VarDeclaration constVarDecl, TypeInfo type)
    {
        var loc = constVarDecl.Declarator switch
        {
            VarRefDeclarator r => constVarDecl.Type.Location.Merge(r.AmpersandToken.Location),
            VarArrayDeclarator a => constVarDecl.Type.Location.Merge(a.Location),
            _ => constVarDecl.Type.Location,
        };
        Error(ErrorCode.SemanticTypeNotAllowedInConstant, $"Type '{type}' is not allowed in constants", loc);
    }
}
