namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Antlr4.Runtime;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Grammar;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static class Test
    {
        const string Code = @"
SCRIPT_NAME test

STRUCT VEC2
    FLOAT x
    FLOAT y
ENDSTRUCT

PROTO PROC DRAW_CALLBACK(INT alpha)
PROTO FUNC INT GET_ALPHA_CALLBACK()

STRUCT CALLBACKS
    DRAW_CALLBACK       draw
    GET_ALPHA_CALLBACK  getAlpha
ENDSTRUCT

CALLBACKS myCallbacks = <<DRAW_OTHER_STUFF, GET_ALPHA_VALUE>>

PROC MAIN()
    VEC2 pos = <<0.5, 0.5>>
    INT a = 10
    INT b = 5 + a * 2

    WHILE TRUE
        WAIT(0)

        DRAW_RECT(pos.x, pos.y, 0.1, 0.1, 255, 0, 0, 255, FALSE)

        myCallbacks.draw(myCallbacks.getAlpha())
    ENDWHILE
ENDPROC

FUNC INT GET_ALPHA_VALUE()
    RETURN 200
ENDFUNC

PROC DRAW_OTHER_STUFF(INT alpha)
    DRAW_RECT(0.6, 0.6, 0.2, 0.2, 100, 100, 20, alpha, FALSE)
ENDPROC

// tmp procs until there are native command definitions
PROC DRAW_RECT(FLOAT x, FLOAT y, FLOAT w, FLOAT h, INT r, INT g, INT b, INT a, BOOL unk)
ENDPROC
PROC WAIT(INT ms)
ENDPROC
";

        public static void DoTest()
        {
            AntlrInputStream inputStream = new AntlrInputStream(Code);

            ScLangLexer lexer = new ScLangLexer(inputStream);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            ScLangParser parser = new ScLangParser(tokens);

            Root root = (Root)parser.script().Accept(new AstBuilder());

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(AstDotGenerator.Generate(root));
            Console.WriteLine();
            Console.WriteLine("===========================");

            const string FilePath = "test.sc";
            DiagnosticsReport d = new DiagnosticsReport();
            d.AddFrom(SyntaxChecker.Check(root, FilePath));

            var (diagnostics, symbols) = SemanticAnalysis.Visit(root);
            d.AddFrom(diagnostics);

            Console.WriteLine("===========================");
            
            Console.WriteLine($"Errors:   {d.HasErrors} ({d.Errors.Count()})");
            Console.WriteLine($"Warnings: {d.HasWarnings} ({d.Warnings.Count()})");
            foreach (var diagnostic in d.AllDiagnostics)
            {
                diagnostic.Print(Console.Out);
            }

            foreach (var s in symbols.Symbols)
            {
                if (s is TypeSymbol t && t.Type is StructType struc)
                {
                    Console.WriteLine($"  > '{t.Name}' Size = {struc.SizeOf}");
                }
            }
            ;
        }
    }
}
