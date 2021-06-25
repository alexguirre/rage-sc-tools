namespace ScTools.ScriptLang.CodeGen
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;

    using EmittedInstruction = CodeGenerator.EmittedInstruction;

    /// <summary>
    /// Optimizes instructions emitted by <see cref="CodeGenerator"/> using pattern matching.
    /// </summary>
    public class PatternOptimizer
    {
        private readonly IPattern[] patterns = new IPattern[]
        {
            new Pattern(new[]{ Opcode.LOCAL_U8, Opcode.STORE }, Opcode.LOCAL_U8_STORE),
            new Pattern(new[]{ Opcode.LOCAL_U8, Opcode.LOAD }, Opcode.LOCAL_U8_LOAD),
            new Pattern(new[]{ Opcode.LOCAL_U16, Opcode.STORE }, Opcode.LOCAL_U16_STORE),
            new Pattern(new[]{ Opcode.LOCAL_U16, Opcode.LOAD }, Opcode.LOCAL_U16_LOAD),

            new Pattern(new[]{ Opcode.STATIC_U8, Opcode.STORE }, Opcode.STATIC_U8_STORE),
            new Pattern(new[]{ Opcode.STATIC_U8, Opcode.LOAD }, Opcode.STATIC_U8_LOAD),
            new Pattern(new[]{ Opcode.STATIC_U16, Opcode.STORE }, Opcode.STATIC_U16_STORE),
            new Pattern(new[]{ Opcode.STATIC_U16, Opcode.LOAD }, Opcode.STATIC_U16_LOAD),

            new Pattern(new[]{ Opcode.GLOBAL_U16, Opcode.STORE }, Opcode.GLOBAL_U16_STORE),
            new Pattern(new[]{ Opcode.GLOBAL_U16, Opcode.LOAD }, Opcode.GLOBAL_U16_LOAD),
            new Pattern(new[]{ Opcode.GLOBAL_U24, Opcode.STORE }, Opcode.GLOBAL_U24_STORE),
            new Pattern(new[]{ Opcode.GLOBAL_U24, Opcode.LOAD }, Opcode.GLOBAL_U24_LOAD),

            new Pattern(new[]{ Opcode.IOFFSET_U8, Opcode.STORE }, Opcode.IOFFSET_U8_STORE),
            new Pattern(new[]{ Opcode.IOFFSET_U8, Opcode.LOAD }, Opcode.IOFFSET_U8_LOAD),
            new Pattern(new[]{ Opcode.IOFFSET_S16, Opcode.STORE }, Opcode.IOFFSET_S16_STORE),
            new Pattern(new[]{ Opcode.IOFFSET_S16, Opcode.LOAD }, Opcode.IOFFSET_S16_LOAD),

            new Pattern(new[]{ Opcode.ARRAY_U8, Opcode.STORE }, Opcode.ARRAY_U8_STORE),
            new Pattern(new[]{ Opcode.ARRAY_U8, Opcode.LOAD }, Opcode.ARRAY_U8_LOAD),
            new Pattern(new[]{ Opcode.ARRAY_U16, Opcode.STORE }, Opcode.ARRAY_U16_STORE),
            new Pattern(new[]{ Opcode.ARRAY_U16, Opcode.LOAD }, Opcode.ARRAY_U16_LOAD),

            new Offset0StorePattern(Opcode.LOCAL_U8, Opcode.LOCAL_U8_STORE),
            new Offset0StorePattern(Opcode.LOCAL_U16, Opcode.LOCAL_U16_STORE),
            new Offset0StorePattern(Opcode.STATIC_U8, Opcode.STATIC_U8_STORE),
            new Offset0StorePattern(Opcode.STATIC_U16, Opcode.STATIC_U16_STORE),
            new Offset0StorePattern(Opcode.GLOBAL_U16, Opcode.GLOBAL_U16_STORE),
            new Offset0StorePattern(Opcode.GLOBAL_U24, Opcode.GLOBAL_U24_STORE),
            new Offset0StorePattern(Opcode.ARRAY_U8, Opcode.ARRAY_U8_STORE),
            new Offset0StorePattern(Opcode.ARRAY_U16, Opcode.ARRAY_U16_STORE),

            new Offset0LoadPattern(Opcode.LOCAL_U8, Opcode.LOCAL_U8_LOAD),
            new Offset0LoadPattern(Opcode.LOCAL_U16, Opcode.LOCAL_U16_LOAD),
            new Offset0LoadPattern(Opcode.STATIC_U8, Opcode.STATIC_U8_LOAD),
            new Offset0LoadPattern(Opcode.STATIC_U16, Opcode.STATIC_U16_LOAD),
            new Offset0LoadPattern(Opcode.GLOBAL_U16, Opcode.GLOBAL_U16_LOAD),
            new Offset0LoadPattern(Opcode.GLOBAL_U24, Opcode.GLOBAL_U24_LOAD),
            new Offset0LoadPattern(Opcode.ARRAY_U8, Opcode.ARRAY_U8_LOAD),
            new Offset0LoadPattern(Opcode.ARRAY_U16, Opcode.ARRAY_U16_LOAD),

            new Offset0AddressPattern(Opcode.LOCAL_U8),
            new Offset0AddressPattern(Opcode.LOCAL_U16),
            new Offset0AddressPattern(Opcode.STATIC_U8),
            new Offset0AddressPattern(Opcode.STATIC_U16),
            new Offset0AddressPattern(Opcode.GLOBAL_U16),
            new Offset0AddressPattern(Opcode.GLOBAL_U24),
            new Offset0AddressPattern(Opcode.ARRAY_U8),
            new Offset0AddressPattern(Opcode.ARRAY_U16),
        };

        public IEnumerable<EmittedInstruction> Optimize(List<EmittedInstruction> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Label is not null)
                {
                    Debug.Assert(instructions[i].Instruction is null, "Optimizer assumes that EmittedInstruction cannot have label and instruction at the same time");
                    yield return instructions[i];
                    continue;
                }

                bool anyMatch = false;
                for (int patternIdx = 0; patternIdx < patterns.Length; patternIdx++)
                {
                    var pattern = patterns[patternIdx];
                    if (pattern.Match(instructions, i, out var replacementInst, out var numInstToReplace))
                    {
                        yield return replacementInst;
                        i += numInstToReplace - 1;
                        anyMatch = true;
                        break;
                    }
                }

                if (!anyMatch)
                {
                    yield return instructions[i];
                }
            }
        }

        public interface IPattern
        {
            bool Match(List<EmittedInstruction> instructions, int index, out EmittedInstruction replacementInstruction, out int numInstructionToReplace);
        }

        public sealed class Pattern : IPattern
        {
            public Opcode[] Target { get; }
            public Opcode Replacement { get; }

            public Pattern(Opcode[] target, Opcode replacement)
            {
                Target = target;
                Replacement = replacement;
            }

            public bool Match(List<EmittedInstruction> instructions, int index, out EmittedInstruction replacementInstruction, out int numInstructionToReplace)
            {
                var target = Target;
                if (index >= (instructions.Count - target.Length + 1))
                {
                    numInstructionToReplace = 0;
                    replacementInstruction = default;
                    return false;
                }

                for (int offset = 0; offset < target.Length; offset++)
                {
                    var inst = instructions[index + offset].Instruction;
                    if (inst.HasValue && inst.Value.Opcode != target[offset])
                    {
                        numInstructionToReplace = 0;
                        replacementInstruction = default;
                        return false;
                    }
                }

                numInstructionToReplace = Target.Length;
                replacementInstruction = new() { Instruction = (Replacement, instructions[index].Instruction!.Value.Operands) };
                return true;
            }
        }

        public abstract class Offset0Pattern : IPattern
        {
            public Opcode Target { get; }
            public Opcode LoadOrStore { get; }
            public Opcode Replacement { get; }

            public Offset0Pattern(Opcode target, Opcode loadOrStore, Opcode replacement)
            {
                Target = target;
                LoadOrStore = loadOrStore;
                Replacement = replacement;
            }

            public bool Match(List<EmittedInstruction> instructions, int index, out EmittedInstruction replacementInstruction, out int numInstructionToReplace)
            {
                const int NumInstructions = 3; // Target -> IOFFSET_U8 0 -> LOAD/STORE

                if (index >= (instructions.Count - NumInstructions + 1))
                {
                    numInstructionToReplace = 0;
                    replacementInstruction = default;
                    return false;
                }

                var inst0 = instructions[index + 0].Instruction;
                var inst1 = instructions[index + 1].Instruction;
                var inst2 = instructions[index + 2].Instruction;
                if (inst0.HasValue && inst0.Value.Opcode == Target &&
                    inst1 is (Opcode.IOFFSET_U8, _) && System.Convert.ToInt32(inst1.Value.Operands[0]) == 0 &&
                    inst2.HasValue && inst2.Value.Opcode == LoadOrStore)
                {
                    numInstructionToReplace = NumInstructions;
                    replacementInstruction = new() { Instruction = (Replacement, inst0.Value.Operands) };
                    return true;
                }

                numInstructionToReplace = 0;
                replacementInstruction = default;
                return false;
            }
        }

        public sealed class Offset0StorePattern : Offset0Pattern
        {
            public Offset0StorePattern(Opcode target, Opcode replacement) : base(target, Opcode.STORE, replacement) { }
        }

        public sealed class Offset0LoadPattern : Offset0Pattern
        {
            public Offset0LoadPattern(Opcode target, Opcode replacement) : base(target, Opcode.LOAD, replacement) { }
        }

        public sealed class Offset0AddressPattern : IPattern
        {
            public Opcode Target { get; }

            public Offset0AddressPattern(Opcode target)
            {
                Target = target;
            }

            public bool Match(List<EmittedInstruction> instructions, int index, out EmittedInstruction replacementInstruction, out int numInstructionToReplace)
            {
                const int NumInstructions = 2; // Target -> IOFFSET_U8 0

                if (index >= (instructions.Count - NumInstructions + 1))
                {
                    numInstructionToReplace = 0;
                    replacementInstruction = default;
                    return false;
                }

                var inst0 = instructions[index + 0].Instruction;
                var inst1 = instructions[index + 1].Instruction;
                if (inst0.HasValue && inst0.Value.Opcode == Target &&
                    inst1 is (Opcode.IOFFSET_U8, _) && System.Convert.ToInt32(inst1.Value.Operands[0]) == 0)
                {
                    numInstructionToReplace = NumInstructions;
                    replacementInstruction = new() { Instruction = inst0 };
                    return true;
                }

                numInstructionToReplace = 0;
                replacementInstruction = default;
                return false;
            }
        }
    }
}
