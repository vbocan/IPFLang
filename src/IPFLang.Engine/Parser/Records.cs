namespace IPFLang.Parser
{
    public abstract record DslInput(string Name, string Text, string Group);
    public record DslInputBoolean(string Name, string Text, string Group, bool DefaultValue) : DslInput(Name, Text, Group);
    public record DslInputList(string Name, string Text, string Group, IList<DslListItem> Items, string DefaultSymbol) : DslInput(Name, Text, Group);
    public record DslInputListMultiple(string Name, string Text, string Group, IList<DslListItem> Items, IList<string> DefaultSymbols) : DslInput(Name, Text, Group);
    public record DslListItem(string Symbol, string Value);
    public record DslInputNumber(string Name, string Text, string Group, int MinValue, int MaxValue, int DefaultValue) : DslInput(Name, Text, Group);
    public record DslInputDate(string Name, string Text, string Group, DateOnly MinValue, DateOnly MaxValue, DateOnly DefaultValue) : DslInput(Name, Text, Group);
    public record DslInputAmount(string Name, string Text, string Group, string Currency, decimal DefaultValue) : DslInput(Name, Text, Group);
    public record DslGroup(string Name, string Text, int Weight);

    public abstract record DslItem(IEnumerable<string> Condition);
    public record DslFee(string Name, bool Optional, IList<DslItem> Cases, IList<DslFeeVar> Vars, string? TypeParameter = null, string? ReturnCurrency = null)
    {
        public bool IsPolymorphic => TypeParameter != null;

        public override string ToString()
        {
            var typeInfo = IsPolymorphic ? $"<{TypeParameter}> RETURN {ReturnCurrency}" : "";
            if (Optional)
            {
                return string.Format("\n\rOPTIONAL FEE: {0}{3}\n\r{2}\n\r{1}\n\r", Name, string.Join(Environment.NewLine, Cases), string.Join(Environment.NewLine, Vars), typeInfo);
            }
            else
            {
                return string.Format("\n\rFEE: {0}{3}\n\r{2}\n\r{1}\n\r", Name, string.Join(Environment.NewLine, Cases), string.Join(Environment.NewLine, Vars), typeInfo);
            }
        }
    }
    public record DslFeeCase(IEnumerable<string> Condition, IList<DslFeeYield> Yields) : DslItem(Condition)
    {
        public override string ToString()
        {
            return string.Format("CASE: {0}\n\r{1}", string.Join(" ", Condition), string.Join(Environment.NewLine, Yields));
        }
    }
    public record DslFeeYield(IEnumerable<string> Condition, IEnumerable<string> Values) : DslItem(Condition)
    {
        public override string ToString()
        {
            return string.Format("YIELD: {0} CONDITION: [{1}]", string.Join(" ", Values), string.Join(" ", Condition));
        }
    }
    public record DslFeeVar(string Name, IEnumerable<string> ValueTokens)
    {
        public override string ToString()
        {
            return string.Format("VAR: {0} AS [{1}]", Name, string.Join(Environment.NewLine, ValueTokens));
        }
    }
    public record DslReturn(string Symbol, string Text)
    {
        public override string ToString()
        {
            return string.Format("RETURN: {0} AS [{1}]", Symbol, Text);
        }
    }

    /// <summary>
    /// Base class for verification directives
    /// </summary>
    public abstract record DslVerify(string FeeName);

    /// <summary>
    /// VERIFY COMPLETE FEE FeeName
    /// </summary>
    public record DslVerifyComplete(string FeeName) : DslVerify(FeeName)
    {
        public override string ToString() => $"VERIFY COMPLETE FEE {FeeName}";
    }

    /// <summary>
    /// VERIFY MONOTONIC FEE FeeName WITH RESPECT TO InputName [DIRECTION direction]
    /// </summary>
    public record DslVerifyMonotonic(string FeeName, string WithRespectTo, string Direction = "NonDecreasing") : DslVerify(FeeName)
    {
        public override string ToString() => $"VERIFY MONOTONIC FEE {FeeName} WITH RESPECT TO {WithRespectTo} DIRECTION {Direction}";
    }

    /// <summary>
    /// VERSION 'versionId' EFFECTIVE yyyy-MM-dd [DESCRIPTION 'description'] [REFERENCE 'reference']
    /// </summary>
    public record DslVersion(string VersionId, DateOnly EffectiveDate, string? Description = null, string? RegulatoryReference = null)
    {
        public override string ToString()
        {
            var desc = !string.IsNullOrWhiteSpace(Description) ? $" DESCRIPTION '{Description}'" : "";
            var refer = !string.IsNullOrWhiteSpace(RegulatoryReference) ? $" REFERENCE '{RegulatoryReference}'" : "";
            return $"VERSION '{VersionId}' EFFECTIVE {EffectiveDate:yyyy-MM-dd}{desc}{refer}";
        }
    }
}
