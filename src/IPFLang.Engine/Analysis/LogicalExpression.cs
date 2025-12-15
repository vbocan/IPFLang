namespace IPFLang.Analysis
{
    /// <summary>
    /// Abstract representation of logical expressions from DSL conditions.
    /// Used for completeness analysis.
    /// </summary>
    public abstract record LogicalExpression
    {
        /// <summary>
        /// Evaluate this expression against an input combination
        /// </summary>
        public abstract bool Evaluate(InputCombination inputs);

        /// <summary>
        /// Get all variable names referenced in this expression
        /// </summary>
        public abstract IEnumerable<string> GetReferencedVariables();

        /// <summary>
        /// Create a string representation
        /// </summary>
        public abstract string ToDisplayString();
    }

    /// <summary>
    /// Constant true expression (unconditional)
    /// </summary>
    public record TrueExpression : LogicalExpression
    {
        public static readonly TrueExpression Instance = new();

        public override bool Evaluate(InputCombination inputs) => true;
        public override IEnumerable<string> GetReferencedVariables() => Enumerable.Empty<string>();
        public override string ToDisplayString() => "TRUE";
    }

    /// <summary>
    /// Constant false expression
    /// </summary>
    public record FalseExpression : LogicalExpression
    {
        public static readonly FalseExpression Instance = new();

        public override bool Evaluate(InputCombination inputs) => false;
        public override IEnumerable<string> GetReferencedVariables() => Enumerable.Empty<string>();
        public override string ToDisplayString() => "FALSE";
    }

    /// <summary>
    /// Logical AND of two expressions
    /// </summary>
    public record AndExpression(LogicalExpression Left, LogicalExpression Right) : LogicalExpression
    {
        public override bool Evaluate(InputCombination inputs) =>
            Left.Evaluate(inputs) && Right.Evaluate(inputs);

        public override IEnumerable<string> GetReferencedVariables() =>
            Left.GetReferencedVariables().Concat(Right.GetReferencedVariables()).Distinct();

        public override string ToDisplayString() => $"({Left.ToDisplayString()} AND {Right.ToDisplayString()})";
    }

    /// <summary>
    /// Logical OR of two expressions
    /// </summary>
    public record OrExpression(LogicalExpression Left, LogicalExpression Right) : LogicalExpression
    {
        public override bool Evaluate(InputCombination inputs) =>
            Left.Evaluate(inputs) || Right.Evaluate(inputs);

        public override IEnumerable<string> GetReferencedVariables() =>
            Left.GetReferencedVariables().Concat(Right.GetReferencedVariables()).Distinct();

        public override string ToDisplayString() => $"({Left.ToDisplayString()} OR {Right.ToDisplayString()})";
    }

    /// <summary>
    /// Logical NOT of an expression
    /// </summary>
    public record NotExpression(LogicalExpression Inner) : LogicalExpression
    {
        public override bool Evaluate(InputCombination inputs) => !Inner.Evaluate(inputs);

        public override IEnumerable<string> GetReferencedVariables() => Inner.GetReferencedVariables();

        public override string ToDisplayString() => $"NOT({Inner.ToDisplayString()})";
    }

    /// <summary>
    /// Comparison expression (variable compared to value)
    /// </summary>
    public record ComparisonExpression(
        string VariableName,
        ComparisonOperator Operator,
        object CompareValue) : LogicalExpression
    {
        public override bool Evaluate(InputCombination inputs)
        {
            if (!inputs.ContainsVariable(VariableName))
            {
                return false;
            }

            var value = inputs[VariableName];
            return EvaluateComparison(value, Operator, CompareValue);
        }

        private static bool EvaluateComparison(DomainValue domainValue, ComparisonOperator op, object compareValue)
        {
            var value = domainValue.GetValue();

            // Handle symbol comparisons
            if (domainValue is SymbolValue sv && compareValue is string targetSymbol)
            {
                return op switch
                {
                    ComparisonOperator.Equal => sv.Symbol == targetSymbol,
                    ComparisonOperator.NotEqual => sv.Symbol != targetSymbol,
                    _ => throw new InvalidOperationException($"Cannot apply {op} to symbols")
                };
            }

            // Handle boolean comparisons
            if (domainValue is BooleanValue bv && compareValue is bool targetBool)
            {
                return op switch
                {
                    ComparisonOperator.Equal => bv.Value == targetBool,
                    ComparisonOperator.NotEqual => bv.Value != targetBool,
                    _ => throw new InvalidOperationException($"Cannot apply {op} to booleans")
                };
            }

            // Handle numeric comparisons
            if (value is decimal numValue && compareValue is decimal targetNum)
            {
                return op switch
                {
                    ComparisonOperator.Equal => numValue == targetNum,
                    ComparisonOperator.NotEqual => numValue != targetNum,
                    ComparisonOperator.LessThan => numValue < targetNum,
                    ComparisonOperator.LessThanOrEqual => numValue <= targetNum,
                    ComparisonOperator.GreaterThan => numValue > targetNum,
                    ComparisonOperator.GreaterThanOrEqual => numValue >= targetNum,
                    _ => false
                };
            }

            // Handle date comparisons
            if (domainValue is DateValue dv && compareValue is DateOnly targetDate)
            {
                return op switch
                {
                    ComparisonOperator.Equal => dv.Value == targetDate,
                    ComparisonOperator.NotEqual => dv.Value != targetDate,
                    ComparisonOperator.LessThan => dv.Value < targetDate,
                    ComparisonOperator.LessThanOrEqual => dv.Value <= targetDate,
                    ComparisonOperator.GreaterThan => dv.Value > targetDate,
                    ComparisonOperator.GreaterThanOrEqual => dv.Value >= targetDate,
                    _ => false
                };
            }

            // Handle IN operator for multi-select
            if (domainValue is MultiSelectValue msv && compareValue is string checkSymbol)
            {
                return op switch
                {
                    ComparisonOperator.In => msv.SelectedSymbols.Contains(checkSymbol),
                    ComparisonOperator.NotIn => !msv.SelectedSymbols.Contains(checkSymbol),
                    _ => false
                };
            }

            return false;
        }

        public override IEnumerable<string> GetReferencedVariables()
        {
            yield return VariableName;
        }

        public override string ToDisplayString() =>
            $"{VariableName} {OperatorToString(Operator)} {CompareValue}";

        private static string OperatorToString(ComparisonOperator op) => op switch
        {
            ComparisonOperator.Equal => "EQ",
            ComparisonOperator.NotEqual => "NEQ",
            ComparisonOperator.LessThan => "LT",
            ComparisonOperator.LessThanOrEqual => "LTE",
            ComparisonOperator.GreaterThan => "GT",
            ComparisonOperator.GreaterThanOrEqual => "GTE",
            ComparisonOperator.In => "IN",
            ComparisonOperator.NotIn => "NIN",
            _ => op.ToString()
        };
    }

    /// <summary>
    /// Comparison operators supported in conditions
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        In,
        NotIn
    }
}
