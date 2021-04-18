namespace ScTools.ScriptAssembly.Grammar
{
    using System;
    using System.Diagnostics;

    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;

    // based on Light* classes from here: https://github.com/PositiveTechnologies/PT.PM/tree/dev/Sources/PT.PM.AntlrUtils

    public readonly struct LightToken : IToken
    {
        private readonly LightInputStream inputStream;
        private readonly sbyte type;
        private readonly sbyte channel;

        public int StartIndex { get; }
        public int StopIndex { get; }
        public int TokenIndex { get; }
        public int Type => type;
        public int Channel => channel;
        public int Line { get; }
        public int Column { get; }
        public ICharStream InputStream => inputStream;
        public ITokenSource TokenSource => new LightTokenSource(InputStream);

        public string Text => Type == Lexer.Eof
            ? "EOF"
            : new string(inputStream.Input.AsSpan(StartIndex, StopIndex - StartIndex + 1));

        public ReadOnlyMemory<char> TextMemory => Type == Lexer.Eof
            ? ReadOnlyMemory<char>.Empty
            : inputStream.Input.AsMemory(StartIndex, StopIndex - StartIndex + 1);

        public LightToken(LightInputStream inputStream, int type, int channel, int index, int start, int stop, int line, int charPositionInLine)
        {
            Debug.Assert(type is >= sbyte.MaxValue and <= sbyte.MaxValue);
            Debug.Assert(channel is >= sbyte.MaxValue and <= sbyte.MaxValue);

            this.inputStream = inputStream;
            this.type = (sbyte)type;
            this.channel = (sbyte)channel;
            StartIndex = start;
            StopIndex = stop;
            TokenIndex = index;
            Line = line;
            Column = charPositionInLine;
        }
    }

    public readonly struct LightTokenSource : ITokenSource
    {
        public LightTokenSource(ICharStream inputStream)
        {
            InputStream = inputStream;
        }

        public ICharStream InputStream { get; }

        public IToken NextToken() => throw new NotImplementedException();
        public int Line => throw new NotImplementedException();
        public int Column => throw new NotImplementedException();
        public string SourceName => throw new NotImplementedException();
        public ITokenFactory TokenFactory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public class LightTokenFactory : ITokenFactory
    {
        private int index;

        public IToken Create(Tuple<ITokenSource, ICharStream> source, int type, string text, int channel, int start, int stop, int line, int charPositionInLine)
            => new LightToken((LightInputStream)source.Item2, type, channel, index++, start, stop, line, charPositionInLine);

        public IToken Create(int type, string text) => throw new NotImplementedException();
    }

    public class LightInputStream : ICharStream
    {
        public string Input { get; }
        public int Index { get; private set; }
        public int Size => Input.Length;
        public string SourceName => IntStreamConstants.UnknownSourceName;

        public LightInputStream(string input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public void Consume()
        {
            if (Index >= Size)
                throw new InvalidOperationException("cannot consume EOF");
            ++Index;
        }

        public int LA(int i)
        {
            if (i == 0)
            {
                throw new ArgumentException("i == 0");
            }

            if (i < 0)
            {
                i++; // e.g., translate LA(-1) to use offset i=0; then Input[Index + 0 - 1]
                if (Index + i - 1 < 0)
                {
                    return IntStreamConstants.EOF; // invalid; no char before first char
                }
            }

            if (Index + i - 1 >= Size)
            {
                return IntStreamConstants.EOF;
            }

            return Input[Index + i - 1];
        }

        public int Mark() => -1;
        public void Release(int marker) { }

        public void Seek(int index) => Index = Math.Min(index, Size);

        public string GetText(Interval interval)
            => Input.Substring(interval.a, interval.b - interval.a + 1);

        public ReadOnlyMemory<char> GetTextMemory(int startIndex, int stopIndex)
            => Input.AsMemory(startIndex, stopIndex - startIndex + 1);
    }
}
