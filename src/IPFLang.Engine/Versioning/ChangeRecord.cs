namespace IPFLang.Versioning
{
    /// <summary>
    /// Represents the type of change between two versions
    /// </summary>
    public enum ChangeType
    {
        Added,
        Removed,
        Modified,
        Unchanged
    }

    /// <summary>
    /// Represents a single change between two versions
    /// </summary>
    public abstract record Change(string Name, ChangeType Type);

    /// <summary>
    /// Fee-related change
    /// </summary>
    public record FeeChange(
        string FeeName,
        ChangeType Type,
        string? OldDefinition = null,
        string? NewDefinition = null,
        bool IsBreaking = false
    ) : Change(FeeName, Type)
    {
        public override string ToString()
        {
            var breaking = IsBreaking ? " [BREAKING]" : "";
            return Type switch
            {
                ChangeType.Added => $"+ Fee '{FeeName}' added{breaking}",
                ChangeType.Removed => $"- Fee '{FeeName}' removed{breaking}",
                ChangeType.Modified => $"* Fee '{FeeName}' modified{breaking}",
                ChangeType.Unchanged => $"  Fee '{FeeName}' unchanged",
                _ => $"? Fee '{FeeName}' unknown change"
            };
        }
    }

    /// <summary>
    /// Input-related change
    /// </summary>
    public record InputChange(
        string InputName,
        ChangeType Type,
        string? OldDefinition = null,
        string? NewDefinition = null,
        bool IsBreaking = false
    ) : Change(InputName, Type)
    {
        public override string ToString()
        {
            var breaking = IsBreaking ? " [BREAKING]" : "";
            return Type switch
            {
                ChangeType.Added => $"+ Input '{InputName}' added{breaking}",
                ChangeType.Removed => $"- Input '{InputName}' removed{breaking}",
                ChangeType.Modified => $"* Input '{InputName}' modified{breaking}",
                ChangeType.Unchanged => $"  Input '{InputName}' unchanged",
                _ => $"? Input '{InputName}' unknown change"
            };
        }
    }

    /// <summary>
    /// Group-related change
    /// </summary>
    public record GroupChange(
        string GroupName,
        ChangeType Type,
        string? OldDefinition = null,
        string? NewDefinition = null
    ) : Change(GroupName, Type)
    {
        public override string ToString()
        {
            return Type switch
            {
                ChangeType.Added => $"+ Group '{GroupName}' added",
                ChangeType.Removed => $"- Group '{GroupName}' removed",
                ChangeType.Modified => $"* Group '{GroupName}' modified",
                ChangeType.Unchanged => $"  Group '{GroupName}' unchanged",
                _ => $"? Group '{GroupName}' unknown change"
            };
        }
    }

    /// <summary>
    /// Complete change report between two versions
    /// </summary>
    public class ChangeReport
    {
        public Version FromVersion { get; init; }
        public Version ToVersion { get; init; }
        public List<FeeChange> FeeChanges { get; } = new();
        public List<InputChange> InputChanges { get; } = new();
        public List<GroupChange> GroupChanges { get; } = new();

        public ChangeReport(Version fromVersion, Version toVersion)
        {
            FromVersion = fromVersion;
            ToVersion = toVersion;
        }

        /// <summary>
        /// All changes across all categories
        /// </summary>
        public IEnumerable<Change> AllChanges =>
            FeeChanges.Cast<Change>()
                .Concat(InputChanges)
                .Concat(GroupChanges);

        /// <summary>
        /// Only breaking changes
        /// </summary>
        public IEnumerable<Change> BreakingChanges =>
            FeeChanges.Where(c => c.IsBreaking).Cast<Change>()
                .Concat(InputChanges.Where(c => c.IsBreaking));

        /// <summary>
        /// Count of changes by type
        /// </summary>
        public int AddedCount => AllChanges.Count(c => c.Type == ChangeType.Added);
        public int RemovedCount => AllChanges.Count(c => c.Type == ChangeType.Removed);
        public int ModifiedCount => AllChanges.Count(c => c.Type == ChangeType.Modified);
        public int UnchangedCount => AllChanges.Count(c => c.Type == ChangeType.Unchanged);
        public int BreakingCount => BreakingChanges.Count();

        /// <summary>
        /// Has any changes
        /// </summary>
        public bool HasChanges => AddedCount > 0 || RemovedCount > 0 || ModifiedCount > 0;

        public override string ToString()
        {
            var summary = $"Changes from {FromVersion.Id} to {ToVersion.Id}:\n";
            summary += $"  Added: {AddedCount}, Removed: {RemovedCount}, Modified: {ModifiedCount}, Unchanged: {UnchangedCount}\n";
            
            if (BreakingCount > 0)
            {
                summary += $"  ⚠️  Breaking changes: {BreakingCount}\n";
            }

            if (HasChanges)
            {
                summary += "\nChanges:\n";
                foreach (var change in AllChanges.Where(c => c.Type != ChangeType.Unchanged))
                {
                    summary += $"  {change}\n";
                }
            }
            else
            {
                summary += "\nNo changes detected.\n";
            }

            return summary;
        }
    }
}
