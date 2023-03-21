namespace ScTools.GameFiles;

using System;
using System.IO;

/// <summary>
/// Options for script dumps.
/// </summary>
/// <param name="IncludeMetadata">Include metadata about the script?</param>
/// <param name="IncludeDisassembly">Include the script disassembly?</param>
/// <param name="IncludeOffsets">Include instruction offsets in the disassembly?</param>
/// <param name="IncludeBytes">Include instruction bytes in the disassembly?</param>
/// <param name="IncludeInstructions">Include text representation of instructions in the disassembly?</param>
public readonly record struct DumpOptions(
    bool IncludeMetadata,
    bool IncludeDisassembly,
    bool IncludeOffsets,
    bool IncludeBytes,
    bool IncludeInstructions)
{
    public static DumpOptions Default => new(true, true, true, true, true);
}
