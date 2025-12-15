using IPFLang.Evaluator;
using IPFLang.Parser;

namespace IPFLang.Analysis
{
    /// <summary>
    /// Verifies that fee values are monotonic with respect to specified input variables.
    /// Monotonicity means: as the input increases, the fee never decreases (or vice versa).
    /// </summary>
    public class MonotonicityChecker
    {
        private readonly DomainAnalyzer _domainAnalyzer = new();

        /// <summary>
        /// Check monotonicity of a fee with respect to a numeric input
        /// </summary>
        public MonotonicityReport CheckMonotonicity(
            DslFee fee,
            IEnumerable<DslInput> allInputs,
            string withRespectTo,
            MonotonicityDirection expectedDirection = MonotonicityDirection.NonDecreasing)
        {
            var inputs = allInputs.ToList();
            var domains = _domainAnalyzer.ExtractDomains(inputs).ToList();

            // Find the target domain
            var targetDomain = domains.FirstOrDefault(d => d.VariableName == withRespectTo);
            if (targetDomain == null)
            {
                return new MonotonicityReport(fee.Name, withRespectTo)
                {
                    IsMonotonic = false,
                    Notes = { $"Variable '{withRespectTo}' not found in inputs" }
                };
            }

            if (targetDomain is not NumericDomain numDomain)
            {
                return new MonotonicityReport(fee.Name, withRespectTo)
                {
                    IsMonotonic = false,
                    Notes = { $"Variable '{withRespectTo}' is not numeric - monotonicity check requires numeric input" }
                };
            }

            // Get other domains for context
            var otherDomains = domains.Where(d => d.VariableName != withRespectTo).ToList();

            var report = new MonotonicityReport(fee.Name, withRespectTo)
            {
                ExpectedDirection = expectedDirection
            };

            // If no other domains, check directly
            if (!otherDomains.Any())
            {
                CheckMonotonicityForContext(fee, numDomain, new InputCombination(new Dictionary<string, DomainValue>()), expectedDirection, report);
            }
            else
            {
                // Check monotonicity for representative contexts
                foreach (var context in _domainAnalyzer.GenerateRepresentativeCombinations(otherDomains))
                {
                    CheckMonotonicityForContext(fee, numDomain, context, expectedDirection, report);

                    // Stop early if we find violations
                    if (report.Violations.Count >= 10)
                    {
                        report.Notes.Add("Stopped after finding 10 violations");
                        break;
                    }
                }
            }

            report.IsMonotonic = report.Violations.Count == 0;
            return report;
        }

        private void CheckMonotonicityForContext(
            DslFee fee,
            NumericDomain targetDomain,
            InputCombination context,
            MonotonicityDirection direction,
            MonotonicityReport report)
        {
            var values = targetDomain.GetRepresentativeValues(20).Cast<NumericValue>().OrderBy(v => v.Value).ToList();

            decimal? previousFeeValue = null;
            decimal? previousInputValue = null;

            foreach (var value in values)
            {
                // Build complete input combination
                var fullInputs = new Dictionary<string, DomainValue>(context.Values)
                {
                    [targetDomain.VariableName] = value
                };
                var combination = new InputCombination(fullInputs);

                // Evaluate the fee
                try
                {
                    var feeValue = EvaluateFee(fee, combination);
                    report.PointsChecked++;

                    if (previousFeeValue.HasValue && previousInputValue.HasValue)
                    {
                        bool isViolation = direction switch
                        {
                            MonotonicityDirection.NonDecreasing => feeValue < previousFeeValue,
                            MonotonicityDirection.NonIncreasing => feeValue > previousFeeValue,
                            MonotonicityDirection.StrictlyIncreasing => feeValue <= previousFeeValue,
                            MonotonicityDirection.StrictlyDecreasing => feeValue >= previousFeeValue,
                            _ => false
                        };

                        if (isViolation)
                        {
                            report.Violations.Add(new MonotonicityViolation(
                                context,
                                previousInputValue.Value,
                                previousFeeValue.Value,
                                value.Value,
                                feeValue
                            ));
                        }
                    }

                    previousFeeValue = feeValue;
                    previousInputValue = value.Value;
                }
                catch (Exception ex)
                {
                    report.Notes.Add($"Evaluation error at {value.Value}: {ex.Message}");
                }
            }
        }

        private decimal EvaluateFee(DslFee fee, InputCombination combination)
        {
            // Convert InputCombination to IPFValue list
            var ipfValues = combination.Values.Select(kv => ConvertToIPFValue(kv.Key, kv.Value)).ToList();

            decimal total = 0;

            // Evaluate LET variables first
            foreach (var letVar in fee.Vars)
            {
                var value = DslEvaluator.EvaluateExpression(letVar.ValueTokens.ToArray(), ipfValues, fee.Name);
                ipfValues.Add(new IPFValueNumber(letVar.Name, value));
            }

            // Evaluate each case/yield
            foreach (var item in fee.Cases)
            {
                if (item is DslFeeCase feeCase)
                {
                    var caseCondition = DslEvaluator.EvaluateLogic(feeCase.Condition.ToArray(), ipfValues, fee.Name);
                    if (caseCondition)
                    {
                        foreach (var yield in feeCase.Yields)
                        {
                            var yieldCondition = DslEvaluator.EvaluateLogic(yield.Condition.ToArray(), ipfValues, fee.Name);
                            if (yieldCondition)
                            {
                                total += DslEvaluator.EvaluateExpression(yield.Values.ToArray(), ipfValues, fee.Name);
                            }
                        }
                    }
                }
                else if (item is DslFeeYield directYield)
                {
                    var condition = DslEvaluator.EvaluateLogic(directYield.Condition.ToArray(), ipfValues, fee.Name);
                    if (condition)
                    {
                        total += DslEvaluator.EvaluateExpression(directYield.Values.ToArray(), ipfValues, fee.Name);
                    }
                }
            }

            return total;
        }

        private IPFValue ConvertToIPFValue(string name, DomainValue value)
        {
            return value switch
            {
                BooleanValue bv => new IPFValueBoolean(name, bv.Value),
                NumericValue nv => new IPFValueNumber(name, nv.Value),
                SymbolValue sv => new IPFValueString(name, sv.Symbol),
                DateValue dv => new IPFValueDate(name, dv.Value),
                MultiSelectValue msv => new IPFValueStringList(name, msv.SelectedSymbols),
                AmountValue av => new IPFValueAmount(name, av.Value, av.Currency),
                _ => throw new NotSupportedException($"Unknown domain value type: {value.GetType().Name}")
            };
        }
    }

    /// <summary>
    /// Expected direction of monotonicity
    /// </summary>
    public enum MonotonicityDirection
    {
        /// <summary>Fee never decreases as input increases</summary>
        NonDecreasing,

        /// <summary>Fee never increases as input increases</summary>
        NonIncreasing,

        /// <summary>Fee always increases as input increases</summary>
        StrictlyIncreasing,

        /// <summary>Fee always decreases as input increases</summary>
        StrictlyDecreasing
    }

    /// <summary>
    /// Report of monotonicity verification
    /// </summary>
    public class MonotonicityReport
    {
        public string FeeName { get; }
        public string WithRespectTo { get; }
        public MonotonicityDirection ExpectedDirection { get; set; }
        public bool IsMonotonic { get; set; }
        public long PointsChecked { get; set; }
        public List<MonotonicityViolation> Violations { get; } = new();
        public List<string> Notes { get; } = new();

        public MonotonicityReport(string feeName, string withRespectTo)
        {
            FeeName = feeName;
            WithRespectTo = withRespectTo;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Monotonicity: {FeeName} with respect to {WithRespectTo}");
            sb.AppendLine($"  Expected: {ExpectedDirection}");
            sb.AppendLine($"  Status: {(IsMonotonic ? "MONOTONIC" : "NOT MONOTONIC")}");
            sb.AppendLine($"  Points checked: {PointsChecked}");

            if (Violations.Any())
            {
                sb.AppendLine($"  Violations: {Violations.Count}");
                foreach (var v in Violations.Take(5))
                {
                    sb.AppendLine($"    - At {WithRespectTo}={v.Input1} fee={v.Fee1}, at {WithRespectTo}={v.Input2} fee={v.Fee2}");
                    if (v.Context.Values.Any())
                    {
                        sb.AppendLine($"      Context: {v.Context}");
                    }
                }
                if (Violations.Count > 5)
                {
                    sb.AppendLine($"    ... and {Violations.Count - 5} more");
                }
            }

            foreach (var note in Notes)
            {
                sb.AppendLine($"  Note: {note}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A specific violation of monotonicity
    /// </summary>
    public record MonotonicityViolation(
        InputCombination Context,
        decimal Input1,
        decimal Fee1,
        decimal Input2,
        decimal Fee2)
    {
        public override string ToString() =>
            $"At input={Input1}, fee={Fee1}; at input={Input2}, fee={Fee2}";
    }
}
