namespace ScTools.Decompilation.Intermediate
{
    using System;

    using ScTools.GameFiles;

    using VInstIterator = ScriptAssembly.InstructionIterator;
    using VOpcode = ScriptAssembly.Opcode;

    /// <summary>
    /// Converts a script from Grand Theft Auto V to the intermediate representation used by the decompiler.
    /// </summary>
    public class FiveConverter
    {
        public Script Input { get; }
        public IntermediateScript Output { get; } = new();

        public FiveConverter(Script input)
        {
            Input = input;
        }

        public void Convert()
        {
            Output.Name = Input.Name;

            using var writer = new InstructionWriter();
            var code = Input.MergeCodePages();

            void WritePushConstIOrStringOrIOffset(ref VInstIterator inst, int value)
            {
                // check if this int is used with a STRING or IOFFSET instruction
                var nextConsumer = inst.Next();
                while (nextConsumer && nextConsumer.GetStackUsage() == (0, 0))
                {
                    // move next until we find a instruction that changes the stack
                    nextConsumer = nextConsumer.Next();
                }

                if (nextConsumer)
                {
                    switch (nextConsumer.Opcode)
                    {
                        case VOpcode.STRING:
                            var str = value >= 0 && value < Input.StringsLength ?
                                        Input.String(unchecked((uint)value)) :
                                        string.Empty;
                            writer.String(str);
                            inst = nextConsumer; // update the current instruction
                            return;

                        case VOpcode.IOFFSET:
                            writer.IOffset(value);
                            inst = nextConsumer; // update the current instruction
                            return;
                    }
                }

                // not used by STRING or IOFFSET, just push the value
                writer.PushConstI(value);
            }

            for (var vInst = VInstIterator.Begin(code); vInst; vInst = vInst.Next())
            {
                writer.Label(vInst.Address);
                switch (vInst.Opcode)
                {
                    case VOpcode.NOP: writer.Nop(); break;
                    case VOpcode.IADD: writer.IAdd(); break;
                    case VOpcode.ISUB: writer.ISub(); break;
                    case VOpcode.IMUL: writer.IMul(); break;
                    case VOpcode.IDIV: writer.IDiv(); break;
                    case VOpcode.IMOD: writer.IMod(); break;
                    case VOpcode.INOT: writer.INot(); break;
                    case VOpcode.INEG: writer.INeg(); break;
                    case VOpcode.IEQ: writer.IEq(); break;
                    case VOpcode.INE: writer.INe(); break;
                    case VOpcode.IGT: writer.IGt(); break;
                    case VOpcode.IGE: writer.IGe(); break;
                    case VOpcode.ILT: writer.ILt(); break;
                    case VOpcode.ILE: writer.ILe(); break;
                    case VOpcode.FADD: writer.FAdd(); break;
                    case VOpcode.FSUB: writer.FSub(); break;
                    case VOpcode.FMUL: writer.FMul(); break;
                    case VOpcode.FDIV: writer.FDiv(); break;
                    case VOpcode.FMOD: writer.FMod(); break;
                    case VOpcode.FNEG: writer.FNeg(); break;
                    case VOpcode.FEQ: writer.FEq(); break;
                    case VOpcode.FNE: writer.FNe(); break;
                    case VOpcode.FGT: writer.FGt(); break;
                    case VOpcode.FGE: writer.FGe(); break;
                    case VOpcode.FLT: writer.FLt(); break;
                    case VOpcode.FLE: writer.FLe(); break;
                    case VOpcode.VADD: writer.VAdd(); break;
                    case VOpcode.VSUB: writer.VSub(); break;
                    case VOpcode.VMUL: writer.VMul(); break;
                    case VOpcode.VDIV: writer.VDiv(); break;
                    case VOpcode.VNEG: writer.VNeg(); break;
                    case VOpcode.IAND: writer.IAnd(); break;
                    case VOpcode.IOR: writer.IOr(); break;
                    case VOpcode.IXOR: writer.IXor(); break;
                    case VOpcode.I2F: writer.I2F(); break;
                    case VOpcode.F2I: writer.F2I(); break;
                    case VOpcode.F2V: writer.F2V(); break;

                    case VOpcode.PUSH_CONST_U8:
                        WritePushConstIOrStringOrIOffset(ref vInst, vInst.GetU8());
                        break;

                    case VOpcode.PUSH_CONST_U8_U8:
                        var twoU8 = vInst.GetTwoU8();
                        writer.PushConstI(twoU8.Value1);
                        WritePushConstIOrStringOrIOffset(ref vInst, twoU8.Value2);
                        break;

                    case VOpcode.PUSH_CONST_U8_U8_U8:
                        var threeU8 = vInst.GetThreeU8();
                        writer.PushConstI(threeU8.Value1);
                        writer.PushConstI(threeU8.Value2);
                        WritePushConstIOrStringOrIOffset(ref vInst, threeU8.Value3);
                        break;

                    case VOpcode.PUSH_CONST_U32:
                        WritePushConstIOrStringOrIOffset(ref vInst, unchecked((int)vInst.GetU32()));
                        break;

                    case VOpcode.PUSH_CONST_F:
                        writer.PushConstF(vInst.GetFloat());
                        break;

                    case VOpcode.DUP: writer.Dup(); break;
                    case VOpcode.DROP: writer.Drop(); break;

                    case VOpcode.NATIVE:
                        var native = vInst.GetNativeOperands();
                        var nativeHash = Input.NativeHash(native.NativeIndex); // TODO: should we solve the native hash here?
                        writer.Native((byte)native.ArgCount, (byte)native.ReturnCount, nativeHash);
                        break;

                    case VOpcode.ENTER:
                        var enter = vInst.GetEnterOperands();
                        var enterName = vInst.GetEnterFunctionName();
                        writer.Enter(enter.ArgCount, enter.FrameSize, enterName);
                        break;

                    case VOpcode.LEAVE:
                        var leave = vInst.GetLeaveOperands();
                        writer.Leave(leave.ArgCount, leave.ReturnCount);
                        break;

                    case VOpcode.LOAD: writer.Load(); break;
                    case VOpcode.STORE: writer.Store(); break;
                    case VOpcode.STORE_REV: writer.StoreRev(); break;
                    case VOpcode.LOAD_N: writer.LoadN(); break;
                    case VOpcode.STORE_N: writer.StoreN(); break;

                    case VOpcode.ARRAY_U8:
                        writer.Array(vInst.GetU8());
                        break;
                    case VOpcode.ARRAY_U8_LOAD:
                        writer.Array(vInst.GetU8());
                        writer.Load();
                        break;
                    case VOpcode.ARRAY_U8_STORE:
                        writer.Array(vInst.GetU8());
                        writer.Store();
                        break;

                    case VOpcode.LOCAL_U8:
                        writer.Local(vInst.GetU8());
                        break;
                    case VOpcode.LOCAL_U8_LOAD:
                        writer.Local(vInst.GetU8());
                        writer.Load();
                        break;
                    case VOpcode.LOCAL_U8_STORE:
                        writer.Local(vInst.GetU8());
                        writer.Store();
                        break;

                    case VOpcode.STATIC_U8:
                        writer.Static(vInst.GetU8());
                        break;
                    case VOpcode.STATIC_U8_LOAD:
                        writer.Static(vInst.GetU8());
                        writer.Load();
                        break;
                    case VOpcode.STATIC_U8_STORE:
                        writer.Static(vInst.GetU8());
                        writer.Store();
                        break;

                    case VOpcode.IADD_U8:
                        writer.PushConstI(vInst.GetU8());
                        writer.IAdd();
                        break;
                    case VOpcode.IMUL_U8:
                        writer.PushConstI(vInst.GetU8());
                        writer.IMul();
                        break;

                    case VOpcode.IOFFSET_U8:
                        writer.IOffset(vInst.GetU8());
                        break;
                    case VOpcode.IOFFSET_U8_LOAD:
                        writer.IOffset(vInst.GetU8());
                        writer.Load();
                        break;
                    case VOpcode.IOFFSET_U8_STORE:
                        writer.IOffset(vInst.GetU8());
                        writer.Store();
                        break;

                    case VOpcode.PUSH_CONST_S16:
                        WritePushConstIOrStringOrIOffset(ref vInst, vInst.GetS16());
                        break;

                    case VOpcode.IADD_S16:
                        writer.PushConstI(vInst.GetS16());
                        writer.IAdd();
                        break;
                    case VOpcode.IMUL_S16:
                        writer.PushConstI(vInst.GetS16());
                        writer.IMul();
                        break;

                    case VOpcode.IOFFSET_S16:
                        writer.IOffset(vInst.GetS16());
                        break;
                    case VOpcode.IOFFSET_S16_LOAD:
                        writer.IOffset(vInst.GetS16());
                        writer.Load();
                        break;
                    case VOpcode.IOFFSET_S16_STORE:
                        writer.IOffset(vInst.GetS16());
                        writer.Store();
                        break;

                    case VOpcode.ARRAY_U16:
                        writer.Array(vInst.GetU16());
                        break;
                    case VOpcode.ARRAY_U16_LOAD:
                        writer.Array(vInst.GetU16());
                        writer.Load();
                        break;
                    case VOpcode.ARRAY_U16_STORE:
                        writer.Array(vInst.GetU16());
                        writer.Store();
                        break;

                    case VOpcode.LOCAL_U16:
                        writer.Local(vInst.GetU16());
                        break;
                    case VOpcode.LOCAL_U16_LOAD:
                        writer.Local(vInst.GetU16());
                        writer.Load();
                        break;
                    case VOpcode.LOCAL_U16_STORE:
                        writer.Local(vInst.GetU16());
                        writer.Store();
                        break;

                    case VOpcode.STATIC_U16:
                        writer.Static(vInst.GetU16());
                        break;
                    case VOpcode.STATIC_U16_LOAD:
                        writer.Static(vInst.GetU16());
                        writer.Load();
                        break;
                    case VOpcode.STATIC_U16_STORE:
                        writer.Static(vInst.GetU16());
                        writer.Store();
                        break;

                    case VOpcode.GLOBAL_U16:
                        writer.Global(vInst.GetU16());
                        break;
                    case VOpcode.GLOBAL_U16_LOAD:
                        writer.Global(vInst.GetU16());
                        writer.Load();
                        break;
                    case VOpcode.GLOBAL_U16_STORE:
                        writer.Global(vInst.GetU16());
                        writer.Store();
                        break;

                    case VOpcode.J:
                        writer.J(vInst.GetJumpAddress()); // TODO: actually convert the jump address
                        break;

                    case VOpcode.JZ:
                        writer.Jz(vInst.GetJumpAddress()); // TODO: actually convert the jump address
                        break;

                    case VOpcode.IEQ_JZ:
                        writer.IEq();
                        goto case VOpcode.JZ;
                    case VOpcode.INE_JZ:
                        writer.INe();
                        goto case VOpcode.JZ;
                    case VOpcode.IGT_JZ:
                        writer.IGt();
                        goto case VOpcode.JZ;
                    case VOpcode.IGE_JZ:
                        writer.IGe();
                        goto case VOpcode.JZ;
                    case VOpcode.ILT_JZ:
                        writer.ILt();
                        goto case VOpcode.JZ;
                    case VOpcode.ILE_JZ:
                        writer.ILe();
                        goto case VOpcode.JZ;

                    case VOpcode.CALL:
                        writer.Call(vInst.GetCallAddress());  // TODO: actually convert the call address
                        break;

                    case VOpcode.GLOBAL_U24:
                        writer.Global(unchecked((int)vInst.GetU24()));
                        break;
                    case VOpcode.GLOBAL_U24_LOAD:
                        writer.Global(unchecked((int)vInst.GetU24()));
                        writer.Load();
                        break;
                    case VOpcode.GLOBAL_U24_STORE:
                        writer.Global(unchecked((int)vInst.GetU24()));
                        writer.Store();
                        break;

                    case VOpcode.PUSH_CONST_U24:
                        WritePushConstIOrStringOrIOffset(ref vInst, unchecked((int)vInst.GetU24()));
                        break;

                    case VOpcode.SWITCH:
                        var caseCount = vInst.GetSwitchCaseCount();
                        var cases = new (int Value, int JumpAddress)[caseCount];
                        for (int i = 0; i < caseCount; i++)
                        {
                            var (caseValue, _, caseJumpAddress) = vInst.GetSwitchCase(i);
                            cases[i] = (caseValue, caseJumpAddress); // TODO: actually convert the switch cases jump addresses
                        }
                        writer.Switch(cases);
                        break;

                    case VOpcode.STRINGHASH: throw new InvalidOperationException("STRINGHASH is not supported");
                    
                    case VOpcode.TEXT_LABEL_ASSIGN_STRING: writer.TextLabelAssignString(vInst.GetU8()); break;
                    case VOpcode.TEXT_LABEL_ASSIGN_INT: writer.TextLabelAssignInt(vInst.GetU8()); break;
                    case VOpcode.TEXT_LABEL_APPEND_STRING: writer.TextLabelAppendString(vInst.GetU8()); break;
                    case VOpcode.TEXT_LABEL_APPEND_INT: writer.TextLabelAppendInt(vInst.GetU8()); break;

                    case VOpcode.TEXT_LABEL_COPY: writer.TextLabelCopy(); break;

                    case VOpcode.CATCH: throw new InvalidOperationException("CATCH is not supported");
                    case VOpcode.THROW: throw new InvalidOperationException("THROW is not supported");

                    case VOpcode.CALLINDIRECT: writer.CallIndirect(); break;

                    case VOpcode.PUSH_CONST_M1:
                    case VOpcode.PUSH_CONST_0:
                    case VOpcode.PUSH_CONST_1:
                    case VOpcode.PUSH_CONST_2:
                    case VOpcode.PUSH_CONST_3:
                    case VOpcode.PUSH_CONST_4:
                    case VOpcode.PUSH_CONST_5:
                    case VOpcode.PUSH_CONST_6:
                    case VOpcode.PUSH_CONST_7:
                        WritePushConstIOrStringOrIOffset(ref vInst, vInst.Opcode - VOpcode.PUSH_CONST_0);
                        break;

                    case VOpcode.PUSH_CONST_FM1:
                    case VOpcode.PUSH_CONST_F0:
                    case VOpcode.PUSH_CONST_F1:
                    case VOpcode.PUSH_CONST_F2:
                    case VOpcode.PUSH_CONST_F3:
                    case VOpcode.PUSH_CONST_F4:
                    case VOpcode.PUSH_CONST_F5:
                    case VOpcode.PUSH_CONST_F6:
                    case VOpcode.PUSH_CONST_F7:
                        writer.PushConstF(vInst.Opcode - VOpcode.PUSH_CONST_F0);
                        break;

                    case VOpcode.STRING:
                    case VOpcode.IOFFSET:
                        // WritePushConstIOrStringOrIOffset should have converted it already
                        // If it gets here is because these instructions had some pattern other than PUSH_CONST + STRING/IOFFSET,
                        // which doesn't happen in vanilla scripts
                        throw new InvalidOperationException("STRING or IOFFSET instructions should have been already converted");
                }
            }
            writer.Finish();

            Output.Code = writer.ToArray();
        }

        public static IntermediateScript Convert(Script input)
        {
            var converter = new FiveConverter(input);
            converter.Convert();
            return converter.Output;
        }
    }
}
