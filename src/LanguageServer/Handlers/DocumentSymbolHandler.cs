namespace ScTools.LanguageServer.Handlers;

using ScTools.LanguageServer.Services;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;

public class DocumentSymbolHandler : ILspRequestHandler<DocumentSymbolParams, DocumentSymbol[]>
{
    private readonly ILogger<DocumentSymbolHandler> logger;
    private readonly ITextDocumentTracker textDocumentTracker;
    private readonly NodeToDocumentSymbol nodeToSymbol = new ();

    public string MethodName => Methods.TextDocumentDocumentSymbolName;

    public DocumentSymbolHandler(ITextDocumentTracker textDocumentTracker, ILogger<DocumentSymbolHandler> logger)
    {
        this.logger = logger;
        this.textDocumentTracker = textDocumentTracker;
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        capabilities.DocumentSymbolProvider = true;
    }

    public async Task<DocumentSymbol[]> HandleAsync(DocumentSymbolParams param, CancellationToken cancellationToken)
    {
        try
        {
            var ast = await textDocumentTracker.GetDocumentAstAsync(param.TextDocument.Uri);
            if (ast is null)
            {
                return Array.Empty<DocumentSymbol>();
            }

            var symbols = new List<DocumentSymbol>(capacity: ast.Declarations.Length);
            foreach (var decl in ast.Declarations)
            {
                var symbol = decl.Accept(nodeToSymbol);
                if (symbol is not null)
                {
                    symbols.Add(symbol);
                }
            }

            return symbols.ToArray();
        }
        catch (Exception e)
        {
            logger.LogError(e,
                "Failed to handle '{method}' request for document '{uri}'",
                MethodName,
                param.TextDocument.Uri);
            throw;
        }
    }

    private class NodeToDocumentSymbol : AstVisitor<DocumentSymbol?>
    {
        public override DocumentSymbol? Visit(ErrorDeclaration node)
            => null;

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

        public override DocumentSymbol Visit(FunctionTypeDefDeclaration node)
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

        public override DocumentSymbol Visit(NativeTypeDeclaration node)
            => new()
            {
                Name = node.Name,
                Kind = SymbolKind.Struct,
                Range = ProtocolConversions.ToLspRange(node.Location),
                SelectionRange = ProtocolConversions.ToLspRange(node.NameToken.Location),
            };
    }
}
