namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using ScTools.ScriptAssembly.Types;
    using ScTools.GameFiles;

    public sealed class RegisterStructs : ScAsmBaseVisitor<StructType[]>
    {
        private readonly Registry registry;

        private RegisterStructs(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override StructType[] VisitScript([NotNull] ScAsmParser.ScriptContext context)
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
            StructType[] structs = new StructType[queuedStructs.Count];
            while (queuedStructs.Count > 0)
            {
                var (s, initialUnresolved) = queuedStructs.Dequeue();

                var fields = s.fieldDeclWithInitializer().Select(decl =>
                {
                    var (name, typeName, type) = ParseFieldDecl.Visit(decl.fieldDecl(), registry);

                    ScriptValue? initialValue = null;
                    if (type != null)
                    {
                        if (type is AutoType)
                        {
                            initialValue = decl switch
                            {
                                _ when decl.@float() != null => new ScriptValue { AsFloat = ParseFloat.Visit(decl.@float()) },
                                _ when decl.integer() != null => new ScriptValue { AsUInt64 = ParseUnsignedInteger.Visit(decl.integer()) },
                                _ => null,
                            };
                        }
                        else if (decl.@float() != null || decl.integer() != null)
                        {
                            throw new InvalidOperationException("Only fields of type AUTO can have initializers");
                        }
                    }

                    return (Name: name, TypeName: typeName, Type: type, InitialValue: initialValue);
                });

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
                    structs[i++] = registry.Types.RegisterStruct(s.identifier().GetText(), fields.Select(f => new StructField(f.Name, f.Type, f.InitialValue)));
                }
            }

            return structs.ToArray();
        }

        public static StructType[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterStructs(registry)) ?? throw new ArgumentNullException(nameof(script));
    }
}
