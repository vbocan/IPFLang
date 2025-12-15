using IPFLang.Engine;
using IPFLang.Evaluator;
using IPFLang.Parser;
using IPFLang.Provenance;

namespace IPFLang.Versioning
{
    /// <summary>
    /// Enables querying fee calculations at specific points in time using versioned scripts
    /// </summary>
    public class TemporalQuery
    {
        private readonly VersionedScript _versionedScript;
        private readonly Func<ParsedScript, IDslCalculator> _calculatorFactory;

        public TemporalQuery(VersionedScript versionedScript, Func<ParsedScript, IDslCalculator> calculatorFactory)
        {
            _versionedScript = versionedScript ?? throw new ArgumentNullException(nameof(versionedScript));
            _calculatorFactory = calculatorFactory ?? throw new ArgumentNullException(nameof(calculatorFactory));
        }

        /// <summary>
        /// Compute fees using the version effective at a specific date
        /// </summary>
        public TemporalResult ComputeAtDate(DateOnly date, IEnumerable<IPFValue> inputs)
        {
            var versionInfo = _versionedScript.GetVersionAtDate(date);
            if (versionInfo == null)
            {
                return new TemporalResult(
                    date,
                    null,
                    0,
                    0,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<(string, string)>(),
                    $"No version effective on {date:yyyy-MM-dd}"
                );
            }

            var (version, script) = versionInfo.Value;
            var calculator = _calculatorFactory(script);
            var (mandatory, optional, steps, returns) = calculator.Compute(inputs);

            return new TemporalResult(
                date,
                version,
                mandatory,
                optional,
                steps,
                returns,
                null
            );
        }

        /// <summary>
        /// Compute fees with provenance at a specific date
        /// </summary>
        public TemporalProvenanceResult ComputeWithProvenanceAtDate(DateOnly date, IEnumerable<IPFValue> inputs)
        {
            var versionInfo = _versionedScript.GetVersionAtDate(date);
            if (versionInfo == null)
            {
                return new TemporalProvenanceResult(
                    date,
                    null,
                    null,
                    $"No version effective on {date:yyyy-MM-dd}"
                );
            }

            var (version, script) = versionInfo.Value;
            var calculator = _calculatorFactory(script);
            var provenance = calculator.ComputeWithProvenance(inputs);

            return new TemporalProvenanceResult(date, version, provenance, null);
        }

        /// <summary>
        /// Compare fee calculations across two dates
        /// </summary>
        public TemporalComparison CompareAcrossDates(
            DateOnly fromDate,
            DateOnly toDate,
            IEnumerable<IPFValue> inputs)
        {
            var fromResult = ComputeAtDate(fromDate, inputs);
            var toResult = ComputeAtDate(toDate, inputs);

            return new TemporalComparison(fromDate, toDate, fromResult, toResult);
        }

        /// <summary>
        /// Get all versions between two dates
        /// </summary>
        public IEnumerable<Version> GetVersionsBetween(DateOnly startDate, DateOnly endDate)
        {
            return _versionedScript.Versions
                .Where(v => v.EffectiveDate >= startDate && v.EffectiveDate <= endDate);
        }

        /// <summary>
        /// Verify that a change preserves completeness across versions
        /// </summary>
        public CompletenessPreservationResult VerifyCompletenessPreserved(
            DateOnly fromDate,
            DateOnly toDate)
        {
            var fromVersion = _versionedScript.GetVersionAtDate(fromDate);
            var toVersion = _versionedScript.GetVersionAtDate(toDate);

            if (fromVersion == null || toVersion == null)
            {
                return new CompletenessPreservationResult(
                    false,
                    "One or both versions not found",
                    null,
                    null
                );
            }

            var fromCalc = _calculatorFactory(fromVersion.Value.Script);
            var toCalc = _calculatorFactory(toVersion.Value.Script);

            var fromCompleteness = fromCalc.VerifyCompleteness();
            var toCompleteness = toCalc.VerifyCompleteness();

            bool preserved = fromCompleteness.AllComplete == toCompleteness.AllComplete;

            return new CompletenessPreservationResult(
                preserved,
                preserved ? "Completeness preserved" : "Completeness NOT preserved",
                fromCompleteness,
                toCompleteness
            );
        }

        /// <summary>
        /// Verify that a change preserves monotonicity for a specific fee
        /// </summary>
        public MonotonicityPreservationResult VerifyMonotonicityPreserved(
            DateOnly fromDate,
            DateOnly toDate,
            string feeName,
            string withRespectTo)
        {
            var fromVersion = _versionedScript.GetVersionAtDate(fromDate);
            var toVersion = _versionedScript.GetVersionAtDate(toDate);

            if (fromVersion == null || toVersion == null)
            {
                return new MonotonicityPreservationResult(
                    false,
                    "One or both versions not found",
                    null,
                    null
                );
            }

            var fromCalc = _calculatorFactory(fromVersion.Value.Script);
            var toCalc = _calculatorFactory(toVersion.Value.Script);

            var fromMonotonicity = fromCalc.VerifyMonotonicity(feeName, withRespectTo);
            var toMonotonicity = toCalc.VerifyMonotonicity(feeName, withRespectTo);

            bool preserved = fromMonotonicity.IsMonotonic == toMonotonicity.IsMonotonic;

            return new MonotonicityPreservationResult(
                preserved,
                preserved ? "Monotonicity preserved" : "Monotonicity NOT preserved",
                fromMonotonicity,
                toMonotonicity
            );
        }
    }

    /// <summary>
    /// Result of a temporal query
    /// </summary>
    public record TemporalResult(
        DateOnly QueryDate,
        Version? ApplicableVersion,
        decimal MandatoryTotal,
        decimal OptionalTotal,
        IEnumerable<string> ComputationSteps,
        IEnumerable<(string, string)> Returns,
        string? Error
    )
    {
        public bool IsSuccess => Error == null;
        public decimal GrandTotal => MandatoryTotal + OptionalTotal;

        public override string ToString()
        {
            if (!IsSuccess)
                return $"Query for {QueryDate:yyyy-MM-dd}: {Error}";

            return $"Query for {QueryDate:yyyy-MM-dd} (Version {ApplicableVersion?.Id}):\n" +
                   $"  Mandatory: {MandatoryTotal:C}\n" +
                   $"  Optional: {OptionalTotal:C}\n" +
                   $"  Total: {GrandTotal:C}";
        }
    }

    /// <summary>
    /// Result of a temporal query with provenance
    /// </summary>
    public record TemporalProvenanceResult(
        DateOnly QueryDate,
        Version? ApplicableVersion,
        ComputationProvenance? Provenance,
        string? Error
    )
    {
        public bool IsSuccess => Error == null;

        public override string ToString()
        {
            if (!IsSuccess)
                return $"Query for {QueryDate:yyyy-MM-dd}: {Error}";

            return $"Query for {QueryDate:yyyy-MM-dd} (Version {ApplicableVersion?.Id}):\n" +
                   $"  Total: {Provenance?.GrandTotal:C}\n" +
                   $"  Fees: {Provenance?.FeeProvenances.Count}";
        }
    }

    /// <summary>
    /// Comparison of fee calculations across two dates
    /// </summary>
    public record TemporalComparison(
        DateOnly FromDate,
        DateOnly ToDate,
        TemporalResult FromResult,
        TemporalResult ToResult
    )
    {
        public decimal TotalDifference => ToResult.GrandTotal - FromResult.GrandTotal;
        public decimal PercentageChange => FromResult.GrandTotal != 0
            ? (TotalDifference / FromResult.GrandTotal) * 100
            : 0;

        public override string ToString()
        {
            var summary = $"Temporal Comparison: {FromDate:yyyy-MM-dd} → {ToDate:yyyy-MM-dd}\n";
            summary += $"  From (v{FromResult.ApplicableVersion?.Id}): {FromResult.GrandTotal:C}\n";
            summary += $"  To (v{ToResult.ApplicableVersion?.Id}): {ToResult.GrandTotal:C}\n";
            summary += $"  Difference: {TotalDifference:C} ({PercentageChange:+0.00;-0.00}%)\n";
            return summary;
        }
    }

    /// <summary>
    /// Result of completeness preservation verification
    /// </summary>
    public record CompletenessPreservationResult(
        bool IsPreserved,
        string Message,
        Analysis.CompletenessReport? FromReport,
        Analysis.CompletenessReport? ToReport
    );

    /// <summary>
    /// Result of monotonicity preservation verification
    /// </summary>
    public record MonotonicityPreservationResult(
        bool IsPreserved,
        string Message,
        Analysis.MonotonicityReport? FromReport,
        Analysis.MonotonicityReport? ToReport
    );
}
