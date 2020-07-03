namespace ScTools.ScriptAssembly.Grammar.Visitors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Misc;
    using ScTools.ScriptAssembly.Definitions;

    public sealed class RegisterFunctions : ScAsmBaseVisitor<FunctionDefinition[]>
    {
        private readonly Registry registry;

        private RegisterFunctions(Registry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public override FunctionDefinition[] VisitScript([NotNull] ScAsmParser.ScriptContext context)
        {
            var funcs = new List<FunctionDefinition>();

            var bodyVisitor = new BodyVisitor();
            foreach (var f in context.statement().Select(stat => stat.function()).Where(f => f != null))
            {
                var name = f.identifier().GetText();
                var naked = f.K_NAKED() != null;
                var args = f.functionArgList()?.fieldDecl().Select(f => ParseFieldDecl.Visit(f, registry)) ?? Enumerable.Empty<(string, string, TypeDefinition)>();
                var locals = f.functionLocalDecl().Select(l => ParseFieldDecl.Visit(l.fieldDecl(), registry));
                var statements = f.functionBody().Select(b => b.Accept(bodyVisitor));

                funcs.Add(registry.RegisterFunction(name, naked, 
                                                    args.Select(a => new FieldDefinition(a.Name,a.Type)),
                                                    locals.Select(l => new FieldDefinition(l.Name, l.Type)),
                                                    statements));
            }

            return funcs.ToArray();
        }

        public static FunctionDefinition[] Visit(ScAsmParser.ScriptContext script, Registry registry)
            => script?.Accept(new RegisterFunctions(registry)) ?? throw new ArgumentNullException(nameof(script));



        private sealed class BodyVisitor : ScAsmBaseVisitor<FunctionDefinition.Statement>
        {
            public override FunctionDefinition.Statement VisitFunctionBody([NotNull] ScAsmParser.FunctionBodyContext context)
                => new FunctionDefinition.Statement(
                        label: context.label()?.identifier().GetText(),
                        mnemonic: context.instruction()?.identifier().GetText(),
                        operands: context.instruction()?.operandList().Accept(ParseOperands.Instance));
        }
    }
}
