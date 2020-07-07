namespace ScTools.ScriptAssembly
{
    using ScTools.GameFiles;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text;
    using System.Threading.Tasks;

    public class Disassembler
    {
        public Script Script { get; }

        public Disassembler(Script sc)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
        }

        public Function[] Disassemble()
        {
            var funcs = ExploreByteCode(Script);
            PostProcess(Script, funcs);

            return funcs;
        }

        public static void Print(TextWriter w, Function[] functions)
        {
            static IEnumerable<string> ToString(Operand[] operands)
                => operands.Select(o => o.Type switch
                {
                    OperandType.U32 => o.U32.ToString(),
                    OperandType.U64 => o.U64.ToString(),
                    OperandType.F32 => o.F32.ToString("0.0#######"),
                    OperandType.Identifier => o.String,
                    OperandType.SwitchCase => $"{o.SwitchCase.Value}:{o.SwitchCase.Label}",
                    OperandType.String => $"\"{o.String.Escape()}\"",
                    _ => throw new InvalidOperationException()
                });

            foreach (Function f in functions)
            {
                w.WriteLine("FUNC NAKED {0} BEGIN", f.Name);
                foreach (Location loc in f.Code)
                {
                    if (loc.Label != null)
                    {
                        w.WriteLine("\t{0}:", loc.Label);
                    }

                    if (loc.HasInstruction)
                    {
                        w.Write("\t\t{0}", Instruction.Set[(byte)loc.Opcode].Mnemonic);
                        if (loc.Operands.Length > 0)
                        {
                            w.Write(' ');
                            w.Write(string.Join(" ", ToString(loc.Operands)));
                        }
                        w.WriteLine();
                    }
                }
                w.WriteLine("END");
                w.WriteLine();
            }
        }

        private static void PostProcess(Script sc, Function[] funcs)
        {
            for (int i = 1; i < funcs.Length; i++)
            {
                Function prevFunc = funcs[i - 1];
                Function currFunc = funcs[i];

                // Functions at page boundaries may not start with an ENTER instruction as ExploreByteCode assumes.
                // They may have NOPs and a J before the ENTER, here explore backwards from the start of the currFunc
                // to see if this is the case, if so, add this instruction to currFunc and remove them from prevFunc and just the StartIP and EndIP
                int k = prevFunc.Code.Count - 1;
                while (k >= 0 && prevFunc.Code[k].Opcode == Opcode.NOP)
                {
                    k--;
                }

                bool changeFuncBounds = false;
                
                Location prevLast = prevFunc.Code[k];
                if (prevLast.Opcode == Opcode.J && // is there a jump to the ENTER instruction?
                    sc.IP<short>(prevLast.IP + 1) == (currFunc.StartIP - (prevLast.IP + 3)))
                {
                    changeFuncBounds = true;
                }
                else if (k < prevFunc.Code.Count - 1 && (prevFunc.Code[k + 1].Opcode == Opcode.NOP))
                {
                    k++;
                    prevLast = prevFunc.Code[k];
                    changeFuncBounds = true;
                }

                if (changeFuncBounds)
                {
                    // add the instructions to the current functions
                    currFunc.Code.InsertRange(0, prevFunc.Code.Skip(k));
                    currFunc.StartIP = prevLast.IP;

                    // and remove them from the previous
                    prevFunc.Code.RemoveRange(k, prevFunc.Code.Count - k);
                    prevFunc.EndIP = prevLast.IP;
                }
            }

            var operandsDecoder = new OperandsDecoder(sc, funcs);
            foreach (var f in funcs)
            {
                ScanLabels(sc, f);
            
                for (int i = 0; i < f.Code.Count; i++)
                {
                    Location loc = f.Code[i];
                    if (loc.HasInstruction)
                    {
                        operandsDecoder.BeginInstruction(f, loc);
                        Instruction.Set[(byte)loc.Opcode].Decode(operandsDecoder);
                        loc.Operands = operandsDecoder.EndInstruction();
                        f.Code[i] = loc;
                    }
                }
            }
        }

        public class Function
        {
            public string Name { get; set; }
            public uint StartIP { get; set; }
            public uint EndIP { get; set; }
            public List<Location> Code { get; set; }
        }

        public struct Location
        {
            public uint IP { get; set; }
            public string Label { get; set; }
            public Opcode Opcode { get; set; }
            public Operand[] Operands { get; set; }
            public bool HasInstruction { get; set; }

            public Location(uint ip, Opcode opcode)
            {
                IP = ip;
                Label = null;
                Opcode = opcode;
                Operands = Array.Empty<Operand>();
                HasInstruction = true;
            }

            public Location(uint ip, string label)
            {
                IP = ip;
                Label = label;
                Opcode = 0;
                Operands = null;
                HasInstruction = false;
            }
        }

        private static Function[] ExploreByteCode(Script sc)
        {
            List<Function> functions = new List<Function>(1024);
            List<Location> codeBuffer = new List<Location>(4096);
            Function currentFunction = null;

            void BeginFunction(uint ip)
            {
                Debug.Assert(currentFunction == null);

                currentFunction = new Function();
                currentFunction.StartIP = ip;
                functions.Add(currentFunction);

                Opcode inst = (Opcode)sc.IP(ip);
                if (inst == Opcode.ENTER)
                {
                    // if we are at an ENTER instruction, check if it has a name
                    byte nameLen = sc.IP(ip + 4);

                    if (nameLen > 0)
                    {
                        StringBuilder nameSB = new StringBuilder(nameLen);
                        for (uint i = 0; i < nameLen - 1; i++)
                        {
                            nameSB.Append((char)sc.IP(ip + 5 + i));
                        }
                        currentFunction.Name = nameSB.ToString();
                    }
                }

                currentFunction.Name ??= currentFunction.StartIP switch
                {
                    0 => "main",
                    _ => currentFunction.StartIP.ToString("func_000000")
                };

                codeBuffer.Clear();
            }

            void EndFunction(uint ip)
            {
                Debug.Assert(currentFunction != null);

                currentFunction.Code = codeBuffer.ToList();
                currentFunction.EndIP = ip;
                currentFunction = null;
            }

            void AddInstructionAt(uint ip)
            {
                byte opcode = ip < sc.CodeLength ? sc.IP(ip) : (byte)0;

                if (opcode >= Instruction.NumberOfInstructions)
                {
                    throw new InvalidOperationException($"Unknown opcode 0x{opcode:X2}");
                }

                if (opcode == (byte)Opcode.ENTER && currentFunction != null)
                {
                    EndFunction(ip);
                }

                if (currentFunction == null)
                {
                    BeginFunction(ip);
                }

                codeBuffer.Add(new Location(ip, (Opcode)opcode));
            }


            for (uint ip = 0; ip < sc.CodeLength; ip += Instruction.SizeOf(sc, ip))
            {
                AddInstructionAt(ip);
            }

            if (currentFunction != null)
            {
                EndFunction(sc.CodeLength);
            }

            return functions.ToArray();
        }

        private static void ScanLabels(Script sc, Function func)
        {
            static string LabelToString(uint labelIP) => labelIP.ToString("lbl_000000");

            static void AddLabel(Function func, uint labelIP)
            {
                if (labelIP == func.EndIP)
                {
                    func.Code.Add(new Location(labelIP, LabelToString(labelIP)));
                }
                else
                {
                    for (int i = 0; i < func.Code.Count; i++)
                    {
                        Location loc = func.Code[i];
                        if (labelIP == loc.IP)
                        {
                            func.Code[i] = new Location(loc.IP, loc.Opcode) { Label = LabelToString(labelIP) };
                            return;
                        }
                        else if (loc.IP > labelIP)
                        {
                            throw new InvalidOperationException($"Label at IP {labelIP:000000} points to the middle of an instruction");
                        }
                    }

                    throw new InvalidOperationException(
                        $"Label at IP {labelIP:000000} is outside the function '{func.Name}' bounds (startIP: {func.StartIP:000000}, endIP: {func.EndIP:000000})");
                }
            }

            foreach (Location loc in func.Code.ToArray()) // TODO: AddLabel modifies the Code list so we can't iterate through it, find some cleaner solution than copying the whole list
            {
                Opcode inst = loc.Opcode;

                if (inst == Opcode.SWITCH)
                {
                    byte count = sc.IP(loc.IP + 1);

                    if (count > 0)
                    {
                        uint currIP = loc.IP + 8;
                        for (uint i = 0; i < count; i++, currIP += 6)
                        {
                            short offset = sc.IP<short>(currIP - 2);
                            uint targetIP = (uint)(currIP + offset);

                            AddLabel(func, targetIP);
                        }
                    }
                }
                else if (inst.IsJump())
                {
                    AddLabel(func, (uint)(sc.IP<short>(loc.IP + 1) + loc.IP + 3));
                }
            }
        }

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

    /// <summary>
    /// Defines the interface used for decoding <see cref="Instruction"/>s.
    /// </summary>
    public interface IInstructionDecoder
    {
        public uint IP { get; }
        public byte Get(uint offset);
        public T Get<T>(uint offset) where T : unmanaged;

        public void U8(byte v);
        public void U16(ushort v);
        public void U24(uint v);
        public void U32(uint v);
        public void S16(short v);
        public void F32(float v);
        public void LabelTarget(uint ip);
        public void FunctionTarget(uint ip);
        public void SwitchCase(uint value, uint ip);
    }
}
