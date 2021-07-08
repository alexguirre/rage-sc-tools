namespace ScTools.Decompilation
{
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;

    public class Decompiler
    {
        private readonly byte[] code;
        private readonly List<Function> functions = new();

        public Script Script { get; }

        public Decompiler(Script sc)
        {
            Script = sc;
            code = sc.MergeCodePages();
        }

        private void IdentifyFunctions()
        {
            functions.Clear();

            var addressAfterLastLeaveInst = 0;
            for (var it = InstructionIterator.Begin(code); it; it = it.Next())
            {
                switch (it.Opcode)
                {
                    case Opcode.ENTER:
                        var funcAddress = it.Address;
                        // Functions at page boundaries may not start with an ENTER instruction, they have NOPs and a J before
                        // the ENTER to skip the page boundary.
                        // To solve those cases, we check if the ENTER comes after a LEAVE instruction, if it doesn't we use the address
                        // after the LEAVE as the function address, which should at least be correct for vanilla scripts
                        if (addressAfterLastLeaveInst != it.Address)
                        {
                            funcAddress = addressAfterLastLeaveInst;
                        }

                        var funcNameLen = it.Bytes[4];
                        var funcName = funcNameLen > 0 ?
                                            System.Text.Encoding.UTF8.GetString(it.Bytes.Slice(5, funcNameLen - 1)) :
                                            (funcAddress == 0 ? Script.Name : $"func_{funcAddress}");
                        if (functions.Count > 0)
                        {
                            functions.Last().EndAddress = funcAddress;
                        }
                        functions.Add(new(funcName) { StartAddress = funcAddress });
                        break;

                    case Opcode.LEAVE:
                        addressAfterLastLeaveInst = it.Address + it.Bytes.Length;
                        break;
                }
            }

            if (functions.Count > 0)
            {
                functions.Last().EndAddress = code.Length;
            }
        }
    }
}
