using IPFLang.Evaluator;
using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class CalculatorTests
    {
        [Fact]
        public void TestEmptyFee()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() { };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalAmount, _, _, _) = calc.Compute(vars);

            Assert.Equal(0, TotalAmount);
        }

        [Fact]
        public void TestSimpleYield()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            YIELD 320
            YIELD 80
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() { };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalAmount, _, _, _) = calc.Compute(vars);

            Assert.Equal(400, TotalAmount);
        }

        [Fact]
        public void TestYieldWithSimpleCondition()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            YIELD 320 IF TRUE
            YIELD 80
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() { };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalAmount, _, _, _) = calc.Compute(vars);

            Assert.Equal(400, TotalAmount);
        }

        [Fact]
        public void TestYieldWithVariableCondition()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            YIELD 320 IF C
            ENDCOMPUTE
            """;

            List<IPFValue> vars = new() {
                new IPFValueBoolean("C", true),
            };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalAmount, _, _, _) = calc.Compute(vars);

            Assert.Equal(320, TotalAmount);
        }

        [Fact]
        public void TestYieldWithComplexCondition()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            YIELD 320 IF EntityType EQ NormalEntity
            YIELD 128 IF EntityType EQ SmallEntity
            YIELD 64 IF EntityType EQ MicroEntity
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() {
                new IPFValueString("EntityType", "NormalEntity"),
            };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalAmount, _, _, _) = calc.Compute(vars);

            Assert.Equal(320, TotalAmount);
        }

        [Fact]
        public void TestComplexYieldWithComplexCondition()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            YIELD (320/10-30)*2 IF EntityType EQ NormalEntity
            YIELD (128/5) IF EntityType EQ SmallEntity
            YIELD (64/8)+2 IF EntityType EQ MicroEntity
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() {
                new IPFValueString("EntityType", "NormalEntity"),
            };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalAmount, _, _, _) = calc.Compute(vars);

            Assert.Equal(4, TotalAmount);
        }

        [Fact]
        public void TestYieldWithVariable()
        {
            string text =
            """
            DEFINE NUMBER A AS 'A number'
            ENDDEFINE

            COMPUTE FEE BasicNationalFee
            YIELD (A/10-30)*2
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() {
                new IPFValueNumber("A", 350),
            };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalAmount, _, _, _) = calc.Compute(vars);

            Assert.Equal(10, TotalAmount);
        }

        [Fact]
        public void TestOptionalFee()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            YIELD 10
            ENDCOMPUTE

            COMPUTE FEE BasicOptionalFee OPTIONAL
            YIELD 20
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() { };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalMandatoryAmount, TotalOptionalAmount, _, _) = calc.Compute(vars);

            Assert.Equal(10, TotalMandatoryAmount);
            Assert.Equal(20, TotalOptionalAmount);
        }

        [Fact]
        public void TestFeeWithVariables1()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            LET A AS 20 * 10
            LET B AS 30 * 11
            YIELD 320 + A IF EntityType EQ NormalEntity
            YIELD 128 + A IF EntityType EQ SmallEntity
            YIELD 64 + B IF EntityType EQ MicroEntity
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() {
                new IPFValueString("EntityType", "NormalEntity"),
            };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalMandatoryAmount, TotalOptionalAmount, _, _) = calc.Compute(vars);

            Assert.Equal(520, TotalMandatoryAmount);
            Assert.Equal(0, TotalOptionalAmount);
        }

        [Fact]
        public void TestFeeWithVariables2()
        {
            string text =
            """
            COMPUTE FEE BasicNationalFee
            LET A AS C * 10
            LET B AS C * 11
            YIELD 320 + A IF EntityType EQ NormalEntity
            YIELD 128 + A IF EntityType EQ SmallEntity
            YIELD 64 + B IF EntityType EQ MicroEntity
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() {
                new IPFValueString("EntityType", "SmallEntity"),
                new IPFValueNumber("C", 2)
            };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            var (TotalMandatoryAmount, TotalOptionalAmount, _, _) = calc.Compute(vars);

            Assert.Equal(148, TotalMandatoryAmount);
            Assert.Equal(0, TotalOptionalAmount);
        }

        [Fact]
        public void TestFeeWithUnknownVariables()
        {
            string text =
            """
            COMPUTE FEE SheetFee
            YIELD 10 IF SheetCount GT 100
            ENDCOMPUTE
            """;
            List<IPFValue> vars = new() { };
            var parser = new DslParser();
            var calc = new DslCalculator(parser);
            calc.Parse(text);
            Assert.Throws<NotSupportedException>(() => calc.Compute(vars));
        }
    }
}
