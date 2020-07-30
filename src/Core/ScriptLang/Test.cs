namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;

    using Antlr4.Runtime;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Grammar;

    public static class Test
    {
        const string Code = @"
SCRIPT_NAME test

SCRIPT_NAME test2

STRUCT VEC3
    FLOAT X
    FLOAT Y
    FLOAT Z
ENDSTRUCT

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

RECT_DETAILS myRect

PROC MAIN()
    
    VEC3 playerPos = <<1.0, 2.0, 3.0>>
    
    VEC2 rectPos = <<0.5, 0.5>>

    INT a = 5
    INT b = 10
    INT c = a + b * b
    c = (a + b) * b
    c = a + (b * b)

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
    ENDWHILE
ENDPROC

PROC DRAW_SOMETHING(INT r, INT g, INT b)
    DRAW_RECT(0.1, 0.1, 0.2, 0.2, r, g, b, GET_ALPHA_VALUE(), FALSE)
    RETURN
ENDPROC

FUNC INT GET_ALPHA_VALUE()
    RETURN 255
ENDFUNC

PROC RUN_SPLINE_CAM_ON_CHAR(PED_INDEX &TargetChar1, PED_INDEX& TargetChar2)
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

            Diagnostics d = new Diagnostics();
            new SimpleVerifier(d, "test.sc", root).Verify();

            foreach (var diagnostic in d.AllDiagnostics)
            {
                diagnostic.Print(Console.Out);
            }
        }

        private sealed class SimpleVerifier : AstVisitor
        {
            private readonly Diagnostics diagnostics;
            private readonly string filePath;
            private readonly Root root;
            private bool foundScriptName;

            public SimpleVerifier(Diagnostics diagnostics, string filePath, Root root)
                => (this.diagnostics, this.filePath, this.root) = (diagnostics, filePath, root);

            public void Verify()
            {
                Visit(root);

                if (!foundScriptName)
                {
                    diagnostics.AddWarning(filePath, "Missing SCRIPT_NAME statement", root.Source);
                }
            }

            public override void VisitScriptNameStatement(ScriptNameStatement node)
            {
                if (foundScriptName)
                {
                    diagnostics.AddError(filePath, "SCRIPT_NAME statement is repeated", node.Source);
                }
                else
                {
                    foundScriptName = true;
                }
            }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    Visit(n);
                }
            }
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

            public override void VisitBasicType(BasicType node) => Check(node.Name, node);
            public override void VisitRefType(RefType node) => Check(node.Name, node);
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
        }
    }
}
