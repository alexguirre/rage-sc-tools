﻿namespace ScTools.Decompilation
{
    using ScTools.ScriptAssembly;

    public class Function
    {
        public DecompiledScript Script { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// Gets the address of the first instruction of this function.
        /// </summary>
        public int StartAddress { get; set; }
        /// <summary>
        /// Gets the address after the last instruction of this function.
        /// </summary>
        public int EndAddress { get; set; }
        public ControlFlowGraph? ControlFlowGraph { get; set; }

        public Function(DecompiledScript script, string name) => (Script, Name) = (script, name);

        public InstructionEnumerator EnumerateInstructions() => new(Script.Code, StartAddress, EndAddress);
    }
}
