namespace ScTools.ScriptLang.CodeGen
{
    using System.Collections.Generic;

    using ScTools.ScriptAssembly;

    public sealed class StringsTable
    {
        private readonly SegmentBuilder segmentBuilder = new(Assembler.GetAddressingUnitByteSize(Assembler.Segment.String), isPaged: true);

        public IDictionary<string, int> StringToID { get; } = new Dictionary<string, int>();
        public int Count => StringToID.Count;
        public int this[string str] => StringToID[str];

        public void Add(string str)
        {
            if (!StringToID.ContainsKey(str))
            {
                StringToID.Add(str, segmentBuilder.Length);
                segmentBuilder.String(str);
            }
        }
    }
}
