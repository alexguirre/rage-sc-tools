namespace ScTools.ScriptAssembly.Disassembly
{
    using System;

    public struct Location
    {
        public uint IP { get; set; }
        public string Label { get; set; }
        public Opcode Opcode { get; set; }
        public Operand[] Operands { get; set; }
        public bool HasInstruction { get; set; }
        public HighLevelInstruction.UniqueId HLId { get; set; }
        public bool HasHLInstruction { get; set; }

        public Location(uint ip, Opcode opcode)
        {
            IP = ip;
            Label = null;
            Opcode = opcode;
            Operands = Array.Empty<Operand>();
            HasInstruction = true;
            HLId = 0;
            HasHLInstruction = false;
        }

        public Location(uint ip, string label)
        {
            IP = ip;
            Label = label;
            Opcode = 0;
            Operands = null;
            HasInstruction = false;
            HLId = 0;
            HasHLInstruction = false;
        }

        public Location(uint ip, HighLevelInstruction.UniqueId hlId)
        {
            IP = ip;
            Label = null;
            Opcode = 0;
            Operands = null;
            HasInstruction = false;
            HLId = hlId;
            HasHLInstruction = true;
        }
    }
}
