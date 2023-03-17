namespace ScTools.ScriptLang.CodeGen.Targets.NY;

using ScTools.GameFiles;
using ScTools.ScriptAssembly.Targets;
using ScTools.ScriptAssembly.Targets.NY;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

public sealed partial class CodeEmitter : ICodeEmitter
{
    private readonly StatementEmitter stmtEmitter;
    private readonly ValueEmitter valueEmitter;
    private readonly AddressEmitter addressEmitter;
    private const bool IncludeFunctionNames = true;

    private readonly InstructionBuffer instBuffer;
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

    public CodeEmitter(VarAllocator statics)
    {
        this.statics = new(statics);
        instBuffer = new();
        instEmitter = new(new AppendInstructionFlushStrategy(instBuffer));

        stmtEmitter = new(this);
        valueEmitter = new(this);
        addressEmitter = new(this);
    }

    private byte[] ToCodeBuffer() => instBuffer.Finish(labels);

    private ScriptValue32[] GetStaticSegment(out int numScriptParams)
    {
        var staticsBuffer = new ScriptValue32[statics.AllocatedSize];

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

    public IScript EmitScript(ScriptDeclaration script)
    {
        EmitScriptEntryPoint(script);

        while (functionsToCompile.TryDequeue(out var function))
        {
            EmitFunction(function);
        }

        return FinalizeScript(script);
    }

    private IScript FinalizeScript(ScriptDeclaration script)
    {
        var code = ToCodeBuffer();
        var statics = GetStaticSegment(out var argsCount);
        //var globals = globalSegmentBuilder.Length != 0 ? globalSegmentBuilder.ToPages<ScriptValue>() : null;
        return new ScriptNY
        {
            GlobalsSignature = 0, // TODO: include a way to set the hash in the SCRIPT declaration
            Code = code,
            CodeLength = (uint)(code?.Length ?? 0),
            //GlobalsPages = globals,
            //GlobalsLength = globals?.Length ?? 0,
            Statics = statics,
            StaticsCount = (uint)(statics?.Length ?? 0),
            ArgsCount = (uint)argsCount,
        };
    }

    private void EmitScriptEntryPoint(ScriptDeclaration script)
    {
        var staticsWithoutScriptParamsSize = statics.AllocatedSize;
        script.Parameters.ForEach(p => statics.Allocate(p));
        numScriptParams = statics.AllocatedSize - staticsWithoutScriptParamsSize;

        EmitFunctionCommon("SCRIPT", script.Parameters, script.Body, VoidType.Instance, isScriptEntryPoint: true);
    }
    
    private void EmitFunction(FunctionDeclaration function)
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
        var enter = instEmitter.EmitEnter(currentFunctionArgCount, frameSize: 0);

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
        instEmitter.FlushStrategy = new UpdateInstructionFlushStrategy(instBuffer, enter);
        instEmitter.EmitEnter(currentFunctionArgCount, (ushort)currentFunctionFrame.AllocatedSize);
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
        ReferenceLabel(label, inst, 1, isFunctionLabel: false);
    }
    public void EmitJumpIfZero(string label)
    {
        var inst = instEmitter.EmitJZ(0);
        ReferenceLabel(label, inst, 1, isFunctionLabel: false);
    }

    public void EmitCall(FunctionDeclaration function)
    {
        var inst = instEmitter.EmitCall(0);
        ReferenceLabel(function.Name, inst, 1, isFunctionLabel: true);
        OnFunctionFound(function);
    }

    public void EmitFunctionAddress(FunctionDeclaration function)
        => throw new NotSupportedException("Function pointers are not supported");

    public void EmitNativeCall(NativeFunctionDeclaration nativeFunction)
    {
        var funcType = (FunctionType)nativeFunction.Semantics.ValueType!;
        var argCount = funcType.Parameters.Sum(p => p.SizeOf);
        var returnCount = funcType.Return.SizeOf;
        Debug.Assert(argCount <= byte.MaxValue);
        Debug.Assert(returnCount <= byte.MaxValue);
        // NOTE: this isn't how hashes since 1.0.7.0 are computed, that's why we allow to override the hash with the Id expression
        var hash = nativeFunction.Name.ToLowercaseHash();
        if (nativeFunction.Id is not null)
        {
            Debug.Assert(nativeFunction.Id is StringLiteralExpression);
            var hashStr = ((StringLiteralExpression)nativeFunction.Id).Value;
            Debug.Assert(hashStr.StartsWith("0x"));
            hash = hashStr.ParseAsUInt();
        }
        instEmitter.EmitNative((byte)argCount, (byte)returnCount, hash);
    }

    public void EmitIndirectCall()
        => throw new NotSupportedException("Function pointers are not supported");

    public void EmitSwitch(IEnumerable<ValueSwitchCase> valueCases)
    {
        var cases = valueCases.ToArray();
        var inst = instEmitter.EmitSwitch(cases.Select(c => (unchecked((uint)c.Semantics.Value!.Value), 0u)).ToArray());
        for (int i = 0; i < cases.Length; i++)
        {
            var @case = cases[i];
            var valueOffset = 2 + i * 8;
            var labelOffset = valueOffset + 4;
            ReferenceLabel(@case.Semantics.Label!, inst, labelOffset, isFunctionLabel: false);
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
        else if (sourceType is IntType && destinationType is TextLabelType destinationTypeTL3)
        {
            // Allow assignment of ints to text labels without intrinsics.
            EmitValue(source);
            EmitAddress(destination);
            Debug.Assert(destinationTypeTL3.Length <= byte.MaxValue);
            instEmitter.EmitTextLabelAssignInt((byte)destinationTypeTL3.Length);
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

    public void EmitCompoundAssignment(IExpression destination, IExpression source, BinaryOperator binOp)
    {

        var sourceType = source.Type!;
        var destinationType = destination.Type!;

        if (sourceType is TextLabelType && destinationType is TextLabelType destinationTypeTL1)
        {
            Debug.Assert(binOp is BinaryOperator.Add, "Only += is supported for text labels");
            Debug.Assert(source.ValueKind.Is(ValueKind.Addressable), "Text label to append must be addressable");
            // Allow appending text labels to text labels without intrinsics.
            EmitAddress(source);
            EmitAddress(destination);
            Debug.Assert(destinationTypeTL1.Length <= byte.MaxValue);
            instEmitter.EmitTextLabelAppendString((byte)destinationTypeTL1.Length);
        }
        else if (sourceType is StringType && destinationType is TextLabelType destinationTypeTL2)
        {
            Debug.Assert(binOp is BinaryOperator.Add, "Only += is supported for text labels");
            // Allow appending strings to text labels without intrinsics.
            EmitValue(source);
            EmitAddress(destination);
            Debug.Assert(destinationTypeTL2.Length <= byte.MaxValue);
            instEmitter.EmitTextLabelAppendString((byte)destinationTypeTL2.Length);
        }
        else if (sourceType is IntType && destinationType is TextLabelType destinationTypeTL3)
        {
            Debug.Assert(binOp is BinaryOperator.Add, "Only += is supported for text labels");
            // Allow appending ints to text labels without intrinsics.
            EmitValue(source);
            EmitAddress(destination);
            Debug.Assert(destinationTypeTL3.Length <= byte.MaxValue);
            instEmitter.EmitTextLabelAppendInt((byte)destinationTypeTL3.Length);
        }
        else
        {
            // synthesize `lhs binOp= rhs` as `lhs = lhs binOp rhs`
            var binExpr = new BinaryExpression(binOp.ToToken().Create(), destination, source)
            {
                Semantics = new(destination.Type, (ValueKind.RValue | ValueKind.Constant) & destination.ValueKind & source.ValueKind, ArgumentKind.None)
            };
            EmitAssignment(destination, binExpr);
        }
    }

    public void EmitArrayIndexing(IndexingExpression expr)
    {
        EmitValue(expr.Index);
        EmitAddress(expr.Array);

        var itemSize = expr.Semantics.Type!.SizeOf;
        EmitPushInt(itemSize);
        instEmitter.EmitArray();
    }

    public void EmitPushArrayLength(IExpression arrayExpr)
    {
        // arrays have a hidden INT at the beginning of its memory layout that stores the length
        EmitAddress(arrayExpr);
        instEmitter.EmitLoad();
    }

    public void EmitOffset(int offset)
    {
        if (offset == 0)
        {
            // offset doesn't change, don't need to emit anything
            return;
        }

        EmitPushInt(offset);
        instEmitter.EmitIAdd();
    }

    private void EmitGlobalAddress(VarDeclaration declaration)
    {
        throw new NotImplementedException(nameof(EmitGlobalAddress));
        // TODO: EmitGlobal
    }
    private void EmitStaticAddress(VarDeclaration declaration)
    {
        int address = statics.OffsetOf(declaration);
        EmitPushInt(address);
        instEmitter.EmitStatic();
    }
    private void EmitScriptParameterAddress(VarDeclaration declaration) => EmitStaticAddress(declaration);
    private void EmitLocalAddress(VarDeclaration declaration)
    {
        var address = currentFunctionFrame.OffsetOf(declaration);
        if (address is >= 0 and <= 7)
        {
            instEmitter.EmitLocalN(address);
        }
        else
        {
            EmitPushInt(address);
            instEmitter.EmitLocal();
        }
    }
    private void EmitParameterAddress(VarDeclaration declaration)
    {
        if (declaration.IsReference)
        {
            // parameter passed by reference, the address is its value
            EmitLocalAddress(declaration);
            instEmitter.EmitLoad();
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

    private static void StaticDefaultInit(Span<ScriptValue32> dest, TypeInfo type)
    {
        Debug.Assert(dest.Length == type.SizeOf);
        switch (type)
        {
            case StructType ty: StaticDefaultInitStruct(dest, ty); break;
            case ArrayType ty: StaticDefaultInitArray(dest, ty); break;
            default: throw new ArgumentException($"Cannot default initialize type '{type.ToPrettyString()}'", nameof(type));
        }
    }

    private static void StaticDefaultInitStruct(Span<ScriptValue32> dest, StructType structTy)
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

    private static void StaticDefaultInitArray(Span<ScriptValue32> dest, ArrayType arrayTy)
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

    public void EmitUnaryOp(UnaryOperator unaryOp, TypeInfo operandType)
    {
        switch (operandType)
        {
            case IntType: EmitUnaryOpInt(unaryOp); break;
            case FloatType: EmitUnaryOpFloat(unaryOp); break;
            case BoolType: EmitUnaryOpBool(unaryOp); break;
            case VectorType: EmitUnaryOpVector(unaryOp); break;
            default: throw new ArgumentException($"Type '{operandType.ToPrettyString()}' does not support any unary operator", nameof(operandType));
        }
    }

    private void EmitUnaryOpInt(UnaryOperator unaryOp)
    {
        _ = unaryOp switch
        {
            UnaryOperator.Negate => instEmitter.EmitINeg(),
            UnaryOperator.LogicalNot => instEmitter.EmitINot(),
            _ => throw new ArgumentException($"Unary operator '{unaryOp}' is not supported for INT type", nameof(unaryOp)),
        };
    }

    private void EmitUnaryOpFloat(UnaryOperator unaryOp)
    {
        _ = unaryOp switch
        {
            UnaryOperator.Negate => instEmitter.EmitFNeg(),
            _ => throw new ArgumentException($"Unary operator '{unaryOp}' is not supported for FLOAT type", nameof(unaryOp)),
        };
    }

    private void EmitUnaryOpBool(UnaryOperator unaryOp)
    {
        _ = unaryOp switch
        {
            UnaryOperator.LogicalNot => instEmitter.EmitINot(),
            _ => throw new ArgumentException($"Unary operator '{unaryOp}' is not supported for BOOL type", nameof(unaryOp)),
        };
    }

    private void EmitUnaryOpVector(UnaryOperator unaryOp)
    {
        _ = unaryOp switch
        {
            UnaryOperator.Negate => instEmitter.EmitVNeg(),
            _ => throw new ArgumentException($"Unary operator '{unaryOp}' is not supported for VECTOR type", nameof(unaryOp)),
        };
    }

    public void EmitBinaryOp(BinaryOperator binaryOp, TypeInfo operandsType)
    {
        switch (operandsType)
        {
            case IntType: EmitBinaryOpInt(binaryOp); break;
            case FloatType: EmitBinaryOpFloat(binaryOp); break;
            case BoolType: EmitBinaryOpBool(binaryOp); break;
            case VectorType: EmitBinaryOpVector(binaryOp); break;
            default: throw new ArgumentException($"Type '{operandsType.ToPrettyString()}' does not support any binary operator", nameof(operandsType));
        }
    }

    private void EmitBinaryOpInt(BinaryOperator binaryOp)
    {
        _ = binaryOp switch
        {
            BinaryOperator.Add => instEmitter.EmitIAdd(),
            BinaryOperator.Subtract => instEmitter.EmitISub(),
            BinaryOperator.Multiply => instEmitter.EmitIMul(),
            BinaryOperator.Divide => instEmitter.EmitIDiv(),
            BinaryOperator.Modulo => instEmitter.EmitIMod(),
            BinaryOperator.And => instEmitter.EmitIAnd(),
            BinaryOperator.Xor => instEmitter.EmitIXor(),
            BinaryOperator.Or => instEmitter.EmitIOr(),
            BinaryOperator.Equals => instEmitter.EmitIEq(),
            BinaryOperator.NotEquals => instEmitter.EmitINe(),
            BinaryOperator.LessThan => instEmitter.EmitILt(),
            BinaryOperator.LessThanOrEqual => instEmitter.EmitILe(),
            BinaryOperator.GreaterThan => instEmitter.EmitIGt(),
            BinaryOperator.GreaterThanOrEqual => instEmitter.EmitIGe(),
            _ => throw new ArgumentException($"Binary operator '{binaryOp}' is not supported for INT type", nameof(binaryOp)),
        };
    }

    private void EmitBinaryOpFloat(BinaryOperator binaryOp)
    {
        _ = binaryOp switch
        {
            BinaryOperator.Add => instEmitter.EmitFAdd(),
            BinaryOperator.Subtract => instEmitter.EmitFSub(),
            BinaryOperator.Multiply => instEmitter.EmitFMul(),
            BinaryOperator.Divide => instEmitter.EmitFDiv(),
            BinaryOperator.Modulo => instEmitter.EmitFMod(),
            BinaryOperator.Equals => instEmitter.EmitFEq(),
            BinaryOperator.NotEquals => instEmitter.EmitFNe(),
            BinaryOperator.LessThan => instEmitter.EmitFLt(),
            BinaryOperator.LessThanOrEqual => instEmitter.EmitFLe(),
            BinaryOperator.GreaterThan => instEmitter.EmitFGt(),
            BinaryOperator.GreaterThanOrEqual => instEmitter.EmitFGe(),
            _ => throw new ArgumentException($"Binary operator '{binaryOp}' is not supported for FLOAT type", nameof(binaryOp)),
        };
    }

    private void EmitBinaryOpBool(BinaryOperator binaryOp)
    {
        _ = binaryOp switch
        {
            BinaryOperator.LogicalAnd => instEmitter.EmitIAnd(),
            BinaryOperator.LogicalOr => instEmitter.EmitIOr(),
            BinaryOperator.Equals => instEmitter.EmitIEq(),
            BinaryOperator.NotEquals => instEmitter.EmitINe(),
            _ => throw new ArgumentException($"Binary operator '{binaryOp}' is not supported for BOOL type", nameof(binaryOp)),
        };
    }

    private void EmitBinaryOpVector(BinaryOperator binaryOp)
    {
        _ = binaryOp switch
        {
            BinaryOperator.Add => instEmitter.EmitVAdd(),
            BinaryOperator.Subtract => instEmitter.EmitVSub(),
            BinaryOperator.Multiply => instEmitter.EmitVMul(),
            BinaryOperator.Divide => instEmitter.EmitVDiv(),
            _ => throw new ArgumentException($"Binary operator '{binaryOp}' is not supported for VECTOR type", nameof(binaryOp)),
        };
    }

    public void EmitCastIntToFloat() => instEmitter.EmitI2F();
    public void EmitCastFloatToInt() => instEmitter.EmitF2I();
    public void EmitCastFloatToVector() => instEmitter.EmitF2V();

    public void EmitStringHash() => throw new NotImplementedException($"{nameof(EmitStringHash)} is not supported"); // TODO: call GET_HASH_KEY for NY's EmitStringHash?

    public void EmitCatch() => instEmitter.EmitCatch();
    public void EmitThrow() => instEmitter.EmitThrow();

    public void EmitDup() => instEmitter.EmitDup();
    public void EmitDrop() => instEmitter.EmitDrop();

    public void EmitPushString(string value) => instEmitter.EmitString(value);
    public void EmitPushNull()
    {
        instEmitter.EmitNull();
        instEmitter.EmitLoad();
    }
    public void EmitPushBool(bool value) => EmitPushInt(value ? 1 : 0);
    public void EmitPushInt(int value)
    {
        switch (value)
        {
            case >= -16 and <= 159:
                instEmitter.EmitPushConstN(value);
                break;
            case >= short.MinValue and <= short.MaxValue:
                instEmitter.EmitPushConstU16(unchecked((ushort)value));
                break;
            default:
                instEmitter.EmitPushConstU32(unchecked((uint)value));
                break;
        }
    }
    public void EmitPushFloat(float value) => instEmitter.EmitPushConstF(value);

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

    public void Label(string name) => Label(name, isFunctionLabel: false);
    private void Label(string name, bool isFunctionLabel = false)
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

    private void ReferenceLabel(string label, InstructionReference instruction, int operandOffset, bool isFunctionLabel)
    {
        var nameToIndex = isFunctionLabel ? functionLabelNameToIndex : localLabelNameToIndex;

        if (nameToIndex.TryGetValue(label, out var idx))
        {
            labels[idx].UnresolvedReferences.Add(new(instruction, operandOffset));
        }
        else
        {
            // first time this label is referenced
            nameToIndex.Add(label, labels.Count);
            labels.Add(new(Instruction: null, UnresolvedReferences: new() { new(instruction, operandOffset) }));
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
