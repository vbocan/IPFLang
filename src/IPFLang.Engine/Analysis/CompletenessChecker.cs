using IPFLang.Parser;

namespace IPFLang.Analysis
{
    /// <summary>
    /// Verifies that fee definitions cover all possible input combinations.
    /// Identifies gaps where no yield condition matches.
    /// </summary>
    public class CompletenessChecker
    {
        private readonly DomainAnalyzer _domainAnalyzer = new();
        private readonly ConditionExtractor _conditionExtractor = new();

        /// <summary>
        /// Check completeness of all fees against the input domains
        /// </summary>
        public CompletenessReport CheckCompleteness(
            IEnumerable<DslInput> inputs,
            IEnumerable<DslFee> fees)
        {
            var domains = _domainAnalyzer.ExtractDomains(inputs).ToList();
            var feeList = fees.ToList();

            var report = new CompletenessReport();

            foreach (var fee in feeList)
            {
                var feeReport = CheckFeeCompleteness(fee, domains);
                report.FeeReports.Add(feeReport);
            }

            return report;
        }

        /// <summary>
        /// Check completeness of a single fee
        /// </summary>
        public FeeCompletenessReport CheckFeeCompleteness(DslFee fee, IEnumerable<InputDomain> domains)
        {
            var domainList = domains.ToList();
            var conditions = _conditionExtractor.ExtractFeeConditions(fee).ToList();

            // Get variables referenced in fee conditions
            var referencedVars = conditions
                .SelectMany(c => c.Condition.GetReferencedVariables())
                .Distinct()
                .ToHashSet();

            // Filter domains to only those referenced in conditions
            var relevantDomains = domainList
                .Where(d => referencedVars.Contains(d.VariableName))
                .ToList();

            var report = new FeeCompletenessReport(fee.Name);

            if (!relevantDomains.Any())
            {
                // No conditions reference any inputs - fee is unconditional
                if (conditions.All(c => c.Condition is TrueExpression))
                {
                    report.IsComplete = true;
                    report.Notes.Add("Fee has unconditional yield - always produces output");
                }
                else
                {
                    report.IsComplete = false;
                    report.Notes.Add("Fee has no yields");
                }
                return report;
            }

            // Check domain size
            var totalSize = _domainAnalyzer.CalculateTotalDomainSize(relevantDomains);

            if (totalSize == null || totalSize > 1_000_000)
            {
                // Use sampling for large domains
                return CheckCompletenessWithSampling(fee, conditions, relevantDomains, report);
            }

            // Exhaustive check for small domains
            return CheckCompletenessExhaustive(fee, conditions, relevantDomains, report);
        }

        private FeeCompletenessReport CheckCompletenessExhaustive(
            DslFee fee,
            List<FeeCondition> conditions,
            List<InputDomain> domains,
            FeeCompletenessReport report)
        {
            report.VerificationMethod = "Exhaustive enumeration";
            long checkedCount = 0;
            var gaps = new List<InputCombination>();

            foreach (var combination in _domainAnalyzer.GenerateAllCombinations(domains))
            {
                checkedCount++;
                bool anyMatch = conditions.Any(c => c.Condition.Evaluate(combination));

                if (!anyMatch)
                {
                    gaps.Add(combination);

                    // Limit gap reporting
                    if (gaps.Count >= 100)
                    {
                        report.Notes.Add($"Gap detection stopped at 100 examples (more may exist)");
                        break;
                    }
                }
            }

            report.TotalCombinationsChecked = checkedCount;
            report.Gaps = gaps;
            report.IsComplete = gaps.Count == 0;

            if (report.IsComplete)
            {
                report.Notes.Add($"All {checkedCount} input combinations are covered");
            }
            else
            {
                report.Notes.Add($"Found {gaps.Count} uncovered input combination(s)");
            }

            return report;
        }

        private FeeCompletenessReport CheckCompletenessWithSampling(
            DslFee fee,
            List<FeeCondition> conditions,
            List<InputDomain> domains,
            FeeCompletenessReport report)
        {
            report.VerificationMethod = "Representative sampling";
            long checkedCount = 0;
            var gaps = new List<InputCombination>();

            foreach (var combination in _domainAnalyzer.GenerateRepresentativeCombinations(domains))
            {
                checkedCount++;
                bool anyMatch = conditions.Any(c => c.Condition.Evaluate(combination));

                if (!anyMatch)
                {
                    gaps.Add(combination);

                    if (gaps.Count >= 100)
                    {
                        report.Notes.Add("Gap detection stopped at 100 examples");
                        break;
                    }
                }
            }

            report.TotalCombinationsChecked = checkedCount;
            report.Gaps = gaps;
            report.IsComplete = gaps.Count == 0;

            if (report.IsComplete)
            {
                report.Notes.Add($"All {checkedCount} sampled combinations are covered");
                report.Notes.Add("Note: Sampling may miss edge cases. Use exhaustive check for critical fees.");
            }
            else
            {
                report.Notes.Add($"Found {gaps.Count} uncovered input combination(s) in sample");
            }

            return report;
        }

        /// <summary>
        /// Quick check if a fee has at least one unconditional yield
        /// </summary>
        public bool HasUnconditionalYield(DslFee fee)
        {
            var conditions = _conditionExtractor.ExtractFeeConditions(fee);
            return conditions.Any(c => c.Condition is TrueExpression);
        }
    }

    /// <summary>
    /// Complete report for all fees
    /// </summary>
    public class CompletenessReport
    {
        public List<FeeCompletenessReport> FeeReports { get; } = new();

        public bool AllComplete => FeeReports.All(r => r.IsComplete);

        public IEnumerable<FeeCompletenessReport> IncompleteFees =>
            FeeReports.Where(r => !r.IsComplete);

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Completeness Verification Report ===");
            sb.AppendLine();

            foreach (var feeReport in FeeReports)
            {
                sb.AppendLine(feeReport.ToString());
                sb.AppendLine();
            }

            sb.AppendLine($"Overall: {(AllComplete ? "COMPLETE" : "INCOMPLETE")}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Completeness report for a single fee
    /// </summary>
    public class FeeCompletenessReport
    {
        public string FeeName { get; }
        public bool IsComplete { get; set; }
        public string VerificationMethod { get; set; } = "Unknown";
        public long TotalCombinationsChecked { get; set; }
        public List<InputCombination> Gaps { get; set; } = new();
        public List<string> Notes { get; } = new();

        public FeeCompletenessReport(string feeName)
        {
            FeeName = feeName;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Fee: {FeeName}");
            sb.AppendLine($"  Status: {(IsComplete ? "COMPLETE" : "INCOMPLETE")}");
            sb.AppendLine($"  Method: {VerificationMethod}");
            sb.AppendLine($"  Combinations checked: {TotalCombinationsChecked}");

            if (Gaps.Any())
            {
                sb.AppendLine($"  Gaps found: {Gaps.Count}");
                foreach (var gap in Gaps.Take(5))
                {
                    sb.AppendLine($"    - {gap}");
                }
                if (Gaps.Count > 5)
                {
                    sb.AppendLine($"    ... and {Gaps.Count - 5} more");
                }
            }

            foreach (var note in Notes)
            {
                sb.AppendLine($"  Note: {note}");
            }

            return sb.ToString();
        }
    }
}
