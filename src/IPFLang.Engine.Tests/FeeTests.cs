using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class FeeTests
    {
        [Fact]
        public void TestFeeWithYields()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            YIELD 320 IF EntityType EQUALS NormalEntity
            YIELD 128 IF EntityType EQUALS SmallEntity
            YIELD 64 IF EntityType EQUALS MicroEntity
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = p.GetFees().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("BasicNationalFee", result.Name);
            Assert.Single(result.Cases);
            var FeeCase = result.Cases[0];
            Assert.IsType<DslFeeCase>(FeeCase);
            var FeeCase1 = (DslFeeCase)result.Cases[0];
            Assert.Empty(FeeCase1.Condition);
            Assert.Equal(3, FeeCase1.Yields.Count);
        }

        [Fact]
        public void TestFeeWithCases()
        {
            string text =
            """
            COMPUTE FEE SearchFee
            CASE SituationType EQUALS PreparedIPEA AS
            	YIELD 0 IF EntityType EQUALS NormalEntity
            	YIELD 0 IF EntityType EQUALS SmallEntity
            	YIELD 0 IF EntityType EQUALS MicroEntity
            ENDCASE
            CASE SituationType EQUALS PaidAsISA OR SituationType EQUALS DeliveredAsISA AS
            	YIELD 140 IF EntityType EQUALS NormalEntity
            	YIELD 56 IF EntityType EQUALS SmallEntity
            ENDCASE
            CASE SituationType EQUALS PreparedISA AS
            	YIELD 540 IF EntityType EQUALS NormalEntity
            ENDCASE
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = p.GetFees().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("SearchFee", result.Name);
            Assert.Equal(3, result.Cases.Count);
            var FeeCase = result.Cases[0];
            Assert.IsType<DslFeeCase>(FeeCase);
            var FeeCase1 = (DslFeeCase)result.Cases[0];
            Assert.Equal(3, FeeCase1.Condition.Count());
            Assert.Equal(3, FeeCase1.Yields.Count);
            var FeeCase2 = (DslFeeCase)result.Cases[1];
            Assert.Equal(7, FeeCase2.Condition.Count());
            Assert.Equal(2, FeeCase2.Yields.Count);
            var FeeCase3 = (DslFeeCase)result.Cases[2];
            Assert.Equal(3, FeeCase3.Condition.Count());
            Assert.Single(FeeCase3.Yields);
        }

        [Fact]
        public void TestFeeWithCasesAndYields()
        {
            string text =
            """
            COMPUTE FEE ExaminationFee
            CASE SituationType EQUALS PreparedIPEA AS
            	YIELD 0 IF EntityType EQUALS NormalEntity
            	YIELD 0 IF EntityType EQUALS SmallEntity
            	YIELD 0 IF EntityType EQUALS MicroEntity
            ENDCASE
            YIELD 800 IF EntityType EQUALS NormalEntity
            YIELD 360 IF EntityType EQUALS SmallEntity
            YIELD 160 IF EntityType EQUALS MicroEntity
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = p.GetFees().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("ExaminationFee", result.Name);
            Assert.Equal(2, result.Cases.Count);
            var FeeCase = result.Cases[0];
            Assert.IsType<DslFeeCase>(FeeCase);
            var FeeCase1 = (DslFeeCase)result.Cases[0];
            Assert.Equal(3, FeeCase1.Condition.Count());
            Assert.Equal(3, FeeCase1.Yields.Count);
            var FeeCase2 = (DslFeeCase)result.Cases[1];
            Assert.Empty(FeeCase2.Condition);
            Assert.Equal(3, FeeCase2.Yields.Count);
        }
    }
}
