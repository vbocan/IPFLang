using System.Globalization;
using IPFLang.Parser;

namespace IPFLang.Analysis
{
    /// <summary>
    /// Extracts logical expressions from DSL condition tokens.
    /// Parses token sequences like "EntityType EQ NormalEntity AND ClaimCount GT 10"
    /// into LogicalExpression objects for analysis.
    /// </summary>
    public class ConditionExtractor
    {
        private static readonly Dictionary<string, ComparisonOperator> ComparisonOperators = new()
        {
            ["EQ"] = ComparisonOperator.Equal,
            ["NEQ"] = ComparisonOperator.NotEqual,
            ["LT"] = ComparisonOperator.LessThan,
            ["LTE"] = ComparisonOperator.LessThanOrEqual,
            ["GT"] = ComparisonOperator.GreaterThan,
            ["GTE"] = ComparisonOperator.GreaterThanOrEqual,
            ["IN"] = ComparisonOperator.In,
            ["NIN"] = ComparisonOperator.NotIn
        };

        private static readonly Dictionary<string, int> OperatorPrecedence = new()
        {
            ["OR"] = 1,
            ["AND"] = 2
        };

        /// <summary>
        /// Extract all conditions from a fee definition
        /// </summary>
        public IEnumerable<FeeCondition> ExtractFeeConditions(DslFee fee)
        {
            foreach (var item in fee.Cases)
            {
                if (item is DslFeeCase feeCase)
                {
                    var caseCondition = ParseCondition(feeCase.Condition);

                    foreach (var yield in feeCase.Yields)
                    {
                        var yieldCondition = ParseCondition(yield.Condition);
                        var combinedCondition = CombineConditions(caseCondition, yieldCondition);

                        yield return new FeeCondition(
                            fee.Name,
                            combinedCondition,
                            yield.Values.ToList()
                        );
                    }
                }
                else if (item is DslFeeYield directYield)
                {
                    var condition = ParseCondition(directYield.Condition);
                    yield return new FeeCondition(
                        fee.Name,
                        condition,
                        directYield.Values.ToList()
                    );
                }
            }
        }

        /// <summary>
        /// Parse a condition token sequence into a logical expression
        /// </summary>
        public LogicalExpression ParseCondition(IEnumerable<string> tokens)
        {
            var tokenList = tokens.ToList();

            if (tokenList.Count == 0)
            {
                return TrueExpression.Instance;
            }

            return ParseExpression(tokenList, 0, tokenList.Count);
        }

        private LogicalExpression ParseExpression(List<string> tokens, int start, int end)
        {
            if (start >= end)
            {
                return TrueExpression.Instance;
            }

            // Strip outer parentheses if present
            (start, end) = StripParentheses(tokens, start, end);

            // Find the lowest precedence operator (rightmost for left-associativity)
            int opIndex = FindMainOperator(tokens, start, end);

            if (opIndex >= 0)
            {
                var op = tokens[opIndex].ToUpperInvariant();
                var left = ParseExpression(tokens, start, opIndex);
                var right = ParseExpression(tokens, opIndex + 1, end);

                return op switch
                {
                    "AND" => new AndExpression(left, right),
                    "OR" => new OrExpression(left, right),
                    _ => throw new InvalidOperationException($"Unknown logical operator: {op}")
                };
            }

            // Check for NOT operator
            if (tokens[start].Equals("NOT", StringComparison.OrdinalIgnoreCase))
            {
                var inner = ParseExpression(tokens, start + 1, end);
                return new NotExpression(inner);
            }

            // Must be a comparison: VAR OP VALUE
            return ParseComparison(tokens, start, end);
        }

        private LogicalExpression ParseComparison(List<string> tokens, int start, int end)
        {
            int length = end - start;

            if (length < 3)
            {
                // Single token - might be TRUE/FALSE
                if (length == 1)
                {
                    var token = tokens[start].ToUpperInvariant();
                    if (token == "TRUE") return TrueExpression.Instance;
                    if (token == "FALSE") return FalseExpression.Instance;
                }
                throw new InvalidOperationException(
                    $"Invalid comparison expression: {string.Join(" ", tokens.Skip(start).Take(length))}");
            }

            // Format: VARIABLE OPERATOR VALUE
            var variableName = tokens[start];
            var operatorToken = tokens[start + 1].ToUpperInvariant();

            if (!ComparisonOperators.TryGetValue(operatorToken, out var compOp))
            {
                throw new InvalidOperationException($"Unknown comparison operator: {operatorToken}");
            }

            // Parse the value (might be multiple tokens for complex values)
            var valueTokens = tokens.Skip(start + 2).Take(end - start - 2).ToList();
            var value = ParseValue(valueTokens);

            return new ComparisonExpression(variableName, compOp, value);
        }

        private object ParseValue(List<string> tokens)
        {
            if (tokens.Count == 0)
            {
                throw new InvalidOperationException("Empty value in comparison");
            }

            var token = tokens[0];

            // Boolean
            if (token.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return true;
            if (token.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return false;

            // Numeric (including currency literals like 100<EUR>)
            var numericPart = token.Contains('<') ? token.Substring(0, token.IndexOf('<')) : token;
            if (decimal.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out var numValue))
            {
                return numValue;
            }

            // Symbol/identifier
            return token;
        }

        private int FindMainOperator(List<string> tokens, int start, int end)
        {
            int parenDepth = 0;
            int lowestPrecedence = int.MaxValue;
            int opIndex = -1;

            for (int i = start; i < end; i++)
            {
                var token = tokens[i];

                if (token == "(") { parenDepth++; continue; }
                if (token == ")") { parenDepth--; continue; }

                if (parenDepth == 0)
                {
                    var upper = token.ToUpperInvariant();
                    if (OperatorPrecedence.TryGetValue(upper, out var precedence))
                    {
                        if (precedence <= lowestPrecedence)
                        {
                            lowestPrecedence = precedence;
                            opIndex = i;
                        }
                    }
                }
            }

            return opIndex;
        }

        private (int start, int end) StripParentheses(List<string> tokens, int start, int end)
        {
            while (start < end && tokens[start] == "(" && tokens[end - 1] == ")")
            {
                // Check if these parens actually wrap the whole expression
                int depth = 0;
                bool wrapsAll = true;

                for (int i = start; i < end - 1; i++)
                {
                    if (tokens[i] == "(") depth++;
                    else if (tokens[i] == ")") depth--;

                    if (depth == 0 && i < end - 1)
                    {
                        wrapsAll = false;
                        break;
                    }
                }

                if (wrapsAll)
                {
                    start++;
                    end--;
                }
                else
                {
                    break;
                }
            }

            return (start, end);
        }

        private LogicalExpression CombineConditions(LogicalExpression outer, LogicalExpression inner)
        {
            if (outer is TrueExpression) return inner;
            if (inner is TrueExpression) return outer;
            return new AndExpression(outer, inner);
        }
    }

    /// <summary>
    /// Represents a condition and its associated yield in a fee
    /// </summary>
    public record FeeCondition(
        string FeeName,
        LogicalExpression Condition,
        IReadOnlyList<string> YieldTokens)
    {
        public override string ToString() =>
            $"YIELD {string.Join(" ", YieldTokens)} IF {Condition.ToDisplayString()}";
    }
}
