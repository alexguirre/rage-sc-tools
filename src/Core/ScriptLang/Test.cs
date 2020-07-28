namespace ScTools.ScriptLang
{
    using System;
    using System.Linq;

    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Grammar;

    public static class Test
    {
        const string Code = @"
SCRIPT_NAME test

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
    
    PED_INDEX playerPed 
    playerPed = <<1>>

    WHILE TRUE
        WAIT(0)

        DRAW_RECT(rectPos.X, rectPos.Y, 0.1, 0.1, 255, 0, 0, 255)
    ENDWHILE
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
        }
    }
}
