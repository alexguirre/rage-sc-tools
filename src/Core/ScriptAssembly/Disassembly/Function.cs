namespace ScTools.ScriptAssembly.Disassembly
{
    using System.Collections.Generic;
    using System.Linq;
    using ScTools.ScriptAssembly.Types;

    public class Function
    {
        public string Name { get; set; }
        public uint StartIP { get; set; }
        public uint EndIP { get; set; }
        public Location CodeStart { get; set; }
        public Location CodeEnd { get; set; }
        public Dictionary<string, uint> Labels { get; set; }
        public bool Naked { get; set; } = true;
        public List<Argument> Arguments { get; set; }
        public List<Local> Locals { get; set; }
        public TypeBase ReturnType { get; set; }

        public uint GetLabelIP(string labelName) => Labels.TryGetValue(labelName, out uint ip) ? ip : 0xFFFFFFFF;

        public void RebuildLabelsDictionary()
        {
            Labels ??= new Dictionary<string, uint>();
            Labels.Clear();

            foreach (var l in CodeStart.EnumerateForward().Where(l => l.Label != null))
            {
                Labels.Add(l.Label, l.IP);
            }
        }
    }
}
