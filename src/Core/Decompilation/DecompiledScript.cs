namespace ScTools.Decompilation
{
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.Decompilation.Intermediate;
    using ScTools.GameFiles;

    public class DecompiledScript
    {
        public IntermediateScript Intermediate { get; }
        public List<Function> Functions { get; } = new();

        public DecompiledScript(Script sc)
        {
            Intermediate = FiveConverter.Convert(sc);
            IdentifyFunctions();
            BuildControlFlowGraphs();
        }

        private void IdentifyFunctions()
        {
            Functions.Clear();

            var addressAfterLastLeaveInst = 0;
            foreach (var inst in Intermediate.EnumerateInstructions())
            {
                switch (inst.Opcode)
                {
                    case Opcode.ENTER:
                        var funcAddress = inst.Address;
                        // Functions at page boundaries may not start with an ENTER instruction, they have NOPs and a J before
                        // the ENTER to skip the page boundary.
                        // To solve those cases, we check if the ENTER comes after a LEAVE instruction, if it doesn't we use the address
                        // after the LEAVE as the function address, which should at least be correct for vanilla scripts
                        if (addressAfterLastLeaveInst != inst.Address)
                        {
                            funcAddress = addressAfterLastLeaveInst;
                        }

                        string funcName;
                        if (funcAddress == 0)
                        {
                            funcName = Intermediate.Name;
                        }
                        else
                        {
                            funcName = inst.GetEnterFunctionName() ?? $"func_{funcAddress}";
                        }

                        if (Functions.Count > 0)
                        {
                            Functions.Last().EndAddress = funcAddress;
                        }
                        Functions.Add(new(this, funcName) { StartAddress = funcAddress });
                        break;

                    case Opcode.LEAVE:
                        addressAfterLastLeaveInst = inst.Address + inst.ByteSize;
                        break;
                }
            }

            if (Functions.Count > 0)
            {
                Functions.Last().EndAddress = Intermediate.Code.Length;
            }
        }

        private void BuildControlFlowGraphs()
        {
            foreach (var func in Functions)
            {
                func.ControlFlowGraph = new ControlFlowGraphBuilder(func).BuildGraph();
            }
        }
    }
}
