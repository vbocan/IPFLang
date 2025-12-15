namespace IPFLang.CurrencyConversion
{
    /// <summary>
    /// Status of exchange rate data
    /// </summary>
    public enum ResponseStatus
    {
        /// <summary>
        /// Fresh data fetched successfully
        /// </summary>
        Online,

        /// <summary>
        /// Data exists but may be outdated
        /// </summary>
        Stale,

        /// <summary>
        /// Data fetch failed or is unusable
        /// </summary>
        Invalid
    }

    /// <summary>
    /// Exchange rate response from the provider
    /// </summary>
    public record ExchangeRateResponse(
        ResponseStatus Status,
        string Reason,
        Dictionary<string, decimal> ExchangeRates,
        DateTime LastUpdatedOn
    )
    {
        /// <summary>
        /// Create an invalid response
        /// </summary>
        public static ExchangeRateResponse Invalid(string reason) =>
            new(ResponseStatus.Invalid, reason, new Dictionary<string, decimal>(), DateTime.Now);

        /// <summary>
        /// Create a successful response
        /// </summary>
        public static ExchangeRateResponse Success(Dictionary<string, decimal> rates, DateTime lastUpdated) =>
            new(ResponseStatus.Online, string.Empty, rates, lastUpdated);
    }
}
