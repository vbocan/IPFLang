using IPFLang.Parser;
using IPFLang.Versioning;

namespace IPFLang.Composition
{
    /// <summary>
    /// Represents a jurisdiction with fee schedule definitions
    /// </summary>
    public class Jurisdiction
    {
        public string Id { get; }
        public string Name { get; }
        public string? ParentJurisdictionId { get; }
        public ParsedScript Script { get; }
        public Dictionary<string, string> Metadata { get; }

        public Jurisdiction(
            string id,
            string name,
            ParsedScript script,
            string? parentJurisdictionId = null,
            Dictionary<string, string>? metadata = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Script = script ?? throw new ArgumentNullException(nameof(script));
            ParentJurisdictionId = parentJurisdictionId;
            Metadata = metadata ?? new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Container for managing multiple jurisdictions with inheritance
    /// </summary>
    public class JurisdictionRegistry
    {
        private readonly Dictionary<string, Jurisdiction> _jurisdictions = new();

        /// <summary>
        /// Register a jurisdiction
        /// </summary>
        public void Register(Jurisdiction jurisdiction)
        {
            if (jurisdiction == null)
                throw new ArgumentNullException(nameof(jurisdiction));

            if (_jurisdictions.ContainsKey(jurisdiction.Id))
                throw new InvalidOperationException($"Jurisdiction '{jurisdiction.Id}' is already registered");

            // Validate parent exists if specified
            if (jurisdiction.ParentJurisdictionId != null)
            {
                if (!_jurisdictions.ContainsKey(jurisdiction.ParentJurisdictionId))
                    throw new InvalidOperationException($"Parent jurisdiction '{jurisdiction.ParentJurisdictionId}' not found");
            }

            _jurisdictions[jurisdiction.Id] = jurisdiction;
        }

        /// <summary>
        /// Get a jurisdiction by ID
        /// </summary>
        public Jurisdiction? GetJurisdiction(string id)
        {
            return _jurisdictions.TryGetValue(id, out var jurisdiction) ? jurisdiction : null;
        }

        /// <summary>
        /// Get all registered jurisdictions
        /// </summary>
        public IEnumerable<Jurisdiction> GetAllJurisdictions()
        {
            return _jurisdictions.Values;
        }

        /// <summary>
        /// Get inheritance chain for a jurisdiction (from root to current)
        /// </summary>
        public List<Jurisdiction> GetInheritanceChain(string jurisdictionId)
        {
            var chain = new List<Jurisdiction>();
            var current = GetJurisdiction(jurisdictionId);

            while (current != null)
            {
                chain.Insert(0, current); // Insert at beginning to build root-to-leaf order
                current = current.ParentJurisdictionId != null
                    ? GetJurisdiction(current.ParentJurisdictionId)
                    : null;
            }

            return chain;
        }

        /// <summary>
        /// Get all child jurisdictions of a parent
        /// </summary>
        public IEnumerable<Jurisdiction> GetChildren(string parentId)
        {
            return _jurisdictions.Values.Where(j => j.ParentJurisdictionId == parentId);
        }

        /// <summary>
        /// Check if a jurisdiction has children
        /// </summary>
        public bool HasChildren(string jurisdictionId)
        {
            return _jurisdictions.Values.Any(j => j.ParentJurisdictionId == jurisdictionId);
        }

        /// <summary>
        /// Get root jurisdictions (those without parents)
        /// </summary>
        public IEnumerable<Jurisdiction> GetRootJurisdictions()
        {
            return _jurisdictions.Values.Where(j => j.ParentJurisdictionId == null);
        }

        /// <summary>
        /// Count total registered jurisdictions
        /// </summary>
        public int Count => _jurisdictions.Count;

        /// <summary>
        /// Clear all jurisdictions
        /// </summary>
        public void Clear()
        {
            _jurisdictions.Clear();
        }
    }

    /// <summary>
    /// Composes fee schedules by applying inheritance and override rules
    /// </summary>
    public class JurisdictionComposer
    {
        private readonly JurisdictionRegistry _registry;

        public JurisdictionComposer(JurisdictionRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Compose a complete fee schedule for a jurisdiction by applying inheritance
        /// </summary>
        public ComposedScript Compose(string jurisdictionId)
        {
            var chain = _registry.GetInheritanceChain(jurisdictionId);
            
            if (chain.Count == 0)
                throw new InvalidOperationException($"Jurisdiction '{jurisdictionId}' not found");

            // Start with empty collections
            var composedInputs = new Dictionary<string, DslInput>();
            var composedFees = new Dictionary<string, DslFee>();
            var composedReturns = new Dictionary<string, DslReturn>();
            var composedGroups = new Dictionary<string, DslGroup>();
            var composedVerifies = new List<DslVerify>();

            var appliedJurisdictions = new List<string>();

            // Apply each jurisdiction in the chain (root to leaf)
            foreach (var jurisdiction in chain)
            {
                appliedJurisdictions.Add(jurisdiction.Id);

                // Merge inputs (child overrides parent)
                foreach (var input in jurisdiction.Script.Inputs)
                {
                    composedInputs[input.Name] = input;
                }

                // Merge fees (child overrides parent)
                foreach (var fee in jurisdiction.Script.Fees)
                {
                    composedFees[fee.Name] = fee;
                }

                // Merge returns (child overrides parent)
                foreach (var returnDef in jurisdiction.Script.Returns)
                {
                    composedReturns[returnDef.Symbol] = returnDef;
                }

                // Merge groups (child overrides parent)
                foreach (var group in jurisdiction.Script.Groups)
                {
                    composedGroups[group.Name] = group;
                }

                // Accumulate verifies (all apply)
                composedVerifies.AddRange(jurisdiction.Script.Verifications);
            }

            // Create composed script
            var composedScript = new ParsedScript(
                composedInputs.Values,
                composedFees.Values,
                composedReturns.Values,
                composedGroups.Values,
                composedVerifies
            );

            return new ComposedScript(
                jurisdictionId,
                composedScript,
                appliedJurisdictions,
                chain.Count - 1 // Number of inherited jurisdictions
            );
        }

        /// <summary>
        /// Analyze what a jurisdiction inherits vs. what it overrides
        /// </summary>
        public InheritanceAnalysis AnalyzeInheritance(string jurisdictionId)
        {
            var chain = _registry.GetInheritanceChain(jurisdictionId);
            
            if (chain.Count == 0)
                throw new InvalidOperationException($"Jurisdiction '{jurisdictionId}' not found");

            var analysis = new InheritanceAnalysis(jurisdictionId);

            if (chain.Count == 1)
            {
                // No inheritance, everything is defined here
                var jurisdiction = chain[0];
                foreach (var input in jurisdiction.Script.Inputs)
                    analysis.DefinedInputs.Add(input.Name);
                foreach (var fee in jurisdiction.Script.Fees)
                    analysis.DefinedFees.Add(fee.Name);
                
                return analysis;
            }

            // Build set of what's defined in parents
            var parentInputs = new HashSet<string>();
            var parentFees = new HashSet<string>();

            for (int i = 0; i < chain.Count - 1; i++)
            {
                foreach (var input in chain[i].Script.Inputs)
                    parentInputs.Add(input.Name);
                foreach (var fee in chain[i].Script.Fees)
                    parentFees.Add(fee.Name);
            }

            // Analyze current jurisdiction
            var current = chain[^1];
            
            foreach (var input in current.Script.Inputs)
            {
                if (parentInputs.Contains(input.Name))
                    analysis.OverriddenInputs.Add(input.Name);
                else
                    analysis.DefinedInputs.Add(input.Name);
            }

            foreach (var fee in current.Script.Fees)
            {
                if (parentFees.Contains(fee.Name))
                    analysis.OverriddenFees.Add(fee.Name);
                else
                    analysis.DefinedFees.Add(fee.Name);
            }

            // What's inherited (in parent but not in current)
            foreach (var inputName in parentInputs)
            {
                if (!current.Script.Inputs.Any(i => i.Name == inputName))
                    analysis.InheritedInputs.Add(inputName);
            }

            foreach (var feeName in parentFees)
            {
                if (!current.Script.Fees.Any(f => f.Name == feeName))
                    analysis.InheritedFees.Add(feeName);
            }

            return analysis;
        }

        /// <summary>
        /// Calculate code reuse metrics across all jurisdictions
        /// </summary>
        public CompositionMetrics CalculateMetrics()
        {
            var metrics = new CompositionMetrics();
            
            foreach (var jurisdiction in _registry.GetAllJurisdictions())
            {
                metrics.TotalJurisdictions++;
                
                if (jurisdiction.ParentJurisdictionId != null)
                {
                    metrics.JurisdictionsWithInheritance++;
                    
                    var analysis = AnalyzeInheritance(jurisdiction.Id);
                    metrics.TotalInheritedFees += analysis.InheritedFees.Count;
                    metrics.TotalOverriddenFees += analysis.OverriddenFees.Count;
                    metrics.TotalDefinedFees += analysis.DefinedFees.Count;
                }
                else
                {
                    metrics.TotalDefinedFees += jurisdiction.Script.Fees.Count();
                }
            }

            return metrics;
        }
    }

    /// <summary>
    /// Result of composing a jurisdiction with inheritance
    /// </summary>
    public record ComposedScript(
        string JurisdictionId,
        ParsedScript Script,
        List<string> AppliedJurisdictions,
        int InheritanceLevels
    );

    /// <summary>
    /// Analysis of what a jurisdiction inherits vs. defines
    /// </summary>
    public class InheritanceAnalysis
    {
        public string JurisdictionId { get; }
        public HashSet<string> InheritedInputs { get; } = new();
        public HashSet<string> InheritedFees { get; } = new();
        public HashSet<string> OverriddenInputs { get; } = new();
        public HashSet<string> OverriddenFees { get; } = new();
        public HashSet<string> DefinedInputs { get; } = new();
        public HashSet<string> DefinedFees { get; } = new();

        public InheritanceAnalysis(string jurisdictionId)
        {
            JurisdictionId = jurisdictionId;
        }

        public int TotalInherited => InheritedInputs.Count + InheritedFees.Count;
        public int TotalOverridden => OverriddenInputs.Count + OverriddenFees.Count;
        public int TotalDefined => DefinedInputs.Count + DefinedFees.Count;
        public int Total => TotalInherited + TotalOverridden + TotalDefined;

        public double ReusePercentage => Total > 0 ? (TotalInherited * 100.0 / Total) : 0.0;
    }

    /// <summary>
    /// Metrics about code reuse through composition
    /// </summary>
    public class CompositionMetrics
    {
        public int TotalJurisdictions { get; set; }
        public int JurisdictionsWithInheritance { get; set; }
        public int TotalInheritedFees { get; set; }
        public int TotalOverriddenFees { get; set; }
        public int TotalDefinedFees { get; set; }

        public double InheritancePercentage => 
            TotalJurisdictions > 0 ? (JurisdictionsWithInheritance * 100.0 / TotalJurisdictions) : 0.0;

        public double ReusePercentage =>
            (TotalInheritedFees + TotalOverriddenFees + TotalDefinedFees) > 0
                ? (TotalInheritedFees * 100.0 / (TotalInheritedFees + TotalOverriddenFees + TotalDefinedFees))
                : 0.0;
    }
}
