namespace ScTools
{
    using System;

    internal static class Token
    {
        public const char CommentChar = ';';
        public const char TargetLabelPrefix = '@';

        public static bool IsString(ReadOnlySpan<char> token, out ReadOnlySpan<char> strContents)
        {
            if (token.Length < 2 || token[0] != '"' || token[^1] != '"')
            {
                strContents = default;
                return false;
            }

            strContents = token[1..^1]; // remove quotes
            return true;
        }

        public static bool IsTargetLabel(ReadOnlySpan<char> token, out ReadOnlySpan<char> label)
        {
            if (token.Length <= 1 || token[0] != TargetLabelPrefix)
            {
                label = default;
                return false;
            }

            label = token[1..]; // remove prefix
            return true;
        }

        public static TokenEnumerable Tokenize(ReadOnlySpan<char> line) => new TokenEnumerable(line);
    }

    internal readonly ref struct TokenEnumerable
    {
        public ReadOnlySpan<char> Line { get; }

        public TokenEnumerable(ReadOnlySpan<char> line) => Line = line;

        public TokenEnumerator GetEnumerator() => new TokenEnumerator(Line);
    }

    internal ref struct TokenEnumerator
    {
        private ReadOnlySpan<char> line;
        private int currentLength;

        public TokenEnumerator(ReadOnlySpan<char> line) : this() => this.line = line;

        public bool MoveNext()
        {
            if (currentLength >= line.Length)
            {
                return false;
            }

            line = line.Slice(currentLength);
            if (line.Length > 0)
            {
                // skip until we find non-whitespace character
                int i = 0;
                while (i < line.Length && char.IsWhiteSpace(line[i])) { i++; }

                line = line.Slice(i);

                if (line.Length > 0 && line[0] == Token.CommentChar)
                {
                    // found a comment, ignore the rest of the line;
                    return false;
                }

                bool isString = line.Length > 0 && line[0] == '"';

                if (isString)
                {
                    // continue until we find the closing quotation mark
                    currentLength = 1;
                    while (currentLength < line.Length && line[currentLength] != '"') { currentLength++; }

                    currentLength++; // include the quotation mark in the token

                    // check if we actually found the closing quotation mark
                    if (currentLength > line.Length || line[currentLength - 1] != '"')
                    {
                        throw new AssemblerSyntaxException($"Unclosed string '{line.ToString()}'");
                    }
                }
                else
                {
                    // continue until we find a whitespace character
                    currentLength = 0;
                    while (currentLength < line.Length && !char.IsWhiteSpace(line[currentLength])) { currentLength++; }
                }

                return currentLength > 0;
            }

            return false;
        }

        public ReadOnlySpan<char> Current => line.Slice(0, currentLength);
    }
}
