namespace IPFLang.Types
{
    /// <summary>
    /// Type environment (Γ) for static type checking.
    /// Maps variable names to their types and tracks type variables for polymorphic fees.
    /// </summary>
    public class TypeEnvironment
    {
        private readonly Dictionary<string, IPFType> _bindings;
        private readonly HashSet<string> _typeVariables;
        private readonly TypeEnvironment? _parent;

        public TypeEnvironment()
        {
            _bindings = new Dictionary<string, IPFType>(StringComparer.OrdinalIgnoreCase);
            _typeVariables = new HashSet<string>();
            _parent = null;
        }

        private TypeEnvironment(TypeEnvironment parent)
        {
            _bindings = new Dictionary<string, IPFType>(StringComparer.OrdinalIgnoreCase);
            _typeVariables = new HashSet<string>(parent._typeVariables);
            _parent = parent;
        }

        /// <summary>
        /// Bind a variable name to a type in this environment
        /// </summary>
        public void Bind(string name, IPFType type)
        {
            _bindings[name] = type;
        }

        /// <summary>
        /// Look up the type of a variable in this environment (or parent scopes)
        /// </summary>
        public IPFType? Lookup(string name)
        {
            if (_bindings.TryGetValue(name, out var type))
            {
                return type;
            }
            return _parent?.Lookup(name);
        }

        /// <summary>
        /// Check if a name is bound in this environment (or parent scopes)
        /// </summary>
        public bool IsBound(string name)
        {
            return _bindings.ContainsKey(name) || (_parent?.IsBound(name) ?? false);
        }

        /// <summary>
        /// Create a new child environment with an additional type variable.
        /// Used for polymorphic fee checking.
        /// </summary>
        public TypeEnvironment WithTypeVariable(string name)
        {
            var child = new TypeEnvironment(this);
            child._typeVariables.Add(name);
            return child;
        }

        /// <summary>
        /// Create a new child environment (for scoped bindings like LET variables)
        /// </summary>
        public TypeEnvironment NewScope()
        {
            return new TypeEnvironment(this);
        }

        /// <summary>
        /// Check if a name is a type variable in scope
        /// </summary>
        public bool IsTypeVariable(string name)
        {
            return _typeVariables.Contains(name) || (_parent?.IsTypeVariable(name) ?? false);
        }

        /// <summary>
        /// Get all bound variable names in this environment (not including parent scopes)
        /// </summary>
        public IEnumerable<string> GetLocalBindings()
        {
            return _bindings.Keys;
        }

        /// <summary>
        /// Get all type variables in scope
        /// </summary>
        public IEnumerable<string> GetTypeVariables()
        {
            var result = new HashSet<string>(_typeVariables);
            if (_parent != null)
            {
                foreach (var tv in _parent.GetTypeVariables())
                {
                    result.Add(tv);
                }
            }
            return result;
        }
    }
}
