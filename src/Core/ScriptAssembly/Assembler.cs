#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Antlr4.Runtime;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Grammar;

    public class Assembler
    {
        public const string DefaultScriptName = "unknown";

        public DiagnosticsReport Diagnostics { get; }
        public Script OutputScript { get; }
        public Dictionary<string, ConstantValue> Constants { get; }
        public bool HasScriptName { get; private set; }
        public bool HasScriptHash { get; private set; }

        public Assembler(string filePath)
        {
            Diagnostics = new(filePath);
            OutputScript = new()
            {
                Name = DefaultScriptName,
                NameHash = DefaultScriptName.ToLowercaseHash(),
            };
            Constants = new(CaseInsensitiveComparer);
        }

        public void Assemble(TextReader input)
        {
            var inputStream = new AntlrInputStream(input);

            var lexer = new ScAsmLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(this));
            var tokens = new CommonTokenStream(lexer);
            var parser = new ScAsmParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(this));

            var program = parser.program();
            foreach (var line in program.line())
            {
                ProcessLine(line);
            }
        }

        private void ProcessLine(ScAsmParser.LineContext line)
        {
            var label = line.label();
            var segmentDirective = line.segmentDirective();
            var directive = line.directive();
            var instruction = line.instruction();

            if (label is not null)
            {
                // TODO: do something
            }

            if (segmentDirective is not null)
            {
                // TODO: do something
            }
            else if (directive is not null)
            {
                ProcessDirective(directive);
            }
            else if (instruction is not null)
            {
                // TODO: do something
            }
        }

        private void ProcessDirective(ScAsmParser.DirectiveContext directive)
        {
            switch (directive)
            {
                case ScAsmParser.ScriptNameDirectiveContext nameDirective:
                    if (HasScriptName)
                    {
                        Diagnostics.AddError($"Directive '.script_name' is repeated", Source(nameDirective));
                    }
                    else
                    {
                        OutputScript.Name = nameDirective.identifier().GetText();
                        OutputScript.NameHash = OutputScript.Name.ToLowercaseHash();
                        HasScriptName = true;
                    }
                    break;
                case ScAsmParser.ScriptHashDirectiveContext hashDirective:
                    if (HasScriptHash)
                    {
                        Diagnostics.AddError($"Directive '.script_hash' is repeated", Source(hashDirective));
                    }
                    else
                    {
                        OutputScript.Hash = (uint)hashDirective.integer().GetText().ParseAsInt();
                        HasScriptHash = true;
                    }
                    break;
                case ScAsmParser.ConstDirectiveContext constDirective:
                    var constName = constDirective.identifier();
                    var constInteger = constDirective.integer();
                    var constFloat = constDirective.@float();

                    var constValue = constInteger != null ?
                                        new ConstantValue(constInteger.GetText().ParseAsInt()) :
                                        new ConstantValue(constFloat.GetText().ParseAsFloat());

                    if (!Constants.TryAdd(constName.GetText(), constValue))
                    {
                        Diagnostics.AddError($"Constant '{constName}' already defined", Source(constName));
                    }
                    break;
            }
        }


        public static Assembler Assemble(TextReader input, string filePath = "tmp.sc")
        {
            var a = new Assembler(filePath);
            a.Assemble(input);
            return a;
        }

        public static StringComparer CaseInsensitiveComparer => StringComparer.OrdinalIgnoreCase;

        private static SourceRange Source(ParserRuleContext context) => SourceRange.FromTokens(context.Start, context.Stop);

        /// <summary>
        /// Adds syntax errors to diagnostics report.
        /// </summary>
        private sealed class SyntaxErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
        {
            private readonly Assembler assembler;

            public SyntaxErrorListener(Assembler assembler) => this.assembler = assembler;

            public void SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                var source = offendingSymbol is IToken t ?
                                SourceRange.FromTokens(t, null) :
                                new SourceRange(new SourceLocation(line, charPositionInLine),
                                                new SourceLocation(line, charPositionInLine));
                assembler.Diagnostics.AddError(msg, source);
            }
        }

        public struct ConstantValue
        {
            public int Integer { get; }
            public float Float { get; }

            public ConstantValue(int value) => (Integer, Float) = (value, value);
            public ConstantValue(float value) => (Integer, Float) = ((int)Math.Truncate(value), value);
        }
    }
}
