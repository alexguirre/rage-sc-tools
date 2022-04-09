namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class SourceLocationTests
    {
        [Fact]
        public void TestEquality()
        {
            var a = new SourceLocation(1, 10, "test.sc");
            var b = new SourceLocation(2, 20, "test.sc");
            var c = a;

            Assert.True(a == c);
            Assert.False(a == b);

            Assert.False(a != c);
            Assert.True(a != b);
        }

        [Fact]
        public void TestComparison()
        {
            var a = new SourceLocation(1, 10, "test.sc");
            var b = new SourceLocation(2, 20, "test.sc");
            var c = a;
            var d = new SourceLocation(1, 15, "test.sc");

            Assert.True(a < b);
            Assert.True(a <= b);
            Assert.True(a <= d);
            Assert.True(a < d);

            Assert.True(a >= c);
            Assert.False(a > c);
            Assert.True(d > a);
            Assert.True(d >= a);
            Assert.True(b > a);
            Assert.True(b >= a);
        }

        [Fact]
        public void TestRangeContains()
        {
            var a = new SourceLocation(1, 15, "test.sc");
            var b = new SourceLocation(2, 10, "test.sc");
            var c = new SourceLocation(5, 10, "test.sc");

            var r = new SourceRange((1, 0, "test.sc"), (3, 20, "test.sc"));
            var r1 = new SourceRange((1, 10, "test.sc"), (2, 10, "test.sc"));
            var r2 = new SourceRange((4, 0, "test.sc"), (5, 10, "test.sc"));

            Assert.True(r.Contains(r.Start));
            Assert.True(r.Contains(r.End));

            Assert.True(r.Contains(a));
            Assert.True(r.Contains(b));
            Assert.False(r.Contains(c));

            Assert.True(r.Contains(r));
            Assert.True(r.Contains(r1));
            Assert.False(r.Contains(r2));
        }
    }
}
