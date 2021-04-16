#nullable enable
namespace ScTools.ScriptAssembly
{
    using ScriptAssembly.Grammar;

    public interface IAssemblySource
    {
        // TODO: can we not depend on ANTLR types on this interface?
        public delegate void ConsumeLineDelegate(IAssemblySource source, ScAsmParser.LineContext line);

        public string FilePath { get; }

        public void Produce(DiagnosticsReport diagnostics, ConsumeLineDelegate consumeLine);
    }
}
