namespace ScTools.ScriptAssembly.Types
{
    public sealed class AutoType : TypeBase
    {
        public override uint SizeOf => 1;

        private AutoType() : base("AUTO")
        {
        }

        public static AutoType Instance { get; } = new AutoType();
    }
}
