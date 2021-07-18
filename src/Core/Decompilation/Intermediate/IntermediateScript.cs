namespace ScTools.Decompilation.Intermediate
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class IntermediateScript
    {
        public string Name { get; set; } = string.Empty;
        public byte[] Code { get; set; } = Array.Empty<byte>();
        public Dictionary<int, int> LabelsOriginalAddressToIntermediateAddress { get; set; } = new Dictionary<int, int>();

        public InstructionEnumerator EnumerateInstructions() => new(Code);

        public void Dump(TextWriter writer)
        {
            foreach (var inst in EnumerateInstructions())
            {
                InstructionFormatter.Format(writer, inst);
                writer.WriteLine();
            }
        }
    }
}
