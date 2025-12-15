using System.Text;
using IPFLang.CurrencyConversion;

namespace IPFLang.Evaluator
{
    class EvaluatorContext : IContext
    {
        private readonly IEnumerable<IPFValue> Vars;
        private readonly string FeeName;
        private readonly ICurrencyConverter? CurrencyConverter;

        public EvaluatorContext(IEnumerable<IPFValue> Vars, string FeeName)
            : this(Vars, FeeName, null)
        {
        }

        public EvaluatorContext(IEnumerable<IPFValue> Vars, string FeeName, ICurrencyConverter? currencyConverter)
        {
            this.Vars = Vars;
            this.FeeName = FeeName;
            this.CurrencyConverter = currencyConverter;
        }

        public decimal ResolveVariable(string name)
        {
            var TokenValue = ProcessTokenAsVariableOrProperty(name);
            if (TokenValue is null)
            {
                throw new InvalidDataException($"No variable [{name}] in fee [{FeeName}]'");
            }
            if (TokenValue is IPFValueNumber numVal)
            {
                return numVal.Value;
            }
            if (TokenValue is IPFValueAmount amtVal)
            {
                return amtVal.Value;
            }
            throw new InvalidDataException($"Invalid variable [{name}] in this context.'");
        }

        public decimal CallFunction(string name, decimal[] arguments)
        {
            return name switch
            {
                "ROUND" => Math.Round(arguments[0]),
                "FLOOR" => Math.Floor(arguments[0]),
                "CEIL" => Math.Ceiling(arguments[0]),
                _ => throw new InvalidDataException($"Unknown function: '{name}'"),
            };
        }

        public decimal ConvertCurrency(decimal amount, string sourceCurrency, string targetCurrency)
        {
            if (CurrencyConverter == null)
            {
                throw new InvalidOperationException("Currency conversion is not available. No currency converter configured.");
            }
            return CurrencyConverter.Convert(amount, sourceCurrency, targetCurrency);
        }

        public IPFValue? ProcessTokenAsVariableOrProperty(string name)
        {
            #region Global variable
            var v1 = Vars.Where(w => w.Name.Equals(name)).SingleOrDefault();
            if (v1 is not null)
            {
                return v1;
            }
            #endregion
            #region Fee local variable
            var VarName = new StringBuilder().AppendFormat($"{FeeName}.{name}").ToString();
            var v2 = Vars.Where(w => w.Name.Equals(VarName)).SingleOrDefault();
            if (v2 is not null)
            {
                return v2;
            }
            #endregion
            #region COUNT property of a multiple selection list
            if (name.EndsWith("!COUNT"))
            {
                // Determine the list name
                var ListName = name[..^6];
                // Get the list by its name
                var List = Vars.SingleOrDefault(s => s.Name.Equals(ListName));
                if (List is not null)
                {
                    // Push the length of the list (number of items)
                    return new IPFValueNumber(string.Empty, (List as IPFValueStringList)!.Value.Count());
                }
            }
            #endregion
            #region YEARSTONOW property of a date
            if (name.EndsWith("!YEARSTONOW"))
            {
                // Determine date name
                var DateName = name[..^11];
                // Get the date by its name
                var Date = Vars.SingleOrDefault(s => s.Name.Equals(DateName));
                if (Date is not null)
                {
                    // Push the difference between dates as years
                    TimeSpan difference = DateTime.Now.Subtract((Date as IPFValueDate)!.Value.ToDateTime(TimeOnly.MinValue));
                    return new IPFValueNumber(string.Empty, (decimal)(difference.TotalDays / 365.25));
                }
            }
            #endregion
            #region MONTHSTONOW property of a date
            if (name.EndsWith("!MONTHSTONOW"))
            {
                // Determine date name
                var DateName = name[..^12];
                // Get the date by its name
                var Date = Vars.SingleOrDefault(s => s.Name.Equals(DateName));
                if (Date is not null)
                {
                    // Push the difference between dates as months
                    TimeSpan difference = DateTime.Now.Subtract((Date as IPFValueDate)!.Value.ToDateTime(TimeOnly.MinValue));
                    return new IPFValueNumber(string.Empty, (decimal)(difference.TotalDays / (365.25 / 12)));
                }
            }
            #endregion
            #region DAYSTONOW property of a date
            if (name.EndsWith("!DAYSTONOW"))
            {
                // Determine date name
                var DateName = name[..^10];
                // Get the date by its name
                var Date = Vars.SingleOrDefault(s => s.Name.Equals(DateName));
                if (Date is not null)
                {
                    // Push the difference between dates as days
                    TimeSpan difference = DateTime.Now.Subtract((Date as IPFValueDate)!.Value.ToDateTime(TimeOnly.MinValue));
                    return new IPFValueNumber(string.Empty, (decimal)difference.TotalDays);
                }
            }
            #endregion
            #region MONTHSTONOW property of a date
            if (name.EndsWith("!MONTHSTONOW_FROMLASTDAY"))
            {
                // Determine date name
                var DateName = name[..^24];
                // Get the date by its name
                var Date = Vars.SingleOrDefault(s => s.Name.Equals(DateName));
                if (Date is not null)
                {
                    var givenDate = (Date as IPFValueDate)!.Value;
                    int daysInMonth = DateTime.DaysInMonth(givenDate.Year, givenDate.Month);
                    DateOnly endOfMonth = new DateOnly(givenDate.Year, givenDate.Month, daysInMonth);
                    // Push the difference between dates as months
                    TimeSpan difference = DateTime.Now.Subtract(endOfMonth.ToDateTime(TimeOnly.MinValue));
                    return new IPFValueNumber(string.Empty, (decimal)(difference.TotalDays / (365.25 / 12)));
                }
            }
            #endregion
            return null;
        }
    }
}
