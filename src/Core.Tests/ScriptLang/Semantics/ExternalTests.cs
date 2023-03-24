namespace ScTools.Tests.ScriptLang.Semantics;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScTools.ScriptLang.Types;
using System.IO;
using ScTools.ScriptLang.Ast.Declarations;

public class ExternalTests : SemanticsTestsBase
{
    [Theory(Timeout = 5000)]
    [MemberData(nameof(GetSourceFiles))]
    public void AnalyzeExternalSourceFile(string fileName, string sourceFilePath)
    {
        var source = File.ReadAllText(sourceFilePath);

        var (s, ast) = AnalyzeAndAst(source);

        var err = s.Diagnostics.Errors.Where(d => d is { Tag: DiagnosticTag.Error, Code: not (int)ErrorCode.SemanticUndefinedSymbol });
        DoesNotContain(s.Diagnostics.Errors, d => d is { Tag: DiagnosticTag.Error, Code: not (int)ErrorCode.SemanticUndefinedSymbol });
    }

    public static IEnumerable<object[]> GetSourceFiles()
    {
        const string Path = "D:\\vm\\sharedwrite\\gta-source-main\\gta-source-main";
        return Directory.GetFiles(Path, "*.sch").Select(p => new object[] { System.IO.Path.GetFileName(p),  p });
    }

    private new static (SemanticsAnalyzer Semantics, CompilationUnit Ast) AnalyzeAndAst(string source, IUsingResolver? usingResolver = null)
    {
        var p = ParserFor(source);

        var u = p.ParseCompilationUnit();
        var s = new SemanticsAnalyzer(p.Diagnostics, usingResolver);
        /*var entIdxTyDecl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("ENTITY_INDEX"), null);
        var pedIdxTyDecl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("PED_INDEX"), new(Token.Identifier("ENTITY_INDEX")));
        var vehIdxTyDecl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("VEHICLE_INDEX"), new(Token.Identifier("ENTITY_INDEX")));
        var objIdxTyDecl = new NativeTypeDeclaration(TokenKind.NATIVE.Create(), Token.Identifier("OBJECT_INDEX"), new(Token.Identifier("ENTITY_INDEX")));
        s.AddSymbol(entIdxTyDecl);
        s.AddSymbol(pedIdxTyDecl);
        s.AddSymbol(vehIdxTyDecl);
        s.AddSymbol(objIdxTyDecl);*/
        u.Accept(s);
        return (s, u);
    }
}
