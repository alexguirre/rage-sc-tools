namespace ScTools.ScriptAssembly.Disassembly
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class Location
    {
        public Location Previous { get; set; }
        public Location Next { get; set; }
        public uint IP { get; set; }
        public string Label { get; set; }
    
        public Location(uint ip, string label)
        {
            IP = ip;
            Label = label;
        }

        public IEnumerable<Location> EnumerateForward(bool skipThis = false)
        {
            var curr = skipThis ? Next : this;
            while (curr != null)
            {
                yield return curr;
                curr = curr.Next;
            }
        }

        public IEnumerable<Location> EnumerateBackwards(bool skipThis = false)
        {
            var curr = skipThis ? Previous : this;
            while (curr != null)
            {
                yield return curr;
                curr = curr.Previous;
            }
        }
    }

    public sealed class EmptyLocation : Location
    {
        public EmptyLocation(uint ip, string label) : base(ip, label)
        {
        }
    }

    public sealed class InstructionLocation : Location
    {
        private Operand[] operands;

        public Opcode Opcode { get; set; }
        public Operand[] Operands
        {
            get => operands;
            set => operands = value ?? throw new ArgumentNullException(nameof(value));
        }

        public InstructionLocation(uint ip, Opcode opcode) : base(ip, null)
        {
            Opcode = opcode;
            Operands = Array.Empty<Operand>();
        }
    }

    public sealed class HLInstructionLocation : Location
    {
        private Operand[] operands;

        public HighLevelInstruction.UniqueId InstructionId { get; set; }
        public Operand[] Operands
        {
            get => operands;
            set => operands = value ?? throw new ArgumentNullException(nameof(value));
        }

        public HLInstructionLocation(uint ip, HighLevelInstruction.UniqueId instructionId) : base(ip, null)
        {
            InstructionId = instructionId;
            Operands = Array.Empty<Operand>();
        }
    }

    public static class LocationExtensions
    {
        private static T Next<T>(this Location location) where T : Location
        {
            location = location ?? throw new ArgumentNullException(nameof(location));

            return (T)location.EnumerateForward(true).SkipWhile(l => !(l is T)).FirstOrDefault();
        }

        private static T Previous<T>(this Location location) where T : Location
        {
            location = location ?? throw new ArgumentNullException(nameof(location));

            return (T)location.EnumerateBackwards(true).SkipWhile(l => !(l is T)).FirstOrDefault();
        }

        public static EmptyLocation NextEmpty(this Location location) => location.Next<EmptyLocation>();
        public static InstructionLocation NextInstruction(this Location location) => location.Next<InstructionLocation>();
        public static HLInstructionLocation NextHLInstruction(this Location location) => location.Next<HLInstructionLocation>();


        public static EmptyLocation PreviousEmpty(this Location location) => location.Previous<EmptyLocation>();
        public static InstructionLocation PreviousInstruction(this Location location) => location.Previous<InstructionLocation>();
        public static HLInstructionLocation PreviousHLInstruction(this Location location) => location.Previous<HLInstructionLocation>();
    }
}
