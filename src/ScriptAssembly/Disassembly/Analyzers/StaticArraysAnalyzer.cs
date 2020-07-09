namespace ScTools.ScriptAssembly.Disassembly.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using ScTools.ScriptAssembly.Types;

    /// <summary>
    /// Analyzes the disassembly to find array types in static variables and script arguments.
    /// </summary>
    public class StaticArraysAnalyzer
    {
        public DisassembledScript Disassembly { get; }
        private Dictionary<uint, uint> changes = new Dictionary<uint, uint>(16); // key = StaticOffset, value = ArrayItemSize

        public StaticArraysAnalyzer(DisassembledScript disassembly)
        {
            Disassembly = disassembly ?? throw new ArgumentNullException(nameof(disassembly));
        }

        public void Analyze(Function function)
        {
            for (int i = 0; i < function.Code.Count; i++)
            {
                Location loc = function.Code[i];

                if (loc.HasInstruction && IsArray(loc.Opcode))
                {
                    // go back until we find the STATIC instruction
                    int k = i;
                    Location prevLoc = default;
                    bool found = false;
                    while (--k >= 0)
                    {
                        prevLoc = function.Code[k];

                        if (!prevLoc.HasInstruction || prevLoc.Opcode == Opcode.NOP)
                        {
                            continue;
                        }

                        // FIXME: here J no longer contains an S16 offset but an Identifer with label name
                        if (prevLoc.Opcode == Opcode.J && // is there a jump to the ARRAY instruction
                            prevLoc.Operands[0].AsS16() == (loc.IP - (prevLoc.IP + 3)))
                        {
                            continue;
                        }

                        if (IsStatic(prevLoc.Opcode))
                        {
                            found = true;
                        }
                        else
                        {
                            found = false;
                        }
                        break;
                    }

                    if (found)
                    {
                        uint staticOffset = GetStaticOffset(prevLoc);
                        uint arrayItemSize = GetArrayItemSize(loc);

                        if (!changes.TryAdd(staticOffset, arrayItemSize))
                        {
                            Debug.Assert(changes[staticOffset] == arrayItemSize);
                        }
                    }
                }
            }
        }

        public void FinalizeAnalysis()
        {
            List<(uint StaticOffset, uint ArraySize, TypeBase ItemType)> l = new List<(uint, uint, TypeBase)>();

            foreach (var (staticOffset, arrayItemSize) in changes)
            {
                (uint, uint, TypeBase) v = (staticOffset, 0, null);

                if (arrayItemSize > 1)
                {
                    v.Item3 = Disassembly.Types.RegisterStruct($"Struct{staticOffset}", Enumerable.Range(0, (int)arrayItemSize).Select(i => new StructField($"f_{i}", AutoType.Instance)));
                }
                else
                {
                    v.Item3 = AutoType.Instance;
                }

                int i = (int)staticOffset;
                v.Item2 = (uint)(i < Disassembly.Statics.Count ? Disassembly.Statics[i] : Disassembly.Args[i - Disassembly.Statics.Count]).InitialValue;
                Debug.Assert(v.Item2 > 0);

                l.Add(v);
            }

            l.Sort((a, b) => a.StaticOffset.CompareTo(b.StaticOffset));

            foreach (var (staticOffset, arraySize, type) in l)
            {
                ArrayType arr = Disassembly.Types.FindOrRegisterArray(type, arraySize);

                bool isArg = staticOffset >= (Disassembly.Args.FirstOrDefault()?.Offset ?? uint.MaxValue);

                if (isArg)
                {
                    int i = Disassembly.Args.FindIndex(a => a.Offset == staticOffset);
                    Debug.Assert(i != -1);
                    Argument old = Disassembly.Args[i];

                    Disassembly.Args.RemoveRange(i, (int)arr.SizeOf);
                    Disassembly.Args.Insert(i, new Argument { Name = old.Name, Offset = staticOffset, Type = arr });
                }
                else
                {
                    int i = Disassembly.Statics.FindIndex(a => a.Offset == staticOffset);
                    Debug.Assert(i != -1);
                    Static old = Disassembly.Statics[i];

                    Disassembly.Statics.RemoveRange(i, (int)arr.SizeOf);
                    Disassembly.Statics.Insert(i, new Static { Name = old.Name, Offset = staticOffset, Type = arr });
                }
            }
        }

        private Static GetStatic(uint offset) => ((int)offset < Disassembly.Statics.Count ? Disassembly.Statics[(int)offset] : Disassembly.Args[(int)offset - Disassembly.Statics.Count]);

        private static bool IsArray(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.ARRAY_U8:
                case Opcode.ARRAY_U8_LOAD:
                case Opcode.ARRAY_U8_STORE:
                case Opcode.ARRAY_U16:
                case Opcode.ARRAY_U16_LOAD:
                case Opcode.ARRAY_U16_STORE:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsStatic(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.STATIC_U8:
                case Opcode.STATIC_U16:
                    return true;
                default:
                    return false;
            }
        }

        private static uint GetArrayItemSize(Location loc)
        {
            Debug.Assert(IsArray(loc.Opcode));

            return loc.Operands[0].U32;
        }

        private static uint GetStaticOffset(Location loc)
        {
            Debug.Assert(IsStatic(loc.Opcode));

            return loc.Operands[0].U32;
        }
    }
}
