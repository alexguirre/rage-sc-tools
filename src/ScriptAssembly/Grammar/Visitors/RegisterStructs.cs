namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;

    public sealed class RegisterStructs : ScAsmBaseVisitor<StructDefinition[]>
    {
        private readonly Registry registry;

        private RegisterStructs(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override StructDefinition[] VisitScript([NotNull] ScAsmParser.ScriptContext context)
        {
            var queuedStructs = new Queue<(ScAsmParser.StructContext Ctx, int UnresolvedDependencies)>();

            foreach (var stat in context.statement())
            {
                if (stat.@struct() != null)
                {
                    queuedStructs.Enqueue((stat.@struct(), -1));
                }
            }

            int i = 0;
            StructDefinition[] structs = new StructDefinition[queuedStructs.Count];
            while (queuedStructs.Count > 0)
            {
                var (s, initialUnresolved) = queuedStructs.Dequeue();

                var fields = s.fieldDecl().Select(f => ParseFieldDecl.Visit(f, registry));

                int unresolved = fields.Count(f => f.Type == null);
                if (unresolved > 0)
                {
                    if (initialUnresolved == unresolved)
                    {
                        // we already went through the queue once, and the unresolved dependencies remained the same, must be undefined or circular
                        string error = fields.Aggregate("", (acc, f) => acc + (acc.Length > 0 ? ", " : "") + f.TypeName);
                        throw new InvalidOperationException($"Unknown types (undefined or circular): {error}");
                    }

                    queuedStructs.Enqueue((s, unresolved)); // still some unresolved fields, try again
                }
                else
                {
                    structs[i++] = registry.RegisterStruct(s.identifier().GetText(), fields.Select(f => new FieldDefinition(f.Name, f.Type)));
                }
            }

            return structs.ToArray();
        }

        public static StructDefinition[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterStructs(registry)) ?? throw new ArgumentNullException(nameof(script));
    }
}
