namespace ScTools.Decompiler.IR;

using System.IO;
using System.Linq;

public static class IRPrinter
{
    public static void PrintAll(IRInstruction? inst, TextWriter writer, bool includeAddress = true)
    {
        var visitor = new Visitor(writer);
        while (inst is not null)
        {
            if (includeAddress)
            {
                writer.Write($"{inst.Address:000000} : ");
            }
            inst.Accept(visitor);
            writer.WriteLine();
            inst = inst.Next;
        }
    }

    public static string PrintAllToString(IRInstruction? inst, bool includeAddress = true)
    {
        using var sw = new StringWriter();
        PrintAll(inst, sw);
        return sw.ToString();
    }
    
    public static void Print(IRInstruction inst, TextWriter writer, bool includeAddress = true)
    {
        var visitor = new Visitor(writer);
        if (includeAddress)
        {
            writer.Write($"{inst.Address:000000} : ");
        }
        inst.Accept(visitor);
    }    

    public static string PrintToString(IRInstruction inst, bool includeAddress = true)
    {
        using var sw = new StringWriter();
        Print(inst, sw);
        return sw.ToString();
    }

    private sealed class Visitor : IIRVisitor
    {
        private readonly TextWriter w;

        public Visitor(TextWriter w) => this.w = w;

        public void Visit(IREndOfScript inst) { }
        public void Visit(IREnter inst) => w.Write(inst.FunctionName is null ?
                                                $"ENTER\t{inst.ParamCount}, {inst.LocalCount}" :
                                                $"ENTER\t{inst.ParamCount}, {inst.LocalCount}\t[{inst.FunctionName}]");
        public void Visit(IRJump inst) => w.Write($"J\t{inst.JumpAddress:000000}");
        public void Visit(IRJumpIfZero inst) => w.Write($"JZ\t{inst.JumpAddress:000000}");
        public void Visit(IRSwitch inst) => w.Write($"SWITCH\t{string.Join(", ", inst.Cases.Select(c => $"{c.Value}:{c.JumpAddress:000000}"))}");
        public void Visit(IRCall inst) => w.Write($"CALL\t{inst.CallAddress:000000}");
        public void Visit(IRCallIndirect inst) => w.Write($"CALLINDIRECT");
        public void Visit(IRNativeCall inst) => w.Write(inst.Command <= uint.MaxValue ?
                                                            $"NATIVE\t{inst.ParamCount}, {inst.ReturnCount}, 0x{inst.Command:X8}" :
                                                            $"NATIVE\t{inst.ParamCount}, {inst.ReturnCount}, 0x{inst.Command:X16}");
        public void Visit(IRLeave inst) => w.Write($"LEAVE\t{inst.ParamCount}, {inst.ReturnCount}");
        public void Visit(IRPushInt inst) => w.Write($"PUSH_INT\t{inst.Value} (0x{unchecked((uint)inst.Value):X8})");
        public void Visit(IRPushFloat inst) => w.Write($"PUSH_FLOAT\t{inst.Value:G9}");
        public void Visit(IRPushString inst) => w.Write($"PUSH_STRING\t'{inst.Value.Escape()}'");
        public void Visit(IRPushStringFromStringTable inst) => w.Write($"PUSH_STRING_S");
        public void Visit(IRIAdd inst) => w.Write($"IADD");
        public void Visit(IRISub inst) => w.Write($"ISUB");
        public void Visit(IRIMul inst) => w.Write($"IMUL");
        public void Visit(IRIDiv inst) => w.Write($"IDIV");
        public void Visit(IRIMod inst) => w.Write($"IMOD");
        public void Visit(IRIAnd inst) => w.Write($"IAND");
        public void Visit(IRIOr inst) => w.Write($"IOR");
        public void Visit(IRIXor inst) => w.Write($"IXOR");
        public void Visit(IRIBitTest inst) => w.Write($"IBITTEST");
        public void Visit(IRINot inst) => w.Write($"INOT");
        public void Visit(IRINeg inst) => w.Write($"INEG");
        public void Visit(IRIEqual inst) => w.Write($"IEQ");
        public void Visit(IRINotEqual inst) => w.Write($"INE");
        public void Visit(IRIGreaterThan inst) => w.Write($"IGT");
        public void Visit(IRIGreaterOrEqual inst) => w.Write($"IGE");
        public void Visit(IRILessThan inst) => w.Write($"ILT");
        public void Visit(IRILessOrEqual inst) => w.Write($"ILE");
        public void Visit(IRFAdd inst) => w.Write($"FADD");
        public void Visit(IRFSub inst) => w.Write($"FSUB");
        public void Visit(IRFMul inst) => w.Write($"FMUL");
        public void Visit(IRFDiv inst) => w.Write($"FDIV");
        public void Visit(IRFMod inst) => w.Write($"FMOD");
        public void Visit(IRFNeg inst) => w.Write($"FNEG");
        public void Visit(IRFEqual inst) => w.Write($"FEQ");
        public void Visit(IRFNotEqual inst) => w.Write($"FNE");
        public void Visit(IRFGreaterThan inst) => w.Write($"FGT");
        public void Visit(IRFGreaterOrEqual inst) => w.Write($"FGE");
        public void Visit(IRFLessThan inst) => w.Write($"FLT");
        public void Visit(IRFLessOrEqual inst) => w.Write($"FLE");
        public void Visit(IRVAdd inst) => w.Write($"VADD");
        public void Visit(IRVSub inst) => w.Write($"VSUB");
        public void Visit(IRVMul inst) => w.Write($"VMUL");
        public void Visit(IRVDiv inst) => w.Write($"VDIV");
        public void Visit(IRVNeg inst) => w.Write($"VNEG");
        public void Visit(IRIntToFloat inst) => w.Write($"I2F");
        public void Visit(IRFloatToInt inst) => w.Write($"F2I");
        public void Visit(IRFloatToVector inst) => w.Write($"F2V");
        public void Visit(IRDup inst) => w.Write($"DUP");
        public void Visit(IRDrop inst) => w.Write($"DROP");
        public void Visit(IRLoad inst) => w.Write($"LOAD");
        public void Visit(IRStore inst) => w.Write($"STORE");
        public void Visit(IRStoreRev inst) => w.Write($"STORE_REV");
        public void Visit(IRLoadN inst) => w.Write($"LOAD_N");
        public void Visit(IRStoreN inst) => w.Write($"STORE_N");
        public void Visit(IRLocalRef inst) => w.Write($"LOCAL\t{inst.VarAddress}");
        public void Visit(IRStaticRef inst) => w.Write($"STATIC\t{inst.VarAddress}");
        public void Visit(IRGlobalRef inst) => w.Write($"GLOBAL\t{inst.VarAddress}");
        public void Visit(IRLocalRefFromStack inst) => w.Write($"LOCAL_S");
        public void Visit(IRStaticRefFromStack inst) => w.Write($"STATIC_S");
        public void Visit(IRGlobalRefFromStack inst) => w.Write($"GLOBAL_S");
        public void Visit(IRArrayItemRef inst) => w.Write($"ARRAY {inst.ItemSize}");
        public void Visit(IRArrayItemRefSizeInStack inst) => w.Write($"ARRAY_S");
        public void Visit(IRNullRef inst) => w.Write($"NULL");
        public void Visit(IRTextLabelAssignString  inst) => w.Write($"TEXT_LABEL_ASSIGN_STRING\t{inst.TextLabelLength}");
        public void Visit(IRTextLabelAssignInt  inst) => w.Write($"TEXT_LABEL_ASSIGN_INT\t{inst.TextLabelLength}");
        public void Visit(IRTextLabelAppendString inst) => w.Write($"TEXT_LABEL_APPEND_STRING\t{inst.TextLabelLength}");
        public void Visit(IRTextLabelAppendInt  inst) => w.Write($"TEXT_LABEL_APPEND_INT\t{inst.TextLabelLength}");
        public void Visit(IRTextLabelCopy inst) => w.Write($"TEXT_LABEL_COPY");
    }
}
