using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class ReturnTests
    {
        [Fact]
        public void TestReturns()
        {
            string text =
            """
            # Define some returns
            RETURN ClaimLimits AS '10 claims included; additional fee as of 11'
            RETURN ExReqDate AS 'Examination request due within 7 years after filing'
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = p.GetReturns().ToArray();
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal("ClaimLimits", result[0].Symbol);
            Assert.Equal("10 claims included; additional fee as of 11", result[0].Text);
            Assert.Equal("ExReqDate", result[1].Symbol);
            Assert.Equal("Examination request due within 7 years after filing", result[1].Text);
        }
    }
}
