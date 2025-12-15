using IPFLang.Types;

namespace IPFLang.CurrencyConversion
{
    /// <summary>
    /// Currency converter implementation using exchange rate data
    /// </summary>
    public class CurrencyConverter : ICurrencyConverter
    {
        public ExchangeRateResponse Response { get; set; } = ExchangeRateResponse.Invalid("Not initialized");

        /// <summary>
        /// Get all available currencies from the ISO 4217 list
        /// </summary>
        public IEnumerable<(string Code, string Name)> GetCurrencies() => Currency.GetAll();

        /// <summary>
        /// Convert an amount from one currency to another
        /// </summary>
        public decimal Convert(decimal amount, string sourceCurrency, string targetCurrency)
        {
            // Same currency - no conversion needed
            if (sourceCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return amount;
            }

            // Validate exchange rate data is available
            if (Response.Status == ResponseStatus.Invalid)
            {
                throw new InvalidOperationException("No currency exchange rate data available");
            }

            // Normalize currency codes
            var source = sourceCurrency.ToUpperInvariant();
            var target = targetCurrency.ToUpperInvariant();

            // Validate currencies exist in exchange rate data
            if (!Response.ExchangeRates.ContainsKey(source))
            {
                throw new ArgumentException($"Unknown source currency: '{sourceCurrency}'", nameof(sourceCurrency));
            }
            if (!Response.ExchangeRates.ContainsKey(target))
            {
                throw new ArgumentException($"Unknown target currency: '{targetCurrency}'", nameof(targetCurrency));
            }

            // Compute conversion (rates are relative to EUR)
            decimal sourceRate = Response.ExchangeRates[source];
            decimal targetRate = Response.ExchangeRates[target];
            decimal convertedAmount = amount / sourceRate * targetRate;

            return convertedAmount;
        }

        /// <summary>
        /// Get the exchange rate between two currencies
        /// </summary>
        public decimal GetRate(string sourceCurrency, string targetCurrency)
        {
            // Same currency - rate is 1
            if (sourceCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            // Validate exchange rate data is available
            if (Response.Status == ResponseStatus.Invalid)
            {
                throw new InvalidOperationException("No currency exchange rate data available");
            }

            // Normalize currency codes
            var source = sourceCurrency.ToUpperInvariant();
            var target = targetCurrency.ToUpperInvariant();

            // Validate currencies exist
            if (!Response.ExchangeRates.ContainsKey(source))
            {
                throw new ArgumentException($"Unknown source currency: '{sourceCurrency}'", nameof(sourceCurrency));
            }
            if (!Response.ExchangeRates.ContainsKey(target))
            {
                throw new ArgumentException($"Unknown target currency: '{targetCurrency}'", nameof(targetCurrency));
            }

            // Compute rate (rates are relative to EUR)
            decimal sourceRate = Response.ExchangeRates[source];
            decimal targetRate = Response.ExchangeRates[target];

            return targetRate / sourceRate;
        }
    }
}
