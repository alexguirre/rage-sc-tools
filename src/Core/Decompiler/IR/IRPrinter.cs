namespace ScTools.Decompiler.IR;

using System.IO;
using System.Linq;

public static class IRPrinter
{
    public static void PrintAll(IRInstruction? inst, TextWriter writer)
    {
        var visitor = new Visitor(writer);
        while (inst is not null)
        {
            writer.Write($"{inst.Address:000000} : ");
            inst.Accept(visitor);
            inst = inst.Next;
        }
    }

    public static string PrintAllToString(IRInstruction? inst)
    {
        using var sw = new StringWriter();
        PrintAll(inst, sw);
        return sw.ToString();
    }

    private sealed class Visitor : IIRVisitor
    {
        private readonly TextWriter w;

        public Visitor(TextWriter w) => this.w = w;

        public void Visit(IREnter inst) => w.WriteLine($"ENTER {inst.ParamCount}, {inst.LocalCount}");
        public void Visit(IRJump inst) => w.WriteLine($"J {inst.JumpAddress:000000}");
        public void Visit(IRJumpIfZero inst) => w.WriteLine($"JZ {inst.JumpAddress:000000}");
        public void Visit(IRSwitch inst) => w.WriteLine($"SWITCH {string.Join(", ", inst.Cases.Select(c => $"{c.Value}:{c.JumpAddress:000000}"))}");
        public void Visit(IRCall inst) => w.WriteLine($"CALL {inst.CallAddress:000000}");
        public void Visit(IRCallIndirect inst) => w.WriteLine($"CALLINDIRECT");
        public void Visit(IRNativeCall inst) => w.WriteLine(inst.Command <= uint.MaxValue ?
                                                            $"NATIVE {inst.ParamCount}, {inst.ReturnCount}, 0x{inst.Command:X8}" :
                                                            $"NATIVE {inst.ParamCount}, {inst.ReturnCount}, 0x{inst.Command:X16}");
        public void Visit(IRLeave inst) => w.WriteLine($"LEAVE {inst.ParamCount}, {inst.ReturnCount}");
        public void Visit(IRPushInt inst) => w.WriteLine($"PUSH_INT {inst.Value} (0x{unchecked((uint)inst.Value):X8})");
        public void Visit(IRPushFloat inst) => w.WriteLine($"PUSH_FLOAT {inst.Value:G9}");
        public void Visit(IRPushString inst) => w.WriteLine($"PUSH_STRING '{inst.Value.Escape()}'");
        public void Visit(IRPushStringFromStringTable inst) => w.WriteLine($"PUSH_STRING_S");
        public void Visit(IRIAdd inst) => w.WriteLine($"IADD");
        public void Visit(IRISub inst) => w.WriteLine($"ISUB");
        public void Visit(IRIMul inst) => w.WriteLine($"IMUL");
        public void Visit(IRIDiv inst) => w.WriteLine($"IDIV");
        public void Visit(IRIMod inst) => w.WriteLine($"IMOD");
        public void Visit(IRIAnd inst) => w.WriteLine($"IAND");
        public void Visit(IRIOr inst) => w.WriteLine($"IOR");
        public void Visit(IRIXor inst) => w.WriteLine($"IXOR");
        public void Visit(IRINot inst) => w.WriteLine($"INOT");
        public void Visit(IRINeg inst) => w.WriteLine($"INEG");
        public void Visit(IRIEqual inst) => w.WriteLine($"IEQ");
        public void Visit(IRINotEqual inst) => w.WriteLine($"INE");
        public void Visit(IRIGreaterThan inst) => w.WriteLine($"IGT");
        public void Visit(IRIGreaterOrEqual inst) => w.WriteLine($"IGE");
        public void Visit(IRILessThan inst) => w.WriteLine($"ILT");
        public void Visit(IRILessOrEqual inst) => w.WriteLine($"ILE");
        public void Visit(IRFAdd inst) => w.WriteLine($"FADD");
        public void Visit(IRFSub inst) => w.WriteLine($"FSUB");
        public void Visit(IRFMul inst) => w.WriteLine($"FMUL");
        public void Visit(IRFDiv inst) => w.WriteLine($"FDIV");
        public void Visit(IRFMod inst) => w.WriteLine($"FMOD");
        public void Visit(IRFNeg inst) => w.WriteLine($"FNEG");
        public void Visit(IRFEqual inst) => w.WriteLine($"FEQ");
        public void Visit(IRFNotEqual inst) => w.WriteLine($"FNE");
        public void Visit(IRFGreaterThan inst) => w.WriteLine($"FGT");
        public void Visit(IRFGreaterOrEqual inst) => w.WriteLine($"FGE");
        public void Visit(IRFLessThan inst) => w.WriteLine($"FLT");
        public void Visit(IRFLessOrEqual inst) => w.WriteLine($"FLE");
        public void Visit(IRVAdd inst) => w.WriteLine($"VADD");
        public void Visit(IRVSub inst) => w.WriteLine($"VSUB");
        public void Visit(IRVMul inst) => w.WriteLine($"VMUL");
        public void Visit(IRVDiv inst) => w.WriteLine($"VDIV");
        public void Visit(IRVNeg inst) => w.WriteLine($"VNEG");
        public void Visit(IRIntToFloat inst) => w.WriteLine($"I2F");
        public void Visit(IRFloatToInt inst) => w.WriteLine($"F2I");
        public void Visit(IRFloatToVector inst) => w.WriteLine($"F2V");
        public void Visit(IRDup inst) => w.WriteLine($"DUP");
        public void Visit(IRDrop inst) => w.WriteLine($"DROP");
        public void Visit(IRLoad inst) => w.WriteLine($"LOAD");
        public void Visit(IRStore inst) => w.WriteLine($"STORE");
        public void Visit(IRStoreRev inst) => w.WriteLine($"STORE_REV");
        public void Visit(IRLoadN inst) => w.WriteLine($"LOAD_N");
        public void Visit(IRStoreN inst) => w.WriteLine($"STORE_N");
        public void Visit(IRLocalRef inst) => w.WriteLine($"LOCAL {inst.VarAddress}");
        public void Visit(IRStaticRef inst) => w.WriteLine($"STATIC {inst.VarAddress}");
        public void Visit(IRGlobalRef inst) => w.WriteLine($"GLOBAL {inst.VarAddress}");
        public void Visit(IRLocalRefFromStack inst) => w.WriteLine($"LOCAL_S");
        public void Visit(IRStaticRefFromStack inst) => w.WriteLine($"STATIC_S");
        public void Visit(IRGlobalRefFromStack inst) => w.WriteLine($"GLOBAL_S");
        public void Visit(IRArrayItemRef inst) => w.WriteLine($"ARRAY");
    }
}
