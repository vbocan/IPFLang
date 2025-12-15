using IPFLang.Analysis;
using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class CompletenessTests
    {
        #region Domain Extraction Tests

        [Fact]
        public void ExtractDomains_BooleanInput_ReturnsBooleanDomain()
        {
            var input = new DslInputBoolean("IsSmall", "Is entity small?", "General", false);
            var analyzer = new DomainAnalyzer();
            var domains = analyzer.ExtractDomains(new[] { input }).ToList();

            Assert.Single(domains);
            Assert.IsType<BooleanDomain>(domains[0]);
            Assert.Equal("IsSmall", domains[0].VariableName);
        }

        [Fact]
        public void ExtractDomains_ListInput_ReturnsListDomain()
        {
            var items = new List<DslListItem>
            {
                new("Normal", "Normal Entity"),
                new("Small", "Small Entity"),
                new("Micro", "Micro Entity")
            };
            var input = new DslInputList("EntityType", "Entity type", "General", items, "Normal");
            var analyzer = new DomainAnalyzer();
            var domains = analyzer.ExtractDomains(new[] { input }).ToList();

            Assert.Single(domains);
            Assert.IsType<ListDomain>(domains[0]);
            var listDomain = (ListDomain)domains[0];
            Assert.Equal(3, listDomain.Symbols.Count);
        }

        [Fact]
        public void ExtractDomains_NumberInput_ReturnsNumericDomain()
        {
            var input = new DslInputNumber("ClaimCount", "Number of claims", "General", 1, 100, 10);
            var analyzer = new DomainAnalyzer();
            var domains = analyzer.ExtractDomains(new[] { input }).ToList();

            Assert.Single(domains);
            Assert.IsType<NumericDomain>(domains[0]);
            var numDomain = (NumericDomain)domains[0];
            Assert.Equal(1, numDomain.MinValue);
            Assert.Equal(100, numDomain.MaxValue);
        }

        #endregion

        #region Completeness Checker Tests

        [Fact]
        public void CheckCompleteness_CompleteFee_ReturnsComplete()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                CHOICE Small AS 'Small Entity'
                CHOICE Micro AS 'Micro Entity'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE BasicFee
                YIELD 320 IF EntityType EQ Normal
                YIELD 128 IF EntityType EQ Small
                YIELD 64 IF EntityType EQ Micro
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            var parseResult = calculator.Parse(dsl);
            if (!parseResult)
            {
                var errors = calculator.GetErrors().Concat(calculator.GetTypeErrors().Select(e => e.ToString())).ToList();
                Assert.Fail($"Parse failed with errors: {string.Join("; ", errors)}");
            }

            var report = calculator.VerifyCompleteness();
            Assert.True(report.AllComplete, $"Report: {report}");
            Assert.Single(report.FeeReports);
            Assert.True(report.FeeReports[0].IsComplete, $"Fee report: {report.FeeReports[0]}");
        }

        [Fact]
        public void CheckCompleteness_IncompleteFee_ReturnsIncomplete()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                CHOICE Small AS 'Small Entity'
                CHOICE Micro AS 'Micro Entity'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE BasicFee
                YIELD 320 IF EntityType EQ Normal
                YIELD 128 IF EntityType EQ Small
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var report = calculator.VerifyCompleteness();
            Assert.False(report.AllComplete);
            Assert.Single(report.FeeReports);
            Assert.False(report.FeeReports[0].IsComplete);
            Assert.NotEmpty(report.FeeReports[0].Gaps);
        }

        [Fact]
        public void CheckCompleteness_UnconditionalYield_ReturnsComplete()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 100
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE BasicFee
                YIELD 100
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var report = calculator.VerifyCompleteness();
            Assert.True(report.AllComplete);
        }

        [Fact]
        public void CheckCompleteness_CombinedConditions_ReturnsComplete()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                CHOICE Small AS 'Small Entity'
                DEFAULT Normal
                ENDDEFINE

                DEFINE BOOLEAN IsUrgent AS 'Is urgent?'
                DEFAULT FALSE
                ENDDEFINE

                COMPUTE FEE CombinedFee
                YIELD 400 IF EntityType EQ Normal AND IsUrgent EQ TRUE
                YIELD 300 IF EntityType EQ Normal AND IsUrgent EQ FALSE
                YIELD 200 IF EntityType EQ Small AND IsUrgent EQ TRUE
                YIELD 100 IF EntityType EQ Small AND IsUrgent EQ FALSE
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var report = calculator.VerifyCompleteness();
            Assert.True(report.AllComplete);
        }

        [Fact]
        public void CheckCompleteness_PartialCoverage_FindsGaps()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                CHOICE Small AS 'Small Entity'
                DEFAULT Normal
                ENDDEFINE

                DEFINE BOOLEAN IsUrgent AS 'Is urgent?'
                DEFAULT FALSE
                ENDDEFINE

                COMPUTE FEE PartialFee
                YIELD 400 IF EntityType EQ Normal AND IsUrgent EQ TRUE
                YIELD 300 IF EntityType EQ Normal AND IsUrgent EQ FALSE
                YIELD 200 IF EntityType EQ Small AND IsUrgent EQ TRUE
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var report = calculator.VerifyCompleteness();
            Assert.False(report.AllComplete);
            var feeReport = report.FeeReports[0];
            Assert.Single(feeReport.Gaps);
        }

        #endregion

        #region Monotonicity Checker Tests

        [Fact]
        public void CheckMonotonicity_MonotonicFee_ReturnsMonotonic()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 20
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE ClaimFee
                YIELD ClaimCount * 10
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var report = calculator.VerifyMonotonicity("ClaimFee", "ClaimCount");
            Assert.True(report.IsMonotonic);
            Assert.Empty(report.Violations);
        }

        [Fact]
        public void CheckMonotonicity_NonMonotonicFee_ReturnsViolations()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 20
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE WeirdFee
                YIELD 100 IF ClaimCount LTE 10
                YIELD 50 IF ClaimCount GT 10
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            var parseResult = calculator.Parse(dsl);
            if (!parseResult)
            {
                var errors = calculator.GetErrors().Concat(calculator.GetTypeErrors().Select(e => e.ToString())).ToList();
                Assert.Fail($"Parse failed with errors: {string.Join("; ", errors)}");
            }

            var report = calculator.VerifyMonotonicity("WeirdFee", "ClaimCount");
            Assert.False(report.IsMonotonic, $"Expected non-monotonic but got: {report}");
            Assert.NotEmpty(report.Violations);
        }

        [Fact]
        public void CheckMonotonicity_NonExistentFee_ThrowsException()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 20
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE ClaimFee
                YIELD ClaimCount * 10
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            Assert.Throws<ArgumentException>(() => calculator.VerifyMonotonicity("NonExistent", "ClaimCount"));
        }

        [Fact]
        public void CheckMonotonicity_NonNumericInput_ReturnsError()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                CHOICE Small AS 'Small Entity'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE BasicFee
                YIELD 100 IF EntityType EQ Normal
                YIELD 50 IF EntityType EQ Small
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var report = calculator.VerifyMonotonicity("BasicFee", "EntityType");
            Assert.False(report.IsMonotonic);
            Assert.Contains(report.Notes, n => n.Contains("not numeric"));
        }

        #endregion

        #region VERIFY Syntax Tests

        [Fact]
        public void Parse_VerifyComplete_ParsesCorrectly()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE BasicFee
                YIELD 100 IF EntityType EQ Normal
                ENDCOMPUTE

                VERIFY COMPLETE FEE BasicFee
                """;

            var parser = new DslParser();
            Assert.True(parser.Parse(dsl));

            var verifications = parser.GetVerifications().ToList();
            Assert.Single(verifications);
            Assert.IsType<DslVerifyComplete>(verifications[0]);
            Assert.Equal("BasicFee", verifications[0].FeeName);
        }

        [Fact]
        public void Parse_VerifyMonotonic_ParsesCorrectly()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 20
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE ClaimFee
                YIELD ClaimCount * 10
                ENDCOMPUTE

                VERIFY MONOTONIC FEE ClaimFee WITH RESPECT TO ClaimCount
                """;

            var parser = new DslParser();
            Assert.True(parser.Parse(dsl));

            var verifications = parser.GetVerifications().ToList();
            Assert.Single(verifications);
            Assert.IsType<DslVerifyMonotonic>(verifications[0]);
            var vm = (DslVerifyMonotonic)verifications[0];
            Assert.Equal("ClaimFee", vm.FeeName);
            Assert.Equal("ClaimCount", vm.WithRespectTo);
            Assert.Equal("NonDecreasing", vm.Direction);
        }

        [Fact]
        public void Parse_VerifyMonotonicWithDirection_ParsesCorrectly()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 20
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE ClaimFee
                YIELD ClaimCount * 10
                ENDCOMPUTE

                VERIFY MONOTONIC FEE ClaimFee WITH RESPECT TO ClaimCount DIRECTION StrictlyIncreasing
                """;

            var parser = new DslParser();
            Assert.True(parser.Parse(dsl));

            var verifications = parser.GetVerifications().ToList();
            var vm = (DslVerifyMonotonic)verifications[0];
            Assert.Equal("StrictlyIncreasing", vm.Direction);
        }

        [Fact]
        public void RunVerifications_ExecutesAllDirectives()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                CHOICE Small AS 'Small Entity'
                DEFAULT Normal
                ENDDEFINE

                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 20
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE BasicFee
                YIELD 100 IF EntityType EQ Normal
                YIELD 50 IF EntityType EQ Small
                ENDCOMPUTE

                COMPUTE FEE ClaimFee
                YIELD ClaimCount * 10
                ENDCOMPUTE

                VERIFY COMPLETE FEE BasicFee
                VERIFY MONOTONIC FEE ClaimFee WITH RESPECT TO ClaimCount
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var results = calculator.RunVerifications();
            Assert.True(results.AllPassed);
            Assert.Single(results.CompletenessReports);
            Assert.Single(results.MonotonicityReports);
            Assert.Empty(results.Errors);
        }

        [Fact]
        public void RunVerifications_NonExistentFee_ReportsError()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal Entity'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE BasicFee
                YIELD 100
                ENDCOMPUTE

                VERIFY COMPLETE FEE NonExistent
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var results = calculator.RunVerifications();
            Assert.False(results.AllPassed);
            Assert.Single(results.Errors);
            Assert.Contains("not found", results.Errors[0]);
        }

        #endregion

        #region Logical Expression Tests

        [Fact]
        public void LogicalExpression_AndExpression_EvaluatesCorrectly()
        {
            var left = new ComparisonExpression("X", ComparisonOperator.Equal, "A");
            var right = new ComparisonExpression("Y", ComparisonOperator.Equal, true);
            var and = new AndExpression(left, right);

            var combination = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new SymbolValue("X", "A"),
                ["Y"] = new BooleanValue("Y", true)
            });

            Assert.True(and.Evaluate(combination));
        }

        [Fact]
        public void LogicalExpression_OrExpression_EvaluatesCorrectly()
        {
            var left = new ComparisonExpression("X", ComparisonOperator.Equal, "A");
            var right = new ComparisonExpression("X", ComparisonOperator.Equal, "B");
            var or = new OrExpression(left, right);

            var combinationA = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new SymbolValue("X", "A")
            });
            var combinationB = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new SymbolValue("X", "B")
            });
            var combinationC = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new SymbolValue("X", "C")
            });

            Assert.True(or.Evaluate(combinationA));
            Assert.True(or.Evaluate(combinationB));
            Assert.False(or.Evaluate(combinationC));
        }

        [Fact]
        public void LogicalExpression_NotExpression_EvaluatesCorrectly()
        {
            var inner = new ComparisonExpression("X", ComparisonOperator.Equal, true);
            var not = new NotExpression(inner);

            var trueCombo = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new BooleanValue("X", true)
            });
            var falseCombo = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new BooleanValue("X", false)
            });

            Assert.False(not.Evaluate(trueCombo));
            Assert.True(not.Evaluate(falseCombo));
        }

        [Fact]
        public void LogicalExpression_NumericComparisons_EvaluateCorrectly()
        {
            var gt = new ComparisonExpression("X", ComparisonOperator.GreaterThan, 10m);
            var lte = new ComparisonExpression("X", ComparisonOperator.LessThanOrEqual, 10m);

            var combo5 = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new NumericValue("X", 5)
            });
            var combo15 = new InputCombination(new Dictionary<string, DomainValue>
            {
                ["X"] = new NumericValue("X", 15)
            });

            Assert.False(gt.Evaluate(combo5));
            Assert.True(gt.Evaluate(combo15));
            Assert.True(lte.Evaluate(combo5));
            Assert.False(lte.Evaluate(combo15));
        }

        #endregion

        #region Condition Extraction Tests

        [Fact]
        public void ConditionExtractor_SimpleFee_ExtractsConditions()
        {
            var fee = new DslFee("TestFee", false,
                new List<DslItem>
                {
                    new DslFeeCase(Enumerable.Empty<string>(),
                        new List<DslFeeYield>
                        {
                            new DslFeeYield(new[] { "X", "EQ", "A" }, new[] { "100" }),
                            new DslFeeYield(new[] { "X", "EQ", "B" }, new[] { "200" })
                        })
                },
                new List<DslFeeVar>());

            var extractor = new ConditionExtractor();
            var conditions = extractor.ExtractFeeConditions(fee).ToList();

            Assert.Equal(2, conditions.Count);
        }

        [Fact]
        public void ConditionExtractor_ParsesAndOperator()
        {
            var tokens = new[] { "X", "EQ", "A", "AND", "Y", "EQ", "B" };
            var extractor = new ConditionExtractor();
            var expr = extractor.ParseCondition(tokens);

            Assert.IsType<AndExpression>(expr);
        }

        [Fact]
        public void ConditionExtractor_ParsesOrOperator()
        {
            var tokens = new[] { "X", "EQ", "A", "OR", "X", "EQ", "B" };
            var extractor = new ConditionExtractor();
            var expr = extractor.ParseCondition(tokens);

            Assert.IsType<OrExpression>(expr);
        }

        [Fact]
        public void ConditionExtractor_EmptyCondition_ReturnsTrue()
        {
            var tokens = Array.Empty<string>();
            var extractor = new ConditionExtractor();
            var expr = extractor.ParseCondition(tokens);

            Assert.IsType<TrueExpression>(expr);
        }

        #endregion
    }
}
