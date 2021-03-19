#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Antlr4.Runtime;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Grammar;

    public class Assembler
    {
        public const string DefaultScriptName = "unknown";

        public enum Segment { None, Global, Static, Arg, String, Code, Include }

        public static int GetAddressingUnitByteSize(Segment segment) => segment switch
        {
            // ScriptValue-addressable
            Segment.Global or Segment.Static or Segment.Arg => Marshal.SizeOf<ScriptValue>(),

            // NativeHash-addressable
            Segment.Include => sizeof(ulong),

            // byte-addressable
            _ => sizeof(byte),
        };

        private SegmentBuilder globalSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Global)),
                               staticSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Static)),
                               argSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Arg)), // appended to the end of the static segment
                               stringSegmentBuilder = new(GetAddressingUnitByteSize(Segment.String)),
                               codeSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Code)),
                               includeSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Include));

        private Segment CurrentSegment { get; set; } = Segment.None;
        private SegmentBuilder CurrentSegmentBuilder => CurrentSegment switch
        {
            Segment.Global => globalSegmentBuilder,
            Segment.Static => staticSegmentBuilder,
            Segment.Arg => argSegmentBuilder,
            Segment.String => stringSegmentBuilder,
            Segment.Code => codeSegmentBuilder,
            Segment.Include => includeSegmentBuilder,
            _ => throw new InvalidOperationException(), 
        };

        public DiagnosticsReport Diagnostics { get; }
        public Script OutputScript { get; }
        public Dictionary<string, ConstantValue> Constants { get; }
        public Dictionary<string, Label> Labels { get; }
        public bool HasScriptName { get; private set; }
        public bool HasScriptHash { get; private set; }
        public bool HasGlobalBlock { get; private set; }

        public Assembler(string filePath)
        {
            Diagnostics = new(filePath);
            OutputScript = new()
            {
                Name = DefaultScriptName,
                NameHash = DefaultScriptName.ToLowercaseHash(),
            };
            Constants = new(CaseInsensitiveComparer);
            Labels = new(CaseInsensitiveComparer);
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
            FixArgLabels();

            (OutputScript.GlobalsPages, OutputScript.GlobalsLength) = SegmentToGlobalsPages(globalSegmentBuilder);
            (OutputScript.Statics, OutputScript.StaticsCount, OutputScript.ArgsCount) = SegmentToStaticsArray(staticSegmentBuilder, argSegmentBuilder);
            (OutputScript.Natives, OutputScript.NativesCount) = SegmentToNativesArray(includeSegmentBuilder, OutputScript.CodeLength);
            (OutputScript.StringsPages, OutputScript.StringsLength) = SegmentToStringsPages(stringSegmentBuilder);
            // TODO: code segment
        }

        private void ProcessLine(ScAsmParser.LineContext line)
        {
            var label = line.label();
            var segmentDirective = line.segmentDirective();
            var directive = line.directive();
            var instruction = line.instruction();

            if (label is not null)
            {
                ProcessLabel(label);
            }

            if (segmentDirective is not null)
            {
                ProcessSegmentDirective(segmentDirective);
            }
            else if (directive is not null)
            {
                ProcessDirective(directive);
            }
            else if (instruction is not null)
            {
                ProcessInstruction(instruction);
            }
        }

        private void ProcessLabel(ScAsmParser.LabelContext label)
        {
            if (CurrentSegment == Segment.None)
            {
                Diagnostics.AddError($"Unexpected label outside any segment", Source(label));
                return;
            }

            var name = label.identifier().GetText();
            var offset = (int)(CurrentSegmentBuilder.ByteLength / GetAddressingUnitByteSize(CurrentSegment));

            if (!Labels.TryAdd(name, new Label(CurrentSegment, offset)))
            {
                Diagnostics.AddError($"Label '{name}' already defined", Source(label));
            }
        }

        private void ProcessSegmentDirective(ScAsmParser.SegmentDirectiveContext directive)
        {
            CurrentSegment = directive switch
            {
                ScAsmParser.GlobalSegmentDirectiveContext => Segment.Global,
                ScAsmParser.StaticSegmentDirectiveContext => Segment.Static,
                ScAsmParser.ArgSegmentDirectiveContext => Segment.Arg,
                ScAsmParser.StringSegmentDirectiveContext => Segment.String,
                ScAsmParser.CodeSegmentDirectiveContext => Segment.Code,
                ScAsmParser.IncludeSegmentDirectiveContext => Segment.Include,
                _ => throw new InvalidOperationException(),
            };
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
                case ScAsmParser.GlobalBlockDirectiveContext globalBlockDirective:
                    if (HasGlobalBlock)
                    {
                        Diagnostics.AddError($"Directive '.global_block' is repeated", Source(globalBlockDirective));
                    }
                    else
                    {
                        OutputScript.GlobalsBlock = (uint)globalBlockDirective.integer().GetText().ParseAsInt();
                        HasGlobalBlock = true;
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
                case ScAsmParser.IntDirectiveContext intDirective:
                    WriteIntFloatDirectiveOperands(intDirective.directiveOperandList(), isFloat: false);
                    break;
                case ScAsmParser.FloatDirectiveContext floatDirective:
                    WriteIntFloatDirectiveOperands(floatDirective.directiveOperandList(), isFloat: true);
                    break;
                case ScAsmParser.StrDirectiveContext strDirective:
                    CurrentSegmentBuilder.String(strDirective.@string().GetText()[1..^1].Unescape());
                    break;
                case ScAsmParser.NativeDirectiveContext nativeDirective:
                    CurrentSegmentBuilder.UInt64(nativeDirective.integer().GetText().ParseAsUInt64());
                    break;
            }
        }

        private void ProcessInstruction(ScAsmParser.InstructionContext instruction)
        {
            if (CurrentSegment != Segment.Code)
            {
                Diagnostics.AddError($"Unexpected instruction in non-code segment", Source(instruction));
                return;
            }


        }

        private void WriteIntFloatDirectiveOperands(ScAsmParser.DirectiveOperandListContext operandList, bool isFloat)
        {
            foreach (var operand in operandList.directiveOperand())
            {
                switch (operand)
                {
                    case ScAsmParser.IdentifierDirectiveOperandContext identifierOperand:
                        if (TryGetConstant(identifierOperand.identifier(), out var constValue))
                        {
                            if (isFloat)
                            {
                                CurrentSegmentBuilder.Float(constValue.Float);
                            }
                            else
                            {
                                CurrentSegmentBuilder.Int(constValue.Integer);
                            }
                        }
                        break;
                    case ScAsmParser.IntegerDirectiveOperandContext integerOperand:
                        var intValue = integerOperand.integer().GetText().ParseAsInt();
                        if (isFloat)
                        {
                            CurrentSegmentBuilder.Float(intValue);
                        }
                        else
                        {
                            CurrentSegmentBuilder.Int(intValue);
                        }
                        break;
                    case ScAsmParser.FloatDirectiveOperandContext floatOperand:
                        var floatValue = floatOperand.@float().GetText().ParseAsFloat();
                        if (isFloat)
                        {
                            CurrentSegmentBuilder.Float(floatValue);
                        }
                        else
                        {
                            CurrentSegmentBuilder.Int((int)Math.Truncate(floatValue));
                        }
                        break;
                    case ScAsmParser.DupDirectiveOperandContext dupOperand:
                        int count = 0;
                        if (dupOperand.identifier() != null && TryGetConstant(dupOperand.identifier(), out var countConst))
                        {
                            count = countConst.Integer;
                        }
                        else if (dupOperand.integer() != null)
                        {
                            count = dupOperand.integer().GetText().ParseAsInt();
                        }

                        for (int i = 0; i < count; i++)
                        {
                            WriteIntFloatDirectiveOperands(dupOperand.directiveOperandList(), isFloat);
                        }
                        break;
                }
            }
        }

        private bool TryGetConstant(ScAsmParser.IdentifierContext identifier, out ConstantValue value)
        {
            var name = identifier.GetText();
            if (Constants.TryGetValue(name, out value))
            {
                return true;
            }
            else
            {
                Diagnostics.AddError($"Undefined constant '{name}'", Source(identifier));
                return false;
            }
        }

        /// <summary>
        /// The args are stored after the static variables, so add the static segment length to the offset of labels in the '.arg' segment.
        /// </summary>
        private void FixArgLabels()
        {
            var staticSegmentLength = (int)(staticSegmentBuilder.ByteLength / GetAddressingUnitByteSize(Segment.Static));
            var argLabels = Labels.Where(kvp => kvp.Value.Segment == Segment.Arg).ToArray();
            foreach (var (name, label) in argLabels)
            {
                Labels[name] = new Label(label.Segment, label.Offset + staticSegmentLength);
            }
        }

        private static (ScriptPageArray<ScriptValue> Pages, uint Length) SegmentToGlobalsPages(SegmentBuilder segment)
        {
            var globals = MemoryMarshal.Cast<byte, ScriptValue>(segment.RawDataBuffer);

            // create pages
            var pageCount = (globals.Length + Script.MaxPageLength) / Script.MaxPageLength;
            var p = new ScriptPage<ScriptValue>[pageCount];
            if (pageCount > 0)
            {
                for (int i = 0; i < pageCount - 1; i++)
                {
                    p[i] = new ScriptPage<ScriptValue> { Data = new ScriptValue[Script.MaxPageLength] };
                    globals.Slice(i * (int)Script.MaxPageLength, (int)Script.MaxPageLength).CopyTo(p[i].Data);
                }

                p[^1] = new ScriptPage<ScriptValue> { Data = new ScriptValue[globals.Length & 0x3FFF] };
                globals.Slice((int)((pageCount - 1) * Script.MaxPageLength), p[^1].Data.Length).CopyTo(p[^1].Data);
            }
            var pages = new ScriptPageArray<ScriptValue> { Items = p };

            return (pages, (uint)globals.Length);
        }

        private static (ScriptValue[] Statics, uint StaticsCount, uint ArgsCount) SegmentToStaticsArray(SegmentBuilder staticSegment, SegmentBuilder argSegment)
        {
            var statics = MemoryMarshal.Cast<byte, ScriptValue>(staticSegment.RawDataBuffer);
            var args = MemoryMarshal.Cast<byte, ScriptValue>(argSegment.RawDataBuffer);
            
            var combined = new ScriptValue[statics.Length + args.Length];
            statics.CopyTo(combined.AsSpan(0, statics.Length));
            args.CopyTo(combined.AsSpan(statics.Length, args.Length));
            return (combined, (uint)combined.Length, (uint)args.Length);
        }

        private static (ulong[] Natives, uint NativesCount) SegmentToNativesArray(SegmentBuilder segment, uint codeLength)
        {
            var nativeHashes = MemoryMarshal.Cast<byte, ulong>(segment.RawDataBuffer).ToArray();
            for (int i = 0; i < nativeHashes.Length; i++)
            {
                nativeHashes[i] = RotateHash(nativeHashes[i], i, codeLength);
            }
            return (nativeHashes, (uint)nativeHashes.Length);

            static ulong RotateHash(ulong hash, int index, uint codeLength)
            {
                byte rotate = (byte)(((uint)index + codeLength) & 0x3F);
                return hash >> rotate | hash << (64 - rotate);
            }
        }

        private static (ScriptPageArray<byte> Pages, uint Length) SegmentToStringsPages(SegmentBuilder segment)
        {
            var strings = segment.RawDataBuffer;

            // create pages
            var pageCount = (strings.Length + Script.MaxPageLength) / Script.MaxPageLength;
            var p = new ScriptPage<byte>[pageCount];
            if (pageCount > 0)
            {
                for (int i = 0; i < pageCount - 1; i++)
                {
                    p[i] = new ScriptPage<byte> { Data = new byte[Script.MaxPageLength] };
                    strings.Slice(i * (int)Script.MaxPageLength, (int)Script.MaxPageLength).CopyTo(p[i].Data);
                }

                p[^1] = new ScriptPage<byte> { Data = new byte[strings.Length & 0x3FFF] };
                strings.Slice((int)((pageCount - 1) * Script.MaxPageLength), p[^1].Data.Length).CopyTo(p[^1].Data);
            }
            var pages = new ScriptPageArray<byte> { Items = p };

            return (pages, (uint)strings.Length);
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

        public struct Label
        {
            public Segment Segment { get; }
            public int Offset { get; }

            public Label(Segment segment, int offset) => (Segment, Offset) = (segment, offset);
        }
    }
}
