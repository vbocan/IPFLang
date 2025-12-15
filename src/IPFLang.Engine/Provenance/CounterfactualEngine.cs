using IPFLang.CurrencyConversion;
using IPFLang.Evaluator;
using IPFLang.Parser;

namespace IPFLang.Provenance
{
    /// <summary>
    /// Generates counterfactual explanations showing what would happen
    /// with different input values.
    /// </summary>
    public class CounterfactualEngine
    {
        private readonly ICurrencyConverter? _currencyConverter;

        public CounterfactualEngine(ICurrencyConverter? currencyConverter = null)
        {
            _currencyConverter = currencyConverter;
        }

        /// <summary>
        /// Generate counterfactuals for the given computation
        /// </summary>
        public List<Counterfactual> GenerateCounterfactuals(
            IEnumerable<DslFee> fees,
            IEnumerable<DslInput> inputs,
            IEnumerable<IPFValue> currentValues,
            decimal currentTotal)
        {
            var counterfactuals = new List<Counterfactual>();
            var feeList = fees.ToList();
            var valueList = currentValues.ToList();
            var inputList = inputs.ToList();

            foreach (var input in inputList)
            {
                var alternatives = GetAlternativeValues(input, valueList);

                foreach (var (altValue, altIPFValue) in alternatives)
                {
                    // Create modified input list
                    var modifiedValues = valueList
                        .Where(v => v.Name != input.Name)
                        .Append(altIPFValue)
                        .ToList();

                    // Compute total with modified values
                    var altTotal = ComputeTotal(feeList, modifiedValues);

                    // Only include if there's a meaningful difference
                    if (altTotal != currentTotal)
                    {
                        var currentValue = GetCurrentValue(input.Name, valueList);
                        counterfactuals.Add(new Counterfactual(
                            input.Name,
                            currentValue,
                            altValue,
                            currentTotal,
                            altTotal));
                    }
                }
            }

            return counterfactuals;
        }

        /// <summary>
        /// Get alternative values to try for an input
        /// </summary>
        private IEnumerable<(object Value, IPFValue IPFValue)> GetAlternativeValues(
            DslInput input,
            List<IPFValue> currentValues)
        {
            var current = currentValues.FirstOrDefault(v => v.Name == input.Name);
            if (current == null) yield break;

            switch (input)
            {
                case DslInputBoolean boolInput:
                    var currentBool = ((IPFValueBoolean)current).Value;
                    yield return (!currentBool, new IPFValueBoolean(input.Name, !currentBool));
                    break;

                case DslInputList listInput:
                    var currentSymbol = ((IPFValueString)current).Value;
                    foreach (var item in listInput.Items)
                    {
                        if (item.Symbol != currentSymbol)
                        {
                            yield return (item.Symbol, new IPFValueString(input.Name, item.Symbol));
                        }
                    }
                    break;

                case DslInputNumber numInput:
                    var currentNum = ((IPFValueNumber)current).Value;
                    // Try boundary values
                    if (currentNum != numInput.MinValue)
                        yield return (numInput.MinValue, new IPFValueNumber(input.Name, numInput.MinValue));
                    if (currentNum != numInput.MaxValue)
                        yield return (numInput.MaxValue, new IPFValueNumber(input.Name, numInput.MaxValue));
                    // Try middle value
                    var mid = (numInput.MinValue + numInput.MaxValue) / 2;
                    if (mid != currentNum && mid != numInput.MinValue && mid != numInput.MaxValue)
                        yield return (mid, new IPFValueNumber(input.Name, mid));
                    break;

                case DslInputAmount amountInput:
                    var currentAmount = ((IPFValueAmount)current).Value;
                    // Try doubled and halved
                    if (currentAmount > 0)
                    {
                        yield return (currentAmount * 2, new IPFValueAmount(input.Name, currentAmount * 2, amountInput.Currency));
                        yield return (currentAmount / 2, new IPFValueAmount(input.Name, currentAmount / 2, amountInput.Currency));
                    }
                    break;
            }
        }

        /// <summary>
        /// Get current value for an input
        /// </summary>
        private object GetCurrentValue(string inputName, List<IPFValue> values)
        {
            var value = values.FirstOrDefault(v => v.Name == inputName);
            return value switch
            {
                IPFValueNumber n => n.Value,
                IPFValueBoolean b => b.Value,
                IPFValueString s => s.Value,
                IPFValueDate d => d.Value,
                IPFValueAmount a => a.Value,
                _ => "unknown"
            };
        }

        /// <summary>
        /// Compute total fees for given inputs
        /// </summary>
        private decimal ComputeTotal(List<DslFee> fees, List<IPFValue> values)
        {
            decimal total = 0;

            foreach (var fee in fees)
            {
                var allVars = new List<IPFValue>(values);

                // Evaluate LET variables
                foreach (var letVar in fee.Vars)
                {
                    var val = DslEvaluator.EvaluateExpression(
                        letVar.ValueTokens.ToArray(),
                        allVars,
                        fee.Name,
                        _currencyConverter);
                    allVars.Add(new IPFValueNumber(letVar.Name, val));
                }

                // Evaluate cases and yields
                foreach (var item in fee.Cases)
                {
                    if (item is DslFeeCase feeCase)
                    {
                        var caseResult = !feeCase.Condition.Any() ||
                            DslEvaluator.EvaluateLogic(feeCase.Condition.ToArray(), allVars, fee.Name, _currencyConverter);

                        if (caseResult)
                        {
                            foreach (var yield in feeCase.Yields)
                            {
                                var yieldResult = !yield.Condition.Any() ||
                                    DslEvaluator.EvaluateLogic(yield.Condition.ToArray(), allVars, fee.Name, _currencyConverter);

                                if (yieldResult)
                                {
                                    total += DslEvaluator.EvaluateExpression(
                                        yield.Values.ToArray(),
                                        allVars,
                                        fee.Name,
                                        _currencyConverter);
                                }
                            }
                        }
                    }
                    else if (item is DslFeeYield directYield)
                    {
                        var yieldResult = !directYield.Condition.Any() ||
                            DslEvaluator.EvaluateLogic(directYield.Condition.ToArray(), allVars, fee.Name, _currencyConverter);

                        if (yieldResult)
                        {
                            total += DslEvaluator.EvaluateExpression(
                                directYield.Values.ToArray(),
                                allVars,
                                fee.Name,
                                _currencyConverter);
                        }
                    }
                }
            }

            return total;
        }
    }
}
