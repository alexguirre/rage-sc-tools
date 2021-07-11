namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public readonly ref struct InstructionIterator
    {
        public bool IsValid => Code != null && ByteSize > 0 && Address >= 0 && Address < Code.Length;
        public byte[] Code { get; init; }
        public int Address { get; init; }
        /// <summary>
        /// Gets the number of bytes of the current instruction. A zero value indicates an invalid address.
        /// </summary>
        public int ByteSize { get; init; }
        public ReadOnlySpan<byte> Bytes => Code.AsSpan(Address, ByteSize);
        public Opcode Opcode => (Opcode)Code[Address];

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
            int nextAddress = Address + ByteSize;
            return CreateIteratorAt(Code, nextAddress);
        }

        public static implicit operator bool(InstructionIterator it) => it.IsValid;

        public static bool operator ==(InstructionIterator a, InstructionIterator b)
            => a.Code == b.Code && a.Address == b.Address && a.Bytes.Length == b.Bytes.Length;

        public static bool operator !=(InstructionIterator a, InstructionIterator b)
            => !(a == b);

        public bool Equals(InstructionIterator other) => this == other;
        public override bool Equals(object? obj) => false; // false always, cannot cast ref struct to object

        public override int GetHashCode() => HashCode.Combine(Code, Address, ByteSize);

        public static InstructionIterator Begin(byte[] code) => BeginAt(code, 0);
        public static InstructionIterator BeginAt(byte[] code, int address) => CreateIteratorAt(code, address);

        private static InstructionIterator CreateIteratorAt(byte[] code, int address)
            => new()
            {
                Code = code,
                Address = address,
                ByteSize = address < 0 || address >= code.Length ? 0 : GetInstructionLength(code, address),
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

    public ref struct InstructionEnumerator
    {
        private readonly byte[] code;
        private readonly int startAddress, endAddress;

        public InstructionIterator Current { get; private set; }

        public InstructionEnumerator(byte[] code) : this(code, startAddress: 0, endAddress: code.Length)
        {
        }

        public InstructionEnumerator(byte[] code, int startAddress) : this(code, startAddress, endAddress: code.Length)
        {
        }

        public InstructionEnumerator(byte[] code, int startAddress, int endAddress)
        {
            this.code = code;
            this.startAddress = startAddress;
            this.endAddress = endAddress;
            Current = default;
        }

        public bool MoveNext()
        {
            if (Current == default)
            {
                Current = InstructionIterator.BeginAt(code, startAddress);
            }
            else
            {
                Current = Current.Next();
            }

            return Current && Current.Address < endAddress;
        }

        public void Reset()
        {
            Current = default;
        }

        public InstructionEnumerator GetEnumerator()
        {
            var copy = this;
            copy.Reset();
            return copy;
        }
    }
}
