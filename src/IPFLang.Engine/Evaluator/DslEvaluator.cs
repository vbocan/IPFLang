using System.Globalization;
using IPFLang.CurrencyConversion;

namespace IPFLang.Evaluator
{
    /// <summary>
    /// Influenced by: https://github.com/toptensoftware/SimpleExpressionEngine
    /// https://medium.com/@toptensoftware/writing-a-simple-math-expression-engine-in-c-d414de18d4ce
    /// </summary>
    public class DslEvaluator
    {
        public DslEvaluator() { }

        private static readonly Dictionary<string, int> Operators = new() { { "LT", 4 }, { "LTE", 4 }, { "GT", 4 }, { "GTE", 4 }, { "EQ", 3 }, { "NEQ", 3 }, { "IN", 3 }, { "NIN", 3 }, { "AND", 2 }, { "OR", 1 } };

        public static decimal EvaluateExpression(string[] Tokens, IEnumerable<IPFValue> Vars) => EvaluateExpression(Tokens, Vars, string.Empty, null);
        public static decimal EvaluateExpression(string[] Tokens, IEnumerable<IPFValue> Vars, string FeeName) => EvaluateExpression(Tokens, Vars, FeeName, null);
        public static decimal EvaluateExpression(string[] Tokens, IEnumerable<IPFValue> Vars, string FeeName, ICurrencyConverter? currencyConverter)
        {
            return Parser.Parse(string.Join(" ", Tokens)).Eval(new EvaluatorContext(Vars, FeeName, currencyConverter));
        }

        public static bool EvaluateLogic(string[] Tokens, IEnumerable<IPFValue> Vars) => EvaluateLogic(Tokens, Vars, string.Empty, null);
        public static bool EvaluateLogic(string[] Tokens, IEnumerable<IPFValue> Vars, string FeeName) => EvaluateLogic(Tokens, Vars, FeeName, null);
        public static bool EvaluateLogic(string[] Tokens, IEnumerable<IPFValue> Vars, string FeeName, ICurrencyConverter? currencyConverter)
        {
            // If no tokens are provided, the logic is implicitly true
            if (Tokens.Length == 0) return true;

            // Initialize context
            var Context = new EvaluatorContext(Vars, FeeName, currencyConverter);

            // Stack for values
            var values = new Stack<IPFValue>();
            // Stack for operators
            var ops = new Stack<string>();

            for (int i = 0; i < Tokens.Length; i++)
            {
                // Attempt to resolve token as a variable
                var v = Context.ProcessTokenAsVariableOrProperty(Tokens[i]);
                if (v is not null)
                {
                    values.Push(v);
                    continue;
                }
                // Current token is an opening brace, push it to 'ops'
                if (Tokens[i].Equals("("))
                {
                    ops.Push(Tokens[i]);
                }
                // Closing brace encountered, solve entire brace
                else if (Tokens[i].Equals(")"))
                {
                    while (ops.Peek() != "(")
                    {
                        values.Push(ApplyOperation(ops.Pop(), values.Pop(), values.Pop()));
                    }
                    ops.Pop();
                }
                // Current token is an operator.
                else if (Operators.ContainsKey(Tokens[i]))
                {
                    // While top of 'ops' has same or greater precedence to current token, which is an operator.
                    // Apply operator on top of 'ops' to top two elements in values stack
                    while (ops.Count > 0 && HasPrecedence(Tokens[i], ops.Peek()))
                    {
                        values.Push(ApplyOperation(ops.Pop(), values.Pop(), values.Pop()));
                    }

                    // Push current token to 'ops'.
                    ops.Push(Tokens[i]);
                }
                // Current token is a number
                else if (decimal.TryParse(Tokens[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal Number))
                {
                    values.Push(new IPFValueNumber(string.Empty, Number));
                }
                // Current token is a boolean (TRUE or FALSE)
                else if (bool.TryParse(Tokens[i], out bool Boolean))
                {
                    values.Push(new IPFValueBoolean(string.Empty, Boolean));
                }
                // Current token is a string
                else
                {
                    values.Push(new IPFValueString(string.Empty, Tokens[i]));
                }
            }

            // Entire expression has been parsed at this point, apply remaining ops to remaining values
            while (ops.Count > 0)
            {
                values.Push(ApplyOperation(ops.Pop(), values.Pop(), values.Pop()));
            }

            // Top of 'values' contains result, return it
            var result = values.Pop();
            if (result is not IPFValueBoolean) throw new NotSupportedException(string.Format("Invalid logic expression: [{0}]", string.Join(' ', Tokens)));
            return (result as IPFValueBoolean)!.Value;
        }

        // Returns true if 'op2' has higher or same precedence as 'op1', otherwise returns false.
        private static bool HasPrecedence(string op1, string op2)
        {
            if (op2 == "(" || op2 == ")")
            {
                return false;
            }
            if (!Operators.ContainsKey(op1)) throw new InvalidDataException($"Unknown operator: '{op1}'");
            if (!Operators.ContainsKey(op2)) throw new InvalidDataException($"Unknown operator: '{op2}'");
            return Operators[op2] > Operators[op1];
        }

        // A utility method to apply an operator 'op' on operands 'a' and 'b'. Return the result.
        private static IPFValueBoolean ApplyOperation(string op, IPFValue b, IPFValue a)
        {
            // Both values are booleans
            if (a is IPFValueBoolean && b is IPFValueBoolean)
            {
                var av = (a as IPFValueBoolean)!.Value;
                var bv = (b as IPFValueBoolean)!.Value;
                return op switch
                {
                    "AND" => new IPFValueBoolean(string.Empty, av && bv),
                    "OR" => new IPFValueBoolean(string.Empty, av || bv),
                    "EQ" => new IPFValueBoolean(string.Empty, av == bv),
                    "NEQ" => new IPFValueBoolean(string.Empty, av != bv),
                    "LT" or "LTE" or "GT" or "GTE" or "IN" or "NIN" => throw new NotSupportedException($"Operator [{op}] cannot compare booleans"),
                    _ => throw new InvalidDataException($"Unknown operator: '{op}'"),
                };
            }
            // Both values are string
            else if (a is IPFValueString && b is IPFValueString)
            {
                var av = (a as IPFValueString)!.Value;
                var bv = (b as IPFValueString)!.Value;
                return op switch
                {
                    "EQ" => new IPFValueBoolean(string.Empty, av == bv),
                    "NEQ" => new IPFValueBoolean(string.Empty, av != bv),
                    "AND" or "OR" or "LT" or "LTE" or "GT" or "GTE" or "IN" or "NIN" => throw new NotSupportedException($"Operator [{op}] cannot compare strings"),
                    _ => throw new InvalidDataException($"Unknown operator: '{op}'"),
                };
            }
            // Left value is string and right value is string list
            else if (a is IPFValueString && b is IPFValueStringList)
            {
                var av = (a as IPFValueString)!.Value;
                var bv = (b as IPFValueStringList)!.Value;
                return op switch
                {
                    "IN" => new IPFValueBoolean(string.Empty, bv.Contains(av)),
                    "NIN" => new IPFValueBoolean(string.Empty, !bv.Contains(av)),
                    "EQ" or "NEQ" or "AND" or "OR" or "LT" or "LTE" or "GT" or "GTE" => throw new NotSupportedException($"Operator [{op}] cannot compare string lists"),
                    _ => throw new InvalidDataException($"Unknown operator: '{op}'"),
                }; ;
            }
            // Both values are numeric
            else if (a is IPFValueNumber && b is IPFValueNumber)
            {
                var av = (a as IPFValueNumber)!.Value;
                var bv = (b as IPFValueNumber)!.Value;
                return op switch
                {
                    "EQ" => new IPFValueBoolean(string.Empty, av == bv),
                    "NEQ" => new IPFValueBoolean(string.Empty, av != bv),
                    "LT" => new IPFValueBoolean(string.Empty, av < bv),
                    "LTE" => new IPFValueBoolean(string.Empty, av <= bv),
                    "GT" => new IPFValueBoolean(string.Empty, av > bv),
                    "GTE" => new IPFValueBoolean(string.Empty, av >= bv),
                    "AND" or "OR" or "IN" or "NIN" => throw new NotSupportedException($"Operator [{op}] cannot compare numbers"),
                    _ => throw new InvalidDataException($"Unknown operator: '{op}'"),
                };
            }
            // Both values are dates
            else if (a is IPFValueDate && b is IPFValueDate)
            {
                var av = (a as IPFValueDate)!.Value;
                var bv = (b as IPFValueDate)!.Value;
                return op switch
                {
                    "EQ" => new IPFValueBoolean(string.Empty, av == bv),
                    "NEQ" => new IPFValueBoolean(string.Empty, av != bv),
                    "LT" => new IPFValueBoolean(string.Empty, av < bv),
                    "LTE" => new IPFValueBoolean(string.Empty, av <= bv),
                    "GT" => new IPFValueBoolean(string.Empty, av > bv),
                    "GTE" => new IPFValueBoolean(string.Empty, av >= bv),
                    "AND" or "OR" or "IN" or "NIN" => throw new NotSupportedException($"Operator [{op}] cannot compare dates"),
                    _ => throw new InvalidDataException($"Unknown operator: '{op}'"),
                };
            }
            else throw new NotSupportedException($"Type mismatch for [{a}] and [{b}]");
        }

    }
}
