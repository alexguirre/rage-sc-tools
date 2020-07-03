namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using System.Diagnostics;

    public sealed class RegisterStaticFields : ScAsmBaseVisitor<StaticFieldDefinition[]>
    {
        private readonly Registry registry;

        private RegisterStaticFields(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override StaticFieldDefinition[] VisitScript([NotNull] ScAsmParser.ScriptContext context)
        {
            var fields = new List<StaticFieldDefinition>();

            foreach (var sf in context.statement().Select(stat => stat.staticFieldDecl()).Where(sf => sf != null))
            {
                if (sf.staticFieldInitializer() != null) throw new NotImplementedException("staticFieldInitializer");

                var f = ParseFieldDecl.Visit(sf.fieldDecl(), registry);

                Debug.Assert(f.Type != null);

                fields.Add(registry.RegisterStaticField(f.Name, f.Type));
            }

            return fields.ToArray();
        }

        public static StaticFieldDefinition[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterStaticFields(registry)) ?? throw new ArgumentNullException(nameof(script));
    }
}
