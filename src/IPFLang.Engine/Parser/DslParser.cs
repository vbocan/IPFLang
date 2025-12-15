using System.Globalization;
using System.Text;

namespace IPFLang.Parser
{
    public class DslParser : IDslParser
    {
        Parsing CurrentlyParsing = Parsing.None;

        private DslInputList CurrentList { get; set; } = new DslInputList(string.Empty, string.Empty, string.Empty, new List<DslListItem>(), string.Empty);
        private DslInputListMultiple CurrentListMultiple { get; set; } = new DslInputListMultiple(string.Empty, string.Empty, string.Empty, new List<DslListItem>(), new List<string>());
        private DslInputNumber CurrentNumber { get; set; } = new DslInputNumber(string.Empty, string.Empty, string.Empty, int.MinValue, int.MaxValue, 0);
        private DslInputDate CurrentDate { get; set; } = new DslInputDate(string.Empty, string.Empty, string.Empty, DateOnly.MinValue, DateOnly.MaxValue, DateOnly.FromDateTime(DateTime.Now));
        private DslInputBoolean CurrentBoolean { get; set; } = new DslInputBoolean(string.Empty, string.Empty, string.Empty, false);
        private DslInputAmount CurrentAmount { get; set; } = new DslInputAmount(string.Empty, string.Empty, string.Empty, string.Empty, 0m);
        private DslFee CurrentFee { get; set; } = new DslFee(string.Empty, false, new List<DslItem>(), new List<DslFeeVar>());
        private DslFeeCase CurrentFeeCase { get; set; } = new DslFeeCase(Enumerable.Empty<string>(), new List<DslFeeYield>());

        private readonly IList<DslGroup> IPFGroups = new List<DslGroup>();
        private readonly IList<DslReturn> IPFReturns = new List<DslReturn>();
        private readonly IList<DslInput> IPFInputs = new List<DslInput>();
        private readonly IList<DslFee> IPFFees = new List<DslFee>();
        private readonly IList<DslVerify> IPFVerifications = new List<DslVerify>();
        private readonly IList<(DslError, string)> IPFErrors = new List<(DslError, string)>();
        private DslVersion? IPFVersion = null;


        public DslParser() { }

        public bool Parse(string source)
        {
            Func<string[], bool>[] IPFParsers = new Func<string[], bool>[]
            {
                ParseVersion,
                ParseGroup,
                ParseList,
                ParseListChoice,
                ParseListDefaultValue,
                ParseListGroup,
                ParseListMultiple,
                ParseListMultipleChoice,
                ParseListMultipleDefaultValues,
                ParseListMultipleGroup,
                ParseNumber,
                ParseNumberBetween,
                ParseNumberDefault,
                ParseNumberGroup,
                ParseDate,
                ParseDateBetween,
                ParseDateDefault,
                ParseDateGroup,
                ParseBoolean,
                ParseBooleanDefault,
                ParseBooleanGroup,
                ParseAmount,
                ParseAmountCurrency,
                ParseAmountDefault,
                ParseAmountGroup,
                ParseEndDefine,
                ParseFee,
                ParseFeeCase,
                ParseFeeYield,
                ParseFeeLet,
                ParseFeeEndCase,
                ParseEndCompute,
                ParseReturn,
                ParseVerifyComplete,
                ParseVerifyMonotonic
            };

            string[] IPFData = source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < IPFData.Length; i++)
            {
                string line = ProcessComment(IPFData[i]);
                // Do not process an empty line
                if (string.IsNullOrEmpty(line)) { continue; }
                // Generate tokens
                var tokens = Tokenize(line).Select(s => s.Trim()).ToArray();
                // Using the chain of command design pattern, try to decode the tokens from the line
                bool LineParsed = false;
                foreach (var p in IPFParsers)
                {
                    try
                    {
                        if (p(tokens))
                        {
                            LineParsed = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        IPFErrors.Add((DslError.SyntaxError, $"[Line {i + 1}] Error: {ex.Message}"));
                    }
                }
                // If the line hasn't been parsed until now, it's something wrong with it
                if (!LineParsed)
                {
                    IPFErrors.Add((DslError.SyntaxError, $"[Line {i + 1}] Error: Invalid syntax"));
                }
            }

            if (IPFErrors.Count > 0) return false;

            // Perform semantic checking
            var errs = DslSemanticChecker.Check(IPFInputs, IPFFees);
            if (errs.Any())
            {
                foreach (var e in errs) IPFErrors.Add(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Strips comment from the input line
        /// </summary>
        /// <param name="line">Original source line</param>
        /// <returns>Source line without comment (everything after the # character)</returns>
        private static string ProcessComment(string line)
        {
            // Ignore comments (after the # sign) but only if the comment is outside a string
            bool InString = false;
            var i = 0;
            for (; i < line.Length; i++)
            {
                if (line[i] == '\'') InString = !InString;
                if (line[i] == '#' && !InString) break;
            }
            string line1 = line[..i].Trim();
            return line1;
        }

        public IEnumerable<DslReturn> GetReturns()
        {
            if (IPFErrors.Count > 0) throw new NotSupportedException("Unable to access returned items. Check the error list.");
            return IPFReturns;
        }

        public IEnumerable<DslGroup> GetGroups()
        {
            if (IPFErrors.Count > 0) throw new NotSupportedException("Unable to access returned items. Check the error list.");
            return IPFGroups;
        }

        public IEnumerable<DslInput> GetInputs()
        {
            if (IPFErrors.Count > 0) throw new NotSupportedException("Unable to access variables. Check the error list.");
            return IPFInputs;
        }

        public IEnumerable<DslFee> GetFees()
        {
            if (IPFErrors.Count > 0) throw new NotSupportedException("Unable to access fees. Check the error list.");
            return IPFFees;
        }
        public IEnumerable<(DslError, string)> GetErrors()
        {
            return IPFErrors;
        }

        public IEnumerable<DslVerify> GetVerifications()
        {
            if (IPFErrors.Count > 0) throw new NotSupportedException("Unable to access verifications. Check the error list.");
            return IPFVerifications;
        }

        public DslVersion? GetVersion()
        {
            if (IPFErrors.Count > 0) throw new NotSupportedException("Unable to access version. Check the error list.");
            return IPFVersion;
        }

        #region Tokenization
        /// <summary>
        /// Tokenize input string, taking care of the single quote strings
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Tokens resulting from the input string</returns>
        IEnumerable<string> Tokenize(string input)
        {
            string token = string.Empty;
            var SingleCharTokens = new List<char> { '(', ')', '+', '-', '*', '/', ',' };
            bool inQuote = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '\'')
                {
                    inQuote = !inQuote;
                    if (!inQuote)
                    {
                        yield return token;
                        token = string.Empty;
                    }
                }
                else if (SingleCharTokens.Contains(c) && !inQuote)
                {
                    if (token != string.Empty)
                    {
                        yield return token;
                        token = string.Empty;
                    }
                    yield return c.ToString();
                }
                else if (c == ' ' && !inQuote)
                {
                    if (token != string.Empty)
                    {
                        yield return token;
                        token = string.Empty;
                    }
                }
                else
                {
                    token += c;
                }
            }
            if (token != string.Empty)
            {
                yield return token;
            }
        }
        #endregion

        #region Group Parsing
        bool ParseGroup(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 8) return false;
            if (tokens[0] != "DEFINE") return false;
            if (tokens[1] != "GROUP") return false;
            if (tokens[3] != "AS") return false;
            if (tokens[5] != "WITH") return false;
            if (tokens[6] != "WEIGHT") return false;
            if (!int.TryParse(tokens[7], out int GroupWeight)) return false;
            var item = new DslGroup(tokens[2], tokens[4], GroupWeight);
            IPFGroups.Add(item);
            return true;
        }
        #endregion

        #region List Parsing
        bool ParseList(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 5) return false;
            if (tokens[0] != "DEFINE") return false;
            if (tokens[1] != "LIST") return false;
            if (tokens[3] != "AS") return false;
            CurrentlyParsing = Parsing.List;
            CurrentList = new DslInputList(tokens[2], tokens[4], string.Empty, new List<DslListItem>(), string.Empty);
            return true;
        }

        bool ParseListChoice(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.List) return false;
            if (tokens.Length != 4) return false;
            if (tokens[0] != "CHOICE") return false;
            if (tokens[2] != "AS") return false;
            var item = new DslListItem(tokens[1], tokens[3]);
            CurrentList.Items.Add(item);
            return true;
        }

        bool ParseListDefaultValue(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.List) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "DEFAULT") return false;
            CurrentList = CurrentList with { DefaultSymbol = tokens[1] };
            return true;
        }

        bool ParseListGroup(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.List) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "GROUP") return false;
            CurrentList = CurrentList with { Group = tokens[1] };
            return true;
        }
        #endregion

        #region List Parsing (Multiple Selection)
        bool ParseListMultiple(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 5) return false;
            if (tokens[0] != "DEFINE") return false;
            if (tokens[1] != "MULTILIST") return false;
            if (tokens[3] != "AS") return false;
            CurrentlyParsing = Parsing.ListMultiple;
            CurrentListMultiple = new DslInputListMultiple(tokens[2], tokens[4], string.Empty, new List<DslListItem>(), new List<string>());
            return true;
        }

        bool ParseListMultipleChoice(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.ListMultiple) return false;
            if (tokens.Length != 4) return false;
            if (tokens[0] != "CHOICE") return false;
            if (tokens[2] != "AS") return false;
            var item = new DslListItem(tokens[1], tokens[3]);
            CurrentListMultiple.Items.Add(item);
            return true;
        }

        bool ParseListMultipleDefaultValues(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.ListMultiple) return false;
            if (!(tokens.Length > 1)) return false;
            if (tokens[0] != "DEFAULT") return false;
            var DefaultSymbols = string.Join("", tokens.Skip(1));
            CurrentListMultiple = CurrentListMultiple with { DefaultSymbols = DefaultSymbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() };
            return true;
        }

        bool ParseListMultipleGroup(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.ListMultiple) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "GROUP") return false;
            CurrentListMultiple = CurrentListMultiple with { Group = tokens[1] };
            return true;
        }
        #endregion

        #region Number Parsing
        bool ParseNumber(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 5) return false;
            if (tokens[0] != "DEFINE") return false;
            if (tokens[1] != "NUMBER") return false;
            if (tokens[3] != "AS") return false;
            CurrentlyParsing = Parsing.Number;
            CurrentNumber = new DslInputNumber(tokens[2], tokens[4], string.Empty, int.MinValue, int.MaxValue, 0);
            return true;
        }

        bool ParseNumberBetween(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Number) return false;
            if (tokens.Length != 4) return false;
            if (tokens[0] != "BETWEEN") return false;
            if (tokens[2] != "AND") return false;
            if (!int.TryParse(tokens[1], out int MinValue)) return false;
            if (!int.TryParse(tokens[3], out int MaxValue)) return false;
            CurrentNumber = CurrentNumber with { MinValue = MinValue, MaxValue = MaxValue };
            return true;
        }

        bool ParseNumberDefault(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Number) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "DEFAULT") return false;
            if (!int.TryParse(tokens[1], out int DefaultValue)) return false;
            CurrentNumber = CurrentNumber with { DefaultValue = DefaultValue };
            return true;
        }

        bool ParseNumberGroup(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Number) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "GROUP") return false;
            CurrentNumber = CurrentNumber with { Group = tokens[1] };
            return true;
        }
        #endregion

        #region Date Parsing
        bool ParseDate(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 5) return false;
            if (tokens[0] != "DEFINE") return false;
            if (tokens[1] != "DATE") return false;
            if (tokens[3] != "AS") return false;
            CurrentlyParsing = Parsing.Date;
            CurrentDate = new DslInputDate(tokens[2], tokens[4], string.Empty, DateOnly.FromDateTime(DateTime.MinValue), DateOnly.FromDateTime(DateTime.MaxValue.Date), DateOnly.FromDateTime(DateTime.Now));
            return true;
        }

        bool ParseDateBetween(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Date) return false;
            if (tokens.Length != 4) return false;
            if (tokens[0] != "BETWEEN") return false;
            if (tokens[2] != "AND") return false;
            for (int i = 0; i < tokens.Length; i++) { if (tokens[i].Equals("TODAY")) tokens[i] = DateTime.Now.ToString("dd.MM.yyyy"); }
            if (!DateTime.TryParseExact(tokens[1], "dd.MM.yyyy", null, DateTimeStyles.None, out DateTime MinValue)) return false;
            if (!DateTime.TryParseExact(tokens[3], "dd.MM.yyyy", null, DateTimeStyles.None, out DateTime MaxValue)) return false;
            CurrentDate = CurrentDate with { MinValue = DateOnly.FromDateTime(MinValue), MaxValue = DateOnly.FromDateTime(MaxValue) };
            return true;
        }

        bool ParseDateDefault(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Date) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "DEFAULT") return false;
            for (int i = 0; i < tokens.Length; i++) { if (tokens[i].Equals("TODAY")) tokens[i] = DateTime.Now.ToString("dd.MM.yyyy"); }
            if (!DateTime.TryParseExact(tokens[1], "dd.MM.yyyy", null, DateTimeStyles.None, out DateTime DefaultValue)) return false;
            CurrentDate = CurrentDate with { DefaultValue = DateOnly.FromDateTime(DefaultValue) };
            return true;
        }

        bool ParseDateGroup(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Date) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "GROUP") return false;
            CurrentDate = CurrentDate with { Group = tokens[1] };
            return true;
        }
        #endregion

        #region Boolean Parsing
        bool ParseBoolean(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 5) return false;
            if (tokens[0] != "DEFINE") return false;
            if (tokens[1] != "BOOLEAN") return false;
            if (tokens[3] != "AS") return false;
            CurrentlyParsing = Parsing.Boolean;
            CurrentBoolean = new DslInputBoolean(tokens[2], tokens[4], string.Empty, false);
            return true;
        }

        bool ParseBooleanDefault(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Boolean) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "DEFAULT") return false;
            if (!bool.TryParse(tokens[1], out bool BooleanValue)) return false;
            CurrentBoolean = CurrentBoolean with { DefaultValue = BooleanValue };
            return true;
        }

        bool ParseBooleanGroup(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Boolean) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "GROUP") return false;
            CurrentBoolean = CurrentBoolean with { Group = tokens[1] };
            return true;
        }
        #endregion

        #region Amount Parsing
        bool ParseAmount(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 5) return false;
            if (tokens[0] != "DEFINE") return false;
            if (tokens[1] != "AMOUNT") return false;
            if (tokens[3] != "AS") return false;
            CurrentlyParsing = Parsing.Amount;
            CurrentAmount = new DslInputAmount(tokens[2], tokens[4], string.Empty, string.Empty, 0m);
            return true;
        }

        bool ParseAmountCurrency(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Amount) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "CURRENCY") return false;
            var currency = tokens[1].ToUpperInvariant();
            if (!Types.Currency.IsValid(currency))
            {
                throw new ArgumentException($"Invalid ISO 4217 currency code: '{tokens[1]}'");
            }
            CurrentAmount = CurrentAmount with { Currency = currency };
            return true;
        }

        bool ParseAmountDefault(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Amount) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "DEFAULT") return false;
            // Parse currency literal: n<CUR> or just n
            var (value, currency) = ParseCurrencyLiteral(tokens[1]);
            if (currency != null && !string.IsNullOrEmpty(CurrentAmount.Currency) && currency != CurrentAmount.Currency)
            {
                throw new ArgumentException($"Default value currency '{currency}' doesn't match declared currency '{CurrentAmount.Currency}'");
            }
            if (currency != null)
            {
                CurrentAmount = CurrentAmount with { DefaultValue = value, Currency = currency };
            }
            else
            {
                CurrentAmount = CurrentAmount with { DefaultValue = value };
            }
            return true;
        }

        bool ParseAmountGroup(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Amount) return false;
            if (tokens.Length != 2) return false;
            if (tokens[0] != "GROUP") return false;
            CurrentAmount = CurrentAmount with { Group = tokens[1] };
            return true;
        }

        /// <summary>
        /// Parse a currency literal like 100&lt;EUR&gt; or just 100
        /// </summary>
        private static (decimal Value, string? Currency) ParseCurrencyLiteral(string token)
        {
            // Check for currency annotation: n<CUR>
            var match = System.Text.RegularExpressions.Regex.Match(token, @"^(-?\d+(?:\.\d+)?)<([A-Z]{3})>$");
            if (match.Success)
            {
                var value = decimal.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                var currency = match.Groups[2].Value;
                if (!Types.Currency.IsValid(currency))
                {
                    throw new ArgumentException($"Invalid ISO 4217 currency code: '{currency}'");
                }
                return (value, currency);
            }

            // Plain number
            if (decimal.TryParse(token, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var plainValue))
            {
                return (plainValue, null);
            }

            throw new ArgumentException($"Invalid amount literal: '{token}'");
        }
        #endregion

        #region Parse EndDefine
        bool ParseEndDefine(string[] tokens)
        {
            if (tokens.Length != 1) return false;
            if (tokens[0] != "ENDDEFINE") return false;
            switch (CurrentlyParsing)
            {
                case Parsing.List:
                    IPFInputs.Add(CurrentList);
                    CurrentlyParsing = Parsing.None;
                    return true;
                case Parsing.ListMultiple:
                    IPFInputs.Add(CurrentListMultiple);
                    CurrentlyParsing = Parsing.None;
                    return true;
                case Parsing.Boolean:
                    IPFInputs.Add(CurrentBoolean);
                    CurrentlyParsing = Parsing.None;
                    return true;
                case Parsing.Number:
                    IPFInputs.Add(CurrentNumber);
                    CurrentlyParsing = Parsing.None;
                    return true;
                case Parsing.Date:
                    IPFInputs.Add(CurrentDate);
                    CurrentlyParsing = Parsing.None;
                    return true;
                case Parsing.Amount:
                    if (string.IsNullOrEmpty(CurrentAmount.Currency))
                    {
                        throw new ArgumentException("Amount definition requires a CURRENCY declaration");
                    }
                    IPFInputs.Add(CurrentAmount);
                    CurrentlyParsing = Parsing.None;
                    return true;
            }
            return false;
        }
        #endregion

        #region Fee Parsing
        bool ParseFee(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length < 3) return false;
            if (tokens[0] != "COMPUTE") return false;
            if (tokens[1] != "FEE") return false;

            var feeName = tokens[2];
            string? typeParameter = null;
            string? returnCurrency = null;
            bool isOptional = false;

            // Check for polymorphic fee: COMPUTE FEE Name<C> RETURN C [OPTIONAL]
            var polyMatch = System.Text.RegularExpressions.Regex.Match(feeName, @"^(\w+)<([A-Z])>$");
            if (polyMatch.Success)
            {
                feeName = polyMatch.Groups[1].Value;
                typeParameter = polyMatch.Groups[2].Value;

                // Must have RETURN clause
                if (tokens.Length < 5 || tokens[3] != "RETURN")
                {
                    throw new ArgumentException($"Polymorphic fee '{feeName}<{typeParameter}>' requires RETURN clause");
                }
                returnCurrency = tokens[4];

                // Return currency must be the type parameter or a concrete currency
                if (returnCurrency != typeParameter && !Types.Currency.IsValid(returnCurrency))
                {
                    throw new ArgumentException($"Return type '{returnCurrency}' must be type parameter '{typeParameter}' or valid ISO 4217 currency");
                }

                isOptional = tokens.Length > 5 && tokens[5] == "OPTIONAL";
            }
            else
            {
                // Non-polymorphic fee: COMPUTE FEE Name [OPTIONAL]
                if (tokens.Length == 4 && tokens[3] == "OPTIONAL")
                {
                    isOptional = true;
                }
                else if (tokens.Length != 3)
                {
                    return false;
                }
            }

            CurrentlyParsing = Parsing.Fee;
            CurrentFee = new DslFee(feeName, isOptional, new List<DslItem>(), new List<DslFeeVar>(), typeParameter, returnCurrency);
            return true;
        }

        bool ParseFeeCase(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Fee) return false;
            if (tokens[0] != "CASE") return false;
            if (tokens[tokens.Length - 1] != "AS") return false;
            CurrentlyParsing = Parsing.FeeCase;
            var ConditionTokens = tokens.Skip(1).Take(tokens.Length - 2);
            CurrentFeeCase = new DslFeeCase(ConditionTokens, new List<DslFeeYield>());
            return true;
        }

        bool ParseFeeYield(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.FeeCase && CurrentlyParsing != Parsing.Fee) return false;
            if (tokens[0] != "YIELD") return false;
            // The yield value is comprised of all tokens until "IF" (will be evaluated)
            var ValueTokens = tokens.AsEnumerable().Skip(1).TakeWhile(w => !w.Equals("IF"));
            // The condition is comprised of all tokens until "IF" or is TRUE if no IF token is found
            if (tokens.Any(a => a.Equals("IF")))
            {
                var ConditionTokens = tokens.AsEnumerable().Reverse().TakeWhile(w => !w.Equals("IF")).Reverse();
                var Yield = new DslFeeYield(ConditionTokens, ValueTokens);
                CurrentFeeCase.Yields.Add(Yield);
            }
            else
            {
                var Yield = new DslFeeYield(Enumerable.Empty<string>(), ValueTokens);
                CurrentFeeCase.Yields.Add(Yield);
            }
            return true;
        }

        bool ParseFeeLet(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Fee) return false;
            if (tokens[0] != "LET") return false;
            if (tokens[2] != "AS") return false;
            var VarName = new StringBuilder().AppendFormat($"{CurrentFee.Name}.{tokens[1]}").ToString();
            var ValueTokens = tokens.AsEnumerable().Skip(3);
            CurrentFee.Vars.Add(new DslFeeVar(VarName, ValueTokens));
            return true;
        }

        bool ParseFeeEndCase(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.FeeCase) return false;
            if (tokens.Length != 1) return false;
            if (tokens[0] != "ENDCASE") return false;
            CurrentlyParsing = Parsing.Fee;
            CurrentFee.Cases.Add(CurrentFeeCase);
            CurrentFeeCase = new DslFeeCase(Enumerable.Empty<string>(), new List<DslFeeYield>());
            return true;
        }

        bool ParseEndCompute(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.Fee) return false;
            if (tokens.Length != 1) return false;
            if (tokens[0] != "ENDCOMPUTE") return false;
            // Dump remaining yields (if any)
            if (CurrentFeeCase.Yields.Count > 0)
            {
                CurrentFee.Cases.Add(CurrentFeeCase);
            }
            IPFFees.Add(CurrentFee);
            CurrentFeeCase = new DslFeeCase(Enumerable.Empty<string>(), new List<DslFeeYield>());
            CurrentlyParsing = Parsing.None;
            return true;
        }
        #endregion

        #region Return parsing
        bool ParseReturn(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 4) return false;
            if (tokens[0] != "RETURN") return false;
            if (tokens[2] != "AS") return false;
            var item = new DslReturn(tokens[1], tokens[3]);
            IPFReturns.Add(item);
            return true;
        }
        #endregion
        #region Reset
        public void Reset()
        {
            IPFReturns.Clear();
            IPFInputs.Clear();
            IPFFees.Clear();
            IPFVerifications.Clear();
            IPFErrors.Clear();
            IPFVersion = null;
        }
        #endregion

        #region Version Parsing
        /// <summary>
        /// Parse: VERSION 'versionId' EFFECTIVE yyyy-MM-dd [DESCRIPTION 'description'] [REFERENCE 'reference']
        /// Note: Date is tokenized as separate yyyy, -, MM, -, dd tokens due to hyphen being a single-char token
        /// </summary>
        bool ParseVersion(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length < 8) return false; // VERSION versionId EFFECTIVE yyyy - MM - dd (minimum 8 tokens)
            if (tokens[0] != "VERSION") return false;
            if (tokens[2] != "EFFECTIVE") return false;
            
            // Date is split as: yyyy - MM - dd (5 tokens)
            if (tokens.Length < 8 || tokens[4] != "-" || tokens[6] != "-") return false;

            var versionId = tokens[1];
            
            // Reconstruct date from tokens[3], tokens[5], tokens[7]
            var dateString = $"{tokens[3]}-{tokens[5]}-{tokens[7]}";
            if (!DateOnly.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var effectiveDate))
            {
                throw new ArgumentException($"Invalid effective date format '{dateString}'. Expected yyyy-MM-dd");
            }

            string? description = null;
            string? reference = null;

            // Parse optional DESCRIPTION and REFERENCE (starting at index 8)
            for (int i = 8; i < tokens.Length; i++)
            {
                if (tokens[i] == "DESCRIPTION" && i + 1 < tokens.Length)
                {
                    description = tokens[i + 1];
                    i++;
                }
                else if (tokens[i] == "REFERENCE" && i + 1 < tokens.Length)
                {
                    reference = tokens[i + 1];
                    i++;
                }
                else
                {
                    return false; // Unknown token
                }
            }

            IPFVersion = new DslVersion(versionId, effectiveDate, description, reference);
            return true;
        }
        #endregion

        #region Verification Parsing
        /// <summary>
        /// Parse: VERIFY COMPLETE FEE FeeName
        /// </summary>
        bool ParseVerifyComplete(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length != 4) return false;
            if (tokens[0] != "VERIFY") return false;
            if (tokens[1] != "COMPLETE") return false;
            if (tokens[2] != "FEE") return false;
            IPFVerifications.Add(new DslVerifyComplete(tokens[3]));
            return true;
        }

        /// <summary>
        /// Parse: VERIFY MONOTONIC FEE FeeName WITH RESPECT TO InputName [DIRECTION direction]
        /// </summary>
        bool ParseVerifyMonotonic(string[] tokens)
        {
            if (CurrentlyParsing != Parsing.None) return false;
            if (tokens.Length < 8) return false;
            if (tokens[0] != "VERIFY") return false;
            if (tokens[1] != "MONOTONIC") return false;
            if (tokens[2] != "FEE") return false;
            if (tokens[4] != "WITH") return false;
            if (tokens[5] != "RESPECT") return false;
            if (tokens[6] != "TO") return false;

            var feeName = tokens[3];
            var withRespectTo = tokens[7];
            var direction = "NonDecreasing"; // default

            // Optional DIRECTION clause
            if (tokens.Length >= 10 && tokens[8] == "DIRECTION")
            {
                direction = tokens[9];
                // Validate direction
                var validDirections = new[] { "NonDecreasing", "NonIncreasing", "StrictlyIncreasing", "StrictlyDecreasing" };
                if (!validDirections.Contains(direction))
                {
                    throw new ArgumentException($"Invalid monotonicity direction '{direction}'. Valid values: {string.Join(", ", validDirections)}");
                }
            }
            else if (tokens.Length != 8)
            {
                return false;
            }

            IPFVerifications.Add(new DslVerifyMonotonic(feeName, withRespectTo, direction));
            return true;
        }
        #endregion
    }

    internal enum Parsing
    {
        None, List, ListMultiple, Number, Date, Boolean, Amount, Fee, FeeCase
    }
}
