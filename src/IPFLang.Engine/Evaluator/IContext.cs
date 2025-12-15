namespace IPFLang.Evaluator
{
    public interface IContext
    {
        decimal ResolveVariable(string name);
        decimal CallFunction(string name, decimal[] arguments);

        /// <summary>
        /// Convert amount from one currency to another using exchange rates
        /// </summary>
        decimal ConvertCurrency(decimal amount, string sourceCurrency, string targetCurrency);
    }
}
