namespace IPFLang.Versioning
{
    /// <summary>
    /// Resolves which version of a fee schedule applies for a given date or query
    /// </summary>
    public class VersionResolver
    {
        private readonly VersionedScript _versionedScript;

        public VersionResolver(VersionedScript versionedScript)
        {
            _versionedScript = versionedScript ?? throw new ArgumentNullException(nameof(versionedScript));
        }

        /// <summary>
        /// Resolve the version that applies on a specific date
        /// </summary>
        public (Version Version, ParsedScript Script)? ResolveByDate(DateOnly date)
        {
            return _versionedScript.GetVersionAtDate(date);
        }

        /// <summary>
        /// Resolve a version by its ID
        /// </summary>
        public (Version Version, ParsedScript Script)? ResolveById(string versionId)
        {
            return _versionedScript.GetVersion(versionId);
        }

        /// <summary>
        /// Resolve the latest version
        /// </summary>
        public (Version Version, ParsedScript Script)? ResolveLatest()
        {
            return _versionedScript.GetLatestVersion();
        }

        /// <summary>
        /// Get all versions between two dates (inclusive)
        /// </summary>
        public IEnumerable<(Version Version, ParsedScript Script)> ResolveRange(DateOnly startDate, DateOnly endDate)
        {
            if (endDate < startDate)
                throw new ArgumentException("End date must be after start date");

            return _versionedScript.Versions
                .Where(v => v.EffectiveDate >= startDate && v.EffectiveDate <= endDate)
                .Select(v => _versionedScript.GetVersion(v.Id)!.Value);
        }

        /// <summary>
        /// Get the version history in chronological order
        /// </summary>
        public IEnumerable<Version> GetHistory()
        {
            return _versionedScript.Versions;
        }
    }
}
