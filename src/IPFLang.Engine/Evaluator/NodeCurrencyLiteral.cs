namespace IPFLang.Evaluator
{
    /// <summary>
    /// Represents a currency-annotated literal value like 100<EUR>
    /// At runtime, evaluates to its numeric value (currency is validated at type-check time)
    /// </summary>
    public class NodeCurrencyLiteral : Node
    {
        private readonly decimal _value;
        private readonly string _currency;

        public NodeCurrencyLiteral(decimal value, string currency)
        {
            _value = value;
            _currency = currency;
        }

        public decimal Value => _value;
        public string Currency => _currency;

        public override decimal Eval(IContext ctx)
        {
            // At runtime, currency literals just return their numeric value
            // Currency compatibility has been verified at type-check time
            return _value;
        }
    }
}
