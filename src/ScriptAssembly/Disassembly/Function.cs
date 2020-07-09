namespace ScTools.ScriptAssembly.Disassembly
{
    using System.Collections.Generic;

    public class Function
    {
        public string Name { get; set; }
        public uint StartIP { get; set; }
        public uint EndIP { get; set; }
        public List<Location> Code { get; set; }

        public uint GetLabelIP(string labelName)
        {
            foreach (Location loc in Code)
            {
                if (loc.Label != null && loc.Label == labelName)
                {
                    return loc.IP;
                }
            }

            return 0xFFFFFFFF;
        }
    }
}
