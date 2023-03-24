namespace ScTools.ScriptAssembly.Targets.GTA4;

internal record struct LabelReference(InstructionReference Instruction, int OperandOffset);
internal record struct LabelInfo(InstructionReference? Instruction, List<LabelReference> UnresolvedReferences);
