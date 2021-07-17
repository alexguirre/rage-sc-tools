namespace ScTools.Decompilation.Intermediate
{
    using System;
    using System.IO;
    using System.Text;

    public class InstructionWriter : IDisposable
    {
        private readonly MemoryStream buffer;
        private readonly BinaryWriter writer;

        public int CodeSize => (int)buffer.Length;

        public InstructionWriter()
        {
            buffer = new MemoryStream();
            writer = new BinaryWriter(buffer, Encoding.UTF8);
        }

        public byte[] ToArray() => buffer.ToArray();

        public void Dispose()
        {
            writer.Dispose();
            buffer.Dispose();
        }

        private void Write(Opcode opcode) => writer.Write((byte)opcode);

        private void Write(Opcode opcode, int value)
        {
            writer.Write((byte)opcode);
            writer.Write(value);
        }

        private void Write(Opcode opcode, byte value)
        {
            writer.Write((byte)opcode);
            writer.Write(value);
        }

        private void Write(Opcode opcode, float value)
        {
            writer.Write((byte)opcode);
            writer.Write(value);
        }

        public void Nop() => Write(Opcode.NOP);
        public void IAdd() => Write(Opcode.IADD);
        public void ISub() => Write(Opcode.ISUB);
        public void IMul() => Write(Opcode.IMUL);
        public void IDiv() => Write(Opcode.IDIV);
        public void IMod() => Write(Opcode.IMOD);
        public void INot() => Write(Opcode.INOT);
        public void INeg() => Write(Opcode.INEG);
        public void IEq() => Write(Opcode.IEQ);
        public void INe() => Write(Opcode.INE);
        public void IGt() => Write(Opcode.IGT);
        public void IGe() => Write(Opcode.IGE);
        public void ILt() => Write(Opcode.ILT);
        public void ILe() => Write(Opcode.ILE);
        public void FAdd() => Write(Opcode.FADD);
        public void FSub() => Write(Opcode.FSUB);
        public void FMul() => Write(Opcode.FMUL);
        public void FDiv() => Write(Opcode.FDIV);
        public void FMod() => Write(Opcode.FMOD);
        public void FNeg() => Write(Opcode.FNEG);
        public void FEq() => Write(Opcode.FEQ);
        public void FNe() => Write(Opcode.FNE);
        public void FGt() => Write(Opcode.FGT);
        public void FGe() => Write(Opcode.FGE);
        public void FLt() => Write(Opcode.FLT);
        public void FLe() => Write(Opcode.FLE);
        public void VAdd() => Write(Opcode.VADD);
        public void VSub() => Write(Opcode.VSUB);
        public void VMul() => Write(Opcode.VMUL);
        public void VDiv() => Write(Opcode.VDIV);
        public void VNeg() => Write(Opcode.VNEG);
        public void IAnd() => Write(Opcode.IAND);
        public void IOr() => Write(Opcode.IOR);
        public void IXor() => Write(Opcode.IXOR);
        public void I2F() => Write(Opcode.I2F);
        public void F2I() => Write(Opcode.F2I);
        public void F2V() => Write(Opcode.F2V);
        public void PushConstI(int value) => Write(Opcode.PUSH_CONST_I, value);
        public void PushConstF(float value) => Write(Opcode.PUSH_CONST_F, value);
        public void Dup() => Write(Opcode.DUP);
        public void Drop() => Write(Opcode.DROP);
        public void Native(byte argCount, byte returnCount, ulong nativeHash)
        {
            Write(Opcode.NATIVE);
            writer.Write(argCount);
            writer.Write(returnCount);
            writer.Write(nativeHash);
        }
        public void Enter(byte argCount, ushort frameSize, string? funcName)
        {
            Write(Opcode.ENTER);
            writer.Write(argCount);
            writer.Write(frameSize);
            if (funcName is null)
            {
                writer.Write((byte)0);
            }
            else
            {
                var utf8Bytes = Encoding.UTF8.GetBytes(funcName);
                var lengthWithNullChar = utf8Bytes.Length + 1;
                if (lengthWithNullChar > byte.MaxValue)
                {
                    throw new ArgumentException("Function name too long", nameof(funcName));
                }

                writer.Write((byte)lengthWithNullChar);
                writer.Write(utf8Bytes);
                writer.Write((byte)0); // null char
            }
        }
        public void Leave(byte argCount, byte returnCount)
        {
            Write(Opcode.LEAVE);
            writer.Write(argCount);
            writer.Write(returnCount);
        }
        public void Load() => Write(Opcode.LOAD);
        public void Store() => Write(Opcode.STORE);
        public void StoreRev() => Write(Opcode.STORE_REV);
        public void LoadN() => Write(Opcode.LOAD_N);
        public void StoreN() => Write(Opcode.STORE_N);
        public void Array(int itemSize) => Write(Opcode.ARRAY, itemSize);
        public void IOffset(int offset) => Write(Opcode.IOFFSET, offset);
        public void Local(int address) => Write(Opcode.LOCAL, address);
        public void Static(int address) => Write(Opcode.STATIC, address);
        public void Global(int address) => Write(Opcode.GLOBAL, address);
        public void J(int jumpAddress) => Write(Opcode.J, jumpAddress);
        public void Jz(int jumpAddress) => Write(Opcode.JZ, jumpAddress);
        public void Call(int callAddress) => Write(Opcode.CALL, callAddress);
        public void Switch((int Value, int JumpAddress)[] cases)
        {
            if (cases.Length > byte.MaxValue)
            {
                throw new ArgumentException("Too many switch cases", nameof(cases));
            }

            Write(Opcode.SWITCH);
            writer.Write((byte)cases.Length);
            foreach (var (value, jumpAddress) in cases)
            {
                writer.Write(value);
                writer.Write(jumpAddress);
            }
        }
        public void String(string value)
        {
            Write(Opcode.STRING);
            var utf8Bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(utf8Bytes.Length);
            writer.Write(utf8Bytes);
        }
        public void TextLabelAssignString(byte textLabelLength) => Write(Opcode.TEXT_LABEL_ASSIGN_STRING, textLabelLength);
        public void TextLabelAssignInt(byte textLabelLength) => Write(Opcode.TEXT_LABEL_ASSIGN_INT, textLabelLength);
        public void TextLabelAppendString(byte textLabelLength) => Write(Opcode.TEXT_LABEL_APPEND_STRING, textLabelLength);
        public void TextLabelAppendInt(byte textLabelLength) => Write(Opcode.TEXT_LABEL_APPEND_INT, textLabelLength);
        public void TextLabelCopy() => Write(Opcode.TEXT_LABEL_COPY);
        public void CallIndirect() => Write(Opcode.CALLINDIRECT);
        public void Label(int originalAddress)
        {
            var lo = (byte)(originalAddress & 0xFF);
            var mi = (byte)((originalAddress & 0xFF00) >> 8);
            var hi = (byte)((originalAddress & 0xFF0000) >> 16);

            Write(Opcode.LABEL);
            writer.Write(lo);
            writer.Write(mi);
            writer.Write(hi);
        }

        public void Finish()
        {
            // TODO: improve how address translation is handled.
            //       This final conversion is too slow and LABEL instructions consume too much memory (4 bytes per original instruction)

            var codeCopy = ToArray();
            foreach (var inst in new InstructionEnumerator(codeCopy))
            {
                switch (inst.Opcode)
                {
                    case Opcode.J:
                    case Opcode.JZ:
                    {
                        var newAddress = OriginalAddressToIntermediateAddress(codeCopy, inst.GetJumpAddress());
                        writer.Seek(inst.Address + 1, SeekOrigin.Begin);
                        writer.Write(newAddress);
                        break;
                    }

                    case Opcode.CALL:
                    {
                        var newAddress = OriginalAddressToIntermediateAddress(codeCopy, inst.GetCallAddress());
                        writer.Seek(inst.Address + 1, SeekOrigin.Begin);
                        writer.Write(newAddress);
                        break;
                    }

                    case Opcode.SWITCH:
                    {
                        var caseCount = inst.GetSwitchCaseCount();
                        for (int i = 0; i < caseCount; i++)
                        {
                            var newAddress = OriginalAddressToIntermediateAddress(codeCopy, inst.GetSwitchCase(i).JumpAddress);
                            writer.Seek(inst.Address + 2 + 8 * i + 4, SeekOrigin.Begin);
                            writer.Write(newAddress);
                        }
                        break;
                    }
                }
            }
        }

        // slow...
        private static int OriginalAddressToIntermediateAddress(byte[] code, int originalAddress)
        {
            foreach (var inst in new InstructionEnumerator(code))
            {
                if (inst.Opcode is Opcode.LABEL && inst.GetLabelAddress() == originalAddress)
                {
                    return inst.Address;
                }
            }

            return -1;
        }
    }
}
