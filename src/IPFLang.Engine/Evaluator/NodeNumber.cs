namespace IPFLang.Evaluator
{
    // NodeNumber represents a literal number in the expression
    class NodeNumber : Node
    {
        public NodeNumber(decimal number)
        {
            _number = number;
        }

        decimal _number;             // The number

        public override decimal Eval(IContext ctx)
        {
            // Just return it.  Too easy.
            return _number;
        }
    }
}
