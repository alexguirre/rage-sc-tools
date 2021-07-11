namespace ScTools.Decompilation
{
    public class GlobalBlock
    {
        public DecompiledScript Owner { get; set; }
        public int BlockIndex => (int)Owner.Script.GlobalsBlock;
        public int Size => (int)Owner.Script.GlobalsLength;

        public GlobalBlock(DecompiledScript owner)
        {
            if (owner.Script.GlobalsLengthAndBlock == 0)
            {
                throw new System.ArgumentException("Script does not have a global block", nameof(owner));
            }

            Owner = owner;
        }
    }
}
