namespace IPFLang.Versioning
{
    /// <summary>
    /// Represents a version of a fee schedule with semantic versioning and effective date
    /// </summary>
    public record Version
    {
        /// <summary>
        /// Version identifier (e.g., "2024.1", "1.0.0")
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// Date when this version becomes effective
        /// </summary>
        public DateOnly EffectiveDate { get; init; }

        /// <summary>
        /// Optional description of changes in this version
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Optional reference to regulatory notice (e.g., Federal Register citation)
        /// </summary>
        public string? RegulatoryReference { get; init; }

        public Version(string id, DateOnly effectiveDate, string? description = null, string? regulatoryReference = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Version ID cannot be empty", nameof(id));

            Id = id;
            EffectiveDate = effectiveDate;
            Description = description;
            RegulatoryReference = regulatoryReference;
        }

        public override string ToString()
        {
            var desc = !string.IsNullOrWhiteSpace(Description) ? $" ({Description})" : "";
            return $"Version {Id} effective {EffectiveDate:yyyy-MM-dd}{desc}";
        }
    }
}
