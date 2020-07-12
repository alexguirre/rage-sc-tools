namespace ScTools.ScriptAssembly.Disassembly
{
    using System.Collections.Generic;
    using ScTools.ScriptAssembly.Types;

    public class Function
    {
        public string Name { get; set; }
        public uint StartIP { get; set; }
        public uint EndIP { get; set; }
        public List<Location> Code { get; set; }
        public Dictionary<string, uint> Labels { get; set; }
        public bool Naked { get; set; } = true;
        public List<Argument> Arguments { get; set; }
        public List<Local> Locals { get; set; }
        public TypeBase ReturnType { get; set; }

        public uint GetLabelIP(string labelName) => Labels.TryGetValue(labelName, out uint ip) ? ip : 0xFFFFFFFF;
    }
}
