using IPFLang.CurrencyConversion;
using IPFLang.Evaluator;
using IPFLang.Parser;

namespace IPFLang.Provenance
{
    /// <summary>
    /// Collects provenance information during fee computation.
    /// Tracks which rules fired, what conditions matched, and each contribution.
    /// </summary>
    public class ProvenanceCollector
    {
        private readonly ICurrencyConverter? _currencyConverter;
        private ComputationProvenance _provenance = new();

        public ProvenanceCollector(ICurrencyConverter? currencyConverter = null)
        {
            _currencyConverter = currencyConverter;
        }

        /// <summary>
        /// Compute fees with full provenance tracking
        /// </summary>
        public ComputationProvenance ComputeWithProvenance(
            IEnumerable<DslFee> fees,
            IEnumerable<IPFValue> inputValues)
        {
            _provenance = new ComputationProvenance();

            // Record input values
            foreach (var input in inputValues)
            {
                _provenance.InputValues[input.Name] = GetInputValue(input);
            }

            // Process each fee
            foreach (var fee in fees)
            {
                var feeProvenance = ComputeFeeWithProvenance(fee, inputValues.ToList());
                _provenance.FeeProvenances.Add(feeProvenance);
            }

            return _provenance;
        }

        /// <summary>
        /// Compute a single fee with provenance tracking
        /// </summary>
        private FeeProvenance ComputeFeeWithProvenance(DslFee fee, List<IPFValue> inputValues)
        {
            var feeProvenance = new FeeProvenance(fee.Name, fee.Optional);

            // Build variable context
            var allVars = new List<IPFValue>(inputValues);

            // Evaluate LET variables and record them
            foreach (var letVar in fee.Vars)
            {
                var value = DslEvaluator.EvaluateExpression(
                    letVar.ValueTokens.ToArray(),
                    allVars,
                    fee.Name,
                    _currencyConverter);

                // Store with unprefixed name for cleaner provenance output
                var varName = letVar.Name;
                var dotIndex = varName.IndexOf('.');
                if (dotIndex >= 0)
                {
                    varName = varName.Substring(dotIndex + 1);
                }
                feeProvenance.LetVariables[varName] = value;
                allVars.Add(new IPFValueNumber(letVar.Name, value));
            }

            // Process cases and yields
            foreach (var item in fee.Cases)
            {
                if (item is DslFeeCase feeCase)
                {
                    ProcessFeeCase(feeCase, allVars, fee.Name, feeProvenance);
                }
                else if (item is DslFeeYield directYield)
                {
                    ProcessYield(directYield, null, true, allVars, fee.Name, feeProvenance);
                }
            }

            return feeProvenance;
        }

        /// <summary>
        /// Process a CASE block with its yields
        /// </summary>
        private void ProcessFeeCase(
            DslFeeCase feeCase,
            List<IPFValue> allVars,
            string feeName,
            FeeProvenance feeProvenance)
        {
            // Evaluate case condition
            var caseConditionStr = feeCase.Condition.Any()
                ? string.Join(" ", feeCase.Condition)
                : null;

            var caseResult = feeCase.Condition.Any()
                ? DslEvaluator.EvaluateLogic(feeCase.Condition.ToArray(), allVars, feeName, _currencyConverter)
                : true;

            // Process each yield
            foreach (var yield in feeCase.Yields)
            {
                ProcessYield(yield, caseConditionStr, caseResult, allVars, feeName, feeProvenance);
            }
        }

        /// <summary>
        /// Process a single YIELD statement
        /// </summary>
        private void ProcessYield(
            DslFeeYield yield,
            string? caseCondition,
            bool caseConditionResult,
            List<IPFValue> allVars,
            string feeName,
            FeeProvenance feeProvenance)
        {
            var yieldConditionStr = yield.Condition.Any()
                ? string.Join(" ", yield.Condition)
                : null;

            var yieldConditionResult = yield.Condition.Any()
                ? DslEvaluator.EvaluateLogic(yield.Condition.ToArray(), allVars, feeName, _currencyConverter)
                : true;

            var expressionStr = string.Join(" ", yield.Values);

            // Evaluate the expression value (even if condition is false, for reporting)
            decimal contribution = 0;
            if (caseConditionResult && yieldConditionResult)
            {
                contribution = DslEvaluator.EvaluateExpression(
                    yield.Values.ToArray(),
                    allVars,
                    feeName,
                    _currencyConverter);
            }

            // Create provenance record
            var record = new ProvenanceRecord(feeName, expressionStr)
            {
                CaseCondition = caseCondition,
                CaseConditionResult = caseConditionResult,
                YieldCondition = yieldConditionStr,
                YieldConditionResult = yieldConditionResult,
                Contribution = contribution,
                ReferencedInputs = ExtractReferencedInputs(yield.Values, allVars),
                LetVariables = new Dictionary<string, decimal>(feeProvenance.LetVariables)
            };

            feeProvenance.Records.Add(record);
        }

        /// <summary>
        /// Extract input values referenced in an expression
        /// </summary>
        private Dictionary<string, object> ExtractReferencedInputs(
            IEnumerable<string> tokens,
            List<IPFValue> allVars)
        {
            var referenced = new Dictionary<string, object>();
            var varNames = allVars.Select(v => v.Name).ToHashSet();

            foreach (var token in tokens)
            {
                if (varNames.Contains(token))
                {
                    var v = allVars.First(x => x.Name == token);
                    referenced[token] = GetInputValue(v);
                }
            }

            return referenced;
        }

        /// <summary>
        /// Get the value from an IPFValue
        /// </summary>
        private object GetInputValue(IPFValue value)
        {
            return value switch
            {
                IPFValueNumber n => n.Value,
                IPFValueBoolean b => b.Value,
                IPFValueString s => s.Value,
                IPFValueDate d => d.Value,
                IPFValueAmount a => $"{a.Value} {a.Currency}",
                IPFValueStringList sl => string.Join(", ", sl.Value),
                _ => value.ToString() ?? "unknown"
            };
        }
    }
}
