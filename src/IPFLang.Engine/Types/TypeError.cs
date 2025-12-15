namespace IPFLang.Types
{
    /// <summary>
    /// Represents a type error detected during static type checking
    /// </summary>
    public record TypeError(TypeErrorKind Kind, string Message, string? Location = null)
    {
        public override string ToString()
        {
            return Location != null
                ? $"[{Kind}] {Message} at {Location}"
                : $"[{Kind}] {Message}";
        }

        public static TypeError CurrencyMismatch(string expected, string actual, string context) =>
            new(TypeErrorKind.CurrencyMismatch,
                $"Currency mismatch: expected '{expected}', got '{actual}'",
                context);

        public static TypeError InvalidCurrency(string currency, string context) =>
            new(TypeErrorKind.InvalidCurrency,
                $"Invalid currency code: '{currency}'",
                context);

        public static TypeError TypeMismatch(IPFType expected, IPFType actual, string context) =>
            new(TypeErrorKind.TypeMismatch,
                $"Type mismatch: expected {expected}, got {actual}",
                context);

        public static TypeError UndefinedVariable(string name, string context) =>
            new(TypeErrorKind.UndefinedVariable,
                $"Undefined variable: '{name}'",
                context);

        public static TypeError ArithmeticOnNonNumeric(IPFType type, string context) =>
            new(TypeErrorKind.ArithmeticOnNonNumeric,
                $"Cannot perform arithmetic on non-numeric type: {type}",
                context);

        public static TypeError MixedCurrencyArithmetic(string currency1, string currency2, string context) =>
            new(TypeErrorKind.MixedCurrencyArithmetic,
                $"Cannot mix currencies in arithmetic: '{currency1}' and '{currency2}'",
                context);

        public static TypeError UnboundTypeVariable(string name, string context) =>
            new(TypeErrorKind.UnboundTypeVariable,
                $"Unbound type variable: '{name}'",
                context);

        public static TypeError ReturnTypeMismatch(IPFType expected, IPFType actual, string feeName) =>
            new(TypeErrorKind.ReturnTypeMismatch,
                $"Fee '{feeName}' return type mismatch: expected {expected}, got {actual}",
                feeName);
    }

    /// <summary>
    /// Categories of type errors
    /// </summary>
    public enum TypeErrorKind
    {
        CurrencyMismatch,
        InvalidCurrency,
        TypeMismatch,
        UndefinedVariable,
        ArithmeticOnNonNumeric,
        MixedCurrencyArithmetic,
        UnboundTypeVariable,
        ReturnTypeMismatch
    }
}
