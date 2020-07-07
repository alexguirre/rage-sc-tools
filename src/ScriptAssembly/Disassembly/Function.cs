namespace ScTools.ScriptAssembly.Disassembly
{
    using System.Collections.Generic;

    public class Function
    {
        public string Name { get; set; }
        public uint StartIP { get; set; }
        public uint EndIP { get; set; }
        public List<Location> Code { get; set; }
    }
}
