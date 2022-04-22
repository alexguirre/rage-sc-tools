namespace ScTools.ScriptLang.CodeGen;

using System.Collections.Generic;

using ScTools.GameFiles;
using ScTools.ScriptAssembly;

public sealed class StringsTable
{
    private readonly SegmentBuilder segmentBuilder = new(Assembler.GetAddressingUnitByteSize(Assembler.Segment.String), isPaged: true);
    private readonly Dictionary<string, int> stringToOffset = new();

    public int ByteLength => segmentBuilder.Length;
    public ScriptPageArray<byte> ToPages() => segmentBuilder.ToPages<byte>();

    public int GetOffsetOf(string str)
    {
        TryAdd(str);
        return stringToOffset[str];
    }

    public bool TryAdd(string str)
    {
        if (stringToOffset.TryAdd(str, segmentBuilder.Length))
        {
            segmentBuilder.String(str);
            return true;
        }

        return false;
    }
}
