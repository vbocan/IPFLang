namespace IPFLang.Parser
{
    public enum DslError
    {
        None,
        SyntaxError,
        VariableDefinedMultipleTimes,
        FeeDefinedMultipleTimes,
        FeeVariableDefinedMultipleTimes,
        InvalidMinMaxDefault,
        VariableNoChoice,
        VariableNoDefaultChoice,
        VariableDuplicateChoices,
        VariableInvalidDefaultChoice,
        ChoiceDefinedInMultipleVariables
    }
}
