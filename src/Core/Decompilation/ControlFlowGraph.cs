namespace ScTools.Decompilation
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using ScTools.ScriptAssembly;

    public class ControlFlowGraph
    {
        public Function Function { get; }
        public CFGBlock[] Blocks { get; }

        public ControlFlowGraph(Function function, IEnumerable<CFGBlock> blocks)
        {
            Function = function;
            Blocks = blocks.OrderBy(b => b.StartAddress).ToArray();
        }

        public string ToDot()
        {
            using var s = new StringWriter();
            ToDot(s);
            return s.ToString();
        }

        public void ToDot(TextWriter w)
        {
            // header
            var escapedFunctionName = Function.Name.Escape();
            w.WriteLine("digraph \"{0}\" {{", escapedFunctionName);
            w.WriteLine("    label=\"{0}\"", escapedFunctionName);
            w.WriteLine("    labelloc=\"t\"");
            w.WriteLine("    graph[splines = ortho, fontname = \"Consolas\"];");
            w.WriteLine("    edge[fontname = \"Consolas\"];");
            w.WriteLine("    node[shape = box, fontname = \"Consolas\"];");
            w.WriteLine();

            // nodes
            var blocksIds = new Dictionary<CFGBlock, int>();
            foreach (var block in Blocks)
            {
                var id = blocksIds.Count;
                blocksIds.Add(block, id);

                w.Write("    b{0} [label=\"", id);
                if (block.IsDelimited && !block.IsEmpty)
                {
                    w.Write("lbl_{0}:\\l", block.StartAddress);
                    foreach (var inst in block.EnumerateInstructions())
                    {
                        w.Write("    ");
                        FormatInstruction(w, inst);
                        w.Write("\\l");  // \l left-aligns the line
                    }
                }
                else
                {
                    w.Write("<empty>");
                }
                w.WriteLine("\"];");
            }
            w.WriteLine();

            // edges
            foreach (var from in Blocks)
            {
                foreach (var to in from.Successors)
                {
                    w.WriteLine("    b{0} -> b{1}", blocksIds[from], blocksIds[to]);
                }
            }

            // footer
            w.WriteLine("}");
        }

        private static void FormatInstruction(TextWriter w, InstructionIterator inst)
        {
            var opcode = inst.Opcode;

            w.Write(opcode.ToString());
            if (opcode.NumberOfOperands() != 0)
            {
                w.Write(' ');
            }

            switch (opcode)
            {
                case Opcode.PUSH_CONST_U8:
                case Opcode.ARRAY_U8:
                case Opcode.ARRAY_U8_LOAD:
                case Opcode.ARRAY_U8_STORE:
                case Opcode.LOCAL_U8:
                case Opcode.LOCAL_U8_LOAD:
                case Opcode.LOCAL_U8_STORE:
                case Opcode.STATIC_U8:
                case Opcode.STATIC_U8_LOAD:
                case Opcode.STATIC_U8_STORE:
                case Opcode.IADD_U8:
                case Opcode.IMUL_U8:
                case Opcode.IOFFSET_U8:
                case Opcode.IOFFSET_U8_LOAD:
                case Opcode.IOFFSET_U8_STORE:
                case Opcode.TEXT_LABEL_ASSIGN_STRING:
                case Opcode.TEXT_LABEL_ASSIGN_INT:
                case Opcode.TEXT_LABEL_APPEND_STRING:
                case Opcode.TEXT_LABEL_APPEND_INT:
                    w.Write(inst.GetU8());
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                {
                    var (v1, v2) = inst.GetTwoU8();
                    w.Write(v1);
                    w.Write(", ");
                    w.Write(v2);
                    break;
                }
                case Opcode.PUSH_CONST_U8_U8_U8:
                {
                    var (v1, v2, v3) = inst.GetThreeU8();
                    w.Write(v1);
                    w.Write(", ");
                    w.Write(v2);
                    w.Write(", ");
                    w.Write(v3);
                    break;
                }
                case Opcode.PUSH_CONST_U32:
                    w.Write(inst.GetU32());
                    break;
                case Opcode.PUSH_CONST_F:
                    w.Write(inst.GetFloat().ToString("R", CultureInfo.InvariantCulture));
                    break;
                case Opcode.NATIVE:
                {
                    var (argCount, returnCount, nativeIndex) = inst.GetNativeOperands();
                    w.Write(argCount);
                    w.Write(", ");
                    w.Write(returnCount);
                    w.Write(", ");
                    w.Write(nativeIndex);
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
                case Opcode.PUSH_CONST_S16:
                case Opcode.IADD_S16:
                case Opcode.IMUL_S16:
                case Opcode.IOFFSET_S16:
                case Opcode.IOFFSET_S16_LOAD:
                case Opcode.IOFFSET_S16_STORE:
                    w.Write(inst.GetS16());
                    break;
                case Opcode.ARRAY_U16:
                case Opcode.ARRAY_U16_LOAD:
                case Opcode.ARRAY_U16_STORE:
                case Opcode.LOCAL_U16:
                case Opcode.LOCAL_U16_LOAD:
                case Opcode.LOCAL_U16_STORE:
                case Opcode.STATIC_U16:
                case Opcode.STATIC_U16_LOAD:
                case Opcode.STATIC_U16_STORE:
                case Opcode.GLOBAL_U16:
                case Opcode.GLOBAL_U16_LOAD:
                case Opcode.GLOBAL_U16_STORE:
                    w.Write(inst.GetU16());
                    break;
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.IEQ_JZ:
                case Opcode.INE_JZ:
                case Opcode.IGT_JZ:
                case Opcode.IGE_JZ:
                case Opcode.ILT_JZ:
                case Opcode.ILE_JZ:
                    w.Write($"lbl_{inst.GetJumpAddress()}");
                    break;
                case Opcode.CALL:
                case Opcode.GLOBAL_U24:
                case Opcode.GLOBAL_U24_LOAD:
                case Opcode.GLOBAL_U24_STORE:
                case Opcode.PUSH_CONST_U24:
                    w.Write(inst.GetU24());
                    break;
                case Opcode.SWITCH:
                    var caseCount = inst.GetSwitchCaseCount();
                    for (int i = 0; i < caseCount; i++)
                    {
                        var (caseValue, _, caseJumpAddress) = inst.GetSwitchCase(i);
                        if (i != 0)
                        {
                            w.Write(", ");
                        }
                        w.Write("{0}:lbl_{1}", caseValue, caseJumpAddress);
                    }
                    break;
            }
        }
    }
}
