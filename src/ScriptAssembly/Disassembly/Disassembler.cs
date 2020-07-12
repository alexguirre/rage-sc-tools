namespace ScTools.ScriptAssembly.Disassembly
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Diagnostics;
    using System.Collections.Generic;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Disassembly.Analysis;
    using ScTools.ScriptAssembly.Types;

    public class DisassembledScript
    {
        public Script Script { get; set; }
        public TypeRegistry Types { get; set; }
        public List<Function> Functions { get; set; }
        public List<Static> Statics { get; set; }
        public List<StaticArgument> Args { get; set; }
    }

    public static class Disassembler
    {
        public static DisassembledScript Disassemble(Script sc)
        {
            sc = sc ?? throw new ArgumentNullException(nameof(sc));

            var funcs = ExploreByteCode(sc);
            PostProcess(sc, funcs);

            var (statics, args) = GetStatics(sc);

            var disassembled = new DisassembledScript { Script = sc, Types = new TypeRegistry(), Functions = funcs, Statics = statics, Args = args };

            PushStringAnalyzer analyzer = new PushStringAnalyzer(sc);
            foreach (Function f in disassembled.Functions)
            {
                analyzer.Analyze(f);
            }

            StaticArraysAnalyzer analyzer2 = new StaticArraysAnalyzer(disassembled);
            foreach (Function f in disassembled.Functions)
            {
                analyzer2.Analyze(f);
            }
            analyzer2.FinalizeAnalysis();

            StaticsAnalyzer analyzer3 = new StaticsAnalyzer(disassembled);
            foreach (Function f in disassembled.Functions)
            {
                analyzer3.Analyze(f);
            }

            return disassembled;
        }

        public static void Print(TextWriter w, Script sc, DisassembledScript disassembly)
        {
            w.WriteLine("$NAME {0}", sc.Name);
            w.WriteLine();

            foreach (var s in disassembly.Types.Structs)
            {
                w.WriteLine(Printer.PrintStruct(s));
                w.WriteLine();
            }

            //w.WriteLine("$ARGS_COUNT {0}", sc.ArgsCount);
            if (disassembly.Args.Count > 0)
            {
                w.WriteLine(Printer.PrintArguments(disassembly.Args));
                w.WriteLine();
            }

            //w.WriteLine("$STATICS_COUNT {0}", sc.StaticsCount);
            //for (int i = 0; i < sc.StaticsCount; i++)
            //{
            //    ScriptValue v = sc.Statics[i];
            //    if (v.AsUInt64 != 0)
            //    {
            //        w.WriteLine("$STATIC_INT_INIT {0} {1}", i, v.AsInt32);

            //        Debug.Assert(v.AsUInt32 == v.AsUInt64, "uint64 found");
            //    }
            //}
            //w.WriteLine();
            if (disassembly.Statics.Count > 0)
            {
                w.WriteLine(Printer.PrintStatics(disassembly.Statics));
                w.WriteLine();
            }

            if (sc.Hash != 0)
            {
                w.WriteLine("$HASH 0x{0:X8}", sc.Hash);
            }
            if (sc.GlobalsLengthAndBlock != 0 && sc.GlobalsPages != null)
            {
                w.WriteLine("$GLOBALS {0} {1}", sc.GlobalsBlock, sc.GlobalsLength);

                uint pageIndex = 0;
                foreach (var page in sc.GlobalsPages)
                {
                    uint i = 0;
                    foreach (ScriptValue g in page.Data)
                    {
                        uint globalId = (sc.GlobalsBlock << 18) | (pageIndex << 14) | i;

                        if (g.AsUInt64 != 0)
                        {
                            w.WriteLine("$GLOBAL_INT_INIT {0} {1}", globalId, g.AsInt32);

                            Debug.Assert(g.AsUInt32 == g.AsUInt64, "uint64 found");
                        }

                        i++;
                    }
                    pageIndex++;
                }
            }
            w.WriteLine();

            for (int i = 0; i < sc.NativesCount; i++)
            {
                ulong hash = sc.NativeHash(i);
                w.WriteLine("$NATIVE_DEF 0x{0:X16}", hash);
            }
            w.WriteLine();

            // disabled while we use PushStringAnalyzer
            //foreach (uint id in sc.StringIds())
            //{
            //    w.WriteLine("$STRING \"{0}\" ; offset: {1}", sc.String(id).Escape(), id);
            //}
            //w.WriteLine();

            foreach (Function f in disassembly.Functions)
            {
                w.WriteLine(Printer.PrintFunction(f));
                w.WriteLine();
            }
        }

        private static (List<Static>, List<StaticArgument>) GetStatics(Script sc)
        {
            uint argsCount = sc.ArgsCount;
            uint staticsCount = sc.StaticsCount - argsCount;

            var statics = new List<Static>((int)staticsCount);
            var args = new List<StaticArgument>((int)argsCount);

            for (uint i = 0; i< staticsCount; i++)
            {
                statics.Add(new Static { Name = $"static_{i}", Offset = i, Type = AutoType.Instance, InitialValue = sc.Statics[i].AsUInt64 });
            }

            for (uint i = 0; i < argsCount; i++)
            {
                uint offset = staticsCount + i;
                args.Add(new StaticArgument { Name = $"arg_{i}", Offset = offset, Type = AutoType.Instance, InitialValue = sc.Statics[offset].AsUInt64 });
            }

            return (statics, args);
        }

        private static void PostProcess(Script sc, IList<Function> funcs)
        {
            for (int i = 1; i < funcs.Count; i++)
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
                        loc.Opcode.Instruction().Decode(operandsDecoder);
                        loc.Operands = operandsDecoder.EndInstruction();
                        f.Code[i] = loc;
                    }
                }
            }
        }

        private static List<Function> ExploreByteCode(Script sc)
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

            functions.TrimExcess();
            return functions;
        }

        private static void ScanLabels(Script sc, Function func)
        {
            static string LabelToString(uint labelIP) => labelIP.ToString("lbl_000000");

            static void AddLabel(Function func, uint labelIP)
            {
                if (labelIP == func.EndIP)
                {
                    Location last = func.Code[^1];
                    if (last.IP != func.EndIP)
                    {
                        last = new Location(labelIP, LabelToString(labelIP));
                        func.Code.Add(last);
                        func.Labels.Add(last.Label, last.IP);
                    }
                    else if (last.Label == null)
                    {
                        last.Label = LabelToString(labelIP);
                        func.Code[^1] = last;
                        func.Labels.Add(last.Label, last.IP);
                    }
                }
                else
                {
                    for (int i = 0; i < func.Code.Count; i++)
                    {
                        Location loc = func.Code[i];
                        if (labelIP == loc.IP)
                        {
                            if (loc.Label == null)
                            {
                                loc.Label = LabelToString(labelIP);
                                func.Code[i] = loc;
                                func.Labels.Add(loc.Label, loc.IP);
                            }
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

            func.Labels = new Dictionary<string, uint>();

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
    }
}
