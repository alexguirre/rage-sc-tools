namespace ScTools.Decompilation
{
    using System;
    using System.Linq;
    using System.Text;

    using ScTools.Decompilation.Intermediate;

    public class ControlFlowBlock
    {
        private InstructionIterator? firstInstruction;
        private InstructionIterator? lastInstruction;

        public Function Function { get; }

        /// <summary>
        /// Gets the address of the first instruction of this block.
        /// </summary>
        public int StartAddress { get; set; }
        /// <summary>
        /// Gets the address after the last instruction of this block.
        /// </summary>
        public int EndAddress { get; set; }

        public bool IsEmpty => StartAddress == EndAddress;
        public bool IsDelimited => StartAddress >= 0 && EndAddress >= 0;

        public ControlFlowBlock[] Successors { get; set; } = Array.Empty<ControlFlowBlock>();

        public InstructionIterator FirstInstruction
        {
            get
            {
                ThrowIfNotDelimited();
                ThrowIfEmpty();
                return firstInstruction ??= EnumerateInstructions().First();
            }
        }

        public InstructionIterator LastInstruction
        {
            get
            {
                ThrowIfNotDelimited();
                ThrowIfEmpty();
                return lastInstruction ??= EnumerateInstructions().Last();
            }
        }

        public ControlFlowBlock(Function function)
        {
            Function = function;
        }

        public InstructionEnumerator EnumerateInstructions()
        {
            ThrowIfNotDelimited();
            ThrowIfEmpty();
            return new(Function.Script.Intermediate.Code, StartAddress, EndAddress);
        }

        private void ThrowIfNotDelimited()
        {
            if (!IsDelimited)
            {
                throw new InvalidOperationException($"The control-flow block is not delimited ({nameof(StartAddress)}: {StartAddress}, {nameof(EndAddress)}: {EndAddress})");
            }
        }

        private void ThrowIfEmpty()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException($"The control-flow block is empty ({nameof(StartAddress)}: {StartAddress}, {nameof(EndAddress)}: {EndAddress})");
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ StartAddress: {0}, EndAddress: {1}, Instructions: [", StartAddress, EndAddress);
            if (IsDelimited && !IsEmpty)
            {
                sb.AppendJoin(", ", EnumerateInstructions().Select(inst => inst.Opcode.ToString()));
            }
            sb.Append("] }}");
            return sb.ToString();
        }
    }
}
