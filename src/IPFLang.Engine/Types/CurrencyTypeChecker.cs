using System.Text.RegularExpressions;
using IPFLang.Parser;

namespace IPFLang.Types
{
    /// <summary>
    /// Static type checker for currency-aware expressions.
    /// Detects cross-currency arithmetic errors at compile time.
    /// </summary>
    public class CurrencyTypeChecker
    {
        private static readonly Regex CurrencyLiteralPattern = new(@"^(-?\d+(?:\.\d+)?)<([A-Z]{3})>$", RegexOptions.Compiled);
        private static readonly Regex ConvertPattern = new(@"^CONVERT$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly HashSet<string> ArithmeticOperators = new() { "+", "-", "*", "/" };
        private static readonly HashSet<string> ComparisonOperators = new() { "EQ", "NEQ", "GT", "GTE", "LT", "LTE", "IN", "NIN" };
        private static readonly HashSet<string> LogicalOperators = new() { "AND", "OR", "NOT" };

        private readonly List<TypeError> _errors = new();

        /// <summary>
        /// Type check all inputs and fees, returning any type errors found
        /// </summary>
        public IEnumerable<TypeError> Check(IEnumerable<DslInput> inputs, IEnumerable<DslFee> fees)
        {
            _errors.Clear();

            // Build initial type environment from inputs
            var env = BuildEnvironment(inputs);

            // Check each fee
            foreach (var fee in fees)
            {
                CheckFee(fee, env);
            }

            return _errors;
        }

        /// <summary>
        /// Build type environment from input declarations
        /// </summary>
        private TypeEnvironment BuildEnvironment(IEnumerable<DslInput> inputs)
        {
            var env = new TypeEnvironment();

            foreach (var input in inputs)
            {
                var type = input switch
                {
                    DslInputBoolean => new IPFTypeBoolean(),
                    DslInputNumber => new IPFTypeNumber(),
                    DslInputDate => new IPFTypeDate(),
                    DslInputList => new IPFTypeString(),
                    DslInputListMultiple => new IPFTypeStringList(),
                    DslInputAmount amount => new IPFTypeAmount(amount.Currency),
                    _ => (IPFType)new IPFTypeError($"Unknown input type: {input.GetType().Name}")
                };

                env.Bind(input.Name, type);
            }

            return env;
        }

        /// <summary>
        /// Type check a fee definition
        /// </summary>
        private void CheckFee(DslFee fee, TypeEnvironment parentEnv)
        {
            // Create fee-scoped environment
            var env = parentEnv.NewScope();

            // If polymorphic, introduce type variable
            if (fee.IsPolymorphic && fee.TypeParameter != null)
            {
                env = env.WithTypeVariable(fee.TypeParameter);
            }

            // Determine expected return type
            IPFType? expectedReturnType = null;
            if (fee.ReturnCurrency != null)
            {
                if (env.IsTypeVariable(fee.ReturnCurrency))
                {
                    expectedReturnType = new IPFTypeVariable(fee.ReturnCurrency);
                }
                else if (Currency.IsValid(fee.ReturnCurrency))
                {
                    expectedReturnType = new IPFTypeAmount(fee.ReturnCurrency);
                }
            }

            // Check LET bindings first
            foreach (var letVar in fee.Vars)
            {
                var inferredType = InferType(letVar.ValueTokens, env, $"fee '{fee.Name}' LET {letVar.Name}");
                env.Bind(letVar.Name, inferredType);

                // Also bind the unprefixed name (parser stores as "FeeName.VarName", but expressions use "VarName")
                var dotIndex = letVar.Name.IndexOf('.');
                if (dotIndex >= 0)
                {
                    var unprefixedName = letVar.Name.Substring(dotIndex + 1);
                    env.Bind(unprefixedName, inferredType);
                }
            }

            // Check each case and yield
            foreach (var item in fee.Cases)
            {
                if (item is DslFeeCase feeCase)
                {
                    // Check case condition
                    if (feeCase.Condition.Any())
                    {
                        var condType = InferType(feeCase.Condition, env, $"fee '{fee.Name}' CASE condition");
                        if (condType is not IPFTypeBoolean and not IPFTypeError)
                        {
                            _errors.Add(TypeError.TypeMismatch(new IPFTypeBoolean(), condType, $"fee '{fee.Name}' CASE condition"));
                        }
                    }

                    // Check yields
                    foreach (var yield in feeCase.Yields)
                    {
                        CheckYield(yield, expectedReturnType, env, fee.Name);
                    }
                }
                else if (item is DslFeeYield yield)
                {
                    CheckYield(yield, expectedReturnType, env, fee.Name);
                }
            }
        }

        /// <summary>
        /// Type check a YIELD statement
        /// </summary>
        private void CheckYield(DslFeeYield yield, IPFType? expectedReturnType, TypeEnvironment env, string feeName)
        {
            var context = $"fee '{feeName}' YIELD";

            // Check condition if present
            if (yield.Condition.Any())
            {
                var condType = InferType(yield.Condition, env, $"{context} IF condition");
                if (condType is not IPFTypeBoolean and not IPFTypeError)
                {
                    _errors.Add(TypeError.TypeMismatch(new IPFTypeBoolean(), condType, $"{context} IF condition"));
                }
            }

            // Check yield value type
            var valueType = InferType(yield.Values, env, context);

            // If we have an expected return type, check compatibility
            if (expectedReturnType != null && valueType is not IPFTypeError)
            {
                if (!TypesCompatible(expectedReturnType, valueType, env))
                {
                    _errors.Add(TypeError.ReturnTypeMismatch(expectedReturnType, valueType, feeName));
                }
            }
        }

        /// <summary>
        /// Infer the type of an expression (token sequence)
        /// </summary>
        private IPFType InferType(IEnumerable<string> tokens, TypeEnvironment env, string context)
        {
            var tokenList = tokens.ToList();
            if (!tokenList.Any())
            {
                return new IPFTypeNumber(); // Empty yields default to 0
            }

            return InferExpressionType(tokenList, env, context);
        }

        /// <summary>
        /// Infer type of an expression, handling operators and function calls
        /// </summary>
        private IPFType InferExpressionType(List<string> tokens, TypeEnvironment env, string context)
        {
            // Handle parenthesized expressions
            tokens = StripOuterParens(tokens);

            if (tokens.Count == 0)
            {
                return new IPFTypeNumber();
            }

            // Check for CONVERT function: CONVERT ( expr , CURRENCY )
            if (tokens.Count >= 6 && ConvertPattern.IsMatch(tokens[0]) && tokens[1] == "(")
            {
                return InferConvertType(tokens, env, context);
            }

            // Single token
            if (tokens.Count == 1)
            {
                return InferAtomType(tokens[0], env, context);
            }

            // Find main operator (lowest precedence, rightmost for left-associativity)
            var (opIndex, op) = FindMainOperator(tokens);
            if (opIndex >= 0)
            {
                return InferBinaryOpType(tokens, opIndex, op!, env, context);
            }

            // Function call or complex expression - try to infer from first token
            return InferAtomType(tokens[0], env, context);
        }

        /// <summary>
        /// Infer type of CONVERT function call
        /// </summary>
        private IPFType InferConvertType(List<string> tokens, TypeEnvironment env, string context)
        {
            // CONVERT ( expr , CURRENCY )
            // Find matching close paren
            int parenDepth = 0;
            int commaIndex = -1;

            for (int i = 1; i < tokens.Count; i++)
            {
                if (tokens[i] == "(") parenDepth++;
                else if (tokens[i] == ")") parenDepth--;
                else if (tokens[i] == "," && parenDepth == 1)
                {
                    commaIndex = i;
                    break;
                }
            }

            if (commaIndex < 0)
            {
                _errors.Add(new TypeError(TypeErrorKind.TypeMismatch, "CONVERT requires two arguments: CONVERT(expr, CURRENCY)", context));
                return new IPFTypeError("Invalid CONVERT syntax");
            }

            // Extract target currency (last token before closing paren)
            var targetCurrency = tokens[commaIndex + 1];

            // Validate target currency
            if (env.IsTypeVariable(targetCurrency))
            {
                return new IPFTypeVariable(targetCurrency);
            }
            else if (Currency.IsValid(targetCurrency))
            {
                // Check that source expression has an amount type
                var exprTokens = tokens.Skip(2).Take(commaIndex - 2).ToList();
                var sourceType = InferExpressionType(exprTokens, env, context);

                if (sourceType is IPFTypeAmount || sourceType is IPFTypeVariable || sourceType is IPFTypeNumber)
                {
                    return new IPFTypeAmount(targetCurrency);
                }
                else
                {
                    _errors.Add(TypeError.ArithmeticOnNonNumeric(sourceType, context));
                    return new IPFTypeError("CONVERT source must be numeric/amount");
                }
            }
            else
            {
                _errors.Add(TypeError.InvalidCurrency(targetCurrency, context));
                return new IPFTypeError($"Invalid currency: {targetCurrency}");
            }
        }

        /// <summary>
        /// Infer type of a single token (literal, variable, etc.)
        /// </summary>
        private IPFType InferAtomType(string token, TypeEnvironment env, string context)
        {
            // Currency literal: 100<EUR>
            var currencyMatch = CurrencyLiteralPattern.Match(token);
            if (currencyMatch.Success)
            {
                var currency = currencyMatch.Groups[2].Value;
                if (env.IsTypeVariable(currency))
                {
                    return new IPFTypeVariable(currency);
                }
                if (!Currency.IsValid(currency))
                {
                    _errors.Add(TypeError.InvalidCurrency(currency, context));
                    return new IPFTypeError($"Invalid currency: {currency}");
                }
                return new IPFTypeAmount(currency);
            }

            // Numeric literal
            if (decimal.TryParse(token, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                return new IPFTypeNumber();
            }

            // Boolean literals
            if (token.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                return new IPFTypeBoolean();
            }

            // Variable lookup
            var varType = env.Lookup(token);
            if (varType != null)
            {
                return varType;
            }

            // Check if it's a type variable reference (in polymorphic context)
            if (env.IsTypeVariable(token))
            {
                return new IPFTypeVariable(token);
            }

            // Unknown - could be a keyword or undefined variable
            // Don't report error for known keywords
            if (IsKeyword(token))
            {
                return new IPFTypeNumber(); // Keywords like IF, THEN handled elsewhere
            }

            // Treat as string literal (for list choice symbols like EntityType EQ NormalEntity)
            // This is a valid token that represents a choice symbol in a comparison
            return new IPFTypeString();
        }

        /// <summary>
        /// Infer type of binary operation
        /// </summary>
        private IPFType InferBinaryOpType(List<string> tokens, int opIndex, string op, TypeEnvironment env, string context)
        {
            var leftTokens = tokens.Take(opIndex).ToList();
            var rightTokens = tokens.Skip(opIndex + 1).ToList();

            var leftType = InferExpressionType(leftTokens, env, context);
            var rightType = InferExpressionType(rightTokens, env, context);

            // Skip further checking if either side has an error
            if (leftType is IPFTypeError || rightType is IPFTypeError)
            {
                return new IPFTypeError("Error in subexpression");
            }

            // Arithmetic operators
            if (ArithmeticOperators.Contains(op))
            {
                return InferArithmeticType(leftType, rightType, op, context);
            }

            // Comparison operators - result is boolean
            if (ComparisonOperators.Contains(op))
            {
                // For comparisons, both sides should be same type
                if (!TypesCompatible(leftType, rightType, env) &&
                    leftType is IPFTypeAmount && rightType is IPFTypeAmount)
                {
                    var leftAmt = (IPFTypeAmount)leftType;
                    var rightAmt = (IPFTypeAmount)rightType;
                    _errors.Add(TypeError.MixedCurrencyArithmetic(leftAmt.Currency, rightAmt.Currency, context));
                }
                return new IPFTypeBoolean();
            }

            // Logical operators
            if (LogicalOperators.Contains(op))
            {
                if (leftType is not IPFTypeBoolean)
                {
                    _errors.Add(TypeError.TypeMismatch(new IPFTypeBoolean(), leftType, context));
                }
                if (rightType is not IPFTypeBoolean)
                {
                    _errors.Add(TypeError.TypeMismatch(new IPFTypeBoolean(), rightType, context));
                }
                return new IPFTypeBoolean();
            }

            // Unknown operator - assume numeric
            return new IPFTypeNumber();
        }

        /// <summary>
        /// Infer type of arithmetic expression with currency awareness
        /// </summary>
        private IPFType InferArithmeticType(IPFType left, IPFType right, string op, string context)
        {
            // Number op Number -> Number
            if (left is IPFTypeNumber && right is IPFTypeNumber)
            {
                return new IPFTypeNumber();
            }

            // Amount op Number -> Amount (scalar multiplication/division)
            if (left is IPFTypeAmount leftAmt && right is IPFTypeNumber)
            {
                return leftAmt;
            }
            if (left is IPFTypeNumber && right is IPFTypeAmount rightAmt)
            {
                return rightAmt;
            }

            // Variable op Number -> Variable (polymorphic scalar)
            if (left is IPFTypeVariable leftVar && right is IPFTypeNumber)
            {
                return leftVar;
            }
            if (left is IPFTypeNumber && right is IPFTypeVariable rightVar)
            {
                return rightVar;
            }

            // Amount op Amount - must be same currency for + and -
            if (left is IPFTypeAmount leftAmount && right is IPFTypeAmount rightAmount)
            {
                if (op == "+" || op == "-")
                {
                    if (leftAmount.Currency != rightAmount.Currency)
                    {
                        _errors.Add(TypeError.MixedCurrencyArithmetic(leftAmount.Currency, rightAmount.Currency, context));
                        return new IPFTypeError("Currency mismatch");
                    }
                    return leftAmount;
                }
                // * and / between amounts is unusual but allow it, returns number
                return new IPFTypeNumber();
            }

            // Variable op Variable - must be same type variable
            if (left is IPFTypeVariable leftV && right is IPFTypeVariable rightV)
            {
                if (op == "+" || op == "-")
                {
                    if (leftV.Name != rightV.Name)
                    {
                        _errors.Add(new TypeError(TypeErrorKind.TypeMismatch,
                            $"Cannot {op} different type variables: {leftV.Name} and {rightV.Name}", context));
                        return new IPFTypeError("Type variable mismatch");
                    }
                    return leftV;
                }
                return new IPFTypeNumber();
            }

            // Amount op Variable or vice versa
            if ((left is IPFTypeAmount && right is IPFTypeVariable) ||
                (left is IPFTypeVariable && right is IPFTypeAmount))
            {
                if (op == "+" || op == "-")
                {
                    _errors.Add(new TypeError(TypeErrorKind.TypeMismatch,
                        $"Cannot {op} concrete currency with type variable", context));
                    return new IPFTypeError("Cannot mix concrete and polymorphic currencies");
                }
                return new IPFTypeNumber();
            }

            // Non-numeric types in arithmetic
            if (left is not (IPFTypeNumber or IPFTypeAmount or IPFTypeVariable))
            {
                _errors.Add(TypeError.ArithmeticOnNonNumeric(left, context));
                return new IPFTypeError("Non-numeric operand");
            }
            if (right is not (IPFTypeNumber or IPFTypeAmount or IPFTypeVariable))
            {
                _errors.Add(TypeError.ArithmeticOnNonNumeric(right, context));
                return new IPFTypeError("Non-numeric operand");
            }

            return new IPFTypeNumber();
        }

        /// <summary>
        /// Check if two types are compatible (for return type checking)
        /// </summary>
        private bool TypesCompatible(IPFType expected, IPFType actual, TypeEnvironment env)
        {
            if (expected.Equals(actual))
            {
                return true;
            }

            // Type variable matches any amount
            if (expected is IPFTypeVariable && actual is IPFTypeAmount)
            {
                return true;
            }
            if (actual is IPFTypeVariable && expected is IPFTypeAmount)
            {
                return true;
            }

            // Number is compatible with number
            if (expected is IPFTypeNumber && actual is IPFTypeNumber)
            {
                return true;
            }

            // Plain number is compatible with amounts (will be treated as unitless)
            if (expected is IPFTypeAmount && actual is IPFTypeNumber)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find the main (lowest precedence) operator in the expression
        /// </summary>
        private (int Index, string? Op) FindMainOperator(List<string> tokens)
        {
            int parenDepth = 0;
            int lowestPrecedence = int.MaxValue;
            int opIndex = -1;
            string? op = null;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token == "(") { parenDepth++; continue; }
                if (token == ")") { parenDepth--; continue; }

                if (parenDepth == 0)
                {
                    var precedence = GetPrecedence(token);
                    if (precedence > 0 && precedence <= lowestPrecedence)
                    {
                        lowestPrecedence = precedence;
                        opIndex = i;
                        op = token;
                    }
                }
            }

            return (opIndex, op);
        }

        /// <summary>
        /// Get operator precedence (lower = binds looser)
        /// </summary>
        private int GetPrecedence(string token)
        {
            return token.ToUpperInvariant() switch
            {
                "OR" => 1,
                "AND" => 2,
                "NOT" => 3,
                "EQ" or "NEQ" or "GT" or "GTE" or "LT" or "LTE" or "IN" or "NIN" => 4,
                "+" or "-" => 5,
                "*" or "/" => 6,
                _ => 0
            };
        }

        /// <summary>
        /// Strip outer parentheses if they wrap the entire expression
        /// </summary>
        private List<string> StripOuterParens(List<string> tokens)
        {
            while (tokens.Count >= 2 && tokens[0] == "(" && tokens[^1] == ")")
            {
                // Check if these parens actually wrap the whole expression
                int depth = 0;
                bool wrapsAll = true;
                for (int i = 0; i < tokens.Count - 1; i++)
                {
                    if (tokens[i] == "(") depth++;
                    else if (tokens[i] == ")") depth--;
                    if (depth == 0 && i < tokens.Count - 1)
                    {
                        wrapsAll = false;
                        break;
                    }
                }
                if (wrapsAll)
                {
                    tokens = tokens.Skip(1).Take(tokens.Count - 2).ToList();
                }
                else
                {
                    break;
                }
            }
            return tokens;
        }

        private bool IsKeyword(string token)
        {
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "IF", "THEN", "ELSE", "YIELD", "LET", "AS", "CASE", "ENDCASE",
                "COMPUTE", "FEE", "ENDCOMPUTE", "OPTIONAL", "RETURN", "CONVERT",
                "CONTAINS", "NOT", "AND", "OR", "TRUE", "FALSE"
            };
            return keywords.Contains(token);
        }
    }
}
