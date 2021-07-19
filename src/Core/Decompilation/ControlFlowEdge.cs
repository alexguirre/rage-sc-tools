namespace ScTools.Decompilation
{
    public enum ControlFlowEdgeKind
    {
        /// <summary>
        /// Edge traversed always, unconditionally.
        /// </summary>
        Unconditional,

        /// <summary>
        /// Edge traversed when the condition of a <see cref="Intermediate.Opcode.JZ"/> instruction is zero (false).
        /// </summary>
        IfFalse,
        /// <summary>
        /// Edge traversed when the condition of a <see cref="Intermediate.Opcode.JZ"/> instruction is not zero (true).
        /// </summary>
        IfTrue,

        /// <summary>
        /// Edge is a case of a <see cref="Intermediate.Opcode.SWITCH"/> instruction.
        /// </summary>
        SwitchCase,
    }

    public readonly struct ControlFlowEdge
    {
        /// <summary>
        /// Gets the block this edge originates from.
        /// </summary>
        public ControlFlowBlock From { get; init; }
        /// <summary>
        /// Gets the block this edge ends in.
        /// </summary>
        public ControlFlowBlock To { get; init; }
        public ControlFlowEdgeKind Kind { get; init; }
        /// <summary>
        /// If <see cref="Kind"/> is <see cref="ControlFlowEdgeKind.SwitchCase"/>, gets the value needed to go through this edge.
        /// A <c>null</c> value represents the default case.
        /// </summary>
        public int? SwitchCaseValue { get; init; }
    }
}
