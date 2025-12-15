using IPFLang.Parser;

namespace IPFLang.Analysis
{
    /// <summary>
    /// Analyzes DSL input definitions to extract their domains.
    /// </summary>
    public class DomainAnalyzer
    {
        /// <summary>
        /// Extract domains from all input definitions
        /// </summary>
        public IEnumerable<InputDomain> ExtractDomains(IEnumerable<DslInput> inputs)
        {
            foreach (var input in inputs)
            {
                yield return ExtractDomain(input);
            }
        }

        /// <summary>
        /// Extract domain from a single input definition
        /// </summary>
        public InputDomain ExtractDomain(DslInput input)
        {
            return input switch
            {
                DslInputBoolean b => new BooleanDomain(b.Name),
                DslInputList l => new ListDomain(l.Name, l.Items.Select(i => i.Symbol).ToList()),
                DslInputListMultiple m => new MultiListDomain(m.Name, m.Items.Select(i => i.Symbol).ToList()),
                DslInputNumber n => new NumericDomain(n.Name, n.MinValue, n.MaxValue),
                DslInputDate d => new DateDomain(d.Name, d.MinValue, d.MaxValue),
                DslInputAmount a => new AmountDomain(a.Name, a.Currency),
                _ => throw new NotSupportedException($"Unknown input type: {input.GetType().Name}")
            };
        }

        /// <summary>
        /// Calculate total domain size (Cartesian product of all domains)
        /// Returns null if any domain is infinite
        /// </summary>
        public long? CalculateTotalDomainSize(IEnumerable<InputDomain> domains)
        {
            long total = 1;
            foreach (var domain in domains)
            {
                if (!domain.IsFinite || domain.Cardinality == null)
                {
                    return null;
                }

                // Check for overflow
                if (domain.Cardinality > long.MaxValue / total)
                {
                    return null; // Too large to enumerate
                }

                total *= domain.Cardinality.Value;
            }
            return total;
        }

        /// <summary>
        /// Generate all input combinations for finite domains.
        /// Returns an enumerable of dictionaries mapping variable names to values.
        /// </summary>
        public IEnumerable<InputCombination> GenerateAllCombinations(IEnumerable<InputDomain> domains)
        {
            var domainList = domains.ToList();
            if (domainList.Count == 0)
            {
                yield return new InputCombination(new Dictionary<string, DomainValue>());
                yield break;
            }

            // Check if enumeration is feasible
            var totalSize = CalculateTotalDomainSize(domainList);
            if (totalSize == null || totalSize > 10_000_000) // Limit to 10M combinations
            {
                throw new InvalidOperationException(
                    $"Domain too large for exhaustive enumeration. " +
                    $"Use representative sampling instead.");
            }

            foreach (var combination in GenerateCombinationsRecursive(domainList, 0, new Dictionary<string, DomainValue>()))
            {
                yield return combination;
            }
        }

        private IEnumerable<InputCombination> GenerateCombinationsRecursive(
            List<InputDomain> domains,
            int index,
            Dictionary<string, DomainValue> current)
        {
            if (index >= domains.Count)
            {
                yield return new InputCombination(new Dictionary<string, DomainValue>(current));
                yield break;
            }

            var domain = domains[index];
            foreach (var value in domain.GetValues())
            {
                current[domain.VariableName] = value;
                foreach (var combination in GenerateCombinationsRecursive(domains, index + 1, current))
                {
                    yield return combination;
                }
                current.Remove(domain.VariableName);
            }
        }

        /// <summary>
        /// Generate representative combinations for large domains.
        /// Uses boundary values and samples.
        /// </summary>
        public IEnumerable<InputCombination> GenerateRepresentativeCombinations(
            IEnumerable<InputDomain> domains,
            int maxSamplesPerDomain = 5)
        {
            var domainList = domains.ToList();
            if (domainList.Count == 0)
            {
                yield return new InputCombination(new Dictionary<string, DomainValue>());
                yield break;
            }

            // Get representative values for each domain
            var representativeValues = domainList.Select(d => GetRepresentativeValues(d, maxSamplesPerDomain).ToList()).ToList();

            foreach (var combination in GenerateRepresentativeCombinationsRecursive(
                domainList, representativeValues, 0, new Dictionary<string, DomainValue>()))
            {
                yield return combination;
            }
        }

        private IEnumerable<DomainValue> GetRepresentativeValues(InputDomain domain, int maxSamples)
        {
            return domain switch
            {
                NumericDomain nd => nd.GetRepresentativeValues(maxSamples),
                DateDomain dd => dd.GetRepresentativeValues(),
                AmountDomain => domain.GetValues(), // Already returns representatives
                _ => domain.GetValues() // For finite small domains, use all values
            };
        }

        private IEnumerable<InputCombination> GenerateRepresentativeCombinationsRecursive(
            List<InputDomain> domains,
            List<List<DomainValue>> representativeValues,
            int index,
            Dictionary<string, DomainValue> current)
        {
            if (index >= domains.Count)
            {
                yield return new InputCombination(new Dictionary<string, DomainValue>(current));
                yield break;
            }

            foreach (var value in representativeValues[index])
            {
                current[domains[index].VariableName] = value;
                foreach (var combination in GenerateRepresentativeCombinationsRecursive(
                    domains, representativeValues, index + 1, current))
                {
                    yield return combination;
                }
                current.Remove(domains[index].VariableName);
            }
        }
    }

    /// <summary>
    /// Represents a specific combination of input values
    /// </summary>
    public record InputCombination(IReadOnlyDictionary<string, DomainValue> Values)
    {
        public DomainValue this[string variableName] => Values[variableName];

        public bool ContainsVariable(string variableName) => Values.ContainsKey(variableName);

        public override string ToString()
        {
            return "{" + string.Join(", ", Values.Select(kv => $"{kv.Key}={kv.Value.ToDisplayString()}")) + "}";
        }
    }
}
