namespace IPFLang.Evaluator
{
    // Node - abstract class representing one node in the expression
    public abstract class Node
    {
        public abstract decimal Eval(IContext ctx);
    }
}
