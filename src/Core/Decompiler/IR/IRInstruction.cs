namespace ScTools.Decompiler.IR;

using System.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.ComponentModel;

[SourceGenerators.GenerateVisitor("IR")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract partial record IRInstruction(int Address)
{
    public IRInstruction? Previous { get; internal set; }
    public IRInstruction? Next { get; internal set; }
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    public string DebuggerDisplay => IRPrinter.PrintToString(this);

    protected virtual bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"Address = {Address:000000}");
        return true;
    }
}

/// <summary>
/// Not a real instruction, but used to represent the end of the script instructions.
/// </summary>
public sealed partial record IREndOfScript(int Address) : IRInstruction(Address);

#region Jumps
/// <summary>
/// Unconditional jump.
/// </summary>
public sealed partial record IRJump(int Address, int JumpAddress) : IRInstruction(Address);

/// <summary>
/// Jump if the top of the stack is <c>0</c> (<c>false</c>).
/// </summary>
public sealed partial record IRJumpIfZero(int Address, int JumpAddress) : IRInstruction(Address);

public readonly record struct IRSwitchCase(int Value, int JumpAddress);
public sealed partial record IRSwitch(int Address, ImmutableArray<IRSwitchCase> Cases) : IRInstruction(Address);
#endregion Jumps

#region Function Calls
/// <summary>
/// Function invocation.
/// </summary>
public sealed partial record IRCall(int Address, int CallAddress) : IRInstruction(Address);

/// <summary>
/// Function pointer invocation.
/// </summary>
public sealed partial record IRCallIndirect(int Address) : IRInstruction(Address);

/// <summary>
/// Native command invocation.
/// </summary>
public sealed partial record IRNativeCall(int Address, int ParamCount, int ReturnCount, ulong Command) : IRInstruction(Address);
#endregion Function Calls

public sealed partial record IREnter(int Address, int ParamCount, int LocalCount, string? FunctionName = null) : IRInstruction(Address);
public sealed partial record IRLeave(int Address, int ParamCount, int ReturnCount) : IRInstruction(Address);

public sealed partial record IRPushInt(int Address, int Value) : IRInstruction(Address);

/// <summary>
/// Push a float literal to the top of the stack.
/// </summary>
/// <param name="Address"></param>
/// <param name="Value"></param>
public sealed partial record IRPushFloat(int Address, float Value) : IRInstruction(Address);

/// <summary>
/// Push a string literal reference to the top of the stack.
/// </summary>
public sealed partial record IRPushString(int Address, string Value) : IRInstruction(Address);

/// <summary>
/// Push a string literal reference from the script string table to the top of the stack.
/// The string offset is located at the top of the stack.
/// </summary>
public sealed partial record IRPushStringFromStringTable(int Address) : IRInstruction(Address);

#region Integer Operations
public sealed partial record IRIAdd(int Address) : IRInstruction(Address);
public sealed partial record IRISub(int Address) : IRInstruction(Address);
public sealed partial record IRIMul(int Address) : IRInstruction(Address);
public sealed partial record IRIDiv(int Address) : IRInstruction(Address);
public sealed partial record IRIMod(int Address) : IRInstruction(Address);
public sealed partial record IRIAnd(int Address) : IRInstruction(Address);
public sealed partial record IRIOr(int Address) : IRInstruction(Address);
public sealed partial record IRIXor(int Address) : IRInstruction(Address);
public sealed partial record IRIBitTest(int Address) : IRInstruction(Address);

public sealed partial record IRINot(int Address) : IRInstruction(Address);
public sealed partial record IRINeg(int Address) : IRInstruction(Address);

public sealed partial record IRIEqual(int Address) : IRInstruction(Address);
public sealed partial record IRINotEqual(int Address) : IRInstruction(Address);
public sealed partial record IRIGreaterThan(int Address) : IRInstruction(Address);
public sealed partial record IRIGreaterOrEqual(int Address) : IRInstruction(Address);
public sealed partial record IRILessThan(int Address) : IRInstruction(Address);
public sealed partial record IRILessOrEqual(int Address) : IRInstruction(Address);
#endregion Integer Operations

#region Float Operations
public sealed partial record IRFAdd(int Address) : IRInstruction(Address);
public sealed partial record IRFSub(int Address) : IRInstruction(Address);
public sealed partial record IRFMul(int Address) : IRInstruction(Address);
public sealed partial record IRFDiv(int Address) : IRInstruction(Address);
public sealed partial record IRFMod(int Address) : IRInstruction(Address);

public sealed partial record IRFNeg(int Address) : IRInstruction(Address);

public sealed partial record IRFEqual(int Address) : IRInstruction(Address);
public sealed partial record IRFNotEqual(int Address) : IRInstruction(Address);
public sealed partial record IRFGreaterThan(int Address) : IRInstruction(Address);
public sealed partial record IRFGreaterOrEqual(int Address) : IRInstruction(Address);
public sealed partial record IRFLessThan(int Address) : IRInstruction(Address);
public sealed partial record IRFLessOrEqual(int Address) : IRInstruction(Address);
#endregion Float Operations

#region Vector Operations
public sealed partial record IRVAdd(int Address) : IRInstruction(Address);
public sealed partial record IRVSub(int Address) : IRInstruction(Address);
public sealed partial record IRVMul(int Address) : IRInstruction(Address);
public sealed partial record IRVDiv(int Address) : IRInstruction(Address);

public sealed partial record IRVNeg(int Address) : IRInstruction(Address);
#endregion Vector Operations

public sealed partial record IRIntToFloat(int Address) : IRInstruction(Address);
public sealed partial record IRFloatToInt(int Address) : IRInstruction(Address);
public sealed partial record IRFloatToVector(int Address) : IRInstruction(Address);

public sealed partial record IRDup(int Address) : IRInstruction(Address);
public sealed partial record IRDrop(int Address) : IRInstruction(Address);

public sealed partial record IRLoad(int Address) : IRInstruction(Address);
public sealed partial record IRStore(int Address) : IRInstruction(Address);
public sealed partial record IRStoreRev(int Address) : IRInstruction(Address);
public sealed partial record IRLoadN(int Address) : IRInstruction(Address);
public sealed partial record IRStoreN(int Address) : IRInstruction(Address);

// GTA IV Win32 special instructions
public sealed partial record IRXProtectLoad(int Address) : IRInstruction(Address);
public sealed partial record IRXProtectStore(int Address) : IRInstruction(Address);
public sealed partial record IRXProtectRef(int Address) : IRInstruction(Address);

#region Vars
/// <summary>
/// Push a variable reference to the top of the stack.
/// </summary>
/// <param name="Address"></param>
/// <param name="VarAddress"></param>
public abstract record IRVarRef(int Address, int VarAddress) : IRInstruction(Address);
public sealed partial record IRLocalRef(int Address, int VarAddress) : IRVarRef(Address, VarAddress);
public sealed partial record IRStaticRef(int Address, int VarAddress) : IRVarRef(Address, VarAddress);
public sealed partial record IRGlobalRef(int Address, int VarAddress) : IRVarRef(Address, VarAddress);

/// <summary>
/// Push a variable reference to the top of the stack. The variable address is located at the top of the stack.
/// </summary>
/// <param name="Address"></param>
public abstract record IRVarRefFromStack(int Address) : IRInstruction(Address);
public sealed partial record IRLocalRefFromStack(int Address) : IRVarRefFromStack(Address);
public sealed partial record IRStaticRefFromStack(int Address) : IRVarRefFromStack(Address);
public sealed partial record IRGlobalRefFromStack(int Address) : IRVarRefFromStack(Address);
#endregion Vars

public sealed partial record IRArrayItemRefSizeInStack(int Address) : IRInstruction(Address);
public sealed partial record IRArrayItemRef(int Address, int ItemSize) : IRInstruction(Address);

public sealed partial record IRNullRef(int Address) : IRInstruction(Address);

public sealed partial record IRTextLabelAssignString(int Address, int TextLabelLength) : IRInstruction(Address);
public sealed partial record IRTextLabelAssignInt(int Address, int TextLabelLength) : IRInstruction(Address);
public sealed partial record IRTextLabelAppendString(int Address, int TextLabelLength) : IRInstruction(Address);
public sealed partial record IRTextLabelAppendInt(int Address, int TextLabelLength) : IRInstruction(Address);
public sealed partial record IRTextLabelCopy(int Address) : IRInstruction(Address);

public sealed partial record IRCatch(int Address) : IRInstruction(Address);
public sealed partial record IRThrow(int Address) : IRInstruction(Address);
