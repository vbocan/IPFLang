namespace IPFLang.Evaluator
{
    public abstract record IPFValue(string Name);
    public record IPFValueString(string Name, string Value) : IPFValue(Name)
    {
        public override string ToString()
        {
            return string.Format("[string: {0} = {1}]", Name, Value);
        }
    }
    public record IPFValueStringList(string Name, IEnumerable<string> Value) : IPFValue(Name)
    {
        public override string ToString()
        {
            return string.Format("[string list: {0} = {1}]", Name, string.Join('|', Value));
        }
    }
    public record IPFValueNumber(string Name, decimal Value) : IPFValue(Name)
    {
        public override string ToString()
        {
            return string.Format("[number: {0} = {1}]", Name, Value);
        }
    };
    public record IPFValueAmount(string Name, decimal Value, string Currency) : IPFValue(Name)
    {
        public override string ToString()
        {
            return string.Format("[amount: {0} = {1}<{2}>]", Name, Value, Currency);
        }
    };
    public record IPFValueDate(string Name, DateOnly Value) : IPFValue(Name)
    {
        public override string ToString()
        {
            return string.Format("[date: {0} = {1}]", Name, Value.ToString("dd.MM.yyyy"));
        }
    };
    public record IPFValueBoolean(string Name, bool Value) : IPFValue(Name)
    {
        public override string ToString()
        {
            return string.Format("[boolean: {0} = {1}]", Name, Value);
        }
    };
}
