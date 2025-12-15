namespace IPFLang.Provenance
{
    /// <summary>
    /// A single provenance entry tracking one contribution to a fee.
    /// Records which rule fired, what condition matched, and the contribution amount.
    /// </summary>
    public class ProvenanceRecord
    {
        /// <summary>
        /// The fee this contribution belongs to
        /// </summary>
        public string FeeName { get; init; }

        /// <summary>
        /// The CASE condition that was evaluated (if any)
        /// </summary>
        public string? CaseCondition { get; init; }

        /// <summary>
        /// Whether the case condition was true
        /// </summary>
        public bool CaseConditionResult { get; init; }

        /// <summary>
        /// The YIELD IF condition (if any)
        /// </summary>
        public string? YieldCondition { get; init; }

        /// <summary>
        /// Whether the yield condition was true
        /// </summary>
        public bool YieldConditionResult { get; init; }

        /// <summary>
        /// The expression that was evaluated
        /// </summary>
        public string Expression { get; init; }

        /// <summary>
        /// The contribution amount (may be 0 if condition was false)
        /// </summary>
        public decimal Contribution { get; init; }

        /// <summary>
        /// Whether this yield actually contributed to the total
        /// </summary>
        public bool DidContribute => CaseConditionResult && YieldConditionResult;

        /// <summary>
        /// Input values that were referenced in the evaluation
        /// </summary>
        public Dictionary<string, object> ReferencedInputs { get; init; } = new();

        /// <summary>
        /// LET variable values used in the evaluation
        /// </summary>
        public Dictionary<string, decimal> LetVariables { get; init; } = new();

        public ProvenanceRecord(string feeName, string expression)
        {
            FeeName = feeName;
            Expression = expression;
            CaseConditionResult = true; // default for unconditional cases
            YieldConditionResult = true; // default for unconditional yields
        }

        public override string ToString()
        {
            var parts = new List<string>();
            parts.Add($"Fee: {FeeName}");

            if (!string.IsNullOrEmpty(CaseCondition))
            {
                parts.Add($"CASE [{CaseCondition}] = {CaseConditionResult}");
            }

            if (!string.IsNullOrEmpty(YieldCondition))
            {
                parts.Add($"IF [{YieldCondition}] = {YieldConditionResult}");
            }

            parts.Add($"YIELD [{Expression}] = {Contribution:0.00}");

            if (DidContribute)
            {
                parts.Add($"(contributed {Contribution:0.00})");
            }
            else
            {
                parts.Add("(skipped)");
            }

            return string.Join(" | ", parts);
        }
    }

    /// <summary>
    /// Provenance for a single fee computation
    /// </summary>
    public class FeeProvenance
    {
        /// <summary>
        /// Name of the fee
        /// </summary>
        public string FeeName { get; init; }

        /// <summary>
        /// Whether this is an optional fee
        /// </summary>
        public bool IsOptional { get; init; }

        /// <summary>
        /// Individual provenance records for each yield evaluated
        /// </summary>
        public List<ProvenanceRecord> Records { get; } = new();

        /// <summary>
        /// LET variable values computed for this fee
        /// </summary>
        public Dictionary<string, decimal> LetVariables { get; } = new();

        /// <summary>
        /// Total amount for this fee
        /// </summary>
        public decimal TotalAmount => Records.Where(r => r.DidContribute).Sum(r => r.Contribution);

        /// <summary>
        /// Records that actually contributed
        /// </summary>
        public IEnumerable<ProvenanceRecord> ContributingRecords => Records.Where(r => r.DidContribute);

        /// <summary>
        /// Records that were skipped
        /// </summary>
        public IEnumerable<ProvenanceRecord> SkippedRecords => Records.Where(r => !r.DidContribute);

        public FeeProvenance(string feeName, bool isOptional = false)
        {
            FeeName = feeName;
            IsOptional = isOptional;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Fee: {FeeName} {(IsOptional ? "(Optional)" : "")} ===");
            sb.AppendLine($"Total: {TotalAmount:0.00}");

            if (LetVariables.Any())
            {
                sb.AppendLine("LET Variables:");
                foreach (var (name, value) in LetVariables)
                {
                    sb.AppendLine($"  {name} = {value:0.00}");
                }
            }

            sb.AppendLine("Contributions:");
            foreach (var record in ContributingRecords)
            {
                sb.AppendLine($"  + {record.Contribution:0.00} from [{record.Expression}]");
                if (!string.IsNullOrEmpty(record.CaseCondition))
                {
                    sb.AppendLine($"      CASE: {record.CaseCondition}");
                }
                if (!string.IsNullOrEmpty(record.YieldCondition))
                {
                    sb.AppendLine($"      IF: {record.YieldCondition}");
                }
            }

            if (SkippedRecords.Any())
            {
                sb.AppendLine("Skipped:");
                foreach (var record in SkippedRecords)
                {
                    var reason = !record.CaseConditionResult
                        ? $"CASE [{record.CaseCondition}] was false"
                        : $"IF [{record.YieldCondition}] was false";
                    sb.AppendLine($"  - [{record.Expression}]: {reason}");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Complete provenance for an entire computation
    /// </summary>
    public class ComputationProvenance
    {
        /// <summary>
        /// Input values provided for the computation
        /// </summary>
        public Dictionary<string, object> InputValues { get; } = new();

        /// <summary>
        /// Provenance for each fee
        /// </summary>
        public List<FeeProvenance> FeeProvenances { get; } = new();

        /// <summary>
        /// Total mandatory amount
        /// </summary>
        public decimal TotalMandatory => FeeProvenances.Where(f => !f.IsOptional).Sum(f => f.TotalAmount);

        /// <summary>
        /// Total optional amount
        /// </summary>
        public decimal TotalOptional => FeeProvenances.Where(f => f.IsOptional).Sum(f => f.TotalAmount);

        /// <summary>
        /// Grand total
        /// </summary>
        public decimal GrandTotal => TotalMandatory + TotalOptional;

        /// <summary>
        /// Counterfactuals generated for this computation
        /// </summary>
        public List<Counterfactual> Counterfactuals { get; } = new();

        /// <summary>
        /// Get all contributing records across all fees
        /// </summary>
        public IEnumerable<ProvenanceRecord> AllContributions =>
            FeeProvenances.SelectMany(f => f.ContributingRecords);

        /// <summary>
        /// Get breakdown by fee
        /// </summary>
        public IEnumerable<(string FeeName, decimal Amount, bool IsOptional)> FeeBreakdown =>
            FeeProvenances.Select(f => (f.FeeName, f.TotalAmount, f.IsOptional));

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("       COMPUTATION PROVENANCE");
            sb.AppendLine("========================================");
            sb.AppendLine();

            // Input values
            sb.AppendLine("INPUT VALUES:");
            foreach (var (name, value) in InputValues)
            {
                sb.AppendLine($"  {name} = {value}");
            }
            sb.AppendLine();

            // Fee breakdown
            sb.AppendLine("FEE BREAKDOWN:");
            foreach (var fee in FeeProvenances)
            {
                var optLabel = fee.IsOptional ? " (optional)" : "";
                sb.AppendLine($"  {fee.FeeName}{optLabel}: {fee.TotalAmount:0.00}");
            }
            sb.AppendLine($"  ────────────────────");
            sb.AppendLine($"  Mandatory Total: {TotalMandatory:0.00}");
            sb.AppendLine($"  Optional Total:  {TotalOptional:0.00}");
            sb.AppendLine($"  Grand Total:     {GrandTotal:0.00}");
            sb.AppendLine();

            // Detailed provenance
            sb.AppendLine("DETAILED PROVENANCE:");
            foreach (var fee in FeeProvenances)
            {
                sb.AppendLine(fee.ToString());
            }

            // Counterfactuals
            if (Counterfactuals.Any())
            {
                sb.AppendLine();
                sb.AppendLine("COUNTERFACTUALS:");
                foreach (var cf in Counterfactuals)
                {
                    sb.AppendLine($"  {cf}");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A counterfactual explanation showing what would happen with different inputs
    /// </summary>
    public class Counterfactual
    {
        /// <summary>
        /// The input that was changed
        /// </summary>
        public string InputName { get; init; }

        /// <summary>
        /// Original value
        /// </summary>
        public object OriginalValue { get; init; }

        /// <summary>
        /// Alternative value considered
        /// </summary>
        public object AlternativeValue { get; init; }

        /// <summary>
        /// Original total
        /// </summary>
        public decimal OriginalTotal { get; init; }

        /// <summary>
        /// Total with alternative value
        /// </summary>
        public decimal AlternativeTotal { get; init; }

        /// <summary>
        /// Difference (positive = increase, negative = decrease)
        /// </summary>
        public decimal Difference => AlternativeTotal - OriginalTotal;

        public Counterfactual(string inputName, object originalValue, object alternativeValue,
            decimal originalTotal, decimal alternativeTotal)
        {
            InputName = inputName;
            OriginalValue = originalValue;
            AlternativeValue = alternativeValue;
            OriginalTotal = originalTotal;
            AlternativeTotal = alternativeTotal;
        }

        public override string ToString()
        {
            var change = Difference >= 0 ? $"+{Difference:0.00}" : $"{Difference:0.00}";
            return $"If {InputName} were {AlternativeValue} instead of {OriginalValue}: " +
                   $"total would be {AlternativeTotal:0.00} ({change})";
        }
    }
}
