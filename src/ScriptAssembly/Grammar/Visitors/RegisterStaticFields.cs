namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;

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

            foreach (var stat in context.statement())
            {
                var sf = stat.staticFieldDecl();
                if (sf != null)
                {
                    if (sf.staticFieldInitializer() != null) throw new NotImplementedException("staticFieldInitializer");

                    var f = sf.fieldDecl();

                    var t = f.type();
                    var arrayType = t.type();
                    bool isArray = arrayType != null;
                    long arrayLength = isArray ? ParseInteger.Visit(t.integer()) : 0;

                    if (arrayLength < 0)
                    {
                        throw new InvalidOperationException("Array length is negative");
                    }

                    var typeName = isArray ? arrayType.identifier().GetText() : f.type().identifier().GetText();
                    var typeDef = isArray ? registry.FindOrRegisterArray(typeName, (uint)arrayLength) : registry.FindType(typeName);
                    fields.Add(registry.RegisterStaticField(f.identifier().GetText(), typeDef));
                }
            }

            return fields.ToArray();
        }

        public static StaticFieldDefinition[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterStaticFields(registry)) ?? throw new ArgumentNullException(nameof(script));
    }
}
