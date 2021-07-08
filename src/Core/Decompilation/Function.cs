namespace ScTools.Decompilation
{
    public class Function
    {
        public string Name { get; set; }
        /// <summary>
        /// Gets the address of the first instruction of this function.
        /// </summary>
        public int StartAddress { get; set; }
        /// <summary>
        /// Gets the address after the last instruction of this function.
        /// </summary>
        public int EndAddress { get; set; }

        public Function(string name) => Name = name;
    }
}
