namespace IPFLang.Analysis
{
    /// <summary>
    /// Represents a specific value from an input domain.
    /// Used in completeness verification to represent test cases.
    /// </summary>
    public abstract record DomainValue(string VariableName)
    {
        /// <summary>
        /// Get the value as an object for evaluation
        /// </summary>
        public abstract object GetValue();

        /// <summary>
        /// Get a string representation for display
        /// </summary>
        public abstract string ToDisplayString();
    }

    /// <summary>
    /// Boolean domain value
    /// </summary>
    public record BooleanValue(string VariableName, bool Value) : DomainValue(VariableName)
    {
        public override object GetValue() => Value;
        public override string ToDisplayString() => Value ? "TRUE" : "FALSE";
    }

    /// <summary>
    /// Symbol/enum domain value (from LIST inputs)
    /// </summary>
    public record SymbolValue(string VariableName, string Symbol) : DomainValue(VariableName)
    {
        public override object GetValue() => Symbol;
        public override string ToDisplayString() => Symbol;
    }

    /// <summary>
    /// Numeric domain value
    /// </summary>
    public record NumericValue(string VariableName, decimal Value) : DomainValue(VariableName)
    {
        public override object GetValue() => Value;
        public override string ToDisplayString() => Value.ToString();
    }

    /// <summary>
    /// Amount domain value (numeric with currency)
    /// </summary>
    public record AmountValue(string VariableName, decimal Value, string Currency) : DomainValue(VariableName)
    {
        public override object GetValue() => Value;
        public override string ToDisplayString() => $"{Value}<{Currency}>";
    }

    /// <summary>
    /// Date domain value
    /// </summary>
    public record DateValue(string VariableName, DateOnly Value) : DomainValue(VariableName)
    {
        public override object GetValue() => Value;
        public override string ToDisplayString() => Value.ToString("dd.MM.yyyy");
    }

    /// <summary>
    /// Multi-select domain value (set of symbols)
    /// </summary>
    public record MultiSelectValue(string VariableName, IReadOnlyList<string> SelectedSymbols) : DomainValue(VariableName)
    {
        public override object GetValue() => SelectedSymbols;
        public override string ToDisplayString() =>
            SelectedSymbols.Count == 0 ? "(none)" : string.Join(", ", SelectedSymbols);

        public int Count => SelectedSymbols.Count;
    }
}
