namespace IPFLang.CurrencyConversion
{
    /// <summary>
    /// Interface for currency conversion operations
    /// </summary>
    public interface ICurrencyConverter
    {
        /// <summary>
        /// The current exchange rate data
        /// </summary>
        ExchangeRateResponse Response { get; set; }

        /// <summary>
        /// Convert an amount from one currency to another
        /// </summary>
        /// <param name="amount">The amount to convert</param>
        /// <param name="sourceCurrency">Source currency (ISO 4217 code)</param>
        /// <param name="targetCurrency">Target currency (ISO 4217 code)</param>
        /// <returns>The converted amount</returns>
        decimal Convert(decimal amount, string sourceCurrency, string targetCurrency);

        /// <summary>
        /// Get the exchange rate between two currencies
        /// </summary>
        decimal GetRate(string sourceCurrency, string targetCurrency);

        /// <summary>
        /// Get all available currencies
        /// </summary>
        IEnumerable<(string Code, string Name)> GetCurrencies();
    }
}
