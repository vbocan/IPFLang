namespace IPFLang.Parser
{
    public interface IDslParser
    {
        IEnumerable<(DslError, string)> GetErrors();
        IEnumerable<DslFee> GetFees();
        IEnumerable<DslInput> GetInputs();
        IEnumerable<DslReturn> GetReturns();
        IEnumerable<DslGroup> GetGroups();
        IEnumerable<DslVerify> GetVerifications();
        DslVersion? GetVersion();
        bool Parse(string source);
        void Reset();
    }
}
