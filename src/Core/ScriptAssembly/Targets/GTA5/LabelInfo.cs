﻿namespace ScTools.ScriptAssembly.Targets.GTA5;

internal enum LabelReferenceKind { Relative, Absolute }
internal record struct LabelReference(InstructionReference Instruction, int OperandOffset, LabelReferenceKind Kind);
internal record struct LabelInfo(InstructionReference? Instruction, List<LabelReference> UnresolvedReferences);
