namespace ScTools.ScriptAssembly
{
    using System;

    public readonly ref struct InstructionIterator
    {
        public bool IsValid => Code != null && Bytes.Length > 0 && Address >= 0 && Address < Code.Length;
        public byte[] Code { get; init; }
        public int Address { get; init; }
        public ReadOnlySpan<byte> Bytes { get; init; }
        public Opcode Opcode => (Opcode)Bytes[0];

        public InstructionIterator Previous()
        {
            int prevAddress = -1;
            int address = 0;
            while (address < Address)
            {
                prevAddress = address;
                address += GetInstructionLength(Code, address);
            }
            return CreateIteratorAt(Code, prevAddress);
        }

        public InstructionIterator Next()
        {
            int nextAddress = Address + Bytes.Length;
            return CreateIteratorAt(Code, nextAddress);
        }

        public static implicit operator bool(InstructionIterator it) => it.IsValid;

        public static InstructionIterator Begin(byte[] code) => CreateIteratorAt(code, 0);


        private static InstructionIterator CreateIteratorAt(byte[] code, int address)
            => new()
            {
                Code = code,
                Address = address,
                Bytes = address < 0 || address >= code.Length ? default : code.AsSpan(address, GetInstructionLength(code, address)),
            };

        private static int GetInstructionLength(byte[] code, int address)
        {
            var opcode = (Opcode)code[address];
            return opcode switch
            {
                Opcode.ENTER => 5 + code[address + 4],  // 5 + nameLength
                Opcode.SWITCH => 2 + 6 * code[address + 1], // 2 + 6 * caseCount
                _ => opcode.ByteSize(),
            };
        }
    }
}
