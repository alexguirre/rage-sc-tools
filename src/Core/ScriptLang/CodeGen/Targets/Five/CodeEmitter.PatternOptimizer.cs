namespace ScTools.ScriptLang.CodeGen.Targets.Five;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ScTools.ScriptAssembly;

public partial class CodeEmitter
{
    /// <summary>
    /// Optimizes instructions emitted by <see cref="CodeEmitter"/> using pattern matching.
    /// </summary>
    private class PatternOptimizer
    {
        private readonly IPattern[] patterns = new IPattern[]
        {
            new CombinedStoreOrLoadPattern(Opcode.LOCAL_U8, Opcode.LOCAL_U8_STORE, Opcode.LOCAL_U8_LOAD),
            new CombinedStoreOrLoadPattern(Opcode.LOCAL_U16, Opcode.LOCAL_U16_STORE, Opcode.LOCAL_U16_LOAD),

            new CombinedStoreOrLoadPattern(Opcode.STATIC_U8, Opcode.STATIC_U8_STORE, Opcode.STATIC_U8_LOAD),
            new CombinedStoreOrLoadPattern(Opcode.STATIC_U16, Opcode.STATIC_U16_STORE, Opcode.STATIC_U16_LOAD),

            new CombinedStoreOrLoadPattern(Opcode.GLOBAL_U16, Opcode.GLOBAL_U16_STORE, Opcode.GLOBAL_U16_LOAD),
            new CombinedStoreOrLoadPattern(Opcode.GLOBAL_U24, Opcode.GLOBAL_U24_STORE, Opcode.GLOBAL_U24_LOAD),

            new CombinedStoreOrLoadPattern(Opcode.IOFFSET_U8, Opcode.IOFFSET_U8_STORE, Opcode.IOFFSET_U8_LOAD),
            new CombinedStoreOrLoadPattern(Opcode.IOFFSET_S16, Opcode.IOFFSET_S16_STORE, Opcode.IOFFSET_S16_LOAD),

            new CombinedStoreOrLoadPattern(Opcode.ARRAY_U8, Opcode.ARRAY_U8_STORE, Opcode.ARRAY_U8_LOAD),
            new CombinedStoreOrLoadPattern(Opcode.ARRAY_U16, Opcode.ARRAY_U16_STORE, Opcode.ARRAY_U16_LOAD),

            new AddMulS16Pattern(),
            new AddMulU8Pattern(),

            new PushConstU8Pattern(),

            new IntCompareAndJZPattern(),
        };

        public void Optimize(CodeBuffer codeBuffer)
        {
            for (int i = 0; i < codeBuffer.NumberOfInstructions; i++)
            {
                var inst = codeBuffer.GetRef(i);
                if (codeBuffer.IsEmpty(inst))
                {
                    continue;
                }

                bool anyMatch = false;
                for (int patternIdx = 0; patternIdx < patterns.Length; patternIdx++)
                {
                    var pattern = patterns[patternIdx];
                    if (pattern.MatchAndOptimize(codeBuffer, inst))
                    {
                        anyMatch = true;
                        break;
                    }
                }

                if (anyMatch)
                {
                    // re-match all patterns on the instruction that was just optimized in case a new pattern appeared
                    i--;
                }
            }
        }

        private static InstructionReference? GetFirstNonEmptyInstruction(CodeBuffer codeBuffer, int start)
        {
            for (int i = start; i < codeBuffer.NumberOfInstructions; i++)
            {
                var inst = codeBuffer.GetRef(i);
                if (!codeBuffer.IsEmpty(inst))
                {
                    return inst;
                }
            }

            return null;
        }

        private interface IPattern
        {
            bool MatchAndOptimize(CodeBuffer codeBuffer, InstructionReference instruction);
        }

        private sealed class CombinedStoreOrLoadPattern : IPattern
        {
            public Opcode Target { get; }
            public Opcode StoreReplacement { get; }
            public Opcode LoadReplacement { get; }

            public CombinedStoreOrLoadPattern(Opcode target, Opcode storeReplacement, Opcode loadReplacement)
            {
                Target = target;
                StoreReplacement = storeReplacement;
                LoadReplacement = loadReplacement;
            }

            public bool MatchAndOptimize(CodeBuffer codeBuffer, InstructionReference instruction)
            {
                var first = GetFirstNonEmptyInstruction(codeBuffer, instruction.Index);
                if (first is null || codeBuffer.GetOpcode(first) != Target)
                {
                    return false;
                }

                var second = GetFirstNonEmptyInstruction(codeBuffer, first.Index + 1);
                if (second is null || codeBuffer.GetOpcode(second) is not (Opcode.STORE or Opcode.LOAD))
                {
                    return false;
                }

                var replacement = codeBuffer.GetOpcode(second) switch { Opcode.STORE => StoreReplacement, Opcode.LOAD => LoadReplacement, _ => throw new NotSupportedException() };

                var newBytes = codeBuffer.GetBytes(first);
                newBytes[0] = (byte)replacement;
                codeBuffer.Update(first, newBytes);
                codeBuffer.Remove(second);
                return true;
            }
        }

        /// <summary>
        /// Merges a PUSH_CONST_S16 instruction followed by IADD/IMUL to IADD_S16/IMUL_S16.
        /// </summary>
        public sealed class AddMulS16Pattern : IPattern
        {
            public bool MatchAndOptimize(CodeBuffer codeBuffer, InstructionReference instruction)
            {
                var first = GetFirstNonEmptyInstruction(codeBuffer, instruction.Index);
                if (first is null || codeBuffer.GetOpcode(first) is not Opcode.PUSH_CONST_S16)
                {
                    return false;
                }

                var second = GetFirstNonEmptyInstruction(codeBuffer, first.Index + 1);
                if (second is null || codeBuffer.GetOpcode(second) is not (Opcode.IADD or Opcode.IMUL))
                {
                    return false;
                }

                var replacement = codeBuffer.GetOpcode(second) switch { Opcode.IADD => Opcode.IADD_S16, Opcode.IMUL => Opcode.IMUL_S16, _ => throw new NotImplementedException() };

                var newBytes = codeBuffer.GetBytes(first);
                newBytes[0] = (byte)replacement;
                codeBuffer.Update(first, newBytes);
                codeBuffer.Remove(second);
                return true;
            }
        }

        /// <summary>
        /// Merges a PUSH_CONST_U8 instruction followed by IADD/IMUL to IADD_U8/IMUL_U8.
        /// </summary>
        public sealed class AddMulU8Pattern : IPattern
        {
            public bool MatchAndOptimize(CodeBuffer codeBuffer, InstructionReference instruction)
            {
                var first = GetFirstNonEmptyInstruction(codeBuffer, instruction.Index);
                if (first is null || codeBuffer.GetOpcode(first) is not (Opcode.PUSH_CONST_U8
                                                                        or Opcode.PUSH_CONST_U8_U8
                                                                        or Opcode.PUSH_CONST_U8_U8_U8
                                                                        or (>= Opcode.PUSH_CONST_0 and <= Opcode.PUSH_CONST_7)))
                {
                    return false;
                }

                var second = GetFirstNonEmptyInstruction(codeBuffer, first.Index + 1);
                if (second is null || codeBuffer.GetOpcode(second) is not (Opcode.IADD or Opcode.IMUL))
                {
                    return false;
                }

                var replacement = codeBuffer.GetOpcode(second) switch { Opcode.IADD => Opcode.IADD_U8, Opcode.IMUL => Opcode.IMUL_U8, _ => throw new NotImplementedException() };
                var newOpBytes = new List<byte>(capacity: 2) { (byte)replacement, 0 };

                codeBuffer.Remove(second);
                switch (codeBuffer.GetOpcode(first))
                {
                    case >= Opcode.PUSH_CONST_0 and <= Opcode.PUSH_CONST_7:
                        newOpBytes[1] = codeBuffer.GetOpcode(first) - Opcode.PUSH_CONST_0;
                        codeBuffer.Update(first, newOpBytes);
                        break;
                    case Opcode.PUSH_CONST_U8:
                        newOpBytes[1] = codeBuffer.GetByte(first, 1);
                        codeBuffer.Update(first, newOpBytes);
                        break;
                    case Opcode.PUSH_CONST_U8_U8:
                        newOpBytes[1] = codeBuffer.GetByte(first, 2);
                        var newPushU8Bytes = codeBuffer.GetBytes(first);
                        newPushU8Bytes[0] = (byte)Opcode.PUSH_CONST_U8;
                        newPushU8Bytes.RemoveAt(newPushU8Bytes.Count - 1);
                        codeBuffer.Update(first, newPushU8Bytes);
                        codeBuffer.InsertAfter(first, newOpBytes);
                        break;
                    case Opcode.PUSH_CONST_U8_U8_U8:
                        newOpBytes[1] = codeBuffer.GetByte(first, 3);
                        var newPushU8U8Bytes = codeBuffer.GetBytes(first);
                        newPushU8U8Bytes[0] = (byte)Opcode.PUSH_CONST_U8_U8;
                        newPushU8U8Bytes.RemoveAt(newPushU8U8Bytes.Count - 1);
                        codeBuffer.Update(first, newPushU8U8Bytes);
                        codeBuffer.InsertAfter(first, newOpBytes);
                        break;
                }

                return true;
            }
        }

        /// <summary>
        /// Merges sequential PUSH_CONST_U8 instructions to PUSH_CONST_U8_U8 and PUSH_CONST_U8_U8_U8.
        /// </summary>
        public sealed class PushConstU8Pattern : IPattern
        {
            public bool MatchAndOptimize(CodeBuffer codeBuffer, InstructionReference instruction)
            {
                var first = GetFirstNonEmptyInstruction(codeBuffer, instruction.Index);
                if (first is null || codeBuffer.GetOpcode(first) is not (Opcode.PUSH_CONST_U8 or Opcode.PUSH_CONST_U8_U8))
                {
                    return false;
                }

                var second = GetFirstNonEmptyInstruction(codeBuffer, first.Index + 1);
                if (second is null || codeBuffer.GetOpcode(second) is not Opcode.PUSH_CONST_U8)
                {
                    return false;
                }

                List<byte> newBytes = codeBuffer.GetBytes(first);
                switch (codeBuffer.GetOpcode(first))
                {
                    case Opcode.PUSH_CONST_U8:
                        newBytes[0] = (byte)Opcode.PUSH_CONST_U8_U8;
                        newBytes.Add(codeBuffer.GetByte(second, 1));
                        break;
                    case Opcode.PUSH_CONST_U8_U8:
                        newBytes[0] = (byte)Opcode.PUSH_CONST_U8_U8_U8;
                        newBytes.Add(codeBuffer.GetByte(second, 1));
                        break;
                }

                codeBuffer.Update(first, newBytes);
                codeBuffer.Remove(second);
                return true;
            }
        }

        /// <summary>
        /// Merges a IEQ/INE/IGT/IGE/ILT/ILE instruction followed by JZ to IEQ_JZ/INE_JZ/IGT_JZ/IGE_JZ/ILT_JZ/ILE_JZ.
        /// </summary>
        public sealed class IntCompareAndJZPattern : IPattern
        {
            public bool MatchAndOptimize(CodeBuffer codeBuffer, InstructionReference instruction)
            {
                var first = GetFirstNonEmptyInstruction(codeBuffer, instruction.Index);
                if (first is null || codeBuffer.GetOpcode(first) is not (>= Opcode.IEQ and <= Opcode.ILE))
                {
                    return false;
                }

                var second = GetFirstNonEmptyInstruction(codeBuffer, first.Index + 1);
                if (second is null || codeBuffer.GetOpcode(second) is not Opcode.JZ)
                {
                    return false;
                }

                var replacement = codeBuffer.GetOpcode(first) - Opcode.IEQ + Opcode.IEQ_JZ;

                var newBytes = codeBuffer.GetBytes(second);
                newBytes[0] = (byte)replacement;
                codeBuffer.Update(second, newBytes);
                codeBuffer.Remove(first);
                return true;
            }
        }
    }
}
