namespace ScTools.ScriptAssembly.CodeGen
{
    public readonly struct CodeGenOptions
    {
        public bool IncludeFunctionNames { get; }

        public CodeGenOptions(bool includeFunctionNames)
        {
            IncludeFunctionNames = includeFunctionNames;
        }
    }
}
