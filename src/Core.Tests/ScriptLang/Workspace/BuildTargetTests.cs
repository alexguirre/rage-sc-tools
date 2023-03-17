namespace ScTools.Tests.ScriptLang.Workspace;

using ScTools.GameFiles;
using ScTools.ScriptAssembly.Targets.Five;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Workspace;

public class BuildTargetTests
{
    [Theory]
    [MemberData(nameof(GetAllBuildTargetCombinations))]
    public void Parse(BuildTarget expected)
    {
        var str = expected.ToString();
        True(BuildTarget.TryParse(str, out var parsed));
        Equal(expected, parsed);
    }

    private static IEnumerable<object[]> GetAllBuildTargetCombinations()
    {
        foreach (var game in Enum.GetValues<Game>())
        {
            foreach (var platform in Enum.GetValues<Platform>())
            {
                yield return new object[] { new BuildTarget(game, platform) };
            }   
        }
    }
}
