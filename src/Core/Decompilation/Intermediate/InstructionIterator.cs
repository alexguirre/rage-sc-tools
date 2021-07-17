namespace ScTools.Decompilation.Intermediate
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public readonly struct InstructionIterator
    {
        public bool IsValid => Code != null && Address >= 0 && Address < Code.Length;
        public byte[] Code { get; init; }
        public int Address { get; init; }
        public int ByteSize => Opcode.ByteSize(Address, Code);
        public ReadOnlySpan<byte> Bytes => Code.AsSpan(Address, ByteSize);
        public Opcode Opcode => (Opcode)Code[Address];

        public InstructionIterator Previous()
        {
            int prevAddress = -1;
            int address = 0;
            while (address < Address)
            {
                prevAddress = address;
                address += OpcodeExtensions.ByteSize(address, Code);
            }
            return CreateIteratorAt(Code, prevAddress);
        }

        public InstructionIterator Next()
        {
            int nextAddress = Address + ByteSize;
            return CreateIteratorAt(Code, nextAddress);
        }

        public int GetInt()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectByteSize(expected: 5);

            return MemoryMarshal.Read<int>(Bytes[1..]);
        }

        public float GetFloat()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectByteSize(expected: 5);

            return MemoryMarshal.Read<float>(Bytes[1..]);
        }

        public int GetJumpAddress()
        {
            ThrowIfInvalid();
            if (Opcode is not (Opcode.J or Opcode.JZ))
            {
                throw new InvalidOperationException("This instruction is not a jump instruction");
            }

            return GetInt();
        }

        public int GetCallAddress()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.CALL);

            return GetInt();
        }

        public byte GetSwitchCaseCount()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.SWITCH);

            return Bytes[1];
        }

        public (int Value, int JumpAddress) GetSwitchCase(int index)
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.SWITCH);

            var bytes = Bytes;
            var caseCount = bytes[1];
            if (index < 0 || index  >= caseCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Case index is out of range");
            }

            var caseSpan = bytes.Slice(2 + 8 * index, 8);
            var value = MemoryMarshal.Read<int>(caseSpan);
            var jumpAddress = MemoryMarshal.Read<int>(caseSpan[4..]);
            return (value, jumpAddress);
        }

        public (byte ArgCount, byte ReturnCount, ulong NativeHash) GetNativeOperands()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.NATIVE);

            var bytes = Bytes;
            var argCount = bytes[1];
            var returnCount = bytes[2];
            var nativeHash = MemoryMarshal.Read<ulong>(bytes[3..]);
            return (argCount, returnCount, nativeHash);
        }

        public (byte ArgCount, ushort FrameSize) GetEnterOperands()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.ENTER);

            var bytes = Bytes;
            var argCount = bytes[1];
            var frameSize = MemoryMarshal.Read<ushort>(bytes[2..]);
            return (argCount, frameSize);
        }

        public string? GetEnterFunctionName()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.ENTER);

            var bytes = Bytes;
            var funcNameLengthIncludingNullChar = bytes[4];
            var funcName = funcNameLengthIncludingNullChar > 0 ?
                                System.Text.Encoding.UTF8.GetString(bytes.Slice(start: 5, length: funcNameLengthIncludingNullChar - 1)) :
                                null;
            return funcName;
        }

        public (byte ArgCount, byte ReturnCount) GetLeaveOperands()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.LEAVE);

            var bytes = Bytes;
            var argCount = bytes[1];
            var returnCount = bytes[2];
            return (argCount, returnCount);
        }

        public string GetStringOperand()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.STRING);

            var bytes = Bytes;
            var length = MemoryMarshal.Read<int>(bytes[1..]); // note: doesn't have a null terminator
            var value = length > 0 ?
                            System.Text.Encoding.UTF8.GetString(bytes.Slice(start: 5, length: length)) :
                            string.Empty;
            return value;
        }

        public byte GetTextLabelLength()
        {
            ThrowIfInvalid();
            if (Opcode is not (Opcode.TEXT_LABEL_ASSIGN_STRING or Opcode.TEXT_LABEL_ASSIGN_INT or Opcode.TEXT_LABEL_APPEND_STRING or Opcode.TEXT_LABEL_APPEND_INT))
            {
                throw new InvalidOperationException("This instruction is not a text label assign/append instruction");
            }

            return Bytes[1];
        }

        public int GetLabelAddress()
        {
            ThrowIfInvalid();
            ThrowIfIncorrectOpcode(expected: Opcode.LABEL);

            var lo = (int)Code[Address + 1];
            var mi = (int)Code[Address + 2];
            var hi = (int)Code[Address + 3];

            return (hi << 16) | (mi << 8) | lo;
        }

        private void ThrowIfInvalid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("This instruction is invalid");
            }
        }

        private void ThrowIfIncorrectOpcode(Opcode expected)
        {
            if (Opcode != expected)
            {
                throw new InvalidOperationException($"This instruction is not a {expected} instruction");
            }
        }

        private void ThrowIfIncorrectByteSize(int expected)
        {
            if (ByteSize != expected)
            {
                throw new InvalidOperationException($"This instruction size is not {expected} bytes");
            }
        }

        public static implicit operator bool(InstructionIterator it) => it.IsValid;

        public static bool operator ==(InstructionIterator a, InstructionIterator b)
            => a.Code == b.Code && a.Address == b.Address;

        public static bool operator !=(InstructionIterator a, InstructionIterator b)
            => !(a == b);

        public bool Equals(InstructionIterator other) => this == other;
        public override bool Equals(object? obj) => obj is InstructionIterator other && this == other;

        public override int GetHashCode() => HashCode.Combine(Code, Address);

        public static InstructionIterator Begin(byte[] code) => BeginAt(code, 0);
        public static InstructionIterator BeginAt(byte[] code, int address) => CreateIteratorAt(code, address);

        private static InstructionIterator CreateIteratorAt(byte[] code, int address)
            => new()
            {
                Code = code,
                Address = address,
            };
    }

    public struct InstructionEnumerator : IEnumerable<InstructionIterator>, IEnumerator<InstructionIterator>
    {
        private readonly byte[] code;
        private readonly int startAddress, endAddress;

        public InstructionIterator Current { get; private set; }

        object IEnumerator.Current => Current;

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

        public void Dispose()
        {
        }

        public InstructionEnumerator GetEnumerator()
        {
            var copy = this;
            copy.Reset();
            return copy;
        }

        IEnumerator<InstructionIterator> IEnumerable<InstructionIterator>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
