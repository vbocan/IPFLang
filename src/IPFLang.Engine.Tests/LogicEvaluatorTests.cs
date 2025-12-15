using IPFLang.Evaluator;
using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class LogicEvaluatorTests
    {
        [Fact]
        public void TestNumericEqual()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 6),
            };
            var tokens = "A EQ 6".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestNumericNotEqual()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 7),
            };
            var tokens = "A NEQ 6".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestBooleanEqual()
        {
            var vars = new IPFValue[] {
                new IPFValueBoolean("B", true),
            };
            var tokens = "B EQ TRUE".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestBooleanNotEqual()
        {
            var vars = new IPFValue[] {
                new IPFValueBoolean("B", false),
            };
            var tokens = "B NEQ TRUE".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestBooleanComplexExpression()
        {
            var vars = new IPFValue[] {
                new IPFValueBoolean("Right_PAT", true),
                new IPFValueString("REC_TYPE", "REC_ADR"),
            };
            var tokens = "Right_PAT EQ TRUE AND ( REC_TYPE EQ REC_ADR OR REC_TYPE EQ REC_NAME )".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestStringEqual()
        {
            var vars = new IPFValue[] {
                new IPFValueString("EntityType", "NormalEntity"),
            };
            var tokens = "EntityType EQ NormalEntity".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestStringNotEqual()
        {
            var vars = new IPFValue[] {
                new IPFValueString("EntityType", "NewEntity"),
            };
            var tokens = "EntityType NEQ NormalEntity".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestStringListIn()
        {
            var vars = new IPFValue[] {
                new IPFValueStringList("EntityType", new[] { "NormalEntity", "SmallEntity" }.AsEnumerable()),
            };
            var tokens = "NormalEntity IN EntityType".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestStringListNotIn()
        {
            var vars = new IPFValue[] {
                new IPFValueStringList("EntityType", new[] { "NormalEntity", "SmallEntity" }.AsEnumerable()),
            };
            var tokens = "TinyEntity NIN EntityType".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestNumbersAnd()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 100),
                new IPFValueNumber("B", 200),
                new IPFValueNumber("C", 300),
            };
            var tokens = "A LT B AND B LT C".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestBooleanOrWithParentheses()
        {
            var vars = new IPFValue[] {
                new IPFValueBoolean("A", true),
                new IPFValueBoolean("B", false),
            };
            var tokens = "( A OR B )".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestStringNumberAnd()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 100),
                new IPFValueNumber("B", 200),
                new IPFValueString("C", "Alibaba"),
            };
            var tokens = "A LT B AND C EQ Alibaba".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestNumbersOr()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 100),
                new IPFValueNumber("B", 200),
            };
            var tokens = "A GT 1000 OR 50 LT B".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestNumbersAndOrWithParentheses()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 100),
                new IPFValueNumber("B", 200),
                new IPFValueNumber("C", 30),
            };
            var tokens = "A LT 1000 AND ( 50 GTE B OR C EQ 30 )".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestMultipleWithParentheses()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 100),
                new IPFValueNumber("B", 200),
                new IPFValueNumber("C", 30),
                new IPFValueBoolean("bo", true),
                new IPFValueString("str", "MyString")
            };
            var tokens = "( A GTE 100 AND B EQ 200 ) OR ( C NEQ 30 AND bo EQ TRUE AND str EQ MyString )".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestStringInvalidComparison()
        {
            var vars = new IPFValue[] {
                new IPFValueString("A", "Alibaba"),
                new IPFValueString("B", "Alibaba"),
            };
            var tokens = "A AND B".Split(new char[] { ' ' }, StringSplitOptions.None);
            Assert.Throws<NotSupportedException>(() => DslEvaluator.EvaluateLogic(tokens, vars));
        }

        [Fact]
        public void TestBooleanInvalidComparison()
        {
            var vars = new IPFValue[] {
                new IPFValueBoolean("A", true),
            };
            var tokens = "A GT TRUE".Split(new char[] { ' ' }, StringSplitOptions.None);
            Assert.Throws<NotSupportedException>(() => DslEvaluator.EvaluateLogic(tokens, vars));
        }

        [Fact]
        public void TestNumberInvalidComparison()
        {
            var vars = new IPFValue[] {
                new IPFValueNumber("A", 10),
            };
            var tokens = "A AND 11".Split(new char[] { ' ' }, StringSplitOptions.None);
            Assert.Throws<NotSupportedException>(() => DslEvaluator.EvaluateLogic(tokens, vars));
        }

        [Fact]
        public void TestDateYearsToNow()
        {
            var vars = new IPFValue[] {
                new IPFValueDate("D", DateOnly.Parse("01.01.1975", null, System.Globalization.DateTimeStyles.None)),
            };
            var tokens = "D!YEARSTONOW GT 30".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestDateMonthsToNow()
        {
            var vars = new IPFValue[] {
                new IPFValueDate("D", DateOnly.Parse("01.01.1975", null, System.Globalization.DateTimeStyles.None)),
            };
            var tokens = "D!MONTHSTONOW GT 300".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestDaysMonthsToNow()
        {
            var vars = new IPFValue[] {
                new IPFValueDate("D", DateOnly.Parse("01.01.1975", null, System.Globalization.DateTimeStyles.None)),
            };
            var tokens = "D!DAYSTONOW GT 3000".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }

        [Fact]
        public void TestDateMonthsToNowFromLastDay()
        {
            var vars = new IPFValue[] {
                new IPFValueDate("D", DateOnly.Parse("01.01.1975", null, System.Globalization.DateTimeStyles.None)),
            };
            var tokens = "D!MONTHSTONOW_FROMLASTDAY GT 300".Split(new char[] { ' ' }, StringSplitOptions.None);
            var ev = DslEvaluator.EvaluateLogic(tokens, vars);
            Assert.True(ev);
        }
    }
}
