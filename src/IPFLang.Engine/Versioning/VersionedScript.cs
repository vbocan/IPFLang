using IPFLang.Parser;

namespace IPFLang.Versioning
{
    /// <summary>
    /// Container for multiple versions of a fee schedule
    /// </summary>
    public class VersionedScript
    {
        private readonly Dictionary<string, (Version Version, ParsedScript Script)> _versions = new();
        private readonly List<Version> _versionHistory = new();

        /// <summary>
        /// All versions in chronological order
        /// </summary>
        public IReadOnlyList<Version> Versions => _versionHistory.AsReadOnly();

        /// <summary>
        /// Add a new version to the script
        /// </summary>
        public void AddVersion(Version version, ParsedScript script)
        {
            if (_versions.ContainsKey(version.Id))
                throw new InvalidOperationException($"Version {version.Id} already exists");

            _versions[version.Id] = (version, script);
            
            // Insert in chronological order
            var index = _versionHistory.FindIndex(v => v.EffectiveDate > version.EffectiveDate);
            if (index == -1)
                _versionHistory.Add(version);
            else
                _versionHistory.Insert(index, version);
        }

        /// <summary>
        /// Get a specific version by ID
        /// </summary>
        public (Version Version, ParsedScript Script)? GetVersion(string versionId)
        {
            return _versions.TryGetValue(versionId, out var result) ? result : null;
        }

        /// <summary>
        /// Get the version effective on a specific date
        /// </summary>
        public (Version Version, ParsedScript Script)? GetVersionAtDate(DateOnly date)
        {
            // Find the latest version whose effective date is <= the query date
            var version = _versionHistory
                .Where(v => v.EffectiveDate <= date)
                .OrderByDescending(v => v.EffectiveDate)
                .FirstOrDefault();

            return version != null ? _versions[version.Id] : null;
        }

        /// <summary>
        /// Get the latest version
        /// </summary>
        public (Version Version, ParsedScript Script)? GetLatestVersion()
        {
            var latest = _versionHistory.LastOrDefault();
            return latest != null ? _versions[latest.Id] : null;
        }

        /// <summary>
        /// Get all version IDs
        /// </summary>
        public IEnumerable<string> GetVersionIds() => _versions.Keys;

        /// <summary>
        /// Check if a version exists
        /// </summary>
        public bool HasVersion(string versionId) => _versions.ContainsKey(versionId);
    }

    /// <summary>
    /// Represents a parsed DSL script with all its components
    /// </summary>
    public record ParsedScript(
        IEnumerable<DslInput> Inputs,
        IEnumerable<DslFee> Fees,
        IEnumerable<DslReturn> Returns,
        IEnumerable<DslGroup> Groups,
        IEnumerable<DslVerify> Verifications
    );
}
