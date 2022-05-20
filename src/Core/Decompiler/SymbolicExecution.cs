namespace ScTools.Decompiler;

using ScTools.Decompiler.IR;

using System;
using System.Collections.Generic;

using SymStack = System.Collections.Immutable.ImmutableStack<SymValue>;

public abstract record SymValue()
{
}

public record SymAnyValue() : SymValue() { }
public record SymIntValue() : SymValue() { }
public record SymFloatValue() : SymValue() { }
public record SymStringValue() : SymValue() { }
public record SymConstIntValue(int Value) : SymIntValue() { }
public record SymConstFloatValue(float Value) : SymFloatValue() { }
public record SymConstStringValue(string Value) : SymStringValue() { }
public record SymVarRef(int VarAddress) : SymValue() { }
public record SymLocalRef(int VarAddress) : SymVarRef(VarAddress) { }
public record SymStaticRef(int VarAddress) : SymVarRef(VarAddress) { }
public record SymGlobalRef(int VarAddress) : SymVarRef(VarAddress) { }
public record SymVarRefOffset(SymVarRef Base, int Offset) : SymVarRef(Base.VarAddress + Offset) { }

public record SymFunctionFrame(Function Function, IRInstruction? Return)
{
    public static SymFunctionFrame Root(Function function) => new(function, Return: null);
}

/// <summary>
/// 
/// </summary>
/// <param name="PC">Instruciton to be executed after the current instruction.</param>
public record SymState(IRInstruction PC, SymStack Stack, SymFunctionFrame Frame, SymState? PreviousState = null)
{
    public SymState Update(IRInstruction pc, SymStack stack)
        => this with
        {
            PreviousState = this,
            PC = pc,
            Stack = stack
        };
}

internal static class SymStackExtensions
{
    public static SymStack PopN(this SymStack stack, int n)
    {
        for (int i = 0; i < n; i++)
        {
            stack = stack.Pop();
        }
        return stack;
    }
    
    public static SymStack PushN(this SymStack stack, int n, SymValue value)
    {
        for (int i = 0; i < n; i++)
        {
            stack = stack.Push(value);
        }
        return stack;
    }
}

public class SymbolicExecutor
{
    private readonly IRScript script;
    private readonly Executor executor;
    private readonly Queue<SymState> states = new();
    private readonly List<Function> functions = new();

    public SymbolicExecutor(IRScript script)
    {
        if (script.Head is null || script.Tail is null)
        {
            throw new ArgumentException("Script is missing instructions.", nameof(script));
        }

        this.script = script;
        var entryFunction = new Function(script, script.Head);
        functions.Add(entryFunction);
        states.Enqueue(new(script.Head, SymStack.Empty, SymFunctionFrame.Root(entryFunction)));
        executor = new(this);
    }

    public void RunOnce()
    {
        if (states.TryDequeue(out var state))
        {
            if (state.PC is IREndOfScript)
            {
                return;
            }
            else
            {
                var newStates = state.PC.Accept(executor, state);
                newStates.ForEach(s => states.Enqueue(s));
            }
        }
    }

    private Function GetFunctionAt(int address)
    {
        foreach (var function in functions)
        {
            if (function.StartAddress == address)
            {
                return function;
            }
        }

        var inst = script.FindInstructionAt(address);
        if (inst is null)
        {
            throw new ArgumentException($"Instruction at address {address:000000} not found.", nameof(address));
        }

        var func = new Function(script, inst);
        functions.Add(func);
        return func;
    }

    private sealed class Executor : IIRVisitor<IEnumerable<SymState>, SymState>
    {
        private readonly SymbolicExecutor parent;
        private readonly SymAnyValue symAny = new();
        private readonly SymIntValue symInt = new();
        private readonly SymFloatValue symFloat = new();

        public Executor(SymbolicExecutor parent)
        {
            this.parent = parent;
        }

        private SymState Step(SymState state, SymStack? newStack = null)
        {
            return state with { PreviousState = state, PC = state.PC.Next!, Stack = newStack ?? state.Stack };
        }
        private SymState JumpTo(SymState state, int address, SymStack? newStack = null)
        {
            var block = state.Frame.Function.GetBlockStartingAtAddress(address);
            return state with { PreviousState = state, PC = block.Start, Stack = newStack ?? state.Stack };
        }
        private SymState Call(SymState state, int address)
        {
            // TODO: this is going to fail because the CFG is only build for the main function
            var func = parent.GetFunctionAt(address);
            return state with { PreviousState = state, PC = func.Start, Frame = new(func, state.PC.Next!) };
        }

        public IEnumerable<SymState> Visit(IREndOfScript inst, SymState state) => Array.Empty<SymState>();

        public IEnumerable<SymState> Visit(IRJump inst, SymState state)
        {
            yield return JumpTo(state, inst.JumpAddress);
        }

        public IEnumerable<SymState> Visit(IRJumpIfZero inst, SymState state)
        {
            var s = state.Stack.Pop();
            yield return JumpTo(state, inst.JumpAddress, s);
            yield return Step(state, s);
        }

        public IEnumerable<SymState> Visit(IRSwitch inst, SymState state)
        {
            var s = state.Stack.Pop();
            foreach (var c in inst.Cases)
            {
                yield return JumpTo(state, c.JumpAddress, s);
            }

            yield return Step(state, s);
        }

        public IEnumerable<SymState> Visit(IRCall inst, SymState state)
        {
            yield return Call(state, inst.CallAddress);
        }

        public IEnumerable<SymState> Visit(IRCallIndirect inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRNativeCall inst, SymState state)
        {
            var s = state.Stack.PopN(inst.ParamCount).PushN(inst.ReturnCount, symAny);
            yield return Step(state, s);
        }

        public IEnumerable<SymState> Visit(IREnter inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRLeave inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRPushInt inst, SymState state)
        {
            yield return Step(state, state.Stack.Push(new SymConstIntValue(inst.Value)));
        }

        public IEnumerable<SymState> Visit(IRPushFloat inst, SymState state)
        {
            yield return Step(state, state.Stack.Push(new SymConstFloatValue(inst.Value)));
        }

        public IEnumerable<SymState> Visit(IRPushString inst, SymState state)
        {
            yield return Step(state, state.Stack.Push(new SymConstStringValue(inst.Value)));
        }

        public IEnumerable<SymState> Visit(IRPushStringFromStringTable inst, SymState state)
        {
            var s = state.Stack.Pop(out var stringOffsetValue);
            var stringOffset = stringOffsetValue is SymConstIntValue intValue ? intValue.Value : throw new InvalidOperationException("Expected an integer constant at the top of the stack");
            yield return Step(state, s.Push(new SymConstStringValue($"<lookup string table at {stringOffset}>")));
        }

        public IEnumerable<SymState> Visit(IRIAdd inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRISub inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIMul inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIDiv inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIMod inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIAnd inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIOr inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIXor inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIBitTest inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRINot inst, SymState state)
        {
            yield return Step(state, state.Stack.Pop().Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRINeg inst, SymState state)
        {
            yield return Step(state, state.Stack.Pop().Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRINotEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIGreaterThan inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRIGreaterOrEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRILessThan inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRILessOrEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRFAdd inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symFloat));
        }

        public IEnumerable<SymState> Visit(IRFSub inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symFloat));
        }

        public IEnumerable<SymState> Visit(IRFMul inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symFloat));
        }

        public IEnumerable<SymState> Visit(IRFDiv inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symFloat));
        }

        public IEnumerable<SymState> Visit(IRFMod inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symFloat));
        }

        public IEnumerable<SymState> Visit(IRFNeg inst, SymState state)
        {
            yield return Step(state, state.Stack.Pop().Push(symFloat));
        }

        public IEnumerable<SymState> Visit(IRFEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRFNotEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRFGreaterThan inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRFGreaterOrEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRFLessThan inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRFLessOrEqual inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(2).Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRVAdd inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(6).PushN(3, symFloat));
        }

        public IEnumerable<SymState> Visit(IRVSub inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(6).PushN(3, symFloat));
        }

        public IEnumerable<SymState> Visit(IRVMul inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(6).PushN(3, symFloat));
        }

        public IEnumerable<SymState> Visit(IRVDiv inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(6).PushN(3, symFloat));
        }

        public IEnumerable<SymState> Visit(IRVNeg inst, SymState state)
        {
            yield return Step(state, state.Stack.PopN(3).PushN(3, symFloat));
        }

        public IEnumerable<SymState> Visit(IRIntToFloat inst, SymState state)
        {
            yield return Step(state, state.Stack.Pop().Push(symFloat));
        }

        public IEnumerable<SymState> Visit(IRFloatToInt inst, SymState state)
        {
            yield return Step(state, state.Stack.Pop().Push(symInt));
        }

        public IEnumerable<SymState> Visit(IRFloatToVector inst, SymState state)
        {
            yield return Step(state, state.Stack.Pop().PushN(3, symFloat));
        }

        public IEnumerable<SymState> Visit(IRDup inst, SymState state)
        {
            yield return Step(state, state.Stack.Push(state.Stack.Peek()));
        }

        public IEnumerable<SymState> Visit(IRDrop inst, SymState state)
        {
            yield return Step(state, state.Stack.Pop());
        }

        public IEnumerable<SymState> Visit(IRLoad inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRStore inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRStoreRev inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRLoadN inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRStoreN inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRLocalRef inst, SymState state)
        {
            yield return Step(state, state.Stack.Push(new SymLocalRef(inst.VarAddress)));
        }

        public IEnumerable<SymState> Visit(IRStaticRef inst, SymState state)
        {
            yield return Step(state, state.Stack.Push(new SymStaticRef(inst.VarAddress)));
        }

        public IEnumerable<SymState> Visit(IRGlobalRef inst, SymState state)
        {
            yield return Step(state, state.Stack.Push(new SymGlobalRef(inst.VarAddress)));
        }

        public IEnumerable<SymState> Visit(IRLocalRefFromStack inst, SymState state)
        {
            var s = state.Stack.Pop(out var varAddressValue);
            var varAddress = varAddressValue is SymConstIntValue intValue ? intValue.Value : throw new InvalidOperationException("Expected an integer constant at the top of the stack");
            yield return Step(state, s.Push(new SymLocalRef(varAddress)));
        }

        public IEnumerable<SymState> Visit(IRStaticRefFromStack inst, SymState state)
        {
            var s = state.Stack.Pop(out var varAddressValue);
            var varAddress = varAddressValue is SymConstIntValue intValue ? intValue.Value : throw new InvalidOperationException("Expected an integer constant at the top of the stack");
            yield return Step(state, s.Push(new SymStaticRef(varAddress)));
        }

        public IEnumerable<SymState> Visit(IRGlobalRefFromStack inst, SymState state)
        {
            var s = state.Stack.Pop(out var varAddressValue);
            var varAddress = varAddressValue is SymConstIntValue intValue ? intValue.Value : throw new InvalidOperationException("Expected an integer constant at the top of the stack");
            yield return Step(state, s.Push(new SymGlobalRef(varAddress)));
        }

        public IEnumerable<SymState> Visit(IRArrayItemRef inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRArrayItemRefSizeInStack inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRNullRef inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRTextLabelAssignString inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRTextLabelAssignInt inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRTextLabelAppendString inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRTextLabelAppendInt inst, SymState state)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymState> Visit(IRTextLabelCopy inst, SymState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
