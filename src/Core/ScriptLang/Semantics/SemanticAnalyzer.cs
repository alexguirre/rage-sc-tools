namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Types;

using System;
using System.Diagnostics.CodeAnalysis;

internal sealed class SemanticAnalyzer : IVisitor
{
    private readonly SymbolTable<IDeclaration> symbols = new();
    private readonly SymbolTable<Label> labels = new();
    private readonly TypeRegistry typeRegistry = new();
    private readonly TypeFactory typeFactory;

    public DiagnosticsReport Diagnostics { get; }

    public SemanticAnalyzer(DiagnosticsReport diagnostics)
    {
        Diagnostics = diagnostics;
        typeFactory = new(this);
    }

    public void Visit(CompilationUnit node)
    {
        // TODO: what to do with the USINGs

        node.Declarations.ForEach(decl => decl.Accept(this));
    }

    public void Visit(UsingDirective node) => throw new InvalidOperationException();

    public void Visit(EnumDeclaration node)
    {
        AddSymbol(node);
    }

    public void Visit(EnumMemberDeclaration node)
    {
        AddSymbol(node);
    }

    public void Visit(FunctionDeclaration node)
    {
        AddSymbol(node);

        symbols.PushScope();
        labels.PushScope();

        node.Parameters.ForEach(p => p.Accept(this));
        node.Body.ForEach(stmt => stmt.Accept(this));

        labels.PopScope();
        symbols.PopScope();
    }

    public void Visit(FunctionPointerDeclaration node)
    {
        AddSymbol(node);
    }

    public void Visit(NativeFunctionDeclaration node)
    {
        AddSymbol(node);
    }

    public void Visit(ScriptDeclaration node)
    {
        AddSymbol(node);

        symbols.PushScope();
        labels.PushScope();

        node.Parameters.ForEach(p => p.Accept(this));
        node.Body.ForEach(stmt => stmt.Accept(this));

        labels.PopScope();
        symbols.PopScope();
    }

    public void Visit(GlobalBlockDeclaration node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(StructDeclaration node)
    {
        AddSymbol(node);
    }

    public void Visit(VarDeclaration node)
    {
        AddSymbol(node);
    }

    public void Visit(VarDeclarator node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(VarRefDeclarator node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(VarArrayDeclarator node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(BinaryExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(BoolLiteralExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(FieldAccessExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(FloatLiteralExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(IndexingExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(IntLiteralExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(InvocationExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(NullExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(StringLiteralExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(UnaryExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(NameExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(VectorExpression node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(Label node)
    {
        AddLabel(node);
    }

    public void Visit(AssignmentStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(BreakStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(ContinueStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(EmptyStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(GotoStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(IfStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(RepeatStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(ReturnStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(SwitchStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(ValueSwitchCase node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(DefaultSwitchCase node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(WhileStatement node)
    {
        throw new System.NotImplementedException();
    }

    public void Visit(TypeName node)
    {
        // empty
    }

    public void Visit(ErrorDeclaration node)
    {
        // empty
    }

    public void Visit(ErrorExpression node)
    {
        // empty
    }

    public void Visit(ErrorStatement node)
    {
        // empty
    }

    private void AddSymbol(IDeclaration declaration)
    {
        if (!typeRegistry.Find(declaration.Name, out _) && symbols.Add(declaration.Name, declaration))
        {
            if (declaration is ITypeDeclaration typeDecl)
            {
                typeRegistry.Register(declaration.Name, typeFactory.GetFrom(typeDecl));
            }
            else if (declaration is IValueDeclaration valueDecl)
            {
                // resolve the value type
                // the TypeFactory takes care of setting ValueDeclarationSemantics.ValueType
                _ = typeFactory.GetFrom(valueDecl);
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
        if (symbols.Find(name, out declaration))
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

    public bool GetTypeSymbol(TypeName typeName, [MaybeNullWhen(false)] out TypeInfo type) => GetTypeSymbol(typeName.NameToken, out type);
    public bool GetTypeSymbol(Token identifier, [MaybeNullWhen(false)] out TypeInfo type) => GetTypeSymbol(identifier.Lexeme.ToString(), identifier.Location, out type);
    public bool GetTypeSymbol(string name, SourceRange location, [MaybeNullWhen(false)] out TypeInfo type)
    {
        if (typeRegistry.Find(name, out type))
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
        if (labels.Find(name, out label))
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
}
