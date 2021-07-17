namespace ScTools.Decompilation.Intermediate
{
    using System;
    using System.IO;

    public class IntermediateScript
    {
        public string Name { get; set; } = string.Empty;
        public byte[] Code { get; set; } = Array.Empty<byte>();

        public InstructionEnumerator EnumerateInstructions() => new(Code);

        public void Dump(TextWriter writer)
        {
            foreach (var inst in EnumerateInstructions())
            {
                if (inst.Opcode is Opcode.LABEL)
                {
                    writer.Write("{0:D8} ({1:D8}):", inst.Address, inst.GetLabelAddress());
                }
                else
                {
                    writer.Write("\t");
                    InstructionFormatter.Format(writer, inst);
                    writer.WriteLine();
                }
            }
        }
    }
}
