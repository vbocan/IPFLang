using IPFLang.Parser;
using IPFLang.Versioning;

namespace IPFLang.Validation
{
    /// <summary>
    /// Framework for validating versioned fee schedules against real-world regulatory changes
    /// </summary>
    public class RealWorldValidator
    {
        private readonly VersionedScript _versionedScript;
        private readonly DiffEngine _diffEngine;
        private readonly ImpactAnalyzer _impactAnalyzer;

        public RealWorldValidator(VersionedScript versionedScript)
        {
            _versionedScript = versionedScript ?? throw new ArgumentNullException(nameof(versionedScript));
            _diffEngine = new DiffEngine();
            _impactAnalyzer = new ImpactAnalyzer();
        }

        /// <summary>
        /// Generate a comprehensive validation report for all versions
        /// </summary>
        public ValidationReport GenerateReport()
        {
            var report = new ValidationReport(_versionedScript.Versions.ToList());

            // Analyze each version transition
            var versions = _versionedScript.Versions.OrderBy(v => v.EffectiveDate).ToList();
            for (int i = 0; i < versions.Count - 1; i++)
            {
                var fromVersion = versions[i];
                var toVersion = versions[i + 1];

                var fromScript = _versionedScript.GetVersion(fromVersion.Id)?.Script;
                var toScript = _versionedScript.GetVersion(toVersion.Id)?.Script;

                if (fromScript != null && toScript != null)
                {
                    var changeReport = _diffEngine.Compare(fromVersion, fromScript, toVersion, toScript);
                    var impact = _impactAnalyzer.AnalyzeImpact(changeReport, fromScript, toScript);

                    report.Transitions.Add(new VersionTransition(
                        fromVersion,
                        toVersion,
                        changeReport,
                        impact
                    ));
                }
            }

            return report;
        }

        /// <summary>
        /// Validate that all versions are well-formed
        /// </summary>
        public IEnumerable<ValidationIssue> ValidateVersions()
        {
            var issues = new List<ValidationIssue>();

            foreach (var version in _versionedScript.Versions)
            {
                var scriptInfo = _versionedScript.GetVersion(version.Id);
                if (scriptInfo == null)
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error,
                        version.Id,
                        "Version not found in script collection"
                    ));
                    continue;
                }

                var script = scriptInfo.Value.Script;

                // Check for empty fee schedules
                if (!script.Fees.Any())
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Warning,
                        version.Id,
                        "Version has no fees defined"
                    ));
                }

                // Check for missing inputs
                if (!script.Inputs.Any() && script.Fees.Any())
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Info,
                        version.Id,
                        "Version has fees but no inputs (may be constant fees)"
                    ));
                }
            }

            return issues;
        }

        /// <summary>
        /// Check chronological ordering of versions
        /// </summary>
        public IEnumerable<ValidationIssue> ValidateChronology()
        {
            var issues = new List<ValidationIssue>();
            var versions = _versionedScript.Versions.ToList();

            for (int i = 0; i < versions.Count - 1; i++)
            {
                if (versions[i].EffectiveDate >= versions[i + 1].EffectiveDate)
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error,
                        versions[i].Id,
                        $"Version {versions[i].Id} effective date ({versions[i].EffectiveDate}) is not before {versions[i + 1].Id} ({versions[i + 1].EffectiveDate})"
                    ));
                }
            }

            return issues;
        }

        /// <summary>
        /// Validate against expected changes from regulatory announcements
        /// </summary>
        public IEnumerable<ValidationIssue> ValidateExpectedChanges(
            string fromVersionId,
            string toVersionId,
            IEnumerable<ExpectedChange> expectedChanges)
        {
            var issues = new List<ValidationIssue>();

            var fromVersion = _versionedScript.Versions.FirstOrDefault(v => v.Id == fromVersionId);
            var toVersion = _versionedScript.Versions.FirstOrDefault(v => v.Id == toVersionId);

            if (fromVersion == null || toVersion == null)
            {
                issues.Add(new ValidationIssue(
                    IssueSeverity.Error,
                    fromVersionId,
                    "Version not found"
                ));
                return issues;
            }

            var fromScript = _versionedScript.GetVersion(fromVersionId)?.Script;
            var toScript = _versionedScript.GetVersion(toVersionId)?.Script;

            if (fromScript == null || toScript == null)
            {
                issues.Add(new ValidationIssue(
                    IssueSeverity.Error,
                    fromVersionId,
                    "Script not found"
                ));
                return issues;
            }

            var changeReport = _diffEngine.Compare(fromVersion, fromScript, toVersion, toScript);

            // Validate each expected change
            foreach (var expected in expectedChanges)
            {
                var found = false;

                switch (expected.ChangeType)
                {
                    case ExpectedChangeType.FeeAdded:
                        found = changeReport.FeeChanges.Any(c => 
                            c.Type == ChangeType.Added && c.FeeName == expected.ItemName);
                        break;

                    case ExpectedChangeType.FeeRemoved:
                        found = changeReport.FeeChanges.Any(c => 
                            c.Type == ChangeType.Removed && c.FeeName == expected.ItemName);
                        break;

                    case ExpectedChangeType.FeeModified:
                        found = changeReport.FeeChanges.Any(c => 
                            c.Type == ChangeType.Modified && c.FeeName == expected.ItemName);
                        break;

                    case ExpectedChangeType.InputAdded:
                        found = changeReport.InputChanges.Any(c => 
                            c.Type == ChangeType.Added && c.InputName == expected.ItemName);
                        break;
                }

                if (!found)
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Warning,
                        toVersionId,
                        $"Expected {expected.ChangeType} for '{expected.ItemName}' not found"
                    ));
                }
            }

            return issues;
        }
    }

    /// <summary>
    /// Comprehensive validation report
    /// </summary>
    public class ValidationReport
    {
        public List<IPFLang.Versioning.Version> Versions { get; }
        public List<VersionTransition> Transitions { get; } = new();
        public DateTime GeneratedAt { get; } = DateTime.UtcNow;

        public ValidationReport(List<IPFLang.Versioning.Version> versions)
        {
            Versions = versions;
        }

        public int TotalVersions => Versions.Count;
        public int TotalTransitions => Transitions.Count;
        public int TotalBreakingChanges => Transitions.Sum(t => t.ChangeReport.BreakingCount);
        public int TotalChanges => Transitions.Sum(t => 
            t.ChangeReport.AddedCount + 
            t.ChangeReport.RemovedCount + 
            t.ChangeReport.ModifiedCount);

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Real-World Validation Report ===");
            sb.AppendLine($"Generated: {GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
            sb.AppendLine();
            sb.AppendLine($"Total Versions: {TotalVersions}");
            sb.AppendLine($"Total Transitions: {TotalTransitions}");
            sb.AppendLine($"Total Changes: {TotalChanges}");
            sb.AppendLine($"Breaking Changes: {TotalBreakingChanges}");
            sb.AppendLine();

            foreach (var transition in Transitions)
            {
                sb.AppendLine($"--- {transition.FromVersion.Id} → {transition.ToVersion.Id} ---");
                sb.AppendLine($"Effective: {transition.ToVersion.EffectiveDate:yyyy-MM-dd}");
                if (!string.IsNullOrEmpty(transition.ToVersion.Description))
                    sb.AppendLine($"Description: {transition.ToVersion.Description}");
                if (!string.IsNullOrEmpty(transition.ToVersion.RegulatoryReference))
                    sb.AppendLine($"Reference: {transition.ToVersion.RegulatoryReference}");
                
                sb.AppendLine($"Changes: +{transition.ChangeReport.AddedCount} ~{transition.ChangeReport.ModifiedCount} -{transition.ChangeReport.RemovedCount}");
                
                if (transition.ChangeReport.BreakingCount > 0)
                    sb.AppendLine($"⚠️  Breaking Changes: {transition.ChangeReport.BreakingCount}");
                
                sb.AppendLine($"Impact: {transition.ImpactReport.TotalAffectedScenarios} scenarios");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a transition between two versions
    /// </summary>
    public record VersionTransition(
        IPFLang.Versioning.Version FromVersion,
        IPFLang.Versioning.Version ToVersion,
        ChangeReport ChangeReport,
        ImpactReport ImpactReport
    );

    /// <summary>
    /// Validation issue found during analysis
    /// </summary>
    public record ValidationIssue(
        IssueSeverity Severity,
        string VersionId,
        string Message
    );

    public enum IssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Expected change from regulatory announcement
    /// </summary>
    public record ExpectedChange(
        ExpectedChangeType ChangeType,
        string ItemName,
        string? Description = null
    );

    public enum ExpectedChangeType
    {
        FeeAdded,
        FeeRemoved,
        FeeModified,
        InputAdded,
        InputRemoved,
        InputModified
    }
}
