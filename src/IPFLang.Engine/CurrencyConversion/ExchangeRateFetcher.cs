using System.Text.Json;

namespace IPFLang.CurrencyConversion
{
    /// <summary>
    /// Fetches exchange rate data from exchangerate-api.com (ECB-based)
    /// </summary>
    public class ExchangeRateFetcher : IExchangeRateFetcher
    {
        private const string BaseCurrency = "EUR";
        private const string ApiUrlTemplate = "https://v6.exchangerate-api.com/v6/{0}/latest/{1}";

        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Create a new exchange rate fetcher
        /// </summary>
        /// <param name="apiKey">API key for exchangerate-api.com</param>
        /// <param name="httpClient">Optional HttpClient (for testing/DI)</param>
        public ExchangeRateFetcher(string apiKey, HttpClient? httpClient = null)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Fetch current exchange rate data
        /// </summary>
        public async Task<ExchangeRateResponse> FetchExchangeRates()
        {
            var url = string.Format(ApiUrlTemplate, _apiKey, BaseCurrency);

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseBody);

                // Check response status
                var result = jsonDocument.RootElement.GetProperty("result").GetString();
                if (!string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
                {
                    return ExchangeRateResponse.Invalid("Invalid response from exchange service");
                }

                // Parse last update timestamp
                var unixTimestamp = jsonDocument.RootElement.GetProperty("time_last_update_unix").GetDouble();
                var lastUpdated = DateTimeOffset.FromUnixTimeSeconds((long)unixTimestamp).LocalDateTime;

                // Parse exchange rates
                var ratesJson = jsonDocument.RootElement.GetProperty("conversion_rates").GetRawText();
                var exchangeRates = ParseExchangeRates(ratesJson);

                return ExchangeRateResponse.Success(exchangeRates, lastUpdated);
            }
            catch (HttpRequestException ex)
            {
                return ExchangeRateResponse.Invalid($"HTTP error fetching exchange rates: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return ExchangeRateResponse.Invalid($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ExchangeRateResponse.Invalid($"Error fetching exchange rates: {ex.Message}");
            }
        }

        private static Dictionary<string, decimal> ParseExchangeRates(string jsonString)
        {
            var exchangeRates = new Dictionary<string, decimal>();
            var jsonDocument = JsonDocument.Parse(jsonString);

            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                var currencyCode = property.Name;
                var exchangeRate = property.Value.GetDecimal();
                exchangeRates[currencyCode] = exchangeRate;
            }

            return exchangeRates;
        }
    }

    /// <summary>
    /// Mock exchange rate fetcher for testing (returns fixed rates)
    /// </summary>
    public class MockExchangeRateFetcher : IExchangeRateFetcher
    {
        private readonly Dictionary<string, decimal> _rates;

        public MockExchangeRateFetcher()
        {
            // Default rates relative to EUR
            _rates = new Dictionary<string, decimal>
            {
                ["EUR"] = 1.0m,
                ["USD"] = 1.08m,
                ["GBP"] = 0.86m,
                ["JPY"] = 162.5m,
                ["CHF"] = 0.94m,
                ["CAD"] = 1.47m,
                ["AUD"] = 1.65m,
                ["CNY"] = 7.85m,
                ["INR"] = 90.2m,
                ["RON"] = 4.97m,
            };
        }

        public MockExchangeRateFetcher(Dictionary<string, decimal> customRates)
        {
            _rates = customRates;
        }

        public Task<ExchangeRateResponse> FetchExchangeRates()
        {
            return Task.FromResult(ExchangeRateResponse.Success(_rates, DateTime.Now));
        }
    }
}
