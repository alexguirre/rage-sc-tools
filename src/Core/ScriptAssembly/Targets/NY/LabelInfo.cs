namespace ScTools.ScriptAssembly.Targets.NY;

internal record struct LabelReference(InstructionReference Instruction, int OperandOffset);
internal record struct LabelInfo(InstructionReference? Instruction, List<LabelReference> UnresolvedReferences);
