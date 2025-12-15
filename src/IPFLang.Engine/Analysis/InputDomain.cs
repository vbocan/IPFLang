namespace IPFLang.Analysis
{
    /// <summary>
    /// Represents the domain of possible values for a DSL input variable.
    /// Used for completeness verification.
    /// </summary>
    public abstract record InputDomain(string VariableName)
    {
        /// <summary>
        /// Get all possible values in this domain (for finite domains)
        /// </summary>
        public abstract IEnumerable<DomainValue> GetValues();

        /// <summary>
        /// Check if this domain is finite and enumerable
        /// </summary>
        public abstract bool IsFinite { get; }

        /// <summary>
        /// Get the cardinality of this domain (null if infinite)
        /// </summary>
        public abstract long? Cardinality { get; }
    }

    /// <summary>
    /// Domain for boolean inputs (exactly 2 values)
    /// </summary>
    public record BooleanDomain(string VariableName) : InputDomain(VariableName)
    {
        public override bool IsFinite => true;
        public override long? Cardinality => 2;

        public override IEnumerable<DomainValue> GetValues()
        {
            yield return new BooleanValue(VariableName, true);
            yield return new BooleanValue(VariableName, false);
        }
    }

    /// <summary>
    /// Domain for list inputs (finite set of symbols)
    /// </summary>
    public record ListDomain(string VariableName, IReadOnlyList<string> Symbols) : InputDomain(VariableName)
    {
        public override bool IsFinite => true;
        public override long? Cardinality => Symbols.Count;

        public override IEnumerable<DomainValue> GetValues()
        {
            foreach (var symbol in Symbols)
            {
                yield return new SymbolValue(VariableName, symbol);
            }
        }
    }

    /// <summary>
    /// Domain for numeric inputs (integer range)
    /// </summary>
    public record NumericDomain(string VariableName, int MinValue, int MaxValue) : InputDomain(VariableName)
    {
        public override bool IsFinite => true;
        public override long? Cardinality => (long)MaxValue - MinValue + 1;

        public override IEnumerable<DomainValue> GetValues()
        {
            for (int i = MinValue; i <= MaxValue; i++)
            {
                yield return new NumericValue(VariableName, i);
            }
        }

        /// <summary>
        /// Get representative values for large domains (boundaries + samples)
        /// </summary>
        public IEnumerable<DomainValue> GetRepresentativeValues(int maxSamples = 10)
        {
            if (Cardinality <= maxSamples)
            {
                foreach (var v in GetValues()) yield return v;
                yield break;
            }

            // Always include boundaries
            yield return new NumericValue(VariableName, MinValue);
            yield return new NumericValue(VariableName, MaxValue);

            // Include midpoint
            int mid = MinValue + (MaxValue - MinValue) / 2;
            yield return new NumericValue(VariableName, mid);

            // Include some intermediate points
            int step = (MaxValue - MinValue) / (maxSamples - 1);
            for (int i = 1; i < maxSamples - 1; i++)
            {
                int val = MinValue + i * step;
                if (val != MinValue && val != MaxValue && val != mid)
                {
                    yield return new NumericValue(VariableName, val);
                }
            }
        }
    }

    /// <summary>
    /// Domain for amount inputs (treated as numeric for verification)
    /// </summary>
    public record AmountDomain(string VariableName, string Currency, decimal MinValue = 0, decimal MaxValue = decimal.MaxValue) : InputDomain(VariableName)
    {
        public override bool IsFinite => false; // Decimal values are effectively infinite
        public override long? Cardinality => null;

        public override IEnumerable<DomainValue> GetValues()
        {
            // For amounts, we return representative values
            yield return new AmountValue(VariableName, MinValue, Currency);
            yield return new AmountValue(VariableName, 100m, Currency);
            yield return new AmountValue(VariableName, 1000m, Currency);
        }
    }

    /// <summary>
    /// Domain for date inputs
    /// </summary>
    public record DateDomain(string VariableName, DateOnly MinDate, DateOnly MaxDate) : InputDomain(VariableName)
    {
        public override bool IsFinite => true;
        public override long? Cardinality => MaxDate.DayNumber - MinDate.DayNumber + 1;

        public override IEnumerable<DomainValue> GetValues()
        {
            for (var date = MinDate; date <= MaxDate; date = date.AddDays(1))
            {
                yield return new DateValue(VariableName, date);
            }
        }

        /// <summary>
        /// Get representative dates for large ranges
        /// </summary>
        public IEnumerable<DomainValue> GetRepresentativeValues()
        {
            yield return new DateValue(VariableName, MinDate);
            yield return new DateValue(VariableName, MaxDate);

            // Midpoint
            int midDays = MinDate.DayNumber + (MaxDate.DayNumber - MinDate.DayNumber) / 2;
            yield return new DateValue(VariableName, DateOnly.FromDayNumber(midDays));
        }
    }

    /// <summary>
    /// Domain for multi-select list inputs (power set of symbols)
    /// </summary>
    public record MultiListDomain(string VariableName, IReadOnlyList<string> Symbols) : InputDomain(VariableName)
    {
        public override bool IsFinite => true;
        public override long? Cardinality => 1L << Symbols.Count; // 2^n combinations

        public override IEnumerable<DomainValue> GetValues()
        {
            int n = Symbols.Count;
            // Generate all subsets using bit manipulation
            for (int mask = 0; mask < (1 << n); mask++)
            {
                var selected = new List<string>();
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        selected.Add(Symbols[i]);
                    }
                }
                yield return new MultiSelectValue(VariableName, selected);
            }
        }
    }
}
