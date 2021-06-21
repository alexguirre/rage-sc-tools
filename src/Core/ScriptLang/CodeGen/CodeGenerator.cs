﻿namespace ScTools.ScriptLang.CodeGen
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.BuiltIns;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.SymbolTables;

    public class CodeGenerator
    {
        public TextWriter Sink { get; }
        public Program Program { get; }
        public GlobalSymbolTable Symbols { get; }
        public DiagnosticsReport Diagnostics { get; }
        public StringsTable Strings { get; private set; }

        public CodeGenerator(TextWriter sink, Program program, GlobalSymbolTable symbols, DiagnosticsReport diagnostics)
            => (Sink, Program, Symbols, Diagnostics, Strings)  = (sink, program, symbols, diagnostics, new());

        public bool Generate()
        {
            if (Diagnostics.HasErrors)
            {
                return false;
            }

            Allocator.AllocateVars(Program, Diagnostics);
            if (Diagnostics.HasErrors)
            {
                return false;
            }

            Strings = StringsTableBuilder.Build(Program);

            EmitDirectives();
            EmitGlobalsSegment();
            EmitStaticsSegment();
            EmitStringsSegment();
            EmitIncludeSegment();
            EmitCodeSegment();
            return true;
        }

        private void EmitDirectives()
        {
            Sink.WriteLine("\t.script_name {0}", Program.ScriptName);
            Sink.WriteLine("\t.script_hash {0}", Program.ScriptHash);
        }

        private void EmitGlobalsSegment()
        {
            var globalBlock = Program.GlobalBlock;
            if (globalBlock is null)
            {
                return;
            }

            var globalVars = InitializeGlobalVars();

            Sink.WriteLine("\t.global_block {0}", globalBlock.BlockIndex);
            Sink.WriteLine("\t.global");
            WriteValues(globalVars);
        }

        private void EmitStaticsSegment()
        {
            var staticVars = InitializeStaticVars();

            if (Program.StaticsSize > Program.ArgsSize)
            {
                Sink.WriteLine("\t.static");
                WriteValues(staticVars.AsSpan(0, Program.StaticsSize - Program.ArgsSize));
            }

            if (Program.ArgsSize > 0)
            {
                Sink.WriteLine("\t.arg");
                WriteValues(staticVars.AsSpan(Program.StaticsSize - Program.ArgsSize, Program.ArgsSize));
            }
        }

        private void EmitStringsSegment()
        {
            if (Strings.Count == 0)
            {
                return;
            }

            Sink.WriteLine("\t.string");
            // TODO: do we want strings to keep their order from the AST?
            foreach (var (str, label) in Strings.StringToLabel)
            {
                Sink.WriteLine("\t{0}:\t.str \"{1}\"", label, str.Escape());
            }
        }

        private void EmitIncludeSegment()
        {
            Sink.WriteLine("\t.include");

        }

        private void EmitCodeSegment()
        {
            Sink.WriteLine("\t.code");
        }

        private void WriteValues(Span<ScriptValue> values)
        {
            long repeatedValue = 0;
            int repeatedCount = 0;
            foreach (var value in values)
            {
                var v = value.AsInt64;
                if (repeatedCount > 0 && v != repeatedValue)
                {
                    FlushValue();
                }

                repeatedValue = v;
                repeatedCount++;
            }

            FlushValue();

            void FlushValue()
            {
                var directive = repeatedValue is < int.MinValue or > int.MaxValue ? "int64" : "int";
                if (repeatedCount > 1)
                {
                    Sink.WriteLine("\t\t.{0} {1} dup ({2})", directive, repeatedCount, repeatedValue);
                }
                else if (repeatedCount == 1)
                {
                    Sink.WriteLine("\t\t.{0} {1}", directive, repeatedValue);
                }

                repeatedCount = 0;
            }
        }

        private ScriptValue[] InitializeGlobalVars()
        {
            Debug.Assert(Program.GlobalBlock is not null);

            var blockSize = Program.GlobalBlock.Size;
            var buffer = new ScriptValue[blockSize];
            foreach (var globalVar in Program.GlobalBlock.Vars)
            {
                var size = globalVar.Type.SizeOf;
                var address = globalVar.Address & ((1 << 18) - 1);

                var dest = buffer.AsSpan(address, size);
                InitializeStaticVar(dest, globalVar.Type, globalVar.Initializer);
            }

            return buffer;
        }

        private ScriptValue[] InitializeStaticVars()
        {
            var buffer = new ScriptValue[Program.StaticsSize];
            for (int address = 0; address < Program.StaticsSize;)
            {
                var var = Program.Statics[address];
                var size = var.Type.SizeOf;

                var dest = buffer.AsSpan(address, size);
                InitializeStaticVar(dest, var.Type, var.Initializer);

                address += size;
            }

            return buffer;
        }

        private void InitializeStaticVar(Span<ScriptValue> dest, IType type, IExpression? initializer)
        {
            Debug.Assert(dest.Length == type.SizeOf);

            switch (type)
            {
                case IntType or EnumType:
                    dest[0].AsInt32 = initializer is not null ? ExpressionEvaluator.EvalInt(initializer, Symbols) : 0;
                    break;

                case FloatType:
                    dest[0].AsFloat = initializer is not null ? ExpressionEvaluator.EvalFloat(initializer, Symbols) : 0.0f;
                    break;

                case BoolType:
                    dest[0].AsInt32 = initializer is not null && ExpressionEvaluator.EvalBool(initializer, Symbols) ? 1 : 0;
                    break;

                case {} when BuiltInTypes.IsVectorType(type):
                    (dest[0].AsFloat, dest[1].AsFloat, dest[2].AsFloat) = initializer is not null ? ExpressionEvaluator.EvalVector(initializer, Symbols) : (0.0f, 0.0f, 0.0f);
                    break;

                case StructType structTy:
                    var offset = 0;
                    foreach (var field in structTy.Declaration.Fields)
                    {
                        var fieldType = field.Type;
                        var fieldSize = fieldType.SizeOf;
                        var fieldDest = dest.Slice(offset, fieldSize);
                        InitializeStaticVar(fieldDest, fieldType, field.Initializer);
                        offset += fieldSize;
                    }
                    break;

                case ArrayType arrayTy:
                    dest[0].AsInt32 = arrayTy.Length;

                    var itemType = arrayTy.ItemType;
                    var itemSize = itemType.SizeOf;
                    for (int i = 0; i < arrayTy.Length; i++)
                    {
                        var itemDest = dest.Slice(1 + itemSize * i, itemSize);
                        InitializeStaticVar(itemDest, itemType, initializer: null);
                    }
                    break;

                case TextLabelType:
                    dest.Fill(default);

                    string? str = null;
                    if (initializer?.Type is IntType)
                    {
                        str = ExpressionEvaluator.EvalInt(initializer, Symbols).ToString(CultureInfo.InvariantCulture);
                    }
                    else if (initializer?.Type is StringType)
                    {
                        str = ExpressionEvaluator.EvalString(initializer, Symbols);
                    }

                    if (str is not null)
                    {
                        var bytes = Encoding.UTF8.GetBytes(str);
                        var destBytes = MemoryMarshal.Cast<ScriptValue, byte>(dest);
                        var n = Math.Min(bytes.Length, destBytes.Length - 1);
                        for (int i = 0; i < n; i++)
                        {
                            destBytes[i] = bytes[i];
                        }
                    }
                    break;


            }
        }
    }
}