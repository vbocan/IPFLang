using IPFLang.Analysis;
using IPFLang.CurrencyConversion;
using IPFLang.Evaluator;
using IPFLang.Parser;
using IPFLang.Provenance;
using IPFLang.Types;

namespace IPFLang.Engine
{
    public interface IDslCalculator
    {
        void Reset();
        bool Parse(string text);
        (decimal, decimal, IEnumerable<string>, IEnumerable<(string, string)>) Compute(IEnumerable<IPFValue> InputValues);
        IEnumerable<string> GetErrors();
        IEnumerable<TypeError> GetTypeErrors();
        IEnumerable<DslFee> GetFees();
        IEnumerable<DslInput> GetInputs();
        IEnumerable<DslGroup> GetGroups();
        IEnumerable<DslReturn> GetReturns();
        IEnumerable<DslVerify> GetVerifications();
        void SetCurrencyConverter(ICurrencyConverter converter);

        /// <summary>
        /// Verify that all fees cover all possible input combinations
        /// </summary>
        CompletenessReport VerifyCompleteness();

        /// <summary>
        /// Verify that a specific fee is monotonic with respect to a numeric input
        /// </summary>
        MonotonicityReport VerifyMonotonicity(string feeName, string withRespectTo, MonotonicityDirection direction = MonotonicityDirection.NonDecreasing);

        /// <summary>
        /// Run all VERIFY directives from the parsed DSL and return combined results
        /// </summary>
        VerificationResults RunVerifications();

        /// <summary>
        /// Compute fees with full provenance tracking
        /// </summary>
        ComputationProvenance ComputeWithProvenance(IEnumerable<IPFValue> inputValues);

        /// <summary>
        /// Compute fees with provenance and counterfactual analysis
        /// </summary>
        ComputationProvenance ComputeWithCounterfactuals(IEnumerable<IPFValue> inputValues);
    }

    /// <summary>
    /// Combined results from running all VERIFY directives
    /// </summary>
    public class VerificationResults
    {
        public List<FeeCompletenessReport> CompletenessReports { get; } = new();
        public List<MonotonicityReport> MonotonicityReports { get; } = new();
        public List<string> Errors { get; } = new();

        public bool AllPassed =>
            CompletenessReports.All(r => r.IsComplete) &&
            MonotonicityReports.All(r => r.IsMonotonic) &&
            !Errors.Any();

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Verification Results ===");
            sb.AppendLine();

            if (CompletenessReports.Any())
            {
                sb.AppendLine("-- Completeness Checks --");
                foreach (var r in CompletenessReports)
                    sb.AppendLine(r.ToString());
            }

            if (MonotonicityReports.Any())
            {
                sb.AppendLine("-- Monotonicity Checks --");
                foreach (var r in MonotonicityReports)
                    sb.AppendLine(r.ToString());
            }

            if (Errors.Any())
            {
                sb.AppendLine("-- Errors --");
                foreach (var e in Errors)
                    sb.AppendLine($"  ERROR: {e}");
            }

            sb.AppendLine($"Overall: {(AllPassed ? "PASSED" : "FAILED")}");
            return sb.ToString();
        }
    }
}
