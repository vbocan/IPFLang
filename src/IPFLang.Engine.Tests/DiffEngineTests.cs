using IPFLang.Parser;
using IPFLang.Versioning;

namespace IPFLang.Engine.Tests
{
    public class DiffEngineTests
    {
        [Fact]
        public void TestDiffEngine_NoChanges()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script = new ParsedScript(
                new[] { CreateNumberInput("ClaimCount") },
                new[] { CreateBasicFee("FilingFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script, version2, script);

            Assert.False(report.HasChanges);
            Assert.Equal(0, report.AddedCount);
            Assert.Equal(0, report.RemovedCount);
            Assert.Equal(0, report.ModifiedCount);
            Assert.Equal(1, report.FeeChanges.Count(c => c.Type == ChangeType.Unchanged));
        }

        [Fact]
        public void TestDiffEngine_FeeAdded()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee"), CreateBasicFee("ExaminationFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            Assert.True(report.HasChanges);
            Assert.Equal(1, report.AddedCount);
            Assert.Single(report.FeeChanges.Where(c => c.Type == ChangeType.Added));
            Assert.Equal("ExaminationFee", report.FeeChanges.First(c => c.Type == ChangeType.Added).FeeName);
        }

        [Fact]
        public void TestDiffEngine_FeeRemoved()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee"), CreateBasicFee("ExaminationFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            Assert.True(report.HasChanges);
            Assert.Equal(1, report.RemovedCount);
            var removed = report.FeeChanges.Single(c => c.Type == ChangeType.Removed);
            Assert.Equal("ExaminationFee", removed.FeeName);
            Assert.True(removed.IsBreaking); // Removing mandatory fee is breaking
        }

        [Fact]
        public void TestDiffEngine_OptionalFeeRemoved_NotBreaking()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee"), CreateOptionalFee("OptionalFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            var removed = report.FeeChanges.Single(c => c.Type == ChangeType.Removed);
            Assert.False(removed.IsBreaking); // Removing optional fee is not breaking
        }

        [Fact]
        public void TestDiffEngine_FeeModified()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var fee1 = CreateBasicFee("FilingFee");
            var fee2 = CreateBasicFee("FilingFee");
            // Add a different case to fee2 to make it different
            var fee2Modified = new DslFee("FilingFee", false, 
                new List<DslItem> { new DslFeeCase(new[] { "TRUE" }, new List<DslFeeYield> { new DslFeeYield(new[] { "TRUE" }, new[] { "200" }) }) },
                new List<DslFeeVar>());

            var script1 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { fee1 },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { fee2Modified },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            Assert.True(report.HasChanges);
            Assert.Equal(1, report.ModifiedCount);
            var modified = report.FeeChanges.Single(c => c.Type == ChangeType.Modified);
            Assert.Equal("FilingFee", modified.FeeName);
            Assert.True(modified.IsBreaking); // Modifying mandatory fee is breaking
        }

        [Fact]
        public void TestDiffEngine_InputAdded()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                new[] { CreateNumberInput("ClaimCount") },
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                new[] { CreateNumberInput("ClaimCount"), CreateNumberInput("PageCount") },
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            Assert.Equal(1, report.AddedCount);
            var added = report.InputChanges.Single(c => c.Type == ChangeType.Added);
            Assert.Equal("PageCount", added.InputName);
            Assert.True(added.IsBreaking); // Adding required input is breaking
        }

        [Fact]
        public void TestDiffEngine_InputRemoved()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                new[] { CreateNumberInput("ClaimCount"), CreateNumberInput("PageCount") },
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                new[] { CreateNumberInput("ClaimCount") },
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            Assert.Equal(1, report.RemovedCount);
            var removed = report.InputChanges.Single(c => c.Type == ChangeType.Removed);
            Assert.Equal("PageCount", removed.InputName);
            Assert.True(removed.IsBreaking);
        }

        [Fact]
        public void TestDiffEngine_InputRangeNarrowed_Breaking()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                new[] { new DslInputNumber("ClaimCount", "Claims", "", 1, 100, 10) },
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                new[] { new DslInputNumber("ClaimCount", "Claims", "", 1, 50, 10) }, // Max reduced from 100 to 50
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            var modified = report.InputChanges.Single(c => c.Type == ChangeType.Modified);
            Assert.True(modified.IsBreaking); // Narrowing range is breaking
        }

        [Fact]
        public void TestDiffEngine_BreakingChangeCount()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                new[] { CreateNumberInput("ClaimCount") },
                new[] { CreateBasicFee("FilingFee"), CreateBasicFee("ExaminationFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                new[] { CreateNumberInput("ClaimCount"), CreateNumberInput("PageCount") }, // Added input (breaking)
                new[] { CreateBasicFee("FilingFee") }, // Removed fee (breaking)
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            Assert.Equal(2, report.BreakingCount);
        }

        [Fact]
        public void TestChangeReport_ToString()
        {
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("1.1", new DateOnly(2024, 6, 1));

            var script1 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var script2 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { CreateBasicFee("FilingFee"), CreateBasicFee("ExaminationFee") },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            var diff = new DiffEngine();
            var report = diff.Compare(version1, script1, version2, script2);

            var str = report.ToString();
            Assert.Contains("1.0", str);
            Assert.Contains("1.1", str);
            Assert.Contains("ExaminationFee", str);
            Assert.Contains("added", str.ToLower());
        }

        // Helper methods
        private DslInputNumber CreateNumberInput(string name)
        {
            return new DslInputNumber(name, $"{name} input", "", 1, 100, 10);
        }

        private DslFee CreateBasicFee(string name)
        {
            return new DslFee(name, false, 
                new List<DslItem> { new DslFeeCase(Array.Empty<string>(), new List<DslFeeYield> { new DslFeeYield(Array.Empty<string>(), new[] { "100" }) }) },
                new List<DslFeeVar>());
        }

        private DslFee CreateOptionalFee(string name)
        {
            return new DslFee(name, true, 
                new List<DslItem> { new DslFeeCase(Array.Empty<string>(), new List<DslFeeYield> { new DslFeeYield(Array.Empty<string>(), new[] { "50" }) }) },
                new List<DslFeeVar>());
        }
    }
}
