namespace IPFLang.Evaluator
{
    // Represents a variable (or a constant) in an expression.  eg: "2 * pi"
    public class NodeVariable : Node
    {
        public NodeVariable(string variableName)
        {
            _variableName = variableName;
        }

        string _variableName;

        public string Name => _variableName;

        public override decimal Eval(IContext ctx)
        {
            return ctx.ResolveVariable(_variableName);
        }
    }
}
