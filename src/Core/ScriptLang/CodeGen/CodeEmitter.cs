namespace ScTools.ScriptLang.CodeGen;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.GameFiles;
using ScTools.GameFiles.Five;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

public sealed partial class CodeEmitter
{
    private enum LabelReferenceKind { Relative, Absolute }
    private record struct LabelReference(InstructionReference Instruction, int OperandOffset, LabelReferenceKind Kind);
    private record struct LabelInfo(InstructionReference? Instruction, List<LabelReference> UnresolvedReferences);

    private readonly StatementEmitter stmtEmitter;
    private readonly ValueEmitter valueEmitter;
    private readonly AddressEmitter addressEmitter;
    private const bool IncludeFunctionNames = true;

    private readonly CodeBuffer codeBuffer;
    private readonly InstructionEmitter instEmitter;

    private readonly HashSet<FunctionDeclaration> usedFunctions = new();
    private readonly Queue<FunctionDeclaration> functionsToCompile = new();

    private readonly VarAllocator statics;
    private int numScriptParams = 0;

    private readonly List<LabelInfo> labels = new();
    private readonly Dictionary<string, int> functionLabelNameToIndex = new(Parser.CaseInsensitiveComparer);
    private readonly Dictionary<string, int> localLabelNameToIndex = new(Parser.CaseInsensitiveComparer);

    private byte currentFunctionArgCount;
    private readonly VarAllocator currentFunctionFrame = new();
    private TypeInfo? currentFunctionReturnType = null;

    public StringsTable Strings { get; } = new();

    public CodeEmitter(VarAllocator statics)
    {
        this.statics = new(statics);
        codeBuffer = new();
        instEmitter = new(new InstructionEmitter.AppendFlushStrategy(codeBuffer));

        stmtEmitter = new(this);
        valueEmitter = new(this);
        addressEmitter = new(this);
    }

    public ScriptPageTable<byte> ToCodePages() => codeBuffer.ToCodePages(labels);

    public ScriptValue64[] GetStaticSegment(out int numScriptParams)
    {
        var staticsBuffer = new ScriptValue64[statics.AllocatedSize];

        foreach (var s in statics)
        {
            var type = s.Semantics.ValueType!;
            if (type.IsDefaultInitialized())
            {
                var offset = statics.OffsetOf(s);
                StaticDefaultInit(staticsBuffer.AsSpan(offset, type.SizeOf), type);
            }
        }

        numScriptParams = this.numScriptParams;
        return staticsBuffer;
    }

    public void EmitScript(ScriptDeclaration script)
    {
        EmitScriptEntryPoint(script);

        while (functionsToCompile.TryDequeue(out var function))
        {
            EmitFunction(function);
        }

        new PatternOptimizer().Optimize(codeBuffer);
    }

    public void EmitScriptEntryPoint(ScriptDeclaration script)
    {
        var staticsWithoutScriptParamsSize = statics.AllocatedSize;
        script.Parameters.ForEach(p => statics.Allocate(p));
        numScriptParams = statics.AllocatedSize - staticsWithoutScriptParamsSize;

        EmitFunctionCommon("SCRIPT", script.Parameters, script.Body, VoidType.Instance, isScriptEntryPoint: true);
    }

    public void EmitFunction(FunctionDeclaration function)
    {
        Label(function.Name, isFunctionLabel: true);
        EmitFunctionCommon(function.Name, function.Parameters, function.Body, ((FunctionType)function.Semantics.ValueType!).Return, isScriptEntryPoint: false);
    }

    private void EmitFunctionCommon(string name, ImmutableArray<VarDeclaration> parameters, ImmutableArray<IStatement> body, TypeInfo returnType, bool isScriptEntryPoint)
    {
        currentFunctionReturnType = returnType;
        Debug.Assert(returnType.SizeOf <= byte.MaxValue, $"Return type too big (sizeof: {returnType.SizeOf})");

        currentFunctionFrame.Clear();
        localLabelNameToIndex.Clear();

        if (!isScriptEntryPoint)
        {
            // allocate space for parameters
            foreach (var p in parameters)
            {
                AllocateFrameSpaceForLocal(p);
            }
        }
        Debug.Assert(currentFunctionFrame.AllocatedSize <= byte.MaxValue, $"Too many parameters (argCount: {currentFunctionFrame.AllocatedSize})");
        currentFunctionArgCount = (byte)currentFunctionFrame.AllocatedSize;

        // allocate space required by the engine to store the return address and caller frame address
        AllocateFrameSpace(2);

        // prologue (frame size is not yet known, it is updated after emitting the function code)
        var enter = instEmitter.EmitEnter(currentFunctionArgCount, frameSize: 0, name);

        if (isScriptEntryPoint)
        {
            EmitStaticInitializers();
            // TODO: EmitGlobalInitializers()
        }

        // body
        EmitStatementBlock(body);

        // epilogue
        if (body.LastOrDefault() is not ReturnStatement)
        {
            EmitEpilogue();
        }

        // update frame size in ENTER instruction
        Debug.Assert(currentFunctionFrame.AllocatedSize <= ushort.MaxValue, $"Function frame size is too big");
        var oldFlushStrategy = instEmitter.FlushStrategy;
        instEmitter.FlushStrategy = new InstructionEmitter.UpdateFlushStrategy(codeBuffer, enter);
        instEmitter.EmitEnter(currentFunctionArgCount, (ushort)currentFunctionFrame.AllocatedSize, name);
        instEmitter.FlushStrategy = oldFlushStrategy;
    }

    private void EmitStaticInitializers()
    {
        foreach (var s in statics)
        {
            if (s.Initializer is not null)
            {
                EmitAssignmentToVar(s, s.Initializer);
            }
        }
    }

    public void EmitEpilogue()
    {
        Debug.Assert(currentFunctionReturnType is not null);
        instEmitter.EmitLeave(currentFunctionArgCount, (byte)currentFunctionReturnType.SizeOf);
    }

    public int AllocateFrameSpace(int size) => currentFunctionFrame.Allocate(size);
    public int AllocateFrameSpaceForLocal(VarDeclaration varDecl) => currentFunctionFrame.Allocate(varDecl);

    public void EmitStatementBlock(ImmutableArray<IStatement> statements)
    {
        foreach (var stmt in statements) EmitStatement(stmt);
    }
    public void EmitStatement(IStatement stmt)
    {
        if (stmt.Label is not null)
        {
            Label(stmt.Label.Name);
        }

        stmt.Accept(stmtEmitter);
    }
    public void EmitValue(IExpression expr) => expr.Accept(valueEmitter);
    public void EmitValueAndDrop(IExpression expr)
    {
        EmitValue(expr);
        var valueSize = expr.Semantics.Type!.SizeOf;
        for (int i = 0; i < valueSize; i++)
        {
            instEmitter.EmitDrop();
        }
    }
    public void EmitAddress(IExpression expr) => expr.Accept(addressEmitter);

    public void EmitArg(IExpression arg)
    {
        if (arg.ArgumentKind is ArgumentKind.ByRef)
        {
            // pass by reference
            Debug.Assert(arg.ValueKind.Is(ValueKind.Addressable));
            EmitAddress(arg);
        }
        else if (arg.ArgumentKind is ArgumentKind.ByValue)
        {
            // pass by value
            EmitValue(arg);
        }
        else
        {
            throw new ArgumentException("Argument kind not set, no semantic analysis done on the arg?", nameof(arg));
        }
    }

    public void EmitJump(string label)
    {
        var inst = instEmitter.EmitJ(0);
        ReferenceLabel(label, inst, 1, LabelReferenceKind.Relative, isFunctionLabel: false);
    }
    public void EmitJumpIfZero(string label)
    {
        var inst = instEmitter.EmitJZ(0);
        ReferenceLabel(label, inst, 1, LabelReferenceKind.Relative, isFunctionLabel: false);
    }

    public void EmitCall(FunctionDeclaration function)
    {
        var inst = instEmitter.EmitCall(0);
        ReferenceLabel(function.Name, inst, 1, LabelReferenceKind.Absolute, isFunctionLabel: true);
        OnFunctionFound(function);
    }

    public void EmitFunctionAddress(FunctionDeclaration function)
    {
        var inst = instEmitter.EmitPushConstU24(0);
        ReferenceLabel(function.Name, inst, 1, LabelReferenceKind.Absolute, isFunctionLabel: true);
        OnFunctionFound(function);
    }

    public void EmitNativeCall(NativeFunctionDeclaration nativeFunction)
    {
        var funcType = (FunctionType)nativeFunction.Semantics.ValueType!;
        var argCount = funcType.Parameters.Sum(p => p.SizeOf);
        var returnCount = funcType.Return.SizeOf;
        Debug.Assert(argCount <= byte.MaxValue);
        Debug.Assert(returnCount <= byte.MaxValue);
        instEmitter.EmitNative((byte)argCount, (byte)returnCount, 0); // TODO: convert native name to index
    }

    public void EmitIndirectCall()
    {
        instEmitter.EmitCallIndirect();
    }

    public void EmitSwitch(IEnumerable<ValueSwitchCase> valueCases)
    {
        var cases = valueCases.OrderBy(c => c.Semantics.Value!.Value).ToArray();
        var inst = instEmitter.EmitSwitch(cases.Select(c => unchecked((uint)c.Semantics.Value!.Value)).ToArray());
        for (int i = 0; i < cases.Length; i++)
        {
            var @case = cases[i];
            var valueOffset = 2 + i * 6;
            var labelOffset = valueOffset + 4;
            ReferenceLabel(@case.Semantics.Label!, inst, labelOffset, LabelReferenceKind.Relative, isFunctionLabel: false);
        }
    }

    public void EmitTextLabelAssignString(IExpression destinationTextLabel, IExpression sourceString)
    {
        EmitArg(sourceString);
        EmitArg(destinationTextLabel);
        var tlType = (TextLabelType)destinationTextLabel.Type!;
        Debug.Assert(tlType.Length <= byte.MaxValue);
        instEmitter.EmitTextLabelAssignString((byte)tlType.Length);
    }

    public void EmitTextLabelAssignInt(IExpression destinationTextLabel, IExpression sourceInt)
    {
        EmitArg(sourceInt);
        EmitArg(destinationTextLabel);
        var tlType = (TextLabelType)destinationTextLabel.Type!;
        Debug.Assert(tlType.Length <= byte.MaxValue);
        instEmitter.EmitTextLabelAssignInt((byte)tlType.Length);
    }

    public void EmitTextLabelAppendString(IExpression destinationTextLabel, IExpression sourceString)
    {
        EmitArg(sourceString);
        EmitArg(destinationTextLabel);
        var tlType = (TextLabelType)destinationTextLabel.Type!;
        Debug.Assert(tlType.Length <= byte.MaxValue);
        instEmitter.EmitTextLabelAppendString((byte)tlType.Length);
    }

    public void EmitTextLabelAppendInt(IExpression destinationTextLabel, IExpression sourceInt)
    {
        EmitArg(sourceInt);
        EmitArg(destinationTextLabel);
        var tlType = (TextLabelType)destinationTextLabel.Type!;
        Debug.Assert(tlType.Length <= byte.MaxValue);
        instEmitter.EmitTextLabelAppendInt((byte)tlType.Length);
    }

    public void EmitTextLabelCopy(IExpression destinationTextLabel, IExpression sourceTextLabel)
    {
        var sourceType = (TextLabelType)sourceTextLabel.Type!;
        var destinationType = (TextLabelType)destinationTextLabel.Type!;

        EmitValue(sourceTextLabel);
        EmitPushInt(sourceType.SizeOf);
        EmitPushInt(destinationType.SizeOf);
        EmitAddress(destinationTextLabel);
        instEmitter.EmitTextLabelCopy();
    }

    public void EmitLoadFrom(IExpression lvalueExpr)
    {
        Debug.Assert(lvalueExpr.Semantics.ValueKind.Is(ValueKind.Addressable));

        var size = lvalueExpr.Type!.SizeOf;
        if (size == 1)
        {
            EmitAddress(lvalueExpr);
            instEmitter.EmitLoad();
        }
        else
        {
            EmitPushInt(size);
            EmitAddress(lvalueExpr);
            instEmitter.EmitLoadN();
        }
    }

    public void EmitStoreAt(IExpression lvalueExpr)
    {
        Debug.Assert(lvalueExpr.Semantics.ValueKind.Is(ValueKind.Addressable));

        var size = lvalueExpr.Type!.SizeOf;
        if (size == 1)
        {
            EmitAddress(lvalueExpr);
            instEmitter.EmitStore();
        }
        else
        {
            EmitPushInt(size);
            EmitAddress(lvalueExpr);
            instEmitter.EmitStoreN();
        }
    }

    public void EmitAssignment(IExpression destination, IExpression source)
    {
        var sourceType = source.Type!;
        var destinationType = destination.Type!;

        if (sourceType is TextLabelType sourceTypeTL && destinationType is TextLabelType destinationTypeTL &&
            sourceTypeTL.Length != destinationTypeTL.Length)
        {
            // Use TEXT_LABEL_COPY for assigning text labels of different lengths
            EmitTextLabelCopy(destination, source);
        }
        else if (sourceType is StringType && destinationType is TextLabelType destinationTypeTL2)
        {
            // Allow assignment of strings to text labels without intrinsics.
            // NOTE: not using EmitTextLabelAssignString() here because it acts as a function call using EmitArg() for source and destination expressions
            //       since it was intended for the intrinsic. The expressions here are not invocation arguments and don't have ArgumentKind set during the
            //       semantic analysis so EmitArg() would fail.
            EmitValue(source);
            EmitAddress(destination);
            Debug.Assert(destinationTypeTL2.Length <= byte.MaxValue);
            instEmitter.EmitTextLabelAssignString((byte)destinationTypeTL2.Length);
        }
        else
        {
            EmitValue(source);
            EmitStoreAt(destination);
        }
    }

    public void EmitAssignmentToVar(VarDeclaration destination, IExpression source)
    {
        Debug.Assert(destination.Kind is not (VarKind.Constant or VarKind.Field), "Destination variable must be addressable");
        EmitAssignment(
            destination: new NameExpression(destination.NameToken)
            {
                Semantics = new(destination.Semantics.ValueType, ValueKind.RValue | ValueKind.Addressable, ArgumentKind.None, destination)
            },
            source);
    }

    public void EmitArrayIndexing(IndexingExpression expr)
    {
        EmitValue(expr.Index);
        EmitAddress(expr.Array);

        var itemSize = expr.Semantics.Type!.SizeOf;
        switch (itemSize)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                instEmitter.EmitArrayU8((byte)itemSize);
                break;

            case >= ushort.MinValue and <= ushort.MaxValue:
                instEmitter.EmitArrayU16((ushort)itemSize);
                break;

            default:
                Debug.Assert(false, $"Array item size too big (itemSize: {itemSize})");
                break;
        }
    }

    public void EmitOffset(int offset)
    {
        switch (offset)
        {
            case 0:
                // offset doesn't change, don't need to emit anything
                break;

            case >= byte.MinValue and <= byte.MaxValue:
                instEmitter.EmitIOffsetU8((byte)offset);
                break;

            case >= short.MinValue and <= short.MaxValue:
                instEmitter.EmitIOffsetS16((short)offset);
                break;

            default:
                EmitPushInt(offset);
                instEmitter.EmitIOffset();
                break;
        }
    }

    private void EmitGlobalAddress(VarDeclaration declaration)
    {
        throw new NotImplementedException(nameof(EmitGlobalAddress));
        // TODO: EmitGlobal
        //switch (varDecl.Address)
        //{
        //    case >= 0 and <= 0x0000FFFF:
        //        CG.Emit(Opcode.GLOBAL_U16, varDecl.Address);
        //        break;

        //    case >= 0 and <= 0x00FFFFFF:
        //        CG.Emit(Opcode.GLOBAL_U24, varDecl.Address);
        //        break;

        //    default: Debug.Assert(false, "Global var address too big"); break;
        //}
    }
    private void EmitStaticAddress(VarDeclaration declaration)
    {
        int address = statics.OffsetOf(declaration);

        switch (address)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                instEmitter.EmitStaticU8((byte)address);
                break;

            case >= ushort.MinValue and <= ushort.MaxValue:
                instEmitter.EmitStaticU16((ushort)address);
                break;

            default: Debug.Assert(false, "Static var address too big"); break;
        }
    }
    private void EmitScriptParameterAddress(VarDeclaration declaration) => EmitStaticAddress(declaration);
    private void EmitLocalAddress(VarDeclaration declaration)
    {
        var address = currentFunctionFrame.OffsetOf(declaration);
        switch (address)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                instEmitter.EmitLocalU8((byte)address);
                break;

            case >= ushort.MinValue and <= ushort.MaxValue:
                instEmitter.EmitLocalU16((ushort)address);
                break;

            default: Debug.Assert(false, "Local var address too big"); break;
        }
    }
    private void EmitParameterAddress(VarDeclaration declaration)
    {
        if (declaration.IsReference)
        {
            // parameter passed by reference, the address is its value
            var paramAddress = currentFunctionFrame.OffsetOf(declaration);
            switch (paramAddress)
            {
                case >= byte.MinValue and <= byte.MaxValue:
                    instEmitter.EmitLocalU8Load((byte)paramAddress);
                    break;

                case >= ushort.MinValue and <= ushort.MaxValue:
                    instEmitter.EmitLocalU16Load((ushort)paramAddress);
                    break;

                default: Debug.Assert(false, "Parameter address too big"); break;
            }
        }
        else
        {
            // parameter passed by value, treat it as a local variable
            EmitLocalAddress(declaration);
        }
    }

    public void EmitVarAddress(VarDeclaration declaration)
    {
        switch (declaration.Kind)
        {
            case VarKind.Global: EmitGlobalAddress(declaration); break;
            case VarKind.Static: EmitStaticAddress(declaration); break;
            case VarKind.ScriptParameter: EmitScriptParameterAddress(declaration); break;
            case VarKind.Local: EmitLocalAddress(declaration); break;
            case VarKind.Parameter: EmitParameterAddress(declaration); break;

            case VarKind.Constant: Debug.Assert(false, "Cannot get address of constant var"); break;
            case VarKind.Field: Debug.Assert(false, "Cannot get address of field directly"); break;
            default: Debug.Assert(false, "Unknown var kind"); break;
        }
    }

    /// <summary>
    /// Emits code to default initialize a local variable.
    /// </summary>
    public void EmitDefaultInit(VarDeclaration declaration)
    {
        Debug.Assert(declaration.Kind is VarKind.Local);
        EmitLocalAddress(declaration);
        EmitDefaultInitNoPushAddress(declaration.Semantics.ValueType!);
        instEmitter.EmitDrop(); // drop local address
    }

    private void EmitDefaultInitNoPushAddress(TypeInfo type)
    {
        switch (type)
        {
            case StructType ty: EmitDefaultInitStruct(ty); break;
            case ArrayType ty: EmitDefaultInitArray(ty); break;
            default: throw new ArgumentException($"Cannot default initialize type '{type.ToPrettyString()}'", nameof(type));
        }
    }

    private void EmitDefaultInitStruct(StructType structTy)
    {
        foreach (var (field, fieldDecl) in structTy.Fields.Zip(structTy.Declaration.Fields))
        {
            var hasInitializer = fieldDecl.Initializer is not null && !(fieldDecl.Initializer.Type?.IsError ?? true);
            if (hasInitializer || field.Type.IsDefaultInitialized())
            {
                instEmitter.EmitDup(); // duplicate struct address
                EmitOffset(field.Offset); // advance to field offset

                // initialize field
                if (hasInitializer)
                {
                    var initValue = fieldDecl.Semantics.ConstantValue;
                    Debug.Assert(initValue is not null);
                    switch (field.Type)
                    {
                        case IntType or FloatType or BoolType:
                            EmitPushConst(initValue);
                            instEmitter.EmitStoreRev();
                            break;
                        case VectorType:
                            var (x, y, z) = initValue.VectorValue;
                            instEmitter.EmitDup(); // duplicate VECTOR address
                            EmitPushFloat(x);
                            instEmitter.EmitStoreRev(); // store X
                            EmitOffset(1); // advance to Y offset
                            EmitPushFloat(y);
                            instEmitter.EmitStoreRev(); // store Y
                            EmitOffset(1); // advance to Z offset
                            EmitPushFloat(z);
                            instEmitter.EmitStoreRev(); // store Z
                            instEmitter.EmitDrop();
                            break;

                        default:
                            Debug.Assert(false, $"Field initializer of type '{field.Type.ToPrettyString()}' is not supported");
                            break;
                    }
                }
                else
                {
                    Debug.Assert(field.Type.IsDefaultInitialized());
                    EmitDefaultInitNoPushAddress(field.Type);
                }

                instEmitter.EmitDrop(); // drop duplicated address
            }
        }
    }

    private void EmitDefaultInitArray(ArrayType arrayTy)
    {
        // write array size
        EmitPushInt(arrayTy.Length);
        instEmitter.EmitStoreRev();

        if (arrayTy.Item.IsDefaultInitialized())
        {
            instEmitter.EmitDup(); // duplicate array address
            EmitOffset(1); // advance duplicated address to the first item (skip array size)
            var itemSize = arrayTy.Item.SizeOf;
            for (int i = 0; i < arrayTy.Length; i++)
            {
                EmitDefaultInitNoPushAddress(arrayTy.Item); // initialize item
                EmitOffset(itemSize); // advance to the next item
            }
            instEmitter.EmitDrop(); // drop duplicated address
        }
    }

    private static void StaticDefaultInit(Span<ScriptValue64> dest, TypeInfo type)
    {
        Debug.Assert(dest.Length == type.SizeOf);
        switch (type)
        {
            case StructType ty: StaticDefaultInitStruct(dest, ty); break;
            case ArrayType ty: StaticDefaultInitArray(dest, ty); break;
            default: throw new ArgumentException($"Cannot default initialize type '{type.ToPrettyString()}'", nameof(type));
        }
    }

    private static void StaticDefaultInitStruct(Span<ScriptValue64> dest, StructType structTy)
    {
        foreach (var (field, fieldDecl) in structTy.Fields.Zip(structTy.Declaration.Fields))
        {
            var hasInitializer = fieldDecl.Initializer is not null && !(fieldDecl.Initializer.Type?.IsError ?? true);
            if (hasInitializer || field.Type.IsDefaultInitialized())
            {
                var fieldDest = dest.Slice(field.Offset, field.Type.SizeOf);

                // initialize field
                if (hasInitializer)
                {
                    var initValue = fieldDecl.Semantics.ConstantValue;
                    Debug.Assert(initValue is not null);
                    switch (field.Type)
                    {
                        case IntType:
                            fieldDest[0].AsInt32 = initValue.IntValue;
                            break;
                        case FloatType:
                            fieldDest[0].AsFloat = initValue.FloatValue;
                            break;
                        case BoolType:
                            fieldDest[0].AsInt32 = initValue.BoolValue ? 1 : 0;
                            break;
                        case VectorType:
                            var (x, y, z) = initValue.VectorValue;
                            fieldDest[0].AsFloat = x;
                            fieldDest[1].AsFloat = y;
                            fieldDest[2].AsFloat = z;
                            break;

                        default:
                            Debug.Assert(false, $"Field initializer of type '{field.Type.ToPrettyString()}' is not supported");
                            break;
                    }
                }
                else
                {
                    Debug.Assert(field.Type.IsDefaultInitialized());
                    StaticDefaultInit(fieldDest, field.Type);
                }
            }
        }
    }

    private static void StaticDefaultInitArray(Span<ScriptValue64> dest, ArrayType arrayTy)
    {
        // write array size
        dest[0].AsInt32 = arrayTy.Length;

        if (arrayTy.Item.IsDefaultInitialized())
        {
            var itemSize = arrayTy.Item.SizeOf;
            for (int i = 0; i < arrayTy.Length; i++)
            {
                StaticDefaultInit(dest.Slice(1 + itemSize * i, itemSize), arrayTy.Item);
            }
        }
    }

    public void EmitCastIntToFloat() => instEmitter.EmitI2F();
    public void EmitCastFloatToInt() => instEmitter.EmitF2I();
    public void EmitCastFloatToVector() => instEmitter.EmitF2V();

    public void EmitPushString(string value)
    {
        var offset = Strings.GetOffsetOf(value);
        EmitPushInt(offset);
        instEmitter.EmitString();
    }

    public void EmitPushNull() => EmitPushInt(0);
    public void EmitPushBool(bool value) => EmitPushInt(value ? 1 : 0);
    public void EmitPushInt(int value)
    {
        switch (value)
        {
            case -1: instEmitter.EmitPushConstM1(); break;
            case 0: instEmitter.EmitPushConst0(); break;
            case 1: instEmitter.EmitPushConst1(); break;
            case 2: instEmitter.EmitPushConst2(); break;
            case 3: instEmitter.EmitPushConst3(); break;
            case 4: instEmitter.EmitPushConst4(); break;
            case 5: instEmitter.EmitPushConst5(); break;
            case 6: instEmitter.EmitPushConst6(); break;
            case 7: instEmitter.EmitPushConst7(); break;

            case >= byte.MinValue and <= byte.MaxValue:
                instEmitter.EmitPushConstU8((byte)value);
                break;

            case >= short.MinValue and <= short.MaxValue:
                instEmitter.EmitPushConstS16((short)value);
                break;

            case >= 0 and <= 0x00FFFFFF:
                instEmitter.EmitPushConstU24(unchecked((uint)value));
                break;

            default:
                instEmitter.EmitPushConstU32(unchecked((uint)value));
                break;
        }
    }

    public void EmitPushFloat(float value)
    {
        switch (value)
        {
            case -1.0f: instEmitter.EmitPushConstFM1(); break;
            case 0.0f: instEmitter.EmitPushConstF0(); break;
            case 1.0f: instEmitter.EmitPushConstF1(); break;
            case 2.0f: instEmitter.EmitPushConstF2(); break;
            case 3.0f: instEmitter.EmitPushConstF3(); break;
            case 4.0f: instEmitter.EmitPushConstF4(); break;
            case 5.0f: instEmitter.EmitPushConstF5(); break;
            case 6.0f: instEmitter.EmitPushConstF6(); break;
            case 7.0f: instEmitter.EmitPushConstF7(); break;
            default: instEmitter.EmitPushConstF(value); break;
        }
    }

    public void EmitPushConst(ConstantValue value)
    {
        value.Match(
            caseNull: EmitPushNull,
            caseInt: EmitPushInt,
            caseFloat: EmitPushFloat,
            caseBool: EmitPushBool,
            caseString: EmitPushString,
            caseVector: (x, y, z) =>
            {
                EmitPushFloat(x);
                EmitPushFloat(y);
                EmitPushFloat(z);
            });
    }

    public void Label(string name, bool isFunctionLabel = false)
    {
        var nameToIndex = isFunctionLabel ? functionLabelNameToIndex : localLabelNameToIndex;

        var labelInstRef = instEmitter.EmitLabelMarker();
        if (nameToIndex.TryGetValue(name, out var idx))
        {
            if (labels[idx].Instruction is not null)
            {
                // the label appeared before and the references are already resolved, so this is a re-definition
                Debug.Assert(false, $"Label name '{name}' is repeated");
            }

            labels[idx] = labels[idx] with { Instruction = labelInstRef };
        }
        else
        {
            nameToIndex.Add(name, labels.Count);
            labels.Add(new(Instruction: labelInstRef, UnresolvedReferences: new()));
        }
    }

    private void ReferenceLabel(string label, InstructionReference instruction, int operandOffset, LabelReferenceKind kind, bool isFunctionLabel)
    {
        var nameToIndex = isFunctionLabel ? functionLabelNameToIndex : localLabelNameToIndex;

        if (nameToIndex.TryGetValue(label, out var idx))
        {
            labels[idx].UnresolvedReferences.Add(new(instruction, operandOffset, kind));
        }
        else
        {
            // first time this label is referenced
            nameToIndex.Add(label, labels.Count);
            labels.Add(new(Instruction: null, UnresolvedReferences: new() { new(instruction, operandOffset, kind) }));
        }
    }

    private void OnFunctionFound(FunctionDeclaration function)
    {
        if (usedFunctions.Add(function))
        {
            functionsToCompile.Enqueue(function);
        }
    }
}