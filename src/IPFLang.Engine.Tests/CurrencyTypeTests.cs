using IPFLang.CurrencyConversion;
using IPFLang.Evaluator;
using IPFLang.Parser;
using IPFLang.Types;

namespace IPFLang.Engine.Tests
{
    public class CurrencyTypeTests
    {
        #region Currency Validation Tests

        [Theory]
        [InlineData("EUR", true)]
        [InlineData("USD", true)]
        [InlineData("GBP", true)]
        [InlineData("JPY", true)]
        [InlineData("RON", true)]
        [InlineData("XXX", false)]
        [InlineData("EURO", false)]
        [InlineData("US", false)]
        [InlineData("", false)]
        public void TestCurrencyValidation(string code, bool expectedValid)
        {
            Assert.Equal(expectedValid, Currency.IsValid(code));
        }

        [Fact]
        public void TestCurrencyParse()
        {
            Assert.Equal("EUR", Currency.Parse("eur"));
            Assert.Equal("USD", Currency.Parse("usd"));
            Assert.Throws<ArgumentException>(() => Currency.Parse("XXX"));
        }

        [Fact]
        public void TestCurrencyGetAll()
        {
            var currencies = Currency.GetAll().ToList();
            Assert.True(currencies.Count > 150); // ISO 4217 has 150+ currencies
            Assert.Contains(currencies, c => c.Code == "EUR" && c.Name == "Euro");
            Assert.Contains(currencies, c => c.Code == "USD" && c.Name == "United States Dollar");
        }

        #endregion

        #region Amount Input Parsing Tests

        [Fact]
        public void TestInputAmount()
        {
            string text =
            """
            DEFINE AMOUNT FilingFee AS 'Filing fee'
            CURRENCY EUR
            DEFAULT 100
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputAmount?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("FilingFee", result.Name);
            Assert.Equal("Filing fee", result.Text);
            Assert.Equal("EUR", result.Currency);
            Assert.Equal(100m, result.DefaultValue);
        }

        [Fact]
        public void TestInputAmountWithCurrencyLiteral()
        {
            string text =
            """
            DEFINE AMOUNT FilingFee AS 'Filing fee'
            CURRENCY EUR
            DEFAULT 100<EUR>
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputAmount?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("EUR", result.Currency);
            Assert.Equal(100m, result.DefaultValue);
        }

        [Fact]
        public void TestInputAmountMissingCurrency()
        {
            string text =
            """
            DEFINE AMOUNT FilingFee AS 'Filing fee'
            DEFAULT 100
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            Assert.Contains(p.GetErrors(), e => e.Item2.Contains("CURRENCY"));
        }

        [Fact]
        public void TestInputAmountInvalidCurrency()
        {
            string text =
            """
            DEFINE AMOUNT FilingFee AS 'Filing fee'
            CURRENCY XXX
            DEFAULT 100
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            Assert.Contains(p.GetErrors(), e => e.Item2.Contains("Invalid ISO 4217"));
        }

        #endregion

        #region Currency Literal Parsing Tests

        [Fact]
        public void TestCurrencyLiteralInYield()
        {
            string text =
            """
            COMPUTE FEE TestFee
            YIELD 100<EUR>
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var fee = p.GetFees().Single();
            var feeCase = fee.Cases.First() as DslFeeCase;
            Assert.NotNull(feeCase);
            var yield = feeCase!.Yields.First();
            Assert.Contains("100<EUR>", yield.Values);
        }

        [Fact]
        public void TestCurrencyLiteralEvaluation()
        {
            var result = DslEvaluator.EvaluateExpression(
                new[] { "100<EUR>", "+", "50<EUR>" },
                Enumerable.Empty<IPFValue>()
            );
            Assert.Equal(150m, result);
        }

        [Fact]
        public void TestCurrencyLiteralWithMultiplication()
        {
            var result = DslEvaluator.EvaluateExpression(
                new[] { "100<EUR>", "*", "2" },
                Enumerable.Empty<IPFValue>()
            );
            Assert.Equal(200m, result);
        }

        #endregion

        #region Polymorphic Fee Parsing Tests

        [Fact]
        public void TestPolymorphicFeeDefinition()
        {
            string text =
            """
            COMPUTE FEE ClaimFee<C> RETURN C
            YIELD 50<EUR>
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var fee = p.GetFees().Single();
            Assert.Equal("ClaimFee", fee.Name);
            Assert.True(fee.IsPolymorphic);
            Assert.Equal("C", fee.TypeParameter);
            Assert.Equal("C", fee.ReturnCurrency);
        }

        [Fact]
        public void TestPolymorphicFeeOptional()
        {
            string text =
            """
            COMPUTE FEE ClaimFee<C> RETURN C OPTIONAL
            YIELD 50<EUR>
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var fee = p.GetFees().Single();
            Assert.True(fee.Optional);
            Assert.True(fee.IsPolymorphic);
        }

        [Fact]
        public void TestPolymorphicFeeMissingReturn()
        {
            string text =
            """
            COMPUTE FEE ClaimFee<C>
            YIELD 50<EUR>
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            Assert.Contains(p.GetErrors(), e => e.Item2.Contains("RETURN"));
        }

        #endregion

        #region Type Checker Tests

        [Fact]
        public void TestTypeCheckerSameCurrencyAddition()
        {
            string text =
            """
            DEFINE AMOUNT Fee1 AS 'Fee 1'
            CURRENCY EUR
            DEFAULT 100
            ENDDEFINE

            DEFINE AMOUNT Fee2 AS 'Fee 2'
            CURRENCY EUR
            DEFAULT 50
            ENDDEFINE

            COMPUTE FEE TotalFee
            YIELD Fee1 + Fee2
            ENDCOMPUTE
            """;
            var p = new DslParser();
            p.Parse(text);

            var checker = new CurrencyTypeChecker();
            var errors = checker.Check(p.GetInputs(), p.GetFees());
            Assert.Empty(errors);
        }

        [Fact]
        public void TestTypeCheckerMixedCurrencyAddition()
        {
            string text =
            """
            DEFINE AMOUNT Fee1 AS 'Fee 1'
            CURRENCY EUR
            DEFAULT 100
            ENDDEFINE

            DEFINE AMOUNT Fee2 AS 'Fee 2'
            CURRENCY USD
            DEFAULT 50
            ENDDEFINE

            COMPUTE FEE TotalFee
            YIELD Fee1 + Fee2
            ENDCOMPUTE
            """;
            var p = new DslParser();
            p.Parse(text);

            var checker = new CurrencyTypeChecker();
            var errors = checker.Check(p.GetInputs(), p.GetFees()).ToList();
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Kind == TypeErrorKind.MixedCurrencyArithmetic);
        }

        [Fact]
        public void TestTypeCheckerCurrencyScalarMultiplication()
        {
            string text =
            """
            DEFINE AMOUNT BaseFee AS 'Base fee'
            CURRENCY EUR
            DEFAULT 100
            ENDDEFINE

            DEFINE NUMBER Multiplier AS 'Multiplier'
            BETWEEN 1 AND 10
            DEFAULT 2
            ENDDEFINE

            COMPUTE FEE TotalFee
            YIELD BaseFee * Multiplier
            ENDCOMPUTE
            """;
            var p = new DslParser();
            p.Parse(text);

            var checker = new CurrencyTypeChecker();
            var errors = checker.Check(p.GetInputs(), p.GetFees());
            Assert.Empty(errors);
        }

        [Fact]
        public void TestTypeCheckerCurrencyLiteralMismatch()
        {
            string text =
            """
            COMPUTE FEE TotalFee
            YIELD 100<EUR> + 50<USD>
            ENDCOMPUTE
            """;
            var p = new DslParser();
            p.Parse(text);

            var checker = new CurrencyTypeChecker();
            var errors = checker.Check(p.GetInputs(), p.GetFees()).ToList();
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Kind == TypeErrorKind.MixedCurrencyArithmetic);
        }

        #endregion

        #region Calculator Integration Tests

        [Fact]
        public void TestCalculatorWithCurrencyLiterals()
        {
            string text =
            """
            COMPUTE FEE Filing
            YIELD 100<EUR>
            ENDCOMPUTE
            """;
            var calc = new DslCalculator(new DslParser());
            var result = calc.Parse(text);
            Assert.True(result);
            Assert.Empty(calc.GetTypeErrors());

            var (mandatory, optional, _, _) = calc.Compute(Enumerable.Empty<IPFValue>());
            Assert.Equal(100m, mandatory);
        }

        [Fact]
        public void TestCalculatorRejectsMixedCurrencies()
        {
            string text =
            """
            COMPUTE FEE Filing
            YIELD 100<EUR> + 50<USD>
            ENDCOMPUTE
            """;
            var calc = new DslCalculator(new DslParser());
            var result = calc.Parse(text);
            Assert.False(result);
            Assert.NotEmpty(calc.GetTypeErrors());
        }

        [Fact]
        public void TestCalculatorWithConvert()
        {
            string text =
            """
            COMPUTE FEE Filing
            YIELD CONVERT(100<USD>, USD, EUR)
            ENDCOMPUTE
            """;
            var calc = new DslCalculator(new DslParser());
            var parseResult = calc.Parse(text);
            Assert.True(parseResult);

            // Set up mock currency converter
            var converter = new CurrencyConverter();
            converter.Response = ExchangeRateResponse.Success(
                new Dictionary<string, decimal>
                {
                    ["EUR"] = 1.0m,
                    ["USD"] = 1.08m
                },
                DateTime.Now
            );
            calc.SetCurrencyConverter(converter);

            var (mandatory, _, _, _) = calc.Compute(Enumerable.Empty<IPFValue>());
            // 100 USD / 1.08 (USD rate) * 1.0 (EUR rate) = ~92.59 EUR
            Assert.True(mandatory > 92m && mandatory < 93m);
        }

        [Fact]
        public void TestCalculatorWithAmountInput()
        {
            string text =
            """
            DEFINE AMOUNT FilingFee AS 'Filing fee'
            CURRENCY EUR
            DEFAULT 100
            ENDDEFINE

            COMPUTE FEE Total
            YIELD FilingFee * 2
            ENDCOMPUTE
            """;
            var calc = new DslCalculator(new DslParser());
            var parseResult = calc.Parse(text);
            Assert.True(parseResult);

            var inputs = new List<IPFValue>
            {
                new IPFValueAmount("FilingFee", 150m, "EUR")
            };

            var (mandatory, _, _, _) = calc.Compute(inputs);
            Assert.Equal(300m, mandatory);
        }

        #endregion

        #region Exchange Rate Fetcher Tests

        [Fact]
        public async Task TestMockExchangeRateFetcher()
        {
            var fetcher = new MockExchangeRateFetcher();
            var response = await fetcher.FetchExchangeRates();

            Assert.Equal(ResponseStatus.Online, response.Status);
            Assert.True(response.ExchangeRates.ContainsKey("EUR"));
            Assert.True(response.ExchangeRates.ContainsKey("USD"));
            Assert.Equal(1.0m, response.ExchangeRates["EUR"]);
        }

        [Fact]
        public void TestCurrencyConverterConversion()
        {
            var converter = new CurrencyConverter();
            converter.Response = ExchangeRateResponse.Success(
                new Dictionary<string, decimal>
                {
                    ["EUR"] = 1.0m,
                    ["USD"] = 1.08m,
                    ["GBP"] = 0.86m
                },
                DateTime.Now
            );

            // Convert 100 EUR to USD
            var result = converter.Convert(100m, "EUR", "USD");
            Assert.Equal(108m, result);

            // Convert 108 USD to EUR
            var result2 = converter.Convert(108m, "USD", "EUR");
            Assert.Equal(100m, result2);

            // Same currency
            var result3 = converter.Convert(100m, "EUR", "EUR");
            Assert.Equal(100m, result3);
        }

        [Fact]
        public void TestCurrencyConverterGetRate()
        {
            var converter = new CurrencyConverter();
            converter.Response = ExchangeRateResponse.Success(
                new Dictionary<string, decimal>
                {
                    ["EUR"] = 1.0m,
                    ["USD"] = 1.08m
                },
                DateTime.Now
            );

            var rate = converter.GetRate("EUR", "USD");
            Assert.Equal(1.08m, rate);

            var reverseRate = converter.GetRate("USD", "EUR");
            Assert.True(reverseRate > 0.92m && reverseRate < 0.93m);
        }

        #endregion

        #region IPFType Tests

        [Fact]
        public void TestIPFTypeAmount()
        {
            var eurType = new IPFTypeAmount("EUR");
            var usdType = new IPFTypeAmount("USD");
            var polymorphicType = new IPFTypeAmount("C", true);

            Assert.Equal("EUR", eurType.Currency);
            Assert.False(eurType.IsPolymorphic);
            Assert.NotEqual(eurType, usdType);
            Assert.True(polymorphicType.IsPolymorphic);
        }

        [Fact]
        public void TestIPFTypeVariable()
        {
            var typeVar = new IPFTypeVariable("C");
            Assert.Equal("C", typeVar.Name);
        }

        #endregion

        #region TypeError Tests

        [Fact]
        public void TestTypeErrorCreation()
        {
            var error = TypeError.CurrencyMismatch("EUR", "USD", "fee TestFee");
            Assert.Equal(TypeErrorKind.CurrencyMismatch, error.Kind);
            Assert.Contains("EUR", error.Message);
            Assert.Contains("USD", error.Message);
            Assert.Equal("fee TestFee", error.Location);
        }

        [Fact]
        public void TestTypeErrorMixedCurrency()
        {
            var error = TypeError.MixedCurrencyArithmetic("EUR", "USD", "YIELD expression");
            Assert.Equal(TypeErrorKind.MixedCurrencyArithmetic, error.Kind);
            Assert.Contains("EUR", error.Message);
            Assert.Contains("USD", error.Message);
        }

        #endregion

        #region TypeEnvironment Tests

        [Fact]
        public void TestTypeEnvironmentBasic()
        {
            var env = new TypeEnvironment();
            env.Bind("x", new IPFTypeNumber());
            env.Bind("amount", new IPFTypeAmount("EUR"));

            Assert.Equal(new IPFTypeNumber(), env.Lookup("x"));
            Assert.Equal(new IPFTypeAmount("EUR"), env.Lookup("amount"));
            Assert.Null(env.Lookup("unknown"));
        }

        [Fact]
        public void TestTypeEnvironmentScope()
        {
            var parent = new TypeEnvironment();
            parent.Bind("x", new IPFTypeNumber());

            var child = parent.NewScope();
            child.Bind("y", new IPFTypeString());

            Assert.Equal(new IPFTypeNumber(), child.Lookup("x")); // inherited
            Assert.Equal(new IPFTypeString(), child.Lookup("y")); // local
            Assert.Null(parent.Lookup("y")); // not visible in parent
        }

        [Fact]
        public void TestTypeEnvironmentTypeVariable()
        {
            var env = new TypeEnvironment();
            Assert.False(env.IsTypeVariable("C"));

            var withTypeVar = env.WithTypeVariable("C");
            Assert.True(withTypeVar.IsTypeVariable("C"));
            Assert.False(env.IsTypeVariable("C")); // original unchanged
        }

        #endregion
    }
}
