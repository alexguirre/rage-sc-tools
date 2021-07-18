namespace ScTools.Decompilation.Intermediate
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public enum Opcode : byte
    {
        NOP,
        IADD,
        ISUB,
        IMUL,
        IDIV,
        IMOD,
        INOT,
        INEG,
        IEQ,
        INE,
        IGT,
        IGE,
        ILT,
        ILE,
        FADD,
        FSUB,
        FMUL,
        FDIV,
        FMOD,
        FNEG,
        FEQ,
        FNE,
        FGT,
        FGE,
        FLT,
        FLE,
        VADD,
        VSUB,
        VMUL,
        VDIV,
        VNEG,
        IAND,
        IOR,
        IXOR,
        I2F,
        F2I,
        F2V,
        PUSH_CONST_I,
        PUSH_CONST_F,
        DUP,
        DROP,
        NATIVE,
        ENTER,
        LEAVE,
        LOAD,
        STORE,
        STORE_REV,
        LOAD_N,
        STORE_N,
        ARRAY,
        IOFFSET,
        LOCAL,
        STATIC,
        GLOBAL,
        J,
        JZ,
        CALL,
        SWITCH,
        STRING,
        TEXT_LABEL_ASSIGN_STRING,
        TEXT_LABEL_ASSIGN_INT,
        TEXT_LABEL_APPEND_STRING,
        TEXT_LABEL_APPEND_INT,
        TEXT_LABEL_COPY,
        CALLINDIRECT,
    }

    public static class OpcodeExtensions
    {
        /// <returns>
        /// The byte size of a instruction with this <paramref name="opcode"/>; or, <c>0</c> if the size is variable (i.e. <paramref name="opcode"/> is <see cref="Opcode.CALL"/>, <see cref="Opcode.SWITCH"/> or  <see cref="Opcode.STRING"/>).
        /// </returns>
        public static int ByteSize(this Opcode opcode)
#pragma warning disable format
            => opcode switch
            {
                Opcode.PUSH_CONST_I                 => 5,   // opcode + int value = 1 + 4 bytes
                Opcode.PUSH_CONST_F                 => 5,   // opcode + float value = 1 + 4 bytes
                Opcode.NATIVE                       => 11,  // opcode + arg count + return count + native hash = 1 + 1 + 1 + 8 bytes
                Opcode.ENTER                        => 0,   // opcode + arg count + frame size + name length + name chars... = 1 + 1 + 2 + 1 + name length bytes
                Opcode.LEAVE                        => 3,   // opcode + arg count + return count = 1 + 1 + 1 bytes
                Opcode.ARRAY                        => 5,   // opcode + item size = 1 + 4 bytes
                Opcode.IOFFSET                      => 5,   // opcode + offset = 1 + 4 bytes
                Opcode.LOCAL or
                Opcode.STATIC or
                Opcode.GLOBAL                       => 5,   // opcode + var address = 1 + 4 bytes
                Opcode.J or
                Opcode.JZ                           => 5,   // opcode + jump address = 1 + 4 bytes
                Opcode.CALL                         => 5,   // opcode + call address = 1 + 4 bytes
                Opcode.SWITCH                       => 0,   // opcode + case count + cases... = 1 + 1 + case count * 8
                Opcode.STRING                       => 0,   // opcode + length + chars... = 1 + 4 + length
                Opcode.TEXT_LABEL_ASSIGN_STRING or
                Opcode.TEXT_LABEL_ASSIGN_INT or
                Opcode.TEXT_LABEL_APPEND_STRING or
                Opcode.TEXT_LABEL_APPEND_INT        => 2,   // opcode + text label length = 1 + 1 bytes
                _                                   => 1,   // opcode = 1 byte
            };
#pragma warning restore format

        /// <returns>
        /// The byte size of a instruction with this <paramref name="opcode"/>, at the specified address.
        /// </returns>
        public static int ByteSize(this Opcode opcode, int address, byte[] code)
        {
            Debug.Assert((byte)opcode == code[address]);
            return ByteSize(address, code);
        }

        /// <returns>
        /// The byte size of the instruction at the specified address.
        /// </returns>
        public static int ByteSize(int address, byte[] code)
        {
            var opcode = (Opcode)code[address];
            return opcode switch
            {
                Opcode.ENTER => 1 + 1 + 2 + 1 + code[address + 4],                          // opcode + arg count + frame size + name length + name chars...
                Opcode.SWITCH => 1 + 1 + code[address + 1] * 8,                             // opcode + case count + cases...
                Opcode.STRING => 1 + 4 + MemoryMarshal.Read<int>(code.AsSpan(address + 1)), // opcode + length + chars...
                _ => opcode.ByteSize(),
            };
        }
    }
}
