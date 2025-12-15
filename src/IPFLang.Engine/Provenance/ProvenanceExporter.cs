using System.Text.Json;
using System.Text.Json.Serialization;

namespace IPFLang.Provenance
{
    /// <summary>
    /// Exports provenance information in various formats
    /// </summary>
    public class ProvenanceExporter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Export provenance to JSON format
        /// </summary>
        public string ToJson(ComputationProvenance provenance)
        {
            var exportModel = new ProvenanceJsonModel
            {
                InputValues = provenance.InputValues.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value?.ToString() ?? "null"),
                TotalMandatory = provenance.TotalMandatory,
                TotalOptional = provenance.TotalOptional,
                GrandTotal = provenance.GrandTotal,
                Fees = provenance.FeeProvenances.Select(f => new FeeJsonModel
                {
                    Name = f.FeeName,
                    IsOptional = f.IsOptional,
                    Total = f.TotalAmount,
                    LetVariables = f.LetVariables,
                    Contributions = f.ContributingRecords.Select(r => new ContributionJsonModel
                    {
                        Expression = r.Expression,
                        Amount = r.Contribution,
                        CaseCondition = r.CaseCondition,
                        YieldCondition = r.YieldCondition
                    }).ToList()
                }).ToList(),
                Counterfactuals = provenance.Counterfactuals.Select(c => new CounterfactualJsonModel
                {
                    Input = c.InputName,
                    OriginalValue = c.OriginalValue?.ToString() ?? "null",
                    AlternativeValue = c.AlternativeValue?.ToString() ?? "null",
                    OriginalTotal = c.OriginalTotal,
                    AlternativeTotal = c.AlternativeTotal,
                    Difference = c.Difference
                }).ToList()
            };

            return JsonSerializer.Serialize(exportModel, JsonOptions);
        }

        /// <summary>
        /// Export provenance to human-readable text format
        /// </summary>
        public string ToText(ComputationProvenance provenance)
        {
            return provenance.ToString();
        }

        /// <summary>
        /// Export provenance to legal citation format
        /// </summary>
        public string ToLegalCitation(ComputationProvenance provenance)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("LEGAL FEE CALCULATION REPORT");
            sb.AppendLine("============================");
            sb.AppendLine();

            // Summary
            sb.AppendLine("1. CALCULATION SUMMARY");
            sb.AppendLine($"   Grand Total: {provenance.GrandTotal:C2}");
            sb.AppendLine($"   - Mandatory Fees: {provenance.TotalMandatory:C2}");
            sb.AppendLine($"   - Optional Fees: {provenance.TotalOptional:C2}");
            sb.AppendLine();

            // Input parameters
            sb.AppendLine("2. INPUT PARAMETERS");
            int inputNum = 1;
            foreach (var (name, value) in provenance.InputValues)
            {
                sb.AppendLine($"   2.{inputNum}. {name}: {value}");
                inputNum++;
            }
            sb.AppendLine();

            // Fee-by-fee breakdown
            sb.AppendLine("3. FEE BREAKDOWN");
            int feeNum = 1;
            foreach (var fee in provenance.FeeProvenances)
            {
                sb.AppendLine($"   3.{feeNum}. {fee.FeeName}");
                sb.AppendLine($"       Subtotal: {fee.TotalAmount:C2}");
                if (fee.IsOptional)
                {
                    sb.AppendLine($"       (Optional fee)");
                }

                // Contributing rules
                int ruleNum = 1;
                foreach (var record in fee.ContributingRecords)
                {
                    sb.AppendLine($"       3.{feeNum}.{ruleNum}. Rule Application:");
                    sb.AppendLine($"           Expression: {record.Expression}");
                    sb.AppendLine($"           Contribution: {record.Contribution:C2}");

                    if (!string.IsNullOrEmpty(record.CaseCondition))
                    {
                        sb.AppendLine($"           Applicable when: {record.CaseCondition}");
                    }
                    if (!string.IsNullOrEmpty(record.YieldCondition))
                    {
                        sb.AppendLine($"           Condition satisfied: {record.YieldCondition}");
                    }
                    ruleNum++;
                }
                sb.AppendLine();
                feeNum++;
            }

            // Counterfactual analysis
            if (provenance.Counterfactuals.Any())
            {
                sb.AppendLine("4. SENSITIVITY ANALYSIS (Counterfactuals)");
                int cfNum = 1;
                foreach (var cf in provenance.Counterfactuals)
                {
                    var changeDesc = cf.Difference >= 0
                        ? $"increase of {cf.Difference:C2}"
                        : $"decrease of {Math.Abs(cf.Difference):C2}";

                    sb.AppendLine($"   4.{cfNum}. If {cf.InputName} were {cf.AlternativeValue}:");
                    sb.AppendLine($"       Total would be {cf.AlternativeTotal:C2} ({changeDesc})");
                    cfNum++;
                }
                sb.AppendLine();
            }

            // Certification
            sb.AppendLine("5. CERTIFICATION");
            sb.AppendLine($"   This calculation was performed on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("   All fee amounts are calculated according to the applicable fee schedule.");
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Export provenance to markdown format (for documentation)
        /// </summary>
        public string ToMarkdown(ComputationProvenance provenance)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("# Fee Calculation Provenance");
            sb.AppendLine();

            // Summary table
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine("| Category | Amount |");
            sb.AppendLine("|----------|--------|");
            sb.AppendLine($"| Mandatory Fees | {provenance.TotalMandatory:0.00} |");
            sb.AppendLine($"| Optional Fees | {provenance.TotalOptional:0.00} |");
            sb.AppendLine($"| **Grand Total** | **{provenance.GrandTotal:0.00}** |");
            sb.AppendLine();

            // Inputs
            sb.AppendLine("## Input Values");
            sb.AppendLine();
            sb.AppendLine("| Input | Value |");
            sb.AppendLine("|-------|-------|");
            foreach (var (name, value) in provenance.InputValues)
            {
                sb.AppendLine($"| {name} | {value} |");
            }
            sb.AppendLine();

            // Fee breakdown
            sb.AppendLine("## Fee Breakdown");
            sb.AppendLine();
            foreach (var fee in provenance.FeeProvenances)
            {
                var optional = fee.IsOptional ? " *(optional)*" : "";
                sb.AppendLine($"### {fee.FeeName}{optional}");
                sb.AppendLine();
                sb.AppendLine($"**Subtotal: {fee.TotalAmount:0.00}**");
                sb.AppendLine();

                if (fee.LetVariables.Any())
                {
                    sb.AppendLine("Local variables:");
                    foreach (var (name, value) in fee.LetVariables)
                    {
                        sb.AppendLine($"- `{name}` = {value:0.00}");
                    }
                    sb.AppendLine();
                }

                if (fee.ContributingRecords.Any())
                {
                    sb.AppendLine("| Expression | Condition | Amount |");
                    sb.AppendLine("|------------|-----------|--------|");
                    foreach (var record in fee.ContributingRecords)
                    {
                        var condition = record.YieldCondition ?? record.CaseCondition ?? "*always*";
                        sb.AppendLine($"| `{record.Expression}` | {condition} | {record.Contribution:0.00} |");
                    }
                    sb.AppendLine();
                }
            }

            // Counterfactuals
            if (provenance.Counterfactuals.Any())
            {
                sb.AppendLine("## What-If Analysis");
                sb.AppendLine();
                sb.AppendLine("| Scenario | Result | Change |");
                sb.AppendLine("|----------|--------|--------|");
                foreach (var cf in provenance.Counterfactuals)
                {
                    var change = cf.Difference >= 0 ? $"+{cf.Difference:0.00}" : $"{cf.Difference:0.00}";
                    sb.AppendLine($"| {cf.InputName} = {cf.AlternativeValue} | {cf.AlternativeTotal:0.00} | {change} |");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    #region JSON Models

    internal class ProvenanceJsonModel
    {
        public Dictionary<string, string> InputValues { get; set; } = new();
        public decimal TotalMandatory { get; set; }
        public decimal TotalOptional { get; set; }
        public decimal GrandTotal { get; set; }
        public List<FeeJsonModel> Fees { get; set; } = new();
        public List<CounterfactualJsonModel> Counterfactuals { get; set; } = new();
    }

    internal class FeeJsonModel
    {
        public string Name { get; set; } = "";
        public bool IsOptional { get; set; }
        public decimal Total { get; set; }
        public Dictionary<string, decimal> LetVariables { get; set; } = new();
        public List<ContributionJsonModel> Contributions { get; set; } = new();
    }

    internal class ContributionJsonModel
    {
        public string Expression { get; set; } = "";
        public decimal Amount { get; set; }
        public string? CaseCondition { get; set; }
        public string? YieldCondition { get; set; }
    }

    internal class CounterfactualJsonModel
    {
        public string Input { get; set; } = "";
        public string OriginalValue { get; set; } = "";
        public string AlternativeValue { get; set; } = "";
        public decimal OriginalTotal { get; set; }
        public decimal AlternativeTotal { get; set; }
        public decimal Difference { get; set; }
    }

    #endregion
}
