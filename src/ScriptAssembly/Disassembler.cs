namespace ScTools.ScriptAssembly
{
    using ScTools.GameFiles;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
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
            PostProcess(funcs);

            return funcs;
        }

        private void PostProcess(Function[] funcs)
        {
            for (int i = 1; i < funcs.Length; i++)
            {
                Function prevFunc = funcs[i - 1];
                Function currFunc = funcs[i];

                // Functions at page boundaries may not start with an ENTER instruction as ExploreByteCode assumes.
                // They may have NOPs and a J before the ENTER, here explore backwards from the start of the currFunc
                // to see if this is the case, if so, add this instruction to currFunc and remove them from prevFunc and just the StartIP and EndIP
                int k = prevFunc.Code.Count - 1;
                while (k >= 0 && prevFunc.Code[k].Opcode == Instruction.NOP.Opcode)
                {
                    k--;
                }

                bool changeFuncBounds = false;
                
                Location prevLast = prevFunc.Code[k];
                if (prevLast.Opcode == Instruction.J.Opcode && // is there a jump to the ENTER instruction?
                    Script.IP<short>(prevLast.IP + 1) == (currFunc.StartIP - (prevLast.IP + 3)))
                {
                    changeFuncBounds = true;
                }
                else if (k < prevFunc.Code.Count - 1 && (prevFunc.Code[k + 1].Opcode == Instruction.NOP.Opcode))
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

            foreach (var f in funcs)
            {
                ScanLabels(Script, f);
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
            public byte Opcode { get; set; }
            public bool HasInstruction { get; set; }

            public Location(uint ip, byte opcode)
            {
                IP = ip;
                Label = null;
                Opcode = opcode;
                HasInstruction = true;
            }

            public Location(uint ip, string label)
            {
                IP = ip;
                Label = label;
                Opcode = 0;
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

                byte inst = sc.IP(ip);
                if (inst == Instruction.ENTER.Opcode)
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

                if (opcode == Instruction.ENTER.Opcode && currentFunction != null)
                {
                    EndFunction(ip);
                }

                if (currentFunction == null)
                {
                    BeginFunction(ip);
                }

                codeBuffer.Add(new Location(ip, opcode));
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
                byte inst = loc.Opcode;

                if (inst == Instruction.SWITCH.Opcode) // SWITCH
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
                else if (Instruction.Set[inst].IsJump)
                {
                    AddLabel(func, (uint)(sc.IP<short>(loc.IP + 1) + loc.IP + 3));
                }
            }
        }
    }
}
