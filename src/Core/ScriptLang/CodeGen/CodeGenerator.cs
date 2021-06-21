namespace ScTools.ScriptLang.CodeGen
{
    using System.IO;

    using ScTools.ScriptLang.Ast;

    public class CodeGenerator
    {
        public TextWriter Sink { get; }
        public Program Program { get; }
        public DiagnosticsReport Diagnostics { get; }

        public CodeGenerator(TextWriter sink, Program program, DiagnosticsReport diagnostics)
            => (Sink, Program, Diagnostics)  = (sink, program, diagnostics);

        public bool Generate()
        {
            if (Diagnostics.HasErrors)
            {
                return false;
            }

            Allocator.AllocateVars(Program, Diagnostics);
            if (Diagnostics.HasErrors)
            {
                return false;
            }

            EmitDirectives();
            return true;
        }

        private void EmitDirectives()
        {
            Sink.WriteLine("\t.script_name {0}", Program.ScriptName);
            Sink.WriteLine("\t.script_hash {0}", Program.ScriptHash);
        }
    }
}
