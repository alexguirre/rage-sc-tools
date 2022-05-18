namespace ScTools.ScriptAssembly;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public class Parser
{
    public sealed record Label(Token Name, Token Colon)
    {
        public SourceRange Location => Name.Location.Merge(Colon.Location);
    }

    public abstract record Line(Label? Label)
    {
        public abstract SourceRange Location { get; }
    }
    public sealed record EmptyLine(Label? Label, SourceRange Location) : Line(Label)
    {
        public override SourceRange Location { get; } = Location;
    }
    public sealed record Directive(Label? Label, Token Dot, Token Name, ImmutableArray<DirectiveOperand> Operands) : Line(Label)
    {
        public override SourceRange Location => Operands.Aggregate(Dot.Location.Merge(Name.Location), (acc, op) => acc.Merge(op.Location));
    }
    public abstract record DirectiveOperand()
    {
        public abstract SourceRange Location { get; }
    }
    public sealed record DirectiveOperandInteger(Token Integer) : DirectiveOperand()
    {
        public override SourceRange Location => Integer.Location;
    }
    public sealed record DirectiveOperandFloat(Token Float) : DirectiveOperand()
    {
        public override SourceRange Location => Float.Location;
    }
    public sealed record DirectiveOperandString(Token String) : DirectiveOperand()
    {
        public override SourceRange Location => String.Location;
    }
    public sealed record DirectiveOperandDup(Token Count/* Integer */, ImmutableArray<DirectiveOperand> InnerOperands) : DirectiveOperand()
    {
        public override SourceRange Location => InnerOperands.Aggregate(Count.Location, (acc, op) => acc.Merge(op.Location));
    }

    public sealed record Instruction(Label? Label, Token Opcode, ImmutableArray<InstructionOperand> Operands) : Line(Label)
    {
        public override SourceRange Location => Operands.Aggregate(Opcode.Location, (acc, op) => acc.Merge(op.Location));
    }

    public enum InstructionOperandType
    {
        Identifier, Integer, Float, SwitchCase
    }

    public record struct InstructionOperand(InstructionOperandType Type, Token A, Token B)
    {
        public SourceRange Location => A.Location.Merge(B.Location);
    }

    internal const string MissingIdentifierLexeme = "<unknown>";
    public static StringComparer CaseInsensitiveComparer => Assembler.CaseInsensitiveComparer;

    private readonly IEnumerator<Token> tokenEnumerator;
    private readonly Queue<Token> lookAheadTokens = new();

    public Lexer Lexer { get; }
    public DiagnosticsReport Diagnostics { get; }
    public bool IsAtEOF => Current.Kind is TokenKind.EOF;
    public bool IsAtEOS => Current.Kind is TokenKind.EOS or TokenKind.EOF;

    private Token Current { get; set; }
    private Diagnostic? LastError { get; set; }

    public Parser(Lexer lexer, DiagnosticsReport diagnostics, bool runPreprocessor = true)
    {
        Lexer = lexer;
        Diagnostics = diagnostics;
        tokenEnumerator = runPreprocessor ?
            new Preprocessor(diagnostics).Preprocess(lexer).GetEnumerator() :
            lexer.GetEnumerator();
        Next();
    }

    #region Rules
    public IEnumerable<Line> ParseProgram()
    {
        Accept(TokenKind.EOS, out _); // ignore empty lines at the beginning

        while (!IsAtEOF)
        {
            // line
            // : label? (directive | instruction)?
            // ;
            Label? label = null;
            if (IsPossibleLabel())
            {
                label = ParseLabel();
            }

            Line line;
            if (IsAtEOS)
            {
                line = new EmptyLine(null, Current.Location);
            }
            else if (IsPossibleDirective())
            {
                line = ParseDirective();
            }
            else if (IsPossibleInstruction())
            {
                line = ParseInstruction();
            }
            else
            {
                UnexpectedTokenError(TokenKind.Dot, TokenKind.Identifier);
                // skip the current line
                while (!IsAtEOS) { Next(); }
                ExpectEOS();
                continue;
            }

            ExpectEOS();
            yield return line with { Label = label };
        }
    }

    public bool IsPossibleLabel()
        => Peek(0).Kind is TokenKind.Identifier && Peek(1).Kind is TokenKind.Colon;
    public Label ParseLabel()
    {
        // label
        //     : identifier ':'
        //     ;
        ExpectOrMissing(TokenKind.Identifier, out var ident, MissingIdentifier);
        ExpectOrMissing(TokenKind.Colon, out var colon);
        return new(ident, colon);
    }

    public bool IsPossibleDirective()
        => Peek(0).Kind is TokenKind.Dot;
    public Directive ParseDirective()
    {
        // directive
        //     : '.' identifier directiveOperandList                  #floatDirective
        //     ;
        ExpectOrMissing(TokenKind.Dot, out var dot);
        ExpectOrMissing(TokenKind.Identifier, out var ident, MissingIdentifier);
        var operands = ImmutableArray<DirectiveOperand>.Empty;
        if (!IsAtEOS)
        {
            operands = ParseDirectiveOperandList();
        }
        return new(null, dot, ident, operands);
    }

    public ImmutableArray<DirectiveOperand> ParseDirectiveOperandList()
    {
        // directiveOperandList
        //     : directiveOperand (',' directiveOperand)*
        //     ;

        var list = ImmutableArray.CreateBuilder<DirectiveOperand>();
        while (!IsAtEOS)
        {
            list.Add(ParseDirectiveOperand());
            if (!Accept(TokenKind.Comma, out _))
            {
                break;
            }
        }

        return list.ToImmutable();
    }

    public DirectiveOperand ParseDirectiveOperand()
    {
        // directiveOperand
        //     | integer                                                       #integerDirectiveOperand
        //     | float                                                         #floatDirectiveOperand
        //     | string                                                        #stringDirectiveOperand
        //     | integer K_DUP '(' directiveOperandList ')'     #dupDirectiveOperand
        //     ;
        if (Peek(1) is { Kind: TokenKind.Identifier } dupToken &&
            dupToken.Lexeme.Span.Equals("dup", StringComparison.InvariantCultureIgnoreCase))
        {
            Expect(TokenKind.Integer, out var count);
            Expect(TokenKind.Identifier, out _); // dupToken
            ExpectOrMissing(TokenKind.OpenParen, out _);
            var operands = ParseDirectiveOperandList();
            ExpectOrMissing(TokenKind.CloseParen, out _);
            return new DirectiveOperandDup(count, operands);
        }

        if (Accept(TokenKind.Integer, out var integer))
        {
            return new DirectiveOperandInteger(integer);
        }
        else if (Accept(TokenKind.Float, out var floatToken))
        {
            return new DirectiveOperandFloat(floatToken);
        }
        else if (Accept(TokenKind.String, out var stringToken))
        {
            return new DirectiveOperandString(stringToken);
        }

        UnexpectedTokenError(TokenKind.Integer, TokenKind.Float, TokenKind.String);
        return new DirectiveOperandString(Token.String("<missing>", Current.Location));
    }

    public bool IsPossibleInstruction()
        => Peek(0).Kind is TokenKind.Identifier;
    public Instruction ParseInstruction()
    {
        // instruction
        //     : opcode operandList?
        //     ;
        ExpectOrMissing(TokenKind.Identifier, out var opcode, MissingIdentifier);
        var operands = ImmutableArray<InstructionOperand>.Empty;
        if (!IsAtEOS)
        {
            operands = ParseInstructionOperandList();
        }
        return new(null, opcode, operands);
    }

    public ImmutableArray<InstructionOperand> ParseInstructionOperandList()
    {
        // operandList
        //     : operand (',' operand)*
        //     ;

        var list = ImmutableArray.CreateBuilder<InstructionOperand>();
        while (!IsAtEOS)
        {
            list.Add(ParseInstructionOperand());
            if (!Accept(TokenKind.Comma, out _))
            {
                break;
            }
        }

        return list.ToImmutable();
    }

    public InstructionOperand ParseInstructionOperand()
    {
        // operand
        //     : identifier                                    #identifierOperand
        //     | integer                                       #integerOperand
        //     | float                                         #floatOperand
        //     | value=operand ':' jumpTo=operand              #switchCaseOperand
        //     ;
        if (ExpectEitherOrMissing(TokenKind.Identifier, TokenKind.Integer, TokenKind.Float, out var a, MissingIdentifier))
        {
            if (Accept(TokenKind.Colon, out _))
            {
                ExpectEitherOrMissing(TokenKind.Identifier, TokenKind.Integer, TokenKind.Float, out var b, MissingIdentifier);
                return new(InstructionOperandType.SwitchCase, a, b);
            }

            var type = a.Kind switch
            {
                TokenKind.Identifier => InstructionOperandType.Identifier,
                TokenKind.Integer => InstructionOperandType.Integer,
                TokenKind.Float => InstructionOperandType.Float,
                _ => throw new InvalidOperationException(),
            };
            return new(type, a, Missing(TokenKind.Bad.Create(string.Empty)));
        }

        var t = MissingIdentifier();
        return new(InstructionOperandType.Identifier, t, t);
    }
    #endregion Rules

    private Token Missing(Token token)
        => token with { IsMissing = true, Location = Current.Location };
    private Token Missing(TokenKind kind)
    {
        Debug.Assert(kind.HasCanonicalLexeme());
        return Missing(kind.Create());
    }
    private Token MissingIdentifier()
        => MissingIdentifier(MissingIdentifierLexeme);
    private Token MissingIdentifier(string identifier)
        => Missing(Token.Identifier(identifier));

    private void Error(ErrorCode code, string message, SourceRange location)
        => LastError = Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);

    private void UnexpectedTokenError(TokenKind expectedToken)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedToken}'", Current.Location);

    private void UnexpectedTokenError(TokenKind expectedTokenA, TokenKind expectedTokenB)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedTokenA}' or '{expectedTokenB}'", Current.Location);

    private void UnexpectedTokenError(TokenKind expectedTokenA, TokenKind expectedTokenB, TokenKind expectedTokenC)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedTokenA}', '{expectedTokenB}' or '{expectedTokenC}'", Current.Location);

    private Token Peek(int offset)
    {
        Debug.Assert(offset >= 0);

        if (offset == 0)
        {
            return Current;
        }

        if (lookAheadTokens.Count < offset)
        {
            while (lookAheadTokens.Count < offset)
            {
                tokenEnumerator.MoveNext();
                lookAheadTokens.Enqueue(tokenEnumerator.Current);
            }
        }
        return lookAheadTokens.ElementAt(offset - 1);
    }

    private void Next()
    {
        if (lookAheadTokens.TryDequeue(out var nextToken))
        {
            Current = nextToken;
        }
        else
        {
            tokenEnumerator.MoveNext();
            Current = tokenEnumerator.Current;
        }
    }

    private bool Accept(TokenKind token, out Token t)
    {
        t = Current;
        if (Current.Kind == token)
        {
            Next();
            return true;
        }

        return false;
    }

    private bool Accept(Predicate<TokenKind> tokenPredicate, out Token t)
    {
        t = Current;
        if (tokenPredicate(Current.Kind))
        {
            Next();
            return true;
        }

        return false;
    }

    private bool Expect(TokenKind token, out Token t)
    {
        if (Accept(token, out t))
        {
            return true;
        }

        UnexpectedTokenError(token);
        return false;
    }

    private bool ExpectOrMissing(TokenKind token, out Token t, Func<Token>? createMissingToken = null)
    {
        if (!Expect(token, out t))
        {
            t = createMissingToken is not null ? createMissingToken() : Missing(token);
            return false;
        }
        return true;
    }

    //private bool Expect<TNode>(Func<TNode> parseFunc, out TNode node) where TNode : INode
    //{
    //    node = parseFunc();
    //    return node is not IError;
    //}

    private bool AcceptEOS() => AcceptEither(TokenKind.EOS, TokenKind.EOF, out _);
    private bool ExpectEOS() => ExpectEither(TokenKind.EOS, TokenKind.EOF, out _);

    private bool AcceptEither(TokenKind tokenA, TokenKind tokenB, out Token t) => Accept(tokenA, out t) || Accept(tokenB, out t);
    private bool AcceptEither(TokenKind tokenA, TokenKind tokenB, TokenKind tokenC, out Token t) => Accept(tokenA, out t) || Accept(tokenB, out t) || Accept(tokenC, out t);
    private bool ExpectEither(TokenKind tokenA, TokenKind tokenB, out Token t)
    {
        if (AcceptEither(tokenA, tokenB, out t))
        {
            return true;
        }

        UnexpectedTokenError(tokenA, tokenB);
        return false;
    }
    private bool ExpectEither(TokenKind tokenA, TokenKind tokenB, TokenKind tokenC, out Token t)
    {
        if (AcceptEither(tokenA, tokenB, tokenC, out t))
        {
            return true;
        }

        UnexpectedTokenError(tokenA, tokenB, tokenC);
        return false;
    }
    private bool ExpectEitherOrMissing(TokenKind tokenA, TokenKind tokenB, TokenKind tokenC, out Token t, Func<Token>? createMissingToken = null)
    {
        if (!ExpectEither(tokenA, tokenB, tokenC, out t))
        {
            t = createMissingToken is not null ? createMissingToken() : Missing(tokenA);
            return false;
        }
        return true;
    }
}
