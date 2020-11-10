namespace ScTools.LanguageServer
{
    using ScTools.ScriptLang;

    using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
    using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;

    internal static class ConversionExtensions
    {
        public static LspRange ToLspRange(this SourceRange r)
            => r.IsUnknown ? new LspRange() : new LspRange
            {
                Start = r.Start.ToLspPosition(),
                End = r.End.ToLspPosition(),
            };

        public static LspPosition ToLspPosition(this SourceLocation l)
            => l.IsUnknown ? new LspPosition() : new LspPosition(l.Line - 1, l.Column - 1);

        public static SourceLocation ToSourceLocation(this LspPosition p)
            => new SourceLocation(p.Line + 1, p.Character + 1);
    }
}
