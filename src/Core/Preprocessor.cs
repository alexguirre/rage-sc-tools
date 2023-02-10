namespace ScTools;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public interface IPreprocessor<TToken, TTokenKind, TErrorCode>
    where TToken : struct, IToken<TToken, TTokenKind>
    where TTokenKind : struct, Enum
    where TErrorCode : struct, Enum
{
    void Define(string symbol);
    void Undefine(string symbol);
    bool IsDefined(string symbol);
    IEnumerable<TToken> Preprocess(IEnumerable<TToken> tokens);
}

public abstract class PreprocessorBase<TToken, TTokenKind, TErrorCode> : IPreprocessor<TToken, TTokenKind, TErrorCode>
    where TToken : struct, IToken<TToken, TTokenKind>
    where TTokenKind : struct, Enum
    where TErrorCode : struct, Enum
{
    private enum ConditionState
    {
        /// <summary>
        /// Default state, emitting.
        /// </summary>
        Root = 0,
        /// <summary>
        /// `#if` condition evaluated to true, emitting this branch.
        /// </summary>
        BranchTrue,
        /// <summary>
        /// `#if` condition evaluated to false, not emitting this branch.
        /// </summary>
        BranchFalse,
        /// <summary>
        /// Nested `#if` when parent branch is not emitting.
        /// </summary>
        BranchDisabled
    }

    public const string IfDirective = "if";
    public const string EndIfDirective = "endif";
    public const string NotOperand = "not";

    private readonly TokenSet tokenSet;
    private readonly ErrorSet errorSet;

    private readonly Stack<ConditionState> conditionStack = new();
    private readonly HashSet<string> definitions = new();


    protected bool IsAtRoot => conditionStack.Peek() is ConditionState.Root;
    protected bool IsEmitting => conditionStack.Peek() is ConditionState.Root or ConditionState.BranchTrue;

    public DiagnosticsReport Diagnostics { get; }

    public PreprocessorBase(DiagnosticsReport diagnostics, TokenSet tokenSet, ErrorSet errorSet)
    {
        Diagnostics = diagnostics;
        this.tokenSet = tokenSet;
        this.errorSet = errorSet;

        conditionStack.Push(ConditionState.Root);
    }

    public void Define(string symbol) => definitions.Add(symbol);
    public void Undefine(string symbol) => definitions.Remove(symbol);
    public bool IsDefined(string symbol) => definitions.Contains(symbol);

    protected void Error(TErrorCode code, string message, SourceRange location)
    => Diagnostics.Add(code.AsInteger<TErrorCode, int>(), DiagnosticTag.Error, message, location);

    protected void ExpectedSymbolError(TToken foundToken)
        => Error(errorSet.UnexpectedToken, $"Unexpected token '{foundToken.Kind}', expected preprocessor symbol", foundToken.Location);
    protected void ExpectedDirectiveError(TToken foundToken)
        => Error(errorSet.UnexpectedToken, $"Unexpected token '{foundToken.Kind}', expected preprocessor directive", foundToken.Location);
    protected void UnknownDirectiveError(TToken foundToken)
        => Error(errorSet.UnknownDirective, $"Unknown preprocessor directive '{foundToken.Lexeme}'", foundToken.Location);

    public IEnumerable<TToken> Preprocess(IEnumerable<TToken> tokens)
    {
        var tokenEnumerator = tokens.GetEnumerator();
        TTokenKind prevTokenKind = default;
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;

            if (TokenKindEquals(token.Kind, tokenSet.Hash))
            {
                ParseDirective(tokenEnumerator);
            }
            else
            {
                var inLineStart = TokenKindEquals(token.Kind, tokenSet.EOS);

                // Do not return multiple sequential EOS tokens, this may happen when leaving a disabled branch.
                // Parsers assume that there are no sequential EOS tokens.
                var isSequentialEOS = inLineStart && TokenKindEquals(prevTokenKind, tokenSet.EOS);

                if (IsEmitting && !isSequentialEOS)
                {
                    prevTokenKind = token.Kind;
                    yield return token;
                }
            }
        }

        if (!IsAtRoot)
        {
            // TODO: error unclosed #if
            throw new NotImplementedException(" TODO: error unclosed #if");
        }

        if (!TokenKindEquals(prevTokenKind, tokenSet.EOF))
        {
            // The EOF token may have been consumed by one of the directives parsers and not yielded in the main loop
            // Return EOF here instead.
            Debug.Assert(TokenKindEquals(tokenEnumerator.Current.Kind, tokenSet.EOF), "The enumerator should have already reached EOF");
            yield return tokenEnumerator.Current;
        }
    }

    private void ParseDirective(IEnumerator<TToken> tokenEnumerator)
    {
        var directive = tokenEnumerator.MoveNext() && !IsEOS(tokenEnumerator.Current) ? tokenEnumerator.Current.Lexeme.Span : default;
        if (directive.IsEmpty)
        {
            ExpectedDirectiveError(tokenEnumerator.Current);
            return;
        }

        const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

        if (directive.Equals(IfDirective, IgnoreCase))
        {
            ParseIf(tokenEnumerator);
        }
        else if (directive.Equals(EndIfDirective, IgnoreCase))
        {
            ParseEndIf(tokenEnumerator);
        }
        else
        {
            UnknownDirectiveError(tokenEnumerator.Current);
            return;
        }
    }

    private void PushCondition(ConditionState condition) => conditionStack.Push(condition);
    private ConditionState PopCondition()
    {
        Debug.Assert(!IsAtRoot, "Cannot pop root condition");
        return conditionStack.Pop();
    }
    
    /// <summary>
    /// Parses and processes `#if` directives.
    /// </summary>
    private void ParseIf(IEnumerator<TToken> tokenEnumerator)
    {
        var not = false;
        var hasNext = tokenEnumerator.MoveNext();
        if (hasNext && tokenEnumerator.Current.Lexeme.Span.Equals(NotOperand,StringComparison.OrdinalIgnoreCase))
        {
            not = true;
            hasNext = tokenEnumerator.MoveNext();
        }
        
        var symbol = hasNext && !IsEOS(tokenEnumerator.Current) ? tokenEnumerator.Current.Lexeme : default;
        if (symbol.IsEmpty)
        {
            ExpectedSymbolError(tokenEnumerator.Current);
            return;
        }

        if (IsEmitting)
        {
            var defined = IsDefined(symbol.ToString());
            defined ^= not;
            PushCondition(defined ? ConditionState.BranchTrue : ConditionState.BranchFalse);
        }
        else
        {
            PushCondition(ConditionState.BranchDisabled);
        }
    }

    /// <summary>
    /// Parses and processes `#endif` directives.
    /// </summary>
    private void ParseEndIf(IEnumerator<TToken> tokenEnumerator)
    {
        if (IsAtRoot)
        {
            // TODO: error #endif without #if
            throw new NotImplementedException(" TODO: error #endif without #if");
            return;
        }

        PopCondition();
    }

    private bool IsEOS(TToken token) => IsEOS(token.Kind);
    private bool IsEOS(TTokenKind kind)
        => TokenKindEquals(kind, tokenSet.EOS) || TokenKindEquals(kind, tokenSet.EOF);

    public readonly record struct TokenSet(
        TTokenKind Hash,
        TTokenKind EOS,
        TTokenKind EOF);
    public readonly record struct ErrorSet(
        TErrorCode DirectiveWrongPlacement,
        TErrorCode UnknownDirective,
        TErrorCode UnexpectedToken);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TokenKindEquals(TTokenKind a, TTokenKind b)
        => EqualityComparer<TTokenKind>.Default.Equals(a, b);
}
