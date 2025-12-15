namespace IPFLang.Evaluator
{
    // NodeBinary for binary operations such as Add, Subtract etc...
    class NodeBinary : Node
    {
        // Constructor accepts the two nodes to be operated on and function
        // that performs the actual operation
        public NodeBinary(Node lhs, Node rhs, Func<decimal, decimal, decimal> op)
        {
            _lhs = lhs;
            _rhs = rhs;
            _op = op;
        }

        Node _lhs;                              // Left hand side of the operation
        Node _rhs;                              // Right hand side of the operation
        Func<decimal, decimal, decimal> _op;   // The callback operator

        public override decimal Eval(IContext ctx)
        {
            // Evaluate both sides
            var lhsVal = _lhs.Eval(ctx);
            var rhsVal = _rhs.Eval(ctx);

            // Evaluate and return
            var result = _op(lhsVal, rhsVal);
            return result;
        }
    }
}
