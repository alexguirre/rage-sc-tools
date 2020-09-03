#nullable enable
namespace ScTools.ScriptLang.CodeGen
{
    using System;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed partial class CodeGenerator
    {
        public ByteCodeBuilder Code { get; } = new ByteCodeBuilder();
        private FunctionSymbol? func = null;

        private void EmitPrologue()
        {
            Debug.Assert(func != null);

            // every function needs at least 2 locals (return address + function frame number)
            const uint MinLocals = 2;

            uint argsSize = (uint)func.LocalArgsSize;
            uint localsSize = (uint)func.LocalsSize + argsSize + MinLocals;
            Code.Emit(Opcode.ENTER, new[] { new Operand(argsSize), new Operand(localsSize) });
        }

        private void EmitEpilogue()
        {
            Debug.Assert(func != null);

            uint argsSize = (uint)func.LocalArgsSize;
            uint returnSize = (uint)(func.Type.ReturnType?.SizeOf ?? 0);
            Code.Emit(Opcode.LEAVE, new[] { new Operand(argsSize), new Operand(returnSize) });
        }

        private void EmitPushInt(int v)
        {
            var inst = v switch
            {
                -1 => (Opcode.PUSH_CONST_M1, Array.Empty<Operand>()),
                0 => (Opcode.PUSH_CONST_0, Array.Empty<Operand>()),
                1 => (Opcode.PUSH_CONST_1, Array.Empty<Operand>()),
                2 => (Opcode.PUSH_CONST_2, Array.Empty<Operand>()),
                3 => (Opcode.PUSH_CONST_3, Array.Empty<Operand>()),
                4 => (Opcode.PUSH_CONST_4, Array.Empty<Operand>()),
                5 => (Opcode.PUSH_CONST_5, Array.Empty<Operand>()),
                6 => (Opcode.PUSH_CONST_6, Array.Empty<Operand>()),
                7 => (Opcode.PUSH_CONST_7, Array.Empty<Operand>()),
                _ when v <= byte.MaxValue => (Opcode.PUSH_CONST_U8, new[] { new Operand(unchecked((uint)v)) }),
                _ when v >= short.MinValue && v <= short.MaxValue => (Opcode.PUSH_CONST_S16, new[] { new Operand(unchecked((uint)v)) }),
                _ when v <= 0x00FFFFFF => (Opcode.PUSH_CONST_U24, new[] { new Operand(unchecked((uint)v)) }),
                _ => (Opcode.PUSH_CONST_U32, new[] { new Operand(unchecked((uint)v)) }),
            };

            Code.Emit(inst.Item1, inst.Item2);
        }

        private void EmitPushFloat(float v)
        {
            var inst = v switch
            {
                -1.0f => (Opcode.PUSH_CONST_FM1, Array.Empty<Operand>()),
                0.0f => (Opcode.PUSH_CONST_F0, Array.Empty<Operand>()),
                1.0f => (Opcode.PUSH_CONST_F1, Array.Empty<Operand>()),
                2.0f => (Opcode.PUSH_CONST_F2, Array.Empty<Operand>()),
                3.0f => (Opcode.PUSH_CONST_F3, Array.Empty<Operand>()),
                4.0f => (Opcode.PUSH_CONST_F4, Array.Empty<Operand>()),
                5.0f => (Opcode.PUSH_CONST_F5, Array.Empty<Operand>()),
                6.0f => (Opcode.PUSH_CONST_F6, Array.Empty<Operand>()),
                7.0f => (Opcode.PUSH_CONST_F7, Array.Empty<Operand>()),
                _ => (Opcode.PUSH_CONST_F, new[] { new Operand(v) }),
            };

            Code.Emit(inst.Item1, inst.Item2);
        }

        private void EmitLocal(int location, Opcode opcodeU8, Opcode opcodeU16)
        {
            var v = unchecked((uint)location);
            var inst = v switch
            {
                var l when l >= 0 && l <= 0x00FF => (opcodeU8, new[] { new Operand(v) }),
                var l when l >= 0 && l <= 0xFFFF => (opcodeU16, new[] { new Operand(v) }),
                _ => throw new InvalidOperationException($"Local at '{location}' out of bounds")
            };

            Code.Emit(inst.Item1, inst.Item2);
        }

        private void EmitLocalAddr(int location) => EmitLocal(location, Opcode.LOCAL_U8, Opcode.LOCAL_U16);
        private void EmitLocalLoad(int location) => EmitLocal(location, Opcode.LOCAL_U8_LOAD, Opcode.LOCAL_U16_LOAD);
        private void EmitLocalStore(int location) => EmitLocal(location, Opcode.LOCAL_U8_STORE, Opcode.LOCAL_U16_STORE);
    }
}
