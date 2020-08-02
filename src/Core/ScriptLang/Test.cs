namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Antlr4.Runtime;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Grammar;
    using ScTools.ScriptLang.Semantics;

    public static class Test
    {
        const string Code = @"
SCRIPT_NAME test

STRUCT VEC2
    FLOAT X
    FLOAT Y
ENDSTRUCT

STRUCT RECT_DETAILS
    FLOAT X = 0.5
    FLOAT Y = 0.5
    FLOAT W = 0.1
    FLOAT H = 0.1
ENDSTRUCT

PROTO PROC DRAW_CALLBACK(myRectdas alpha)
PROTO FUNC INT GET_ALPHA_CALLBACK()

STRUCT CALLBACKS
    DRAW_CALLBACK       draw
    GET_ALPHA_CALLBACK  getAlpha
ENDSTRUCT

RECT_DETAILS myRect
CALLBACKS myCallbacks = <<DRAW_OTHER_STUFF, GET_ALPHA_VALUE>>

PROC MAIN()
    
    myCallbacks.draw = DRAW_OTHER_STUFF
    myCallbacks.getAlpha = GET_ALPHA_VALUE

    VEC3 playerPos = <<1.0, 2.0, 3.0>>
    
    VEC2 rectPos = <<0.5, 0.5>>

    INT a = 5
    INT b = 10
    INT c = a + b * b
    c = (a + b) * b
    c = a + (b * b)

    INT myVar = 10
    myVar = myVar + 5 * myVar

    INT intAdd = 10 + 5
    FLOAT floatAdd = 10.0 + 5.0
    FLOAT floatAdd2 = 10.0 + 5

    WHILE TRUE
        WAIT(0)

        DRAW_RECT(rectPos.X, rectPos.Y, 0.1, 0.1, 255, 0, 0, 255, FALSE)
        DRAW_SOMETHING(0, 255, 0)

        IF a
            DRAW_SOMETHING(0, 0, 255)
        ELSE
            DRAW_SOMETHING(255, 0, 0)
        ENDIF

        IF b
            a = a + 1
        ENDIF

        myCallbacks.draw(myCallbacks.getAlpha())
    ENDWHILE
ENDPROC

PROC DRAW_SOMETHING(INT r, INT g, INT b)
    DRAW_RECT(0.1, 0.1, 0.2, 0.2, r, g, b, GET_ALPHA_VALUE(), FALSE)
    RETURN
ENDPROC

FUNC INT GET_ALPHA_VALUE()
    RETURN 200
ENDFUNC

PROC DRAW_OTHER_STUFF(INT alpha)
    DRAW_RECT(0.6, 0.6, 0.2, 0.2, 100, 100, 20, alpha, FALSE)
ENDPROC

PROC RUN_SPLINE_CAM_ON_CHAR(PED_INDEX &TargetChar1, PED_INDEX& TargetChar2)
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
            Console.WriteLine(AstDotGenerator.Generate(root));
            Console.WriteLine();
            Console.WriteLine("===========================");

            root.Accept(new SimpleVisitorTest());

            Console.WriteLine();
            Console.WriteLine("===========================");

            const string FilePath = "test.sc";
            Diagnostics d = new Diagnostics();
            d.AddFrom(SyntaxChecker.Check(root, FilePath));

            var (scope, scopeDiagnostics) = ScopeBuilder.Explore(root, FilePath);
            d.AddFrom(scopeDiagnostics);

            Console.WriteLine($"Errors:   {d.HasErrors} ({d.Errors.Count()})");
            Console.WriteLine($"Warnings: {d.HasWarnings} ({d.Warnings.Count()})");
            foreach (var diagnostic in d.AllDiagnostics)
            {
                diagnostic.Print(Console.Out);
            }
            ;
        }

        private sealed class SimpleVisitorTest : AstVisitor
        {
            private readonly HashSet<string> foundTypes = new HashSet<string>();

            private void Check(Identifier id, Node node)
            {
                string typeName = id.Name;
                if (foundTypes.Add(typeName))
                {
                    Console.WriteLine($"{typeName}\t{node.Source}");
                }
            }

            public override void VisitType(Ast.Type node) => Check(node.Name, node);
            public override void VisitStructStatement(StructStatement node)
            {
                Check(node.Name, node);
                DefaultVisit(node);
            }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    Visit(n);
                }
            }

            public override void VisitBinaryExpression(BinaryExpression node)
            {
                if (node.Left is LiteralExpression && node.Right is LiteralExpression)
                {
                    var b = TypeOf.Expression(node, out var t);
                    Console.WriteLine($"expr '{node}' = type '{b}, {t}'");
                }
            }
        }
    }
}
