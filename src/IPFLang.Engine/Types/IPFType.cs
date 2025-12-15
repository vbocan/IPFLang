namespace IPFLang.Types
{
    /// <summary>
    /// Base type for the IPFLang type system
    /// </summary>
    public abstract record IPFType
    {
        /// <summary>
        /// Check if this type is compatible with another type for arithmetic operations
        /// </summary>
        public virtual bool IsCompatibleWith(IPFType other) => this.Equals(other);
    }

    /// <summary>
    /// Numeric type (untyped decimal)
    /// </summary>
    public record IPFTypeNumber : IPFType
    {
        public override string ToString() => "Number";
    }

    /// <summary>
    /// String type
    /// </summary>
    public record IPFTypeString : IPFType
    {
        public override string ToString() => "String";
    }

    /// <summary>
    /// Boolean type
    /// </summary>
    public record IPFTypeBoolean : IPFType
    {
        public override string ToString() => "Boolean";
    }

    /// <summary>
    /// Date type
    /// </summary>
    public record IPFTypeDate : IPFType
    {
        public override string ToString() => "Date";
    }

    /// <summary>
    /// String list type (for MULTILIST)
    /// </summary>
    public record IPFTypeStringList : IPFType
    {
        public override string ToString() => "StringList";
    }

    /// <summary>
    /// Currency-typed amount. Currency can be:
    /// - A concrete ISO 4217 code (e.g., "EUR", "USD")
    /// - A type variable name (e.g., "C") for polymorphic fees
    /// </summary>
    public record IPFTypeAmount : IPFType
    {
        /// <summary>
        /// The currency code (ISO 4217) or type variable name.
        /// </summary>
        public string Currency { get; init; }

        /// <summary>
        /// Whether this is a type variable (polymorphic) rather than a concrete currency
        /// </summary>
        public bool IsPolymorphic { get; init; }

        public IPFTypeAmount(string currency, bool isPolymorphic = false)
        {
            Currency = currency;
            IsPolymorphic = isPolymorphic;
        }

        public override bool IsCompatibleWith(IPFType other)
        {
            if (other is not IPFTypeAmount otherAmount)
                return false;

            // Polymorphic types are compatible with anything
            if (IsPolymorphic || otherAmount.IsPolymorphic)
                return true;

            // Concrete currencies must match
            return Currency.Equals(otherAmount.Currency, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => IsPolymorphic ? $"Amount<{Currency}>" : $"Amount<{Currency}>";
    }

    /// <summary>
    /// Type variable for polymorphic type parameters (e.g., C in FEE<C>)
    /// </summary>
    public record IPFTypeVariable : IPFType
    {
        public string Name { get; init; }

        public IPFTypeVariable(string name)
        {
            Name = name;
        }

        public override bool IsCompatibleWith(IPFType other)
        {
            // Type variables are compatible with anything during type checking
            // They get unified/substituted during instantiation
            return true;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Represents a type error
    /// </summary>
    public record IPFTypeError : IPFType
    {
        public string Message { get; init; }

        public IPFTypeError(string message)
        {
            Message = message;
        }

        public override string ToString() => $"Error: {Message}";
    }
}
