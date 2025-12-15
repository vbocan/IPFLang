namespace IPFLang.CurrencyConversion
{
    /// <summary>
    /// Interface for fetching exchange rate data
    /// </summary>
    public interface IExchangeRateFetcher
    {
        /// <summary>
        /// Fetch current exchange rate data from the provider
        /// </summary>
        Task<ExchangeRateResponse> FetchExchangeRates();
    }
}
