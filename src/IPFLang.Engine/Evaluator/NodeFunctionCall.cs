namespace IPFLang.Evaluator
{
    public class NodeFunctionCall : Node
    {
        public NodeFunctionCall(string functionName, Node[] arguments)
        {
            _functionName = functionName;
            _arguments = arguments;
        }

        string _functionName;
        Node[] _arguments;

        public override decimal Eval(IContext ctx)
        {
            // Special handling for CONVERT function
            if (_functionName.Equals("CONVERT", StringComparison.OrdinalIgnoreCase))
            {
                return EvalConvert(ctx);
            }

            // Evaluate all arguments
            var argVals = new decimal[_arguments.Length];
            for (int i = 0; i < _arguments.Length; i++)
            {
                argVals[i] = _arguments[i].Eval(ctx);
            }

            // Call the function
            return ctx.CallFunction(_functionName, argVals);
        }

        private decimal EvalConvert(IContext ctx)
        {
            // CONVERT(amount, TargetCurrency) or CONVERT(amount, SourceCurrency, TargetCurrency)
            if (_arguments.Length < 2)
            {
                throw new SyntaxException("CONVERT requires at least 2 arguments: CONVERT(amount, TargetCurrency)");
            }

            // First argument is the amount expression
            var amount = _arguments[0].Eval(ctx);

            // Get currency codes from arguments
            string sourceCurrency;
            string targetCurrency;

            if (_arguments.Length == 2)
            {
                // CONVERT(amount, TargetCurrency)
                // Source currency must be determined from the first argument
                if (_arguments[0] is NodeCurrencyLiteral currLit)
                {
                    sourceCurrency = currLit.Currency;
                }
                else if (_arguments[0] is NodeVariable varNode)
                {
                    // Try to get currency from an Amount variable
                    // For now, assume we need explicit source currency
                    throw new SyntaxException("CONVERT with 2 arguments requires a currency literal as first argument. Use CONVERT(amount, SourceCurrency, TargetCurrency) for variables.");
                }
                else
                {
                    throw new SyntaxException("CONVERT with 2 arguments requires a currency literal as first argument");
                }

                targetCurrency = GetCurrencyFromNode(_arguments[1]);
            }
            else
            {
                // CONVERT(amount, SourceCurrency, TargetCurrency)
                sourceCurrency = GetCurrencyFromNode(_arguments[1]);
                targetCurrency = GetCurrencyFromNode(_arguments[2]);
            }

            return ctx.ConvertCurrency(amount, sourceCurrency, targetCurrency);
        }

        private static string GetCurrencyFromNode(Node node)
        {
            if (node is NodeVariable varNode)
            {
                return varNode.Name;
            }
            if (node is NodeCurrencyLiteral currNode)
            {
                return currNode.Currency;
            }
            throw new SyntaxException("Expected currency code identifier");
        }
    }
}
