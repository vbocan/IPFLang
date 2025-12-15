namespace IPFLang.Evaluator
{
    public class ReflectionContext : IContext
    {
        public ReflectionContext(object targetObject)
        {
            _targetObject = targetObject;
        }

        object _targetObject;

        public decimal ResolveVariable(string name)
        {
            // Find property
            var pi = _targetObject.GetType().GetProperty(name);
            if (pi == null)
                throw new InvalidDataException($"Unknown variable: '{name}'");

            // Call the property
            return (decimal)pi.GetValue(_targetObject)!;
        }

        public decimal CallFunction(string name, decimal[] arguments)
        {
            // Find method
            var mi = _targetObject.GetType().GetMethod(name);
            if (mi == null)
                throw new InvalidDataException($"Unknown function: '{name}'");

            // Convert decimal array to object array
            var argObjs = arguments.Select(x => (object)x).ToArray();

            // Call the method
            return (decimal)mi.Invoke(_targetObject, argObjs)!;
        }

        public decimal ConvertCurrency(decimal amount, string sourceCurrency, string targetCurrency)
        {
            throw new NotSupportedException("Currency conversion is not supported in ReflectionContext");
        }
    }
}
