namespace ScTools.GTA5
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct scrProgramId
    {
        public uint Value;

        public override string ToString() => Value.ToString("X08");

        public static implicit operator scrProgramId(uint id) => new scrProgramId { Value = id };
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x80)]
    internal unsafe struct scrProgram
    {
        [FieldOffset(0x18)] public uint Hash;
        [FieldOffset(0x1C)] public uint CodeLength;
        [FieldOffset(0x20)] public uint ArgsCount;
        [FieldOffset(0x24)] public uint StaticsCount;
        [FieldOffset(0x28)] public uint GlobalsLengthAndBlock;
        [FieldOffset(0x28)] public uint NativesCount;

        [FieldOffset(0x58)] public scrProgramId Id;

        [FieldOffset(0x60)] public IntPtr Name;

        public uint GlobalsLength
        {
            get => GlobalsLengthAndBlock & 0x3FFFF;
            set => GlobalsLengthAndBlock = (GlobalsLengthAndBlock & 0xFFFC0000) | (value & 0x3FFFF);
        }

        public uint GlobalsBlock
        {
            get => GlobalsLengthAndBlock >> 18;
            set => GlobalsLengthAndBlock = GlobalsLength | (value << 18);
        }

        public string GetName() => Marshal.PtrToStringAnsi(Name) ?? "<null>";
    }
}
