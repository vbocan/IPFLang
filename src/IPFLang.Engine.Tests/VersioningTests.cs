using IPFLang.Parser;
using IPFLang.Versioning;

namespace IPFLang.Engine.Tests
{
    public class VersioningTests
    {
        [Fact]
        public void TestVersionParsing_Basic()
        {
            string text =
            """
            VERSION '2024.1' EFFECTIVE 2024-01-15
            
            COMPUTE FEE BasicFee
            YIELD 100
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            if (!result)
            {
                foreach (var error in p.GetErrors())
                {
                    System.Console.WriteLine($"Parse error: {error.Item2}");
                }
            }
            Assert.True(result);
            var version = p.GetVersion();
            Assert.NotNull(version);
            Assert.Equal("2024.1", version.VersionId);
            Assert.Equal(new DateOnly(2024, 1, 15), version.EffectiveDate);
            Assert.Null(version.Description);
            Assert.Null(version.RegulatoryReference);
        }

        [Fact]
        public void TestVersionParsing_WithDescription()
        {
            string text =
            """
            VERSION '2024.1' EFFECTIVE 2024-01-15 DESCRIPTION 'USPTO fee increase'
            
            COMPUTE FEE BasicFee
            YIELD 100
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var version = p.GetVersion();
            Assert.NotNull(version);
            Assert.Equal("2024.1", version.VersionId);
            Assert.Equal(new DateOnly(2024, 1, 15), version.EffectiveDate);
            Assert.Equal("USPTO fee increase", version.Description);
            Assert.Null(version.RegulatoryReference);
        }

        [Fact]
        public void TestVersionParsing_WithReference()
        {
            string text =
            """
            VERSION '2024.1' EFFECTIVE 2024-01-15 REFERENCE 'Federal Register Vol. 89, No. 123'
            
            COMPUTE FEE BasicFee
            YIELD 100
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var version = p.GetVersion();
            Assert.NotNull(version);
            Assert.Equal("2024.1", version.VersionId);
            Assert.Equal(new DateOnly(2024, 1, 15), version.EffectiveDate);
            Assert.Null(version.Description);
            Assert.Equal("Federal Register Vol. 89, No. 123", version.RegulatoryReference);
        }

        [Fact]
        public void TestVersionParsing_WithBothDescriptionAndReference()
        {
            string text =
            """
            VERSION '2024.1' EFFECTIVE 2024-01-15 DESCRIPTION 'USPTO fee increase' REFERENCE 'Federal Register Vol. 89, No. 123'
            
            COMPUTE FEE BasicFee
            YIELD 100
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var version = p.GetVersion();
            Assert.NotNull(version);
            Assert.Equal("2024.1", version.VersionId);
            Assert.Equal(new DateOnly(2024, 1, 15), version.EffectiveDate);
            Assert.Equal("USPTO fee increase", version.Description);
            Assert.Equal("Federal Register Vol. 89, No. 123", version.RegulatoryReference);
        }

        [Fact]
        public void TestVersionedScript_AddVersion()
        {
            var versionedScript = new VersionedScript();
            var version1 = new IPFLang.Versioning.Version("2023.1", new DateOnly(2023, 1, 1));
            var script1 = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            versionedScript.AddVersion(version1, script1);

            Assert.Single(versionedScript.Versions);
            Assert.True(versionedScript.HasVersion("2023.1"));
        }

        [Fact]
        public void TestVersionedScript_MultipleVersionsInChronologicalOrder()
        {
            var versionedScript = new VersionedScript();
            
            var version1 = new IPFLang.Versioning.Version("2023.1", new DateOnly(2023, 1, 1));
            var version2 = new IPFLang.Versioning.Version("2024.1", new DateOnly(2024, 1, 15));
            var version3 = new IPFLang.Versioning.Version("2023.5", new DateOnly(2023, 6, 1));
            
            var emptyScript = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            versionedScript.AddVersion(version1, emptyScript);
            versionedScript.AddVersion(version2, emptyScript);
            versionedScript.AddVersion(version3, emptyScript);

            Assert.Equal(3, versionedScript.Versions.Count);
            
            // Should be in chronological order
            Assert.Equal("2023.1", versionedScript.Versions[0].Id);
            Assert.Equal("2023.5", versionedScript.Versions[1].Id);
            Assert.Equal("2024.1", versionedScript.Versions[2].Id);
        }

        [Fact]
        public void TestVersionedScript_GetVersionAtDate()
        {
            var versionedScript = new VersionedScript();
            
            var version1 = new IPFLang.Versioning.Version("2023.1", new DateOnly(2023, 1, 1));
            var version2 = new IPFLang.Versioning.Version("2024.1", new DateOnly(2024, 1, 15));
            
            var emptyScript = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            versionedScript.AddVersion(version1, emptyScript);
            versionedScript.AddVersion(version2, emptyScript);

            // Query before first version
            var result1 = versionedScript.GetVersionAtDate(new DateOnly(2022, 12, 31));
            Assert.Null(result1);

            // Query on first version's effective date
            var result2 = versionedScript.GetVersionAtDate(new DateOnly(2023, 1, 1));
            Assert.NotNull(result2);
            Assert.Equal("2023.1", result2.Value.Version.Id);

            // Query between versions
            var result3 = versionedScript.GetVersionAtDate(new DateOnly(2023, 6, 1));
            Assert.NotNull(result3);
            Assert.Equal("2023.1", result3.Value.Version.Id);

            // Query on second version's effective date
            var result4 = versionedScript.GetVersionAtDate(new DateOnly(2024, 1, 15));
            Assert.NotNull(result4);
            Assert.Equal("2024.1", result4.Value.Version.Id);

            // Query after second version
            var result5 = versionedScript.GetVersionAtDate(new DateOnly(2024, 6, 1));
            Assert.NotNull(result5);
            Assert.Equal("2024.1", result5.Value.Version.Id);
        }

        [Fact]
        public void TestVersionedScript_GetLatestVersion()
        {
            var versionedScript = new VersionedScript();
            
            var version1 = new IPFLang.Versioning.Version("2023.1", new DateOnly(2023, 1, 1));
            var version2 = new IPFLang.Versioning.Version("2024.1", new DateOnly(2024, 1, 15));
            
            var emptyScript = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            versionedScript.AddVersion(version1, emptyScript);
            versionedScript.AddVersion(version2, emptyScript);

            var latest = versionedScript.GetLatestVersion();
            Assert.NotNull(latest);
            Assert.Equal("2024.1", latest.Value.Version.Id);
        }

        [Fact]
        public void TestVersionResolver_ResolveByDate()
        {
            var versionedScript = new VersionedScript();
            var resolver = new VersionResolver(versionedScript);
            
            var version1 = new IPFLang.Versioning.Version("2023.1", new DateOnly(2023, 1, 1));
            var version2 = new IPFLang.Versioning.Version("2024.1", new DateOnly(2024, 1, 15));
            
            var emptyScript = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            versionedScript.AddVersion(version1, emptyScript);
            versionedScript.AddVersion(version2, emptyScript);

            var result = resolver.ResolveByDate(new DateOnly(2023, 6, 1));
            Assert.NotNull(result);
            Assert.Equal("2023.1", result.Value.Version.Id);
        }

        [Fact]
        public void TestVersionResolver_ResolveById()
        {
            var versionedScript = new VersionedScript();
            var resolver = new VersionResolver(versionedScript);
            
            var version1 = new IPFLang.Versioning.Version("2023.1", new DateOnly(2023, 1, 1));
            
            var emptyScript = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            versionedScript.AddVersion(version1, emptyScript);

            var result = resolver.ResolveById("2023.1");
            Assert.NotNull(result);
            Assert.Equal("2023.1", result.Value.Version.Id);

            var noResult = resolver.ResolveById("9999.9");
            Assert.Null(noResult);
        }

        [Fact]
        public void TestVersionResolver_ResolveRange()
        {
            var versionedScript = new VersionedScript();
            var resolver = new VersionResolver(versionedScript);
            
            var version1 = new IPFLang.Versioning.Version("2023.1", new DateOnly(2023, 1, 1));
            var version2 = new IPFLang.Versioning.Version("2023.5", new DateOnly(2023, 6, 1));
            var version3 = new IPFLang.Versioning.Version("2024.1", new DateOnly(2024, 1, 15));
            
            var emptyScript = new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );

            versionedScript.AddVersion(version1, emptyScript);
            versionedScript.AddVersion(version2, emptyScript);
            versionedScript.AddVersion(version3, emptyScript);

            var results = resolver.ResolveRange(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31)).ToList();
            Assert.Equal(2, results.Count);
            Assert.Equal("2023.1", results[0].Version.Id);
            Assert.Equal("2023.5", results[1].Version.Id);
        }
    }
}
