namespace IPFLang.Evaluator
{
    public enum Token
    {
        EOF,
        Add,
        Subtract,
        Multiply,
        Divide,
        OpenParens,
        CloseParens,
        Comma,
        Identifier,
        Number,
        CurrencyLiteral,
    }
}
