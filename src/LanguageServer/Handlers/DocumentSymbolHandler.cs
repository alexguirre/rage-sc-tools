namespace ScTools.LanguageServer.Handlers;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using ScTools.LanguageServer.Services;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class DocumentSymbolHandler : ILspRequestHandler<DocumentSymbolParams, DocumentSymbol[]>
{
    private readonly ITextDocumentTracker textDocumentTracker;
    private readonly NodeToDocumentSymbol nodeToSymbol = new ();

    public string MethodName => Methods.TextDocumentDocumentSymbolName;

    public DocumentSymbolHandler(ITextDocumentTracker textDocumentTracker)
    {
        this.textDocumentTracker = textDocumentTracker;
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        capabilities.DocumentSymbolProvider = true;
    }

    public async Task<DocumentSymbol[]> HandleAsync(DocumentSymbolParams param, CancellationToken cancellationToken)
    {
        var ast = await textDocumentTracker.GetDocumentAstAsync(param.TextDocument.Uri);
        if (ast is null)
        {
            return Array.Empty<DocumentSymbol>();
        }

        var symbols = new List<DocumentSymbol>(ast.Declarations.Length);
        foreach (var decl in ast.Declarations)
        {
            symbols.Add(decl.Accept(nodeToSymbol));
        }
        return symbols.ToArray();
    }

    private class NodeToDocumentSymbol : AstVisitor<DocumentSymbol>
    {
        public override DocumentSymbol Visit(EnumDeclaration node)
            => new()
            {
                Name = node.Name,
                Kind = SymbolKind.Enum,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
                Children = node.Members.Select(Visit).ToArray(),
            };

        public override DocumentSymbol Visit(EnumMemberDeclaration node)
            => new()
            {
                Name = node.Name,
                Detail = node.Semantics.ConstantValue?.IntValue.ToString() ?? null,
                Kind = SymbolKind.EnumMember,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.Location),
            };

        public override DocumentSymbol Visit(StructDeclaration node)
            => new()
            {
                Name = node.Name,
                Kind = SymbolKind.Struct,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
                Children = node.Fields.Select(Visit).ToArray(),
            };

        public override DocumentSymbol Visit(VarDeclaration node)
            => new()
            {
                Name = node.Name,
                Kind = node.Kind switch
                {
                    VarKind.Constant => SymbolKind.Constant,
                    VarKind.Global => SymbolKind.Variable,
                    VarKind.Static => SymbolKind.Variable,
                    VarKind.ScriptParameter => SymbolKind.Variable,
                    VarKind.Local => SymbolKind.Variable,
                    VarKind.Parameter => SymbolKind.Variable,
                    VarKind.Field => SymbolKind.Field,
                    _ => throw new InvalidOperationException("Unknown kind"),
                },
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
            };

        public override DocumentSymbol Visit(FunctionDeclaration node)
            => new()
            {
                Name = node.Name,
                Detail = $"({string.Join(", ", node.Parameters.Select(p => $"{p.Type.Name} {p.Name}"))})",
                Kind = SymbolKind.Function,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
            };

        public override DocumentSymbol Visit(NativeFunctionDeclaration node)
            => new()
            {
                Name = node.Name,
                Detail = $"({string.Join(", ", node.Parameters.Select(p => $"{p.Type.Name} {p.Name}"))})",
                Kind = SymbolKind.Function,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
            };

        public override DocumentSymbol Visit(FunctionPointerTypeDeclaration node)
            => new()
            {
                Name = node.Name,
                Detail = $"({string.Join(", ", node.Parameters.Select(p => $"{p.Type.Name} {p.Name}"))})",
                Kind = SymbolKind.Interface,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
            };

        public override DocumentSymbol Visit(ScriptDeclaration node)
            => new()
            {
                Name = node.Name,
                Detail = node.Parameters.Length == 0 ? null : $"({string.Join(", ", node.Parameters.Select(p => $"{p.Type.Name} {p.Name}"))})",
                Kind = SymbolKind.Event,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
            };

        public override DocumentSymbol Visit(GlobalBlockDeclaration node)
            => new()
            {
                Name = node.Name,
                Kind = SymbolKind.Package,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
                Children = node.Vars.Select(Visit).ToArray(),
            };
    }
}
