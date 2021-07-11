namespace ScTools.Decompilation
{
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;

    public class DecompiledScript
    {
        public Script Script { get; }
        public byte[] Code { get; }
        public List<Function> Functions { get; } = new();

        public DecompiledScript(Script sc)
        {
            Script = sc;
            Code = sc.MergeCodePages();
            IdentifyFunctions();
        }

        private void IdentifyFunctions()
        {
            Functions.Clear();

            var addressAfterLastLeaveInst = 0;
            foreach (var inst in new InstructionEnumerator(Code))
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

                        var funcNameLen = inst.Bytes[4];
                        var funcName = funcNameLen > 0 ?
                                            System.Text.Encoding.UTF8.GetString(inst.Bytes.Slice(5, funcNameLen - 1)) :
                                            (funcAddress == 0 ? Script.Name : $"func_{funcAddress}");
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
                Functions.Last().EndAddress = Code.Length;
            }
        }
    }
}
