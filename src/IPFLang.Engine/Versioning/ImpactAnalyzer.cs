using IPFLang.Analysis;
using IPFLang.Parser;

namespace IPFLang.Versioning
{
    /// <summary>
    /// Analyzes the impact of changes between versions on actual calculations
    /// </summary>
    public class ImpactAnalyzer
    {
        private readonly DomainAnalyzer _domainAnalyzer = new();

        /// <summary>
        /// Analyze the impact of changes on calculations
        /// </summary>
        public ImpactReport AnalyzeImpact(ChangeReport changeReport, ParsedScript oldScript, ParsedScript newScript)
        {
            var report = new ImpactReport(changeReport);

            // Analyze fee changes
            foreach (var feeChange in changeReport.FeeChanges.Where(c => c.Type == ChangeType.Modified))
            {
                AnalyzeFeeImpact(feeChange, oldScript, newScript, report);
            }

            // Analyze input changes
            foreach (var inputChange in changeReport.InputChanges.Where(c => c.Type == ChangeType.Modified))
            {
                AnalyzeInputImpact(inputChange, oldScript, newScript, report);
            }

            return report;
        }

        private void AnalyzeFeeImpact(FeeChange feeChange, ParsedScript oldScript, ParsedScript newScript, ImpactReport report)
        {
            var oldFee = oldScript.Fees.FirstOrDefault(f => f.Name == feeChange.FeeName);
            var newFee = newScript.Fees.FirstOrDefault(f => f.Name == feeChange.FeeName);

            if (oldFee == null || newFee == null) return;

            // Estimate affected scenarios
            var affectedScenarios = EstimateAffectedScenarios(oldFee, newFee, oldScript.Inputs, newScript.Inputs);

            report.FeeImpacts.Add(new FeeImpact(
                feeChange.FeeName,
                affectedScenarios,
                feeChange.IsBreaking
            ));
        }

        private void AnalyzeInputImpact(InputChange inputChange, ParsedScript oldScript, ParsedScript newScript, ImpactReport report)
        {
            // Count how many fees use this input
            var affectedFees = CountFeesUsingInput(inputChange.InputName, newScript.Fees);

            report.InputImpacts.Add(new InputImpact(
                inputChange.InputName,
                affectedFees,
                inputChange.IsBreaking
            ));
        }

        private int EstimateAffectedScenarios(DslFee oldFee, DslFee newFee, IEnumerable<DslInput> oldInputs, IEnumerable<DslInput> newInputs)
        {
            // For small input domains, we can enumerate all combinations
            var domains = _domainAnalyzer.ExtractDomains(newInputs.ToList()).ToList();
            var totalSize = _domainAnalyzer.CalculateTotalDomainSize(domains);

            // If domain is too large, return estimate
            if (totalSize == null || totalSize > 10000)
            {
                return (int?)totalSize ?? 1000000; // Return estimate
            }

            try
            {
                var combinations = _domainAnalyzer.GenerateAllCombinations(domains);
                return combinations.Count();
            }
            catch
            {
                // If generation fails, return domain size estimate
                return (int)totalSize.Value;
            }
        }

        private int CountFeesUsingInput(string inputName, IEnumerable<DslFee> fees)
        {
            int count = 0;
            foreach (var fee in fees)
            {
                // Check if fee references this input in conditions or expressions
                // This is a simplified check - a full implementation would parse expressions
                if (fee.ToString().Contains(inputName))
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Report of impact analysis
    /// </summary>
    public class ImpactReport
    {
        public ChangeReport ChangeReport { get; }
        public List<FeeImpact> FeeImpacts { get; } = new();
        public List<InputImpact> InputImpacts { get; } = new();

        public ImpactReport(ChangeReport changeReport)
        {
            ChangeReport = changeReport;
        }

        public int TotalAffectedScenarios => FeeImpacts.Sum(f => f.AffectedScenarios);
        public int TotalAffectedFees => InputImpacts.Sum(i => i.AffectedFees);

        public override string ToString()
        {
            var summary = $"Impact Analysis: {ChangeReport.FromVersion.Id} → {ChangeReport.ToVersion.Id}\n";
            summary += $"  Total affected scenarios: {TotalAffectedScenarios}\n";
            summary += $"  Total fees affected by input changes: {TotalAffectedFees}\n\n";

            if (FeeImpacts.Any())
            {
                summary += "Fee Impacts:\n";
                foreach (var impact in FeeImpacts)
                {
                    summary += $"  {impact}\n";
                }
                summary += "\n";
            }

            if (InputImpacts.Any())
            {
                summary += "Input Impacts:\n";
                foreach (var impact in InputImpacts)
                {
                    summary += $"  {impact}\n";
                }
            }

            return summary;
        }
    }

    /// <summary>
    /// Impact of a fee change
    /// </summary>
    public record FeeImpact(string FeeName, int AffectedScenarios, bool IsBreaking)
    {
        public override string ToString()
        {
            var breaking = IsBreaking ? " [BREAKING]" : "";
            return $"Fee '{FeeName}': {AffectedScenarios} scenarios affected{breaking}";
        }
    }

    /// <summary>
    /// Impact of an input change
    /// </summary>
    public record InputImpact(string InputName, int AffectedFees, bool IsBreaking)
    {
        public override string ToString()
        {
            var breaking = IsBreaking ? " [BREAKING]" : "";
            return $"Input '{InputName}': {AffectedFees} fees affected{breaking}";
        }
    }
}
