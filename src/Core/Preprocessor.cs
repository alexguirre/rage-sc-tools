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
    void Define(string symbol, IEnumerable<TToken>? replacementTokens);
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
        /// `#ifdef`/`#elifdef`/`#else` condition evaluated to true, emitting this branch.
        /// </summary>
        BranchTrue,
        /// <summary>
        /// `#ifdef`/`#elifdef`/`#else` condition evaluated to false, not emitting this branch.
        /// </summary>
        BranchFalse,
        /// <summary>
        /// Inside `#elifdef`/`#else` when a previous `#ifdef`/`#elifdef` was already emitted.
        /// </summary>
        BranchAlreadyTaken,
        /// <summary>
        /// Nested `#ifdef` when parent branch is not emitting.
        /// </summary>
        BranchDisabled
    }

    private readonly record struct Definition(string Name, ImmutableArray<TToken> ReplacementTokens = default);

    public const string IfDefDirective = "ifdef";
    public const string IfNotDefDirective = "ifndef";
    public const string ElifDefDirective = "elifdef";
    public const string ElifNotDefDirective = "elifndef";
    public const string ElseDirective = "else";
    public const string EndIfDirective = "endif";
    public const string DefineDirective = "define";
    public const string UndefDirective = "undef";

    private readonly TokenSet tokenSet;
    private readonly ErrorSet errorSet;

    private readonly Stack<ConditionState> conditionStack = new();
    private readonly Dictionary<string, Definition> definitions = new();


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

    public void Define(string symbol) => definitions[symbol] = new(symbol);
    public void Define(string symbol, IEnumerable<TToken>? replacementTokens) => definitions[symbol] = new(symbol, replacementTokens?.ToImmutableArray() ?? default);
    public void Undefine(string symbol) => definitions.Remove(symbol);
    public bool IsDefined(string symbol) => definitions.ContainsKey(symbol);

    protected void Error(TErrorCode code, string message, SourceRange location)
    => Diagnostics.Add(code.AsInteger<TErrorCode, int>(), DiagnosticTag.Error, message, location);

    protected void UnexpectedTokenError(TToken foundToken, TTokenKind expectedToken)
        => Error(errorSet.UnexpectedToken, $"Unexpected token '{foundToken.Kind}', expected '{expectedToken}'", foundToken.Location);
    protected void ExpectedSymbolError(TToken foundToken)
        => Error(errorSet.UnexpectedToken, $"Unexpected token '{foundToken.Kind}', expected preprocessor symbol", foundToken.Location);
    protected void ExpectedDirectiveError(TToken foundToken)
        => Error(errorSet.UnexpectedToken, $"Unexpected token '{foundToken.Kind}', expected preprocessor directive", foundToken.Location);
    protected void UnknownDirectiveError(TToken foundToken)
        => Error(errorSet.UnknownDirective, $"Unknown preprocessor directive '{foundToken.Lexeme}'", foundToken.Location);
    protected void DirectiveNotAtLineStartError(TToken hashToken)
        => Error(errorSet.DirectiveWrongPlacement, $"Preprocessor directives must appear at the line beginning", hashToken.Location);

    public IEnumerable<TToken> Preprocess(IEnumerable<TToken> tokens)
    {
        bool inLineStart = true;

        var tokenEnumerator = tokens.GetEnumerator();
        TTokenKind prevTokenKind = default;
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;

            if (TokenKindEquals(token.Kind, tokenSet.Hash))
            {
                if (inLineStart)
                {
                    ParseDirective(tokenEnumerator);
                }
                else
                {
                    DirectiveNotAtLineStartError(token);
                    continue;
                }
            }
            else
            {
                inLineStart = TokenKindEquals(token.Kind, tokenSet.EOS);

                // Do not return multiple sequential EOS tokens, this may happen when leaving a disabled branch.
                // Parsers assume that there are no sequential EOS tokens.
                var isSequentialEOS = inLineStart && TokenKindEquals(prevTokenKind, tokenSet.EOS);

                if (IsEmitting && !isSequentialEOS) 
                {
                    foreach (var expandedToken in Expand(token))
                    {
                        prevTokenKind = expandedToken.Kind;
                        yield return expandedToken;
                    }
                }
            }
        }

        if (!IsAtRoot)
        {
            // TODO: error unclosed ifdef/ifndef
            throw new NotImplementedException(" TODO: error unclosed ifdef/ifndef");
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

        if (directive.Equals(IfDefDirective, IgnoreCase))
        {
            ParseIfDef(tokenEnumerator, not: false);
        }
        else if (directive.Equals(IfNotDefDirective, IgnoreCase))
        {
            ParseIfDef(tokenEnumerator, not: true);
        }
        else if (directive.Equals(ElifDefDirective, IgnoreCase))
        {
            ParseElifDef(tokenEnumerator, not: false);
        }
        else if (directive.Equals(ElifNotDefDirective, IgnoreCase))
        {
            ParseElifDef(tokenEnumerator, not: true);
        }
        else if (directive.Equals(ElseDirective, IgnoreCase))
        {
            ParseElse(tokenEnumerator);
        }
        else if (directive.Equals(EndIfDirective, IgnoreCase))
        {
            ParseEndIf(tokenEnumerator);
        }
        else if (directive.Equals(DefineDirective, IgnoreCase))
        {
            ParseDefine(tokenEnumerator);
        }
        else if (directive.Equals(UndefDirective, IgnoreCase))
        {
            ParseUndef(tokenEnumerator);
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
    /// Parses and processes `#ifdef` or `#ifndef` directives.
    /// </summary>
    /// <param name="not"><c>true</c> if parsing `#ifndef`; otherwise, <c>false</c>.</param>
    private void ParseIfDef(IEnumerator<TToken> tokenEnumerator, bool not)
    {
        var symbol = tokenEnumerator.MoveNext() && !IsEOS(tokenEnumerator.Current) ? tokenEnumerator.Current.Lexeme : default;
        if (symbol.IsEmpty)
        {
            ExpectedSymbolError(tokenEnumerator.Current);
            return;
        }
        ExpectEOS(tokenEnumerator);

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
    /// Parses and processes `#elifdef` or `#elifndef` directives.
    /// </summary>
    /// <param name="not"><c>true</c> if parsing `#elifndef`; otherwise, <c>false</c>.</param>
    private void ParseElifDef(IEnumerator<TToken> tokenEnumerator, bool not)
    {
        var symbol = tokenEnumerator.MoveNext() && !IsEOS(tokenEnumerator.Current) ? tokenEnumerator.Current.Lexeme : default;
        if (symbol.IsEmpty)
        {
            ExpectedSymbolError(tokenEnumerator.Current);
            return;
        }
        ExpectEOS(tokenEnumerator);

        var c = PopCondition();
        switch (c)
        {
            case ConditionState.BranchDisabled:
                PushCondition(ConditionState.BranchDisabled);
                break;
            case ConditionState.BranchTrue:
            case ConditionState.BranchAlreadyTaken:
                PushCondition(ConditionState.BranchAlreadyTaken);
                break;
            case ConditionState.BranchFalse:
                var defined = IsDefined(symbol.ToString());
                defined ^= not;
                PushCondition(defined ? ConditionState.BranchTrue : ConditionState.BranchFalse);
                break;
        }
    }

    /// <summary>
    /// Parses and processes `#else` directives.
    /// </summary>
    private void ParseElse(IEnumerator<TToken> tokenEnumerator)
    {
        ExpectEOS(tokenEnumerator);

        if (IsAtRoot)
        {
            // TODO: error #else without #ifdef
            throw new NotImplementedException(" TODO: error #else without #ifdef");
            return;
        }

        var c = PopCondition();
        switch (c)
        {
            case ConditionState.BranchDisabled:
                PushCondition(ConditionState.BranchDisabled);
                break;
            case ConditionState.BranchTrue:
            case ConditionState.BranchAlreadyTaken:
                PushCondition(ConditionState.BranchAlreadyTaken);
                break;
            case ConditionState.BranchFalse:
                PushCondition(ConditionState.BranchTrue);
                break;
        }
    }

    /// <summary>
    /// Parses and processes `#endif` directives.
    /// </summary>
    private void ParseEndIf(IEnumerator<TToken> tokenEnumerator)
    {
        ExpectEOS(tokenEnumerator);

        if (IsAtRoot)
        {
            // TODO: error #endif without #ifdef
            throw new NotImplementedException(" TODO: error #endif without #ifdef");
            return;
        }

        PopCondition();
    }

    /// <summary>
    /// Parses and processes `#define` directives.
    /// </summary>
    private void ParseDefine(IEnumerator<TToken> tokenEnumerator)
    {
        var symbol = tokenEnumerator.MoveNext() && !IsEOS(tokenEnumerator.Current) ? tokenEnumerator.Current.Lexeme : default;
        if (symbol.IsEmpty)
        {
            ExpectedSymbolError(tokenEnumerator.Current);
            return;
        }

        List<TToken>? replacementTokens = null;
        while (tokenEnumerator.MoveNext() && !IsEOS(tokenEnumerator.Current))
        {
            if (!IsEmitting)
            {
                continue;
            }

            var tokens = Expand(tokenEnumerator.Current);
            if (replacementTokens is null)
            {
                replacementTokens = new(tokens);
            }
            else
            {
                replacementTokens.AddRange(tokens);
            }
        }
        ExpectEOS(tokenEnumerator, advance: false);

        if (IsEmitting)
        {
            Define(symbol.ToString(), replacementTokens);
        }
    }

    /// <summary>
    /// Parses and processes `#undef` directives.
    /// </summary>
    private void ParseUndef(IEnumerator<TToken> tokenEnumerator)
    {
        var symbol = tokenEnumerator.MoveNext() && !IsEOS(tokenEnumerator.Current) ? tokenEnumerator.Current.Lexeme : default;
        if (symbol.IsEmpty)
        {
            ExpectedSymbolError(tokenEnumerator.Current);
            return;
        }
        ExpectEOS(tokenEnumerator);

        if (IsEmitting)
        {
            Undefine(symbol.ToString());
        }
    }

    private ImmutableArray<TToken> Expand(TToken token)
    {
        if (!definitions.TryGetValue(token.Lexeme.ToString(), out var definition))
        {
            // no expansion
            return ImmutableArray.Create(token);
        }

        return definition.ReplacementTokens;
    }

    private void ExpectEOS(IEnumerator<TToken> tokenEnumerator, bool advance = true)
    {
        var isEOS = true;
        if (advance && !tokenEnumerator.MoveNext())
        {
            isEOS = false;
        }
        else
        {
            isEOS = IsEOS(tokenEnumerator.Current);
        }

        if (!isEOS)
        {
            UnexpectedTokenError(tokenEnumerator.Current, tokenSet.EOS);
        }
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
