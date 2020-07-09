namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;
    using System.Diagnostics;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Types;

    public sealed class RegisterStaticFields : ScAsmBaseVisitor<StaticFieldDefinition[]>
    {
        private readonly Registry registry;

        private RegisterStaticFields(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override StaticFieldDefinition[] VisitScript([NotNull] ScAsmParser.ScriptContext context)
         => DoParse<StaticFieldDefinition>(registry, context.statement()
                                                        .Select(stat => stat.statics())
                                                        .Where(s => s != null)
                                                        .SelectMany(s => s.fieldDeclWithInitializer())
                                                        .Where(s => s != null));

        internal static T[] DoParse<T>(Registry registry, IEnumerable<ScAsmParser.FieldDeclWithInitializerContext> decls) where T : StaticFieldDefinition
        {
            var fields = new List<T>();

            foreach (var decl in decls)
            {
                ScriptValue initialValue = decl switch
                {
                    _ when decl.@float() != null => new ScriptValue { AsFloat = ParseFloat.Visit(decl.@float()) },
                    _ when decl.integer() != null => new ScriptValue { AsUInt64 = ParseUnsignedInteger.Visit(decl.integer()) },
                    _ => default
                };

                var f = ParseFieldDecl.Visit(decl.fieldDecl(), registry);

                Debug.Assert(f.Type != null);

                if (initialValue.AsUInt64 != 0 && !(f.Type is AutoType))
                {
                    throw new InvalidOperationException("Only static fields of type AUTO can have initializers");
                }

                if (typeof(T) == typeof(StaticFieldDefinition))
                {
                    fields.Add((T)registry.RegisterStaticField(f.Name, f.Type, initialValue));
                }
                else if (typeof(T) == typeof(ArgDefinition))
                {
                    fields.Add((T)(StaticFieldDefinition)registry.RegisterArg(f.Name, f.Type, initialValue));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return fields.ToArray();
        }

        public static StaticFieldDefinition[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterStaticFields(registry)) ?? throw new ArgumentNullException(nameof(script));
    }

    public sealed class RegisterArgs : ScAsmBaseVisitor<ArgDefinition[]>
    {
        private readonly Registry registry;

        private RegisterArgs(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }
        public override ArgDefinition[] VisitScript([NotNull] ScAsmParser.ScriptContext context)
         => RegisterStaticFields.DoParse<ArgDefinition>(registry, context.statement()
                                                                    .Select(stat => stat.args())
                                                                    .Where(s => s != null)
                                                                    .SelectMany(s => s.fieldDeclWithInitializer())
                                                                    .Where(s => s != null));

        public static ArgDefinition[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterArgs(registry)) ?? throw new ArgumentNullException(nameof(script));
    }
}
