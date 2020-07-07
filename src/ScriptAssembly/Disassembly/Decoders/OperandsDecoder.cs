namespace ScTools.ScriptAssembly.Disassembly
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using ScTools.GameFiles;

    public class OperandsDecoder : IInstructionDecoder
    {
        private readonly List<Operand> operands = new List<Operand>(16);
        private readonly Script script;
        private readonly Function[] functions;
        private Function currentFunction;
        private Location currentLocation;

        public OperandsDecoder(Script script, Function[] functions)
        {
            this.script = script ?? throw new ArgumentNullException(nameof(script));
            this.functions = functions ?? throw new ArgumentNullException(nameof(functions));
        }

        public void BeginInstruction(Function func, Location loc)
        {
            Debug.Assert(func != null);
            Debug.Assert(loc.HasInstruction);

            currentFunction = func;
            currentLocation = loc;

            operands.Clear();
        }

        public Operand[] EndInstruction()
        {
            currentFunction = null;
            currentLocation = default;
            return operands.ToArray();
        }

        public uint IP => currentLocation.IP;
        public byte Get(uint offset) => script.IP(currentLocation.IP + offset);
        public T Get<T>(uint offset) where T : unmanaged => script.IP<T>(currentLocation.IP + offset);

        public void U8(byte v) => operands.Add(new Operand(v));
        public void U16(ushort v) => operands.Add(new Operand(v));
        public void U24(uint v) => operands.Add(new Operand(v));
        public void U32(uint v) => operands.Add(new Operand(v));
        public void S16(short v) => operands.Add(new Operand(unchecked((ushort)v)));
        public void F32(float v) => operands.Add(new Operand(v));

        public void LabelTarget(uint ip)
        {
            string label = GetLabel(ip);
            Debug.Assert(label != null);

            operands.Add(new Operand(label, OperandType.Identifier));
        }

        public void FunctionTarget(uint ip)
        {
            string name = null;
            for (int i = 0; i < functions.Length; i++)
            {
                if (functions[i].StartIP == ip)
                {
                    name = functions[i].Name;
                    break;
                }
            }

            Debug.Assert(name != null);

            operands.Add(new Operand(name, OperandType.Identifier));
        }

        public void SwitchCase(uint value, uint ip)
        {
            string label = GetLabel(ip);
            Debug.Assert(label != null);

            operands.Add(new Operand((value, label)));
        }

        private string GetLabel(uint ip)
        {
            for (int i = 0; i < currentFunction.Code.Count; i++)
            {
                if (currentFunction.Code[i].IP == ip)
                {
                    return currentFunction.Code[i].Label;
                }
            }

            return null;
        }
    }
}
