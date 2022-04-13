namespace ScTools.ScriptLang.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.BuiltIns;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.SymbolTables;

    public class CodeGenerator
    {
        private readonly StatementEmitter stmtEmitter;
        private readonly ValueEmitter valueEmitter;
        private readonly AddressEmitter addressEmitter;
        private readonly PatternOptimizer optimizer;
        private readonly List<EmittedInstruction> funcInstructions;

        public TextWriter Sink { get; }
        public Program Program { get; }
        public GlobalSymbolTable Symbols { get; }
        public DiagnosticsReport Diagnostics { get; }
        public NativeDB NativeDB { get; }
        public StringsTable Strings { get; private set; }

        public CodeGenerator(TextWriter sink, Program program, GlobalSymbolTable symbols, DiagnosticsReport diagnostics, NativeDB nativeDB)
        {
            Sink = sink;
            Program = program;
            Symbols = symbols;
            Diagnostics = diagnostics;
            NativeDB = nativeDB;
            Strings = new();
            stmtEmitter = new(this);
            valueEmitter = new(this);
            addressEmitter = new(this);
            optimizer = new();
            funcInstructions = new();
        }

        public bool Generate()
        {
            if (Diagnostics.HasErrors)
            {
                return false;
            }

            Allocator.Allocate(Program, Diagnostics);
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
            Sink.WriteLine("\t.script_name {0}", Program.Script!.Name);
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

            if (Program.StaticsSize > Program.ScriptParametersSize)
            {
                Sink.WriteLine("\t.static");
                WriteValues(staticVars.AsSpan(0, Program.StaticsSize - Program.ScriptParametersSize));
            }

            if (Program.ScriptParametersSize > 0)
            {
                Sink.WriteLine("\t.arg");
                WriteValues(staticVars.AsSpan(Program.StaticsSize - Program.ScriptParametersSize, Program.ScriptParametersSize));
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
            foreach (var (str, id) in Strings.StringToID.OrderBy(p => p.Value))
            {
                Sink.WriteLine("\t.str \"{0}\"", str.Escape());
            }
        }

        private void EmitIncludeSegment()
        {
            var nativesDecls = Program.Declarations.Where(decl => decl is FuncDeclaration { Prototype: { Kind: FuncKind.Native } }).Cast<FuncDeclaration>();
            if (!nativesDecls.Any())
            {
                return;
            }

            var natives = nativesDecls.Select(decl =>
            {
                var hash = NativeDB.FindOriginalHash(decl.Name) ?? throw new InvalidOperationException($"Unknown native '{decl.Name}'");
                return (decl.Name, hash);
            });

            Sink.WriteLine("\t.include");
            foreach (var (name, hash) in natives)
            {
                Sink.WriteLine("\t{0}:\t.native 0x{1:X16}", name, hash);
            }
        }

        private void EmitCodeSegment()
        {
            Debug.Assert(Program.Script is not null);

            var funcs = Program.Declarations.Where(decl => decl is FuncDeclaration { Prototype: { Kind: FuncKind.UserDefined } }).Cast<FuncDeclaration>();
            funcs = funcs.Prepend(Program.Script); // place SCRIPT first, it has to be compiled at address 0

            Sink.WriteLine("\t.code");
            funcs.ForEach(EmitFunc);
        }

        private void EmitFunc(FuncDeclaration func)
        {
            Debug.Assert(func.Prototype.Kind is FuncKind.Script or FuncKind.UserDefined);
            Debug.Assert(funcInstructions.Count == 0);

            var isScript = func.Prototype.Kind is FuncKind.Script;

            EmitLabel(isScript ? "SCRIPT" : func.Name);

            // prologue
            Emit(Opcode.ENTER, func.ParametersSize, func.FrameSize);

            // body
            func.Body.ForEach(stmt => EmitStatement(stmt, func));

            // epilogue
            if (func.Body.LastOrDefault() is not ReturnStatement)
            {
                Emit(Opcode.LEAVE, func.ParametersSize, func.Prototype.ReturnType.SizeOf);
            }

            // optimize and write instructions
            foreach (var inst in optimizer.Optimize(funcInstructions))
            {
                if (inst.Label is not null)
                {
                    Sink.WriteLine("{0}:", inst.Label);
                }

                if (inst.Instruction is not null)
                {
                    var (opcode, operands) = inst.Instruction.Value;
                    Sink.WriteLine("\t{0} {1}", opcode.Mnemonic(), string.Join(", ", operands));
                }
            }
            funcInstructions.Clear();
        }

        public void EmitStatement(IStatement stmt, FuncDeclaration func)
        {
            EmitLabel(stmt);
            stmt.Accept(stmtEmitter, func);
        }
        public void EmitValue(IExpression expr) => expr.Accept(valueEmitter, default);
        public void EmitAddress(IExpression expr) => expr.Accept(addressEmitter, default);

        public void Emit(Opcode opcode, IEnumerable<object> operands) => Emit(opcode, operands.ToArray());
        public void Emit(Opcode opcode, params object[] operands) => funcInstructions.Add(new() { Instruction = (opcode, operands) });

        public void EmitLabel(IStatement stmt)
        {
            // TODO: support local labels in assembler to prevent conflicts in assembly generated by the compiler
            if (stmt.Label is not null)
            {
                EmitLabel(stmt.Label.Name);
            }
        }
        public void EmitLabel(string label) => funcInstructions.Add(new() { Label = label });

        public void EmitCall(string label) => Emit(Opcode.CALL, label);
        public void EmitNativeCall(int argsSize, int returnSize, string label) => Emit(Opcode.NATIVE, argsSize, returnSize, label);
        public void EmitJump(string label) => Emit(Opcode.J, label);
        public void EmitJumpIfZero(string label) => Emit(Opcode.JZ, label);

        public void EmitSwitch(IEnumerable<ValueSwitchCase> cases)
            => Emit(Opcode.SWITCH, cases.Select(c => $"{unchecked((uint)ExpressionEvaluator.EvalInt(c.Value, Symbols))}:{c.Semantics.Label}"));

        public void EmitLoadFrom(IExpression lvalueExpr)
        {
            Debug.Assert(lvalueExpr.IsLValue);

            var size = lvalueExpr.Type!.SizeOf;
            if (size == 1)
            {
                EmitAddress(lvalueExpr);
                Emit(Opcode.LOAD);
            }
            else
            {
                EmitPushConstInt(size);
                EmitAddress(lvalueExpr);
                Emit(Opcode.LOAD_N);
            }
        }

        public void EmitStoreAt(IExpression lvalueExpr)
        {
            Debug.Assert(lvalueExpr.IsLValue);

            var size = lvalueExpr.Type!.SizeOf;
            if (size == 1)
            {
                EmitAddress(lvalueExpr);
                Emit(Opcode.STORE);
            }
            else
            {
                EmitPushConstInt(size);
                EmitAddress(lvalueExpr);
                Emit(Opcode.STORE_N);
            }
        }

        public void EmitPushConstInt(int value)
        {
            switch (value)
            {
                case >= -1 and <= 7:
                    Emit((Opcode)((int)Opcode.PUSH_CONST_M1 + value + 1));
                    break;

                case >= byte.MinValue and <= byte.MaxValue:
                    Emit(Opcode.PUSH_CONST_U8, value);
                    break;

                case >= short.MinValue and <= short.MaxValue:
                    Emit(Opcode.PUSH_CONST_S16, value);
                    break;

                case >= 0 and <= 0x00FFFFFF:
                    Emit(Opcode.PUSH_CONST_U24, value);
                    break;

                default:
                    Emit(Opcode.PUSH_CONST_U32, unchecked((uint)value));
                    break;
            }
        }

        public void EmitPushConstFloat(float value)
        {
            switch (value)
            {
                case -1.0f: Emit(Opcode.PUSH_CONST_FM1); break;
                case 0.0f: Emit(Opcode.PUSH_CONST_F0); break;
                case 1.0f: Emit(Opcode.PUSH_CONST_F1); break;
                case 2.0f: Emit(Opcode.PUSH_CONST_F2); break;
                case 3.0f: Emit(Opcode.PUSH_CONST_F3); break;
                case 4.0f: Emit(Opcode.PUSH_CONST_F4); break;
                case 5.0f: Emit(Opcode.PUSH_CONST_F5); break;
                case 6.0f: Emit(Opcode.PUSH_CONST_F6); break;
                case 7.0f: Emit(Opcode.PUSH_CONST_F7); break;
                default: Emit(Opcode.PUSH_CONST_F, value.ToString("R", CultureInfo.InvariantCulture)); break;
            }
        }

        public void EmitOffset(int offset)
        {
            switch (offset)
            {
                case >= byte.MinValue and <= byte.MaxValue:
                    Emit(Opcode.IOFFSET_U8, offset);
                    break;

                case >= short.MinValue and <= short.MaxValue:
                    Emit(Opcode.IOFFSET_S16, offset);
                    break;

                default:
                    EmitPushConstInt(offset);
                    Emit(Opcode.IOFFSET);
                    break;
            }
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

                case VectorType:
                    (dest[0].AsFloat, dest[1].AsFloat, dest[2].AsFloat) = initializer is not null ? ExpressionEvaluator.EvalVector(initializer, Symbols) : (0.0f, 0.0f, 0.0f);
                    break;

                case StructType structTy:
                    foreach (var field in structTy.Declaration.Fields)
                    {
                        var fieldDest = dest.Slice(field.Offset, field.Type.SizeOf);
                        InitializeStaticVar(fieldDest, field.Type, field.Initializer);
                    }
                    break;

                case ArrayType arrayTy:
                    dest[0].AsInt32 = arrayTy.Rank;

                    var itemType = arrayTy.ItemType;
                    var itemSize = itemType.SizeOf;
                    for (int i = 0; i < arrayTy.Rank; i++)
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

        public struct EmittedInstruction
        {
            public string? Label { get; init; }
            public (Opcode Opcode, object[] Operands)? Instruction { get; init; }
        }
    }
}
