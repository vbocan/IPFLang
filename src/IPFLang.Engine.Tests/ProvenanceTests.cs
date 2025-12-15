using IPFLang.Evaluator;
using IPFLang.Parser;
using IPFLang.Provenance;

namespace IPFLang.Engine.Tests
{
    public class ProvenanceTests
    {
        #region Basic Provenance Tests

        [Fact]
        public void ComputeWithProvenance_SimpleFee_TracksContribution()
        {
            string dsl = """
                COMPUTE FEE BasicFee
                YIELD 100
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            Assert.Single(provenance.FeeProvenances);
            Assert.Equal("BasicFee", provenance.FeeProvenances[0].FeeName);
            Assert.Equal(100, provenance.FeeProvenances[0].TotalAmount);
            Assert.Equal(100, provenance.GrandTotal);
        }

        [Fact]
        public void ComputeWithProvenance_ConditionalYield_TracksMatchingCondition()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal'
                CHOICE Small AS 'Small'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE EntityFee
                YIELD 100 IF EntityType EQ Normal
                YIELD 50 IF EntityType EQ Small
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueString("EntityType", "Normal")
            };

            var provenance = calculator.ComputeWithProvenance(inputs);

            Assert.Single(provenance.FeeProvenances);
            var fee = provenance.FeeProvenances[0];
            Assert.Equal(100, fee.TotalAmount);

            // Should have 2 records (one for each yield)
            Assert.Equal(2, fee.Records.Count);

            // First yield contributed
            Assert.True(fee.Records[0].DidContribute);
            Assert.Equal(100, fee.Records[0].Contribution);
            Assert.Equal("EntityType EQ Normal", fee.Records[0].YieldCondition);

            // Second yield was skipped
            Assert.False(fee.Records[1].DidContribute);
            Assert.Equal("EntityType EQ Small", fee.Records[1].YieldCondition);
        }

        [Fact]
        public void ComputeWithProvenance_CaseCondition_TracksCaseAndYield()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal'
                CHOICE Small AS 'Small'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE CaseFee
                CASE EntityType EQ Normal AS
                  YIELD 100
                  YIELD 50
                ENDCASE
                CASE EntityType EQ Small AS
                  YIELD 25
                ENDCASE
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueString("EntityType", "Normal")
            };

            var provenance = calculator.ComputeWithProvenance(inputs);

            var fee = provenance.FeeProvenances[0];
            Assert.Equal(150, fee.TotalAmount); // 100 + 50

            // Both yields in first case contributed
            var contributing = fee.ContributingRecords.ToList();
            Assert.Equal(2, contributing.Count);
            Assert.Equal("EntityType EQ Normal", contributing[0].CaseCondition);

            // Third yield (in second case) was skipped
            var skipped = fee.SkippedRecords.ToList();
            Assert.Single(skipped);
            Assert.Equal("EntityType EQ Small", skipped[0].CaseCondition);
        }

        [Fact]
        public void ComputeWithProvenance_LetVariables_TracksVariableValues()
        {
            // Test LET variable tracking with literal values only
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal'
                CHOICE Small AS 'Small'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE ClaimFee
                LET Multiplier AS 10 * 2
                LET BaseFee AS 100
                YIELD BaseFee + Multiplier IF EntityType EQ Normal
                YIELD 50 + Multiplier IF EntityType EQ Small
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl), string.Join("; ", calculator.GetErrors().Concat(calculator.GetTypeErrors().Select(e => e.Message))));

            var inputs = new List<IPFValue>
            {
                new IPFValueString("EntityType", "Normal")
            };

            var provenance = calculator.ComputeWithProvenance(inputs);

            var fee = provenance.FeeProvenances[0];

            // Check LET variables were tracked
            Assert.True(fee.LetVariables.ContainsKey("Multiplier"));
            Assert.Equal(20, fee.LetVariables["Multiplier"]); // 10 * 2 = 20
            Assert.True(fee.LetVariables.ContainsKey("BaseFee"));
            Assert.Equal(100, fee.LetVariables["BaseFee"]);

            // Check total is correct (100 + 20 = 120)
            Assert.Equal(120, fee.TotalAmount);
        }

        [Fact]
        public void ComputeWithProvenance_MultipleYields_AccumulatesTotal()
        {
            string dsl = """
                COMPUTE FEE MultipleFee
                YIELD 100
                YIELD 50
                YIELD 25
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            Assert.Equal(175, provenance.GrandTotal);
            Assert.Equal(3, provenance.FeeProvenances[0].ContributingRecords.Count());
        }

        [Fact]
        public void ComputeWithProvenance_OptionalFee_SeparatesFromMandatory()
        {
            string dsl = """
                COMPUTE FEE MandatoryFee
                YIELD 100
                ENDCOMPUTE

                COMPUTE FEE OptionalFee OPTIONAL
                YIELD 50
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            Assert.Equal(100, provenance.TotalMandatory);
            Assert.Equal(50, provenance.TotalOptional);
            Assert.Equal(150, provenance.GrandTotal);

            Assert.False(provenance.FeeProvenances[0].IsOptional);
            Assert.True(provenance.FeeProvenances[1].IsOptional);
        }

        [Fact]
        public void ComputeWithProvenance_RecordsInputValues()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Claims'
                BETWEEN 1 AND 100
                DEFAULT 10
                ENDDEFINE

                DEFINE BOOLEAN IsUrgent AS 'Urgent'
                DEFAULT FALSE
                ENDDEFINE

                COMPUTE FEE TestFee
                YIELD 100
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueNumber("ClaimCount", 15),
                new IPFValueBoolean("IsUrgent", true)
            };

            var provenance = calculator.ComputeWithProvenance(inputs);

            Assert.Equal(15m, provenance.InputValues["ClaimCount"]);
            Assert.Equal(true, provenance.InputValues["IsUrgent"]);
        }

        #endregion

        #region Counterfactual Tests

        [Fact]
        public void ComputeWithCounterfactuals_BooleanInput_GeneratesOpposite()
        {
            string dsl = """
                DEFINE BOOLEAN IsUrgent AS 'Urgent processing'
                DEFAULT FALSE
                ENDDEFINE

                COMPUTE FEE UrgentFee
                YIELD 500 IF IsUrgent EQ TRUE
                YIELD 100 IF IsUrgent EQ FALSE
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueBoolean("IsUrgent", false)
            };

            var provenance = calculator.ComputeWithCounterfactuals(inputs);

            Assert.Equal(100, provenance.GrandTotal);
            Assert.Single(provenance.Counterfactuals);

            var cf = provenance.Counterfactuals[0];
            Assert.Equal("IsUrgent", cf.InputName);
            Assert.Equal(false, cf.OriginalValue);
            Assert.Equal(true, cf.AlternativeValue);
            Assert.Equal(500, cf.AlternativeTotal);
            Assert.Equal(400, cf.Difference); // +400 if urgent
        }

        [Fact]
        public void ComputeWithCounterfactuals_ListInput_GeneratesAlternatives()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal'
                CHOICE Small AS 'Small'
                CHOICE Micro AS 'Micro'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE EntityFee
                YIELD 1000 IF EntityType EQ Normal
                YIELD 500 IF EntityType EQ Small
                YIELD 250 IF EntityType EQ Micro
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueString("EntityType", "Normal")
            };

            var provenance = calculator.ComputeWithCounterfactuals(inputs);

            Assert.Equal(1000, provenance.GrandTotal);

            // Should have counterfactuals for Small and Micro
            Assert.Equal(2, provenance.Counterfactuals.Count);

            var smallCf = provenance.Counterfactuals.First(cf => cf.AlternativeValue.ToString() == "Small");
            Assert.Equal(500, smallCf.AlternativeTotal);
            Assert.Equal(-500, smallCf.Difference);

            var microCf = provenance.Counterfactuals.First(cf => cf.AlternativeValue.ToString() == "Micro");
            Assert.Equal(250, microCf.AlternativeTotal);
            Assert.Equal(-750, microCf.Difference);
        }

        [Fact]
        public void ComputeWithCounterfactuals_NumericInput_GeneratesBoundaryValues()
        {
            string dsl = """
                DEFINE NUMBER ClaimCount AS 'Number of claims'
                BETWEEN 1 AND 100
                DEFAULT 10
                ENDDEFINE

                COMPUTE FEE ClaimFee
                YIELD ClaimCount * 10
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueNumber("ClaimCount", 50)
            };

            var provenance = calculator.ComputeWithCounterfactuals(inputs);

            Assert.Equal(500, provenance.GrandTotal); // 50 * 10

            // Should have counterfactuals for boundaries (1 and 100)
            // Convert to string for reliable comparison since AlternativeValue is boxed
            var altValues = provenance.Counterfactuals.Select(cf => cf.AlternativeValue?.ToString()).ToList();
            Assert.Contains("1", altValues);
            Assert.Contains("100", altValues);
        }

        #endregion

        #region Export Tests

        [Fact]
        public void ProvenanceExporter_ToJson_ProducesValidJson()
        {
            string dsl = """
                COMPUTE FEE TestFee
                YIELD 100
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            var exporter = new ProvenanceExporter();
            var json = exporter.ToJson(provenance);

            Assert.Contains("\"grandTotal\"", json);
            Assert.Contains("100", json);
            Assert.Contains("\"fees\"", json);
            Assert.Contains("\"TestFee\"", json);
        }

        [Fact]
        public void ProvenanceExporter_ToMarkdown_ProducesValidMarkdown()
        {
            string dsl = """
                COMPUTE FEE TestFee
                YIELD 100
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            var exporter = new ProvenanceExporter();
            var markdown = exporter.ToMarkdown(provenance);

            Assert.Contains("# Fee Calculation Provenance", markdown);
            Assert.Contains("## Summary", markdown);
            Assert.Contains("TestFee", markdown);
            Assert.Contains("100", markdown);
        }

        [Fact]
        public void ProvenanceExporter_ToLegalCitation_ProducesFormattedReport()
        {
            string dsl = """
                COMPUTE FEE FilingFee
                YIELD 500
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            var exporter = new ProvenanceExporter();
            var legal = exporter.ToLegalCitation(provenance);

            Assert.Contains("LEGAL FEE CALCULATION REPORT", legal);
            Assert.Contains("CALCULATION SUMMARY", legal);
            Assert.Contains("FEE BREAKDOWN", legal);
            Assert.Contains("FilingFee", legal);
        }

        [Fact]
        public void ProvenanceExporter_WithCounterfactuals_IncludesWhatIfAnalysis()
        {
            string dsl = """
                DEFINE BOOLEAN IsUrgent AS 'Urgent'
                DEFAULT FALSE
                ENDDEFINE

                COMPUTE FEE UrgentFee
                YIELD 500 IF IsUrgent EQ TRUE
                YIELD 100 IF IsUrgent EQ FALSE
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueBoolean("IsUrgent", false)
            };

            var provenance = calculator.ComputeWithCounterfactuals(inputs);

            var exporter = new ProvenanceExporter();
            var markdown = exporter.ToMarkdown(provenance);

            Assert.Contains("What-If Analysis", markdown);
            Assert.Contains("IsUrgent", markdown);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ComputationProvenance_ToString_ProducesReadableOutput()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE TestFee
                YIELD 100
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueString("EntityType", "Normal")
            };

            var provenance = calculator.ComputeWithProvenance(inputs);
            var output = provenance.ToString();

            Assert.Contains("COMPUTATION PROVENANCE", output);
            Assert.Contains("INPUT VALUES", output);
            Assert.Contains("EntityType = Normal", output);
            Assert.Contains("FEE BREAKDOWN", output);
            Assert.Contains("Grand Total", output);
        }

        [Fact]
        public void FeeProvenance_ToString_ShowsContributionsAndSkipped()
        {
            string dsl = """
                DEFINE LIST EntityType AS 'Entity type'
                CHOICE Normal AS 'Normal'
                CHOICE Small AS 'Small'
                DEFAULT Normal
                ENDDEFINE

                COMPUTE FEE TestFee
                YIELD 100 IF EntityType EQ Normal
                YIELD 50 IF EntityType EQ Small
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueString("EntityType", "Normal")
            };

            var provenance = calculator.ComputeWithProvenance(inputs);
            var feeOutput = provenance.FeeProvenances[0].ToString();

            Assert.Contains("Fee: TestFee", feeOutput);
            Assert.Contains("Contributions:", feeOutput);
            Assert.Contains("100", feeOutput);
            Assert.Contains("Skipped:", feeOutput);
        }

        [Fact]
        public void Counterfactual_ToString_ShowsChange()
        {
            var cf = new Counterfactual("EntityType", "Normal", "Small", 1000, 500);
            var output = cf.ToString();

            Assert.Contains("EntityType", output);
            Assert.Contains("Small", output);
            Assert.Contains("Normal", output);
            Assert.Contains("-500", output);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ComputeWithProvenance_EmptyFee_ReturnsZeroTotal()
        {
            string dsl = """
                COMPUTE FEE EmptyFee
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            Assert.Equal(0, provenance.GrandTotal);
            Assert.Single(provenance.FeeProvenances);
            Assert.Empty(provenance.FeeProvenances[0].Records);
        }

        [Fact]
        public void ComputeWithProvenance_AllConditionsFalse_ReturnsZeroWithSkippedRecords()
        {
            string dsl = """
                DEFINE BOOLEAN Flag AS 'Flag'
                DEFAULT FALSE
                ENDDEFINE

                COMPUTE FEE ConditionalFee
                YIELD 100 IF Flag EQ TRUE
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var inputs = new List<IPFValue>
            {
                new IPFValueBoolean("Flag", false)
            };

            var provenance = calculator.ComputeWithProvenance(inputs);

            Assert.Equal(0, provenance.GrandTotal);
            var fee = provenance.FeeProvenances[0];
            Assert.Empty(fee.ContributingRecords);
            Assert.Single(fee.SkippedRecords);
        }

        [Fact]
        public void ComputeWithProvenance_MultipleFees_TracksAll()
        {
            string dsl = """
                COMPUTE FEE Fee1
                YIELD 100
                ENDCOMPUTE

                COMPUTE FEE Fee2
                YIELD 200
                ENDCOMPUTE

                COMPUTE FEE Fee3
                YIELD 300
                ENDCOMPUTE
                """;

            var calculator = new DslCalculator(new DslParser());
            Assert.True(calculator.Parse(dsl));

            var provenance = calculator.ComputeWithProvenance(new List<IPFValue>());

            Assert.Equal(3, provenance.FeeProvenances.Count);
            Assert.Equal(600, provenance.GrandTotal);

            var breakdown = provenance.FeeBreakdown.ToList();
            Assert.Contains(breakdown, b => b.FeeName == "Fee1" && b.Amount == 100);
            Assert.Contains(breakdown, b => b.FeeName == "Fee2" && b.Amount == 200);
            Assert.Contains(breakdown, b => b.FeeName == "Fee3" && b.Amount == 300);
        }

        #endregion
    }
}
