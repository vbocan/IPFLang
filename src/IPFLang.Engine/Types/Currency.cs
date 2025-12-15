namespace IPFLang.Types
{
    /// <summary>
    /// ISO 4217 Currency codes and validation
    /// </summary>
    public static class Currency
    {
        private static readonly CurrencyInfo[] Currencies =
        {
            new("AED", "UAE Dirham"),
            new("AFN", "Afghan Afghani"),
            new("ALL", "Albanian Lek"),
            new("AMD", "Armenian Dram"),
            new("ANG", "Netherlands Antillian Guilder"),
            new("AOA", "Angolan Kwanza"),
            new("ARS", "Argentine Peso"),
            new("AUD", "Australian Dollar"),
            new("AWG", "Aruban Florin"),
            new("AZN", "Azerbaijani Manat"),
            new("BAM", "Bosnia and Herzegovina Mark"),
            new("BBD", "Barbados Dollar"),
            new("BDT", "Bangladeshi Taka"),
            new("BGN", "Bulgarian Lev"),
            new("BHD", "Bahraini Dinar"),
            new("BIF", "Burundian Franc"),
            new("BMD", "Bermudian Dollar"),
            new("BND", "Brunei Dollar"),
            new("BOB", "Bolivian Boliviano"),
            new("BRL", "Brazilian Real"),
            new("BSD", "Bahamian Dollar"),
            new("BTN", "Bhutanese Ngultrum"),
            new("BWP", "Botswana Pula"),
            new("BYN", "Belarusian Ruble"),
            new("BZD", "Belize Dollar"),
            new("CAD", "Canadian Dollar"),
            new("CDF", "Congolese Franc"),
            new("CHF", "Swiss Franc"),
            new("CLP", "Chilean Peso"),
            new("CNY", "Chinese Renminbi"),
            new("COP", "Colombian Peso"),
            new("CRC", "Costa Rican Colon"),
            new("CUP", "Cuban Peso"),
            new("CVE", "Cape Verdean Escudo"),
            new("CZK", "Czech Koruna"),
            new("DJF", "Djiboutian Franc"),
            new("DKK", "Danish Krone"),
            new("DOP", "Dominican Peso"),
            new("DZD", "Algerian Dinar"),
            new("EGP", "Egyptian Pound"),
            new("ERN", "Eritrean Nakfa"),
            new("ETB", "Ethiopian Birr"),
            new("EUR", "Euro"),
            new("FJD", "Fiji Dollar"),
            new("FKP", "Falkland Islands Pound"),
            new("FOK", "Faroese Króna"),
            new("GBP", "Pound Sterling"),
            new("GEL", "Georgian Lari"),
            new("GGP", "Guernsey Pound"),
            new("GHS", "Ghanaian Cedi"),
            new("GIP", "Gibraltar Pound"),
            new("GMD", "Gambian Dalasi"),
            new("GNF", "Guinean Franc"),
            new("GTQ", "Guatemalan Quetzal"),
            new("GYD", "Guyanese Dollar"),
            new("HKD", "Hong Kong Dollar"),
            new("HNL", "Honduran Lempira"),
            new("HRK", "Croatian Kuna"),
            new("HTG", "Haitian Gourde"),
            new("HUF", "Hungarian Forint"),
            new("IDR", "Indonesian Rupiah"),
            new("ILS", "Israeli New Shekel"),
            new("IMP", "Manx Pound"),
            new("INR", "Indian Rupee"),
            new("IQD", "Iraqi Dinar"),
            new("IRR", "Iranian Rial"),
            new("ISK", "Icelandic Króna"),
            new("JEP", "Jersey Pound"),
            new("JMD", "Jamaican Dollar"),
            new("JOD", "Jordanian Dinar"),
            new("JPY", "Japanese Yen"),
            new("KES", "Kenyan Shilling"),
            new("KGS", "Kyrgyzstani Som"),
            new("KHR", "Cambodian Riel"),
            new("KID", "Kiribati Dollar"),
            new("KMF", "Comorian Franc"),
            new("KRW", "South Korean Won"),
            new("KWD", "Kuwaiti Dinar"),
            new("KYD", "Cayman Islands Dollar"),
            new("KZT", "Kazakhstani Tenge"),
            new("LAK", "Lao Kip"),
            new("LBP", "Lebanese Pound"),
            new("LKR", "Sri Lanka Rupee"),
            new("LRD", "Liberian Dollar"),
            new("LSL", "Lesotho Loti"),
            new("LYD", "Libyan Dinar"),
            new("MAD", "Moroccan Dirham"),
            new("MDL", "Moldovan Leu"),
            new("MGA", "Malagasy Ariary"),
            new("MKD", "Macedonian Denar"),
            new("MMK", "Burmese Kyat"),
            new("MNT", "Mongolian Tögrög"),
            new("MOP", "Macanese Pataca"),
            new("MRU", "Mauritanian Ouguiya"),
            new("MUR", "Mauritian Rupee"),
            new("MVR", "Maldivian Rufiyaa"),
            new("MWK", "Malawian Kwacha"),
            new("MXN", "Mexican Peso"),
            new("MYR", "Malaysian Ringgit"),
            new("MZN", "Mozambican Metical"),
            new("NAD", "Namibian Dollar"),
            new("NGN", "Nigerian Naira"),
            new("NIO", "Nicaraguan Córdoba"),
            new("NOK", "Norwegian Krone"),
            new("NPR", "Nepalese Rupee"),
            new("NZD", "New Zealand Dollar"),
            new("OMR", "Omani Rial"),
            new("PAB", "Panamanian Balboa"),
            new("PEN", "Peruvian Sol"),
            new("PGK", "Papua New Guinean Kina"),
            new("PHP", "Philippine Peso"),
            new("PKR", "Pakistani Rupee"),
            new("PLN", "Polish Złoty"),
            new("PYG", "Paraguayan Guaraní"),
            new("QAR", "Qatari Riyal"),
            new("RON", "Romanian Leu"),
            new("RSD", "Serbian Dinar"),
            new("RUB", "Russian Ruble"),
            new("RWF", "Rwandan Franc"),
            new("SAR", "Saudi Riyal"),
            new("SBD", "Solomon Islands Dollar"),
            new("SCR", "Seychellois Rupee"),
            new("SDG", "Sudanese Pound"),
            new("SEK", "Swedish Krona"),
            new("SGD", "Singapore Dollar"),
            new("SHP", "Saint Helena Pound"),
            new("SLE", "Sierra Leonean Leone"),
            new("SOS", "Somali Shilling"),
            new("SRD", "Surinamese Dollar"),
            new("SSP", "South Sudanese Pound"),
            new("STN", "São Tomé and Príncipe Dobra"),
            new("SYP", "Syrian Pound"),
            new("SZL", "Eswatini Lilangeni"),
            new("THB", "Thai Baht"),
            new("TJS", "Tajikistani Somoni"),
            new("TMT", "Turkmenistan Manat"),
            new("TND", "Tunisian Dinar"),
            new("TOP", "Tongan Paʻanga"),
            new("TRY", "Turkish Lira"),
            new("TTD", "Trinidad and Tobago Dollar"),
            new("TVD", "Tuvaluan Dollar"),
            new("TWD", "New Taiwan Dollar"),
            new("TZS", "Tanzanian Shilling"),
            new("UAH", "Ukrainian Hryvnia"),
            new("UGX", "Ugandan Shilling"),
            new("USD", "United States Dollar"),
            new("UYU", "Uruguayan Peso"),
            new("UZS", "Uzbekistani So'm"),
            new("VES", "Venezuelan Bolívar Soberano"),
            new("VND", "Vietnamese Đồng"),
            new("VUV", "Vanuatu Vatu"),
            new("WST", "Samoan Tālā"),
            new("XAF", "Central African CFA Franc"),
            new("XCD", "East Caribbean Dollar"),
            new("XDR", "Special Drawing Rights"),
            new("XOF", "West African CFA franc"),
            new("XPF", "CFP Franc"),
            new("YER", "Yemeni Rial"),
            new("ZAR", "South African Rand"),
            new("ZMW", "Zambian Kwacha"),
            new("ZWL", "Zimbabwean Dollar"),
        };

        private static readonly HashSet<string> ValidCodes = new(Currencies.Select(c => c.Code));

        /// <summary>
        /// Check if a currency code is a valid ISO 4217 code
        /// </summary>
        public static bool IsValid(string code)
        {
            return !string.IsNullOrEmpty(code) && ValidCodes.Contains(code.ToUpperInvariant());
        }

        /// <summary>
        /// Get all supported currencies
        /// </summary>
        public static IEnumerable<(string Code, string Name)> GetAll()
        {
            return Currencies.OrderBy(c => c.Code).Select(c => (c.Code, c.Name));
        }

        /// <summary>
        /// Get the name of a currency by its code
        /// </summary>
        public static string? GetName(string code)
        {
            return Currencies.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Name;
        }

        /// <summary>
        /// Parse a currency code, throwing if invalid
        /// </summary>
        public static string Parse(string code)
        {
            var normalized = code.ToUpperInvariant();
            if (!IsValid(normalized))
            {
                throw new ArgumentException($"Invalid ISO 4217 currency code: '{code}'", nameof(code));
            }
            return normalized;
        }

        /// <summary>
        /// Try to parse a currency code
        /// </summary>
        public static bool TryParse(string code, out string result)
        {
            var normalized = code.ToUpperInvariant();
            if (IsValid(normalized))
            {
                result = normalized;
                return true;
            }
            result = string.Empty;
            return false;
        }

        private record CurrencyInfo(string Code, string Name);
    }
}
