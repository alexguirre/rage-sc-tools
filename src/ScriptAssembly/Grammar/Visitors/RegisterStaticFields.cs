namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using System.Diagnostics;
    using ScTools.GameFiles;

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
                var init = sf.staticFieldInitializer();
                ScriptValue initialValue = init switch
                {
                    null => default,
                    _ when init.@float() != null => new ScriptValue { AsFloat = ParseFloat.Visit(init.@float()) },
                    _ when init.integer() != null => new ScriptValue { AsUInt64 = ParseUnsignedInteger.Visit(init.integer()) },
                    _ => throw new InvalidOperationException()
                };

                var f = ParseFieldDecl.Visit(sf.fieldDecl(), registry);

                Debug.Assert(f.Type != null);

                if (initialValue.AsUInt64 != 0 && !(f.Type is AutoTypeDefintion))
                {
                    throw new InvalidOperationException("Only static fields of type AUTO can have initializers");
                }

                fields.Add(registry.RegisterStaticField(f.Name, f.Type, initialValue));
            }

            return fields.ToArray();
        }

        public static StaticFieldDefinition[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterStaticFields(registry)) ?? throw new ArgumentNullException(nameof(script));
    }
}
