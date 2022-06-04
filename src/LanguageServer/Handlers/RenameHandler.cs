namespace ScTools.LanguageServer.Handlers;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using ScTools.LanguageServer.Services;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class RenameHandler : ILspRequestHandler<RenameParams, WorkspaceEdit?>
{
    private readonly ITextDocumentTracker textDocumentTracker;

    public string MethodName => Methods.TextDocumentRenameName;

    public RenameHandler(ITextDocumentTracker textDocumentTracker)
    {
        this.textDocumentTracker = textDocumentTracker;
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        capabilities.RenameProvider = true;
    }

    public Task<WorkspaceEdit?> HandleAsync(RenameParams param, CancellationToken cancellationToken)
    {
        var pos = ProtocolConversions.FromLspPosition(param.Position);
        var ast = textDocumentTracker.GetDocumentAst(param.TextDocument.Uri);
        var node = AstNodeLocator.Locate(ast, pos);

        IDeclaration? declToRename = null;
        if (node is IDeclaration decl && decl.NameToken.Location.Contains(pos))
        {
            declToRename = decl;
        }
        else if (node is NameExpression nameExpr)
        {
            declToRename = nameExpr.Semantics.Symbol as IDeclaration;
        }

        if (declToRename == null)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }

        var edits = new List<TextEdit>();
        SearchAllChanges(ast, declToRename, edits, param.NewName);

        var result = new WorkspaceEdit
        {
            DocumentChanges = new[]
            {
                new TextDocumentEdit
                {
                    TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = param.TextDocument.Uri },
                    Edits = edits.ToArray(),
                }
            }
        };

        return Task.FromResult<WorkspaceEdit?>(result);

        static void SearchAllChanges(INode where, IDeclaration decl, List<TextEdit> changes, string newName)
        {
            if (where == decl)
            {
                changes.Add(CreateEdit(decl.NameToken, newName));
            }

            if (where is NameExpression nameExpr && nameExpr.Semantics.Symbol == decl)
            {
                changes.Add(CreateEdit(nameExpr.NameToken, newName));
            }

            foreach (var childNode in where.Children)
            {
                SearchAllChanges(childNode, decl, changes, newName);
            }
        }

        static TextEdit CreateEdit(Token identifier, string newName)
        {
            Debug.Assert(identifier.Kind is TokenKind.Identifier);
            return new()
            {
                Range = ProtocolConversions.ToLspRange(identifier.Location),
                NewText = newName,
            };
        }
    }
}
