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
        private readonly Pattern[] patterns = new Pattern[]
        {
            new(new[]{ Opcode.LOCAL_U8, Opcode.STORE }, Opcode.LOCAL_U8_STORE),
            new(new[]{ Opcode.LOCAL_U8, Opcode.LOAD }, Opcode.LOCAL_U8_LOAD),
            new(new[]{ Opcode.LOCAL_U16, Opcode.STORE }, Opcode.LOCAL_U16_STORE),
            new(new[]{ Opcode.LOCAL_U16, Opcode.LOAD }, Opcode.LOCAL_U16_LOAD),

            new(new[]{ Opcode.STATIC_U8, Opcode.STORE }, Opcode.STATIC_U8_STORE),
            new(new[]{ Opcode.STATIC_U8, Opcode.LOAD }, Opcode.STATIC_U8_LOAD),
            new(new[]{ Opcode.STATIC_U16, Opcode.STORE }, Opcode.STATIC_U16_STORE),
            new(new[]{ Opcode.STATIC_U16, Opcode.LOAD }, Opcode.STATIC_U16_LOAD),

            new(new[]{ Opcode.GLOBAL_U16, Opcode.STORE }, Opcode.GLOBAL_U16_STORE),
            new(new[]{ Opcode.GLOBAL_U16, Opcode.LOAD }, Opcode.GLOBAL_U16_LOAD),
            new(new[]{ Opcode.GLOBAL_U24, Opcode.STORE }, Opcode.GLOBAL_U24_STORE),
            new(new[]{ Opcode.GLOBAL_U24, Opcode.LOAD }, Opcode.GLOBAL_U24_LOAD),
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
                    if (pattern.Match(instructions, i, out var replacementInst))
                    {
                        yield return replacementInst;
                        i += pattern.Target.Length - 1;
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

        public class Pattern
        {
            public Opcode[] Target { get; }
            public Opcode Replacement { get; }

            public Pattern(Opcode[] target, Opcode replacement)
            {
                Target = target;
                Replacement = replacement;
            }

            public bool Match(List<EmittedInstruction> instructions, int index, out EmittedInstruction replacementInstruction)
            {
                var target = Target;
                if (index >= (instructions.Count - target.Length + 1))
                {
                    replacementInstruction = default;
                    return false;
                }

                for (int offset = 0; offset < target.Length; offset++)
                {
                    var inst = instructions[index + offset].Instruction;
                    if (inst.HasValue && inst.Value.Opcode != target[offset])
                    {
                        replacementInstruction = default;
                        return false;
                    }
                }

                replacementInstruction = new() { Instruction = (Replacement, instructions[index].Instruction!.Value.Operands) };
                return true;
            }
        }
    }
}
