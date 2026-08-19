using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IPFLang.Corpus
{
    /// <summary>
    /// The production jurisdiction corpus, embedded in this assembly.
    ///
    /// Each jurisdiction is one IPFLang script covering PCT national or regional phase entry
    /// fees. Members of a regional patent system declare the base they extend in their header,
    /// and <see cref="GetCompositionChain"/> returns base and jurisdiction in the order the
    /// composer expects.
    /// </summary>
    public static class JurisdictionCorpus
    {
        private const string Prefix = "Corpus.";
        private const string BasesSegment = "bases.";

        private static readonly Lazy<Index> Resources = new(Build);

        private sealed record Index(
            ImmutableDictionary<string, string> Jurisdictions,
            ImmutableDictionary<string, string> Bases);

        /// <summary>Two-letter codes of every jurisdiction in the corpus, e.g. "RO", "US".</summary>
        public static IReadOnlyList<string> Codes => Resources.Value.Jurisdictions.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        /// <summary>Names of the regional base schedules, e.g. "base_ep".</summary>
        public static IReadOnlyList<string> BaseNames => Resources.Value.Bases.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        /// <summary>Source of one jurisdiction, by two-letter code.</summary>
        /// <exception cref="KeyNotFoundException">The corpus holds no such jurisdiction.</exception>
        public static string GetJurisdiction(string code)
        {
            var key = Normalize(code);
            return Resources.Value.Jurisdictions.TryGetValue(key, out var source)
                ? source
                : throw new KeyNotFoundException($"The corpus contains no jurisdiction '{code}'.");
        }

        /// <summary>Source of one regional base, by name.</summary>
        /// <exception cref="KeyNotFoundException">The corpus holds no such base.</exception>
        public static string GetBase(string name)
        {
            var key = Normalize(name);
            return Resources.Value.Bases.TryGetValue(key, out var source)
                ? source
                : throw new KeyNotFoundException($"The corpus contains no base '{name}'.");
        }

        /// <summary>True when the corpus holds the given jurisdiction.</summary>
        public static bool Contains(string code) => Resources.Value.Jurisdictions.ContainsKey(Normalize(code));

        /// <summary>
        /// The name of the base a jurisdiction extends, or null when it stands alone.
        /// </summary>
        public static string? GetBaseNameFor(string code)
        {
            var match = Regex.Match(GetJurisdiction(code), @"Inherits\s+from:\s*bases/(?<name>[A-Za-z0-9_]+)\.ipf", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var name = Normalize(match.Groups["name"].Value);
            return Resources.Value.Bases.ContainsKey(name) ? name : null;
        }

        /// <summary>
        /// Every source that makes up a jurisdiction, ordered from root to leaf: the regional
        /// base first where one applies, then the jurisdiction itself. Feed these to
        /// <c>JurisdictionRegistry</c> in order, chaining each as the parent of the next.
        /// </summary>
        public static IReadOnlyList<(string Id, string Source)> GetCompositionChain(string code)
        {
            var key = Normalize(code);
            var chain = new List<(string, string)>();

            var baseName = GetBaseNameFor(key);
            if (baseName is not null) chain.Add((baseName, GetBase(baseName)));

            chain.Add((key, GetJurisdiction(key)));
            return chain;
        }

        private static string Normalize(string value) => value.Trim().ToUpperInvariant();

        private static Index Build()
        {
            var assembly = typeof(JurisdictionCorpus).Assembly;
            var jurisdictions = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
            var bases = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

            foreach (var resource in assembly.GetManifestResourceNames())
            {
                var marker = resource.IndexOf(Prefix, StringComparison.Ordinal);
                if (marker < 0 || !resource.EndsWith(".ipf", StringComparison.OrdinalIgnoreCase)) continue;

                var tail = resource[(marker + Prefix.Length)..^".ipf".Length];
                var source = Read(assembly, resource);

                if (tail.StartsWith(BasesSegment, StringComparison.OrdinalIgnoreCase))
                {
                    bases[Normalize(tail[BasesSegment.Length..])] = source;
                }
                else
                {
                    // Files are named PCT-XX; the code is what follows the prefix.
                    var dash = tail.LastIndexOf('-');
                    var code = dash >= 0 ? tail[(dash + 1)..] : tail;
                    jurisdictions[Normalize(code)] = source;
                }
            }

            return new Index(jurisdictions.ToImmutable(), bases.ToImmutable());
        }

        private static string Read(Assembly assembly, string resource)
        {
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Embedded resource '{resource}' could not be opened.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
