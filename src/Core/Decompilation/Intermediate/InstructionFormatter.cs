namespace ScTools.Decompilation.Intermediate
{
    using System.Globalization;
    using System.IO;

    public static class InstructionFormatter
    {
        public static void Format(TextWriter w, InstructionIterator inst)
        {
            var opcode = inst.Opcode;

            if (opcode is Opcode.LABEL)
            {
                return;
            }

            w.Write(opcode.ToString());
            if (opcode.ByteSize() != 1)
            {
                w.Write(' ');
            }

            switch (opcode)
            {
                case Opcode.PUSH_CONST_I:
                case Opcode.ARRAY:
                case Opcode.IOFFSET:
                case Opcode.LOCAL:
                case Opcode.STATIC:
                case Opcode.GLOBAL:
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.CALL:
                    w.Write(inst.GetInt());
                    break;

                case Opcode.PUSH_CONST_F:
                    w.Write(inst.GetFloat().ToString("R", CultureInfo.InvariantCulture));
                    break;

                case Opcode.NATIVE:
                {
                    var (argCount, returnCount, nativeHash) = inst.GetNativeOperands();
                    w.Write("{0}, {1}, 0x{2:X16}", argCount, returnCount, nativeHash);
                    break;
                }

                case Opcode.ENTER:
                {
                    var (argCount, frameSize) = inst.GetEnterOperands();
                    w.Write(argCount);
                    w.Write(", ");
                    w.Write(frameSize);
                    break;
                }

                case Opcode.LEAVE:
                {
                    var (argCount, returnCount) = inst.GetLeaveOperands();
                    w.Write(argCount);
                    w.Write(", ");
                    w.Write(returnCount);
                    break;
                }

                case Opcode.SWITCH:
                    var caseCount = inst.GetSwitchCaseCount();
                    for (int i = 0; i < caseCount; i++)
                    {
                        var (caseValue, caseJumpAddress) = inst.GetSwitchCase(i);
                        if (i != 0)
                        {
                            w.Write(", ");
                        }
                        w.Write("{0}:{1}", caseValue, caseJumpAddress);
                    }
                    break;

                case Opcode.STRING:
                    w.Write("'{0}'", inst.GetStringOperand().Escape());
                    break;

                case Opcode.TEXT_LABEL_ASSIGN_STRING:
                case Opcode.TEXT_LABEL_ASSIGN_INT:
                case Opcode.TEXT_LABEL_APPEND_STRING:
                case Opcode.TEXT_LABEL_APPEND_INT:
                    w.Write(inst.GetTextLabelLength());
                    break;
            }
        }
    }
}
