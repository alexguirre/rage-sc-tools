namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.AstOld;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Binding;

    using Xunit;

    public class EvaluatorTests
    {
        [Fact]
        public void TestBinaryExpression()
        {
            const int Left = -5, Right = 3;

            var expr = new BoundBinaryExpression(
                new BoundIntLiteralExpression(Left),
                new BoundIntLiteralExpression(Right),
                BinaryOperator.Add
            );

            var result = Evaluator.Evaluate(expr);

            Assert.Single(result);
            Assert.Equal(Left + Right, result[0].AsInt32);
        }

        [Fact]
        public void TestComplexExpression()
        {
            const bool Expected = (-5 * 3 + 2 * (2 | 4)) == 4 && 3.0f != (8.0f / 2.0f);
            var comp = new Compilation();
            comp.SetMainModule(new StringReader(@"
                BOOL dummy = (-5 * 3 + 2 * (2 | 4)) == 4 AND 3.0 <> (8.0 / 2.0)

                PROC MAIN()
                ENDPROC
            "));
            comp.Compile();

            var diagnostics = comp.GetAllDiagnostics();
            Assert.False(diagnostics.HasErrors);
            Assert.NotNull(comp.CompiledScript.Statics);
            Assert.Single(comp.CompiledScript.Statics);
            Assert.Equal(Expected, comp.CompiledScript.Statics[0].AsInt64 == 1);
        }
    }
}
