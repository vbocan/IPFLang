using IPFLang.Engine;
using IPFLang.Evaluator;
using IPFLang.Parser;
using IPFLang.Validation;
using IPFLang.Versioning;

namespace IPFLang.Engine.Tests
{
    public class RealWorldValidationTests
    {
        [Fact]
        public void TestRealWorldValidator_USPTO_TwoVersions()
        {
            var versionedScript = CreateUSPTOSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var report = validator.GenerateReport();

            Assert.Equal(2, report.TotalVersions);
            Assert.Equal(1, report.TotalTransitions);
            Assert.True(report.TotalChanges > 0);
        }

        [Fact]
        public void TestRealWorldValidator_GenerateReport()
        {
            var versionedScript = CreateSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var report = validator.GenerateReport();

            Assert.NotNull(report);
            Assert.Equal(2, report.TotalVersions);
            Assert.Single(report.Transitions);
            
            var transition = report.Transitions[0];
            Assert.Equal("2023.1", transition.FromVersion.Id);
            Assert.Equal("2024.1", transition.ToVersion.Id);
        }

        [Fact]
        public void TestRealWorldValidator_ValidateVersions()
        {
            var versionedScript = CreateSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var issues = validator.ValidateVersions().ToList();

            // Should have info about inputs
            Assert.Contains(issues, i => i.Severity == IssueSeverity.Info);
        }

        [Fact]
        public void TestRealWorldValidator_ValidateChronology()
        {
            var versionedScript = CreateSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var issues = validator.ValidateChronology().ToList();

            // No chronology errors expected
            Assert.Empty(issues.Where(i => i.Severity == IssueSeverity.Error));
        }

        [Fact]
        public void TestRealWorldValidator_ChronologyValidation()
        {
            var versionedScript = new VersionedScript();
            
            // Add versions in correct order (VersionedScript sorts them automatically)
            var version1 = new IPFLang.Versioning.Version("1.0", new DateOnly(2024, 1, 1));
            var version2 = new IPFLang.Versioning.Version("2.0", new DateOnly(2024, 6, 1));
            
            var script = CreateSimpleFeeScript(100m);
            versionedScript.AddVersion(version1, script);
            versionedScript.AddVersion(version2, script);

            var validator = new RealWorldValidator(versionedScript);
            var issues = validator.ValidateChronology().ToList();

            // VersionedScript automatically maintains chronological order, so no errors expected
            Assert.DoesNotContain(issues, i => i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void TestRealWorldValidator_ValidateExpectedChanges()
        {
            var versionedScript = CreateSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var expectedChanges = new List<ExpectedChange>
            {
                new ExpectedChange(ExpectedChangeType.FeeModified, "BasicFee", "Fee increase")
            };

            var issues = validator.ValidateExpectedChanges("2023.1", "2024.1", expectedChanges).ToList();

            // Should find the expected change
            Assert.Empty(issues.Where(i => i.Severity == IssueSeverity.Warning));
        }

        [Fact]
        public void TestRealWorldValidator_MissingExpectedChange()
        {
            var versionedScript = CreateSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var expectedChanges = new List<ExpectedChange>
            {
                new ExpectedChange(ExpectedChangeType.FeeAdded, "NewFee", "Expected new fee")
            };

            var issues = validator.ValidateExpectedChanges("2023.1", "2024.1", expectedChanges).ToList();

            // Should report missing expected change
            Assert.Contains(issues, i => 
                i.Severity == IssueSeverity.Warning && 
                i.Message.Contains("NewFee"));
        }

        [Fact]
        public void TestValidationReport_ToString()
        {
            var versionedScript = CreateSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var report = validator.GenerateReport();
            var str = report.ToString();

            Assert.Contains("Real-World Validation Report", str);
            Assert.Contains("2023.1", str);
            Assert.Contains("2024.1", str);
            Assert.Contains("Total Versions: 2", str);
        }

        [Fact]
        public void TestValidationReport_Statistics()
        {
            var versionedScript = CreateSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var report = validator.GenerateReport();

            Assert.True(report.TotalVersions >= 2);
            Assert.True(report.TotalTransitions >= 1);
            Assert.True(report.GeneratedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void TestUSPTOSample_FeeIncrease()
        {
            var versionedScript = CreateUSPTOSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var report = validator.GenerateReport();
            var transition = report.Transitions.First();

            // USPTO 2024 had fee increases
            Assert.True(transition.ChangeReport.ModifiedCount > 0);
            Assert.Contains("USPTO", transition.ToVersion.Description ?? "");
        }

        [Fact]
        public void TestEPOSample_Creation()
        {
            // Create EPO-style fee schedule
            var versionedScript = CreateEPOSampleSchedule();
            var validator = new RealWorldValidator(versionedScript);

            var report = validator.GenerateReport();

            Assert.NotNull(report);
            Assert.True(report.TotalVersions >= 2);
        }

        // Helper methods
        private VersionedScript CreateSampleSchedule()
        {
            var versionedScript = new VersionedScript();
            
            var version2023 = new IPFLang.Versioning.Version(
                "2023.1",
                new DateOnly(2023, 1, 1),
                "Initial version",
                "Sample Reference"
            );
            
            var version2024 = new IPFLang.Versioning.Version(
                "2024.1",
                new DateOnly(2024, 1, 15),
                "Fee increase",
                "Sample FR 2024-001"
            );

            var script2023 = CreateSimpleFeeScript(100m);
            var script2024 = CreateSimpleFeeScript(150m);

            versionedScript.AddVersion(version2023, script2023);
            versionedScript.AddVersion(version2024, script2024);

            return versionedScript;
        }

        private VersionedScript CreateUSPTOSampleSchedule()
        {
            var versionedScript = new VersionedScript();
            
            var version2023 = new IPFLang.Versioning.Version(
                "USPTO-2023",
                new DateOnly(2023, 1, 16),
                "USPTO Fee Schedule 2023",
                "Federal Register Vol. 88"
            );
            
            var version2024 = new IPFLang.Versioning.Version(
                "USPTO-2024",
                new DateOnly(2024, 1, 15),
                "USPTO Fee Schedule 2024 - Annual Adjustments",
                "Federal Register Vol. 89, No. 10"
            );

            // Simplified USPTO fees
            var script2023 = CreateUSPTOFeeScript(2023);
            var script2024 = CreateUSPTOFeeScript(2024);

            versionedScript.AddVersion(version2023, script2023);
            versionedScript.AddVersion(version2024, script2024);

            return versionedScript;
        }

        private VersionedScript CreateEPOSampleSchedule()
        {
            var versionedScript = new VersionedScript();
            
            var version2023 = new IPFLang.Versioning.Version(
                "EPO-2023",
                new DateOnly(2023, 4, 1),
                "EPO Fee Schedule 2023",
                "EPO Official Journal 2023/A"
            );
            
            var version2024 = new IPFLang.Versioning.Version(
                "EPO-2024",
                new DateOnly(2024, 4, 1),
                "EPO Fee Schedule 2024",
                "EPO Official Journal 2024/A"
            );

            var script2023 = CreateEPOFeeScript(2023);
            var script2024 = CreateEPOFeeScript(2024);

            versionedScript.AddVersion(version2023, script2023);
            versionedScript.AddVersion(version2024, script2024);

            return versionedScript;
        }

        private ParsedScript CreateSimpleFeeScript(decimal amount)
        {
            var fee = new DslFee("BasicFee", false,
                new List<DslItem> {
                    new DslFeeCase(Array.Empty<string>(),
                        new List<DslFeeYield> {
                            new DslFeeYield(Array.Empty<string>(), new[] { amount.ToString() })
                        })
                },
                new List<DslFeeVar>());

            return new ParsedScript(
                Enumerable.Empty<DslInput>(),
                new[] { fee },
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );
        }

        private ParsedScript CreateUSPTOFeeScript(int year)
        {
            // Simplified USPTO filing fees (representative values)
            var filingFee = year == 2023 ? 320m : 336m;  // ~5% increase
            var searchFee = year == 2023 ? 700m : 735m;
            var examFee = year == 2023 ? 800m : 840m;

            var fees = new List<DslFee>
            {
                new DslFee("FilingFee", false,
                    new List<DslItem> {
                        new DslFeeCase(Array.Empty<string>(),
                            new List<DslFeeYield> {
                                new DslFeeYield(Array.Empty<string>(), new[] { filingFee.ToString() })
                            })
                    },
                    new List<DslFeeVar>()),
                
                new DslFee("SearchFee", false,
                    new List<DslItem> {
                        new DslFeeCase(Array.Empty<string>(),
                            new List<DslFeeYield> {
                                new DslFeeYield(Array.Empty<string>(), new[] { searchFee.ToString() })
                            })
                    },
                    new List<DslFeeVar>()),
                
                new DslFee("ExaminationFee", false,
                    new List<DslItem> {
                        new DslFeeCase(Array.Empty<string>(),
                            new List<DslFeeYield> {
                                new DslFeeYield(Array.Empty<string>(), new[] { examFee.ToString() })
                            })
                    },
                    new List<DslFeeVar>())
            };

            return new ParsedScript(
                Enumerable.Empty<DslInput>(),
                fees,
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );
        }

        private ParsedScript CreateEPOFeeScript(int year)
        {
            // Simplified EPO fees (in EUR)
            var filingFee = year == 2023 ? 120m : 125m;
            var searchFee = year == 2023 ? 1400m : 1460m;
            var examFee = year == 2023 ? 1950m : 2030m;

            var fees = new List<DslFee>
            {
                new DslFee("FilingFee", false,
                    new List<DslItem> {
                        new DslFeeCase(Array.Empty<string>(),
                            new List<DslFeeYield> {
                                new DslFeeYield(Array.Empty<string>(), new[] { filingFee.ToString() })
                            })
                    },
                    new List<DslFeeVar>()),
                
                new DslFee("SearchFee", false,
                    new List<DslItem> {
                        new DslFeeCase(Array.Empty<string>(),
                            new List<DslFeeYield> {
                                new DslFeeYield(Array.Empty<string>(), new[] { searchFee.ToString() })
                            })
                    },
                    new List<DslFeeVar>()),
                
                new DslFee("ExaminationFee", false,
                    new List<DslItem> {
                        new DslFeeCase(Array.Empty<string>(),
                            new List<DslFeeYield> {
                                new DslFeeYield(Array.Empty<string>(), new[] { examFee.ToString() })
                            })
                    },
                    new List<DslFeeVar>())
            };

            return new ParsedScript(
                Enumerable.Empty<DslInput>(),
                fees,
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );
        }
    }
}
