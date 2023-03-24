namespace ScTools.ScriptLang.CodeGen;

using System.Collections.Generic;

using GameFiles.GTA5;
using ScTools.ScriptAssembly;
using ScriptAssembly.Targets.GTA5;

public sealed class StringsTable
{
    private readonly SegmentBuilder segmentBuilder = new(Assembler.GetSegmentAlignment(Assembler.Segment.String), isPaged: true);
    private readonly Dictionary<string, int> stringToOffset = new();

    public int ByteLength => segmentBuilder.Length;
    public ScriptPageTable<byte> ToPages() => segmentBuilder.ToPages<byte>();

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
