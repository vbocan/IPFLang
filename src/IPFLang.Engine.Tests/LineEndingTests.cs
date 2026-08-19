using IPFLang.Evaluator;
using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    /// <summary>
    /// A script does not necessarily arrive with the host platform's line endings. It may be
    /// read from a database column, an HTTP request body, or a file authored on a different
    /// operating system. The parser must accept all three conventions identically.
    ///
    /// These tests build their sources with explicit escapes rather than raw string literals,
    /// because a raw literal inherits whatever line endings this file happens to be stored with.
    /// </summary>
    public class LineEndingTests
    {
        private const string Crlf = "COMPUTE FEE FilingFee\r\nYIELD 320\r\nYIELD 80\r\nENDCOMPUTE";
        private const string Lf = "COMPUTE FEE FilingFee\nYIELD 320\nYIELD 80\nENDCOMPUTE";
        private const string Cr = "COMPUTE FEE FilingFee\rYIELD 320\rYIELD 80\rENDCOMPUTE";

        [Theory]
        [InlineData(Crlf)]
        [InlineData(Lf)]
        [InlineData(Cr)]
        public void ParsesRegardlessOfLineEnding(string source)
        {
            var parser = new DslParser();

            Assert.True(parser.Parse(source), $"Parse failed: {string.Join(" | ", parser.GetErrors().Select(e => e.Item2))}");
            Assert.Empty(parser.GetErrors());

            var fee = Assert.Single(parser.GetFees());
            Assert.Equal("FilingFee", fee.Name);
        }

        [Theory]
        [InlineData(Crlf)]
        [InlineData(Lf)]
        [InlineData(Cr)]
        public void ComputesTheSameAmountRegardlessOfLineEnding(string source)
        {
            var calculator = new DslCalculator(new DslParser());

            Assert.True(calculator.Parse(source));
            var (mandatory, optional, _, _) = calculator.Compute(new List<IPFValue>());

            Assert.Equal(400M, mandatory);
            Assert.Equal(0M, optional);
        }

        [Fact]
        public void ParsesMixedLineEndingsWithinOneScript()
        {
            // Concatenating fragments from different sources is a realistic way to end up
            // with a script that mixes conventions.
            var source = "COMPUTE FEE FilingFee\r\nYIELD 320\nYIELD 80\rENDCOMPUTE";
            var calculator = new DslCalculator(new DslParser());

            Assert.True(calculator.Parse(source));
            var (mandatory, _, _, _) = calculator.Compute(new List<IPFValue>());

            Assert.Equal(400M, mandatory);
        }

        [Theory]
        [InlineData(Crlf)]
        [InlineData(Lf)]
        [InlineData(Cr)]
        public void ReportsAccurateLineNumbersRegardlessOfLineEnding(string prefix)
        {
            // An error on the final line must be reported as line 4, not line 1, whichever
            // convention the caller used.
            var newline = prefix.Contains("\r\n") ? "\r\n" : prefix.Contains('\n') ? "\n" : "\r";
            var source = string.Join(newline, "COMPUTE FEE FilingFee", "YIELD 320", "ENDCOMPUTE", "THIS IS NOT VALID SYNTAX");

            var parser = new DslParser();

            Assert.False(parser.Parse(source));
            Assert.Contains(parser.GetErrors(), e => e.Item2.Contains("Line 4"));
        }
    }
}
