using IPFLang.Composition;
using IPFLang.Parser;
using IPFLang.Versioning;

namespace IPFLang.Engine.Tests
{
    public class JurisdictionCompositionTests
    {
        [Fact]
        public void TestJurisdiction_Creation()
        {
            var script = CreateSimpleScript();
            var jurisdiction = new Jurisdiction("US", "United States", script);

            Assert.Equal("US", jurisdiction.Id);
            Assert.Equal("United States", jurisdiction.Name);
            Assert.Null(jurisdiction.ParentJurisdictionId);
            Assert.NotNull(jurisdiction.Script);
        }

        [Fact]
        public void TestJurisdiction_WithParent()
        {
            var script = CreateSimpleScript();
            var jurisdiction = new Jurisdiction("US-CA", "California", script, parentJurisdictionId: "US");

            Assert.Equal("US", jurisdiction.ParentJurisdictionId);
        }

        [Fact]
        public void TestJurisdictionRegistry_Register()
        {
            var registry = new JurisdictionRegistry();
            var jurisdiction = new Jurisdiction("USPTO", "US Patent Office", CreateSimpleScript());

            registry.Register(jurisdiction);

            Assert.Equal(1, registry.Count);
            Assert.NotNull(registry.GetJurisdiction("USPTO"));
        }

        [Fact]
        public void TestJurisdictionRegistry_DuplicateThrows()
        {
            var registry = new JurisdictionRegistry();
            var jurisdiction = new Jurisdiction("USPTO", "US Patent Office", CreateSimpleScript());

            registry.Register(jurisdiction);

            Assert.Throws<InvalidOperationException>(() => registry.Register(jurisdiction));
        }

        [Fact]
        public void TestJurisdictionRegistry_ParentMustExist()
        {
            var registry = new JurisdictionRegistry();
            var child = new Jurisdiction("EPO-DE", "Germany", CreateSimpleScript(), parentJurisdictionId: "EPO");

            Assert.Throws<InvalidOperationException>(() => registry.Register(child));
        }

        [Fact]
        public void TestJurisdictionRegistry_GetInheritanceChain()
        {
            var registry = CreateThreeLevelHierarchy();
            var chain = registry.GetInheritanceChain("EPO-DE-BY");

            Assert.Equal(3, chain.Count);
            Assert.Equal("EPO", chain[0].Id);
            Assert.Equal("EPO-DE", chain[1].Id);
            Assert.Equal("EPO-DE-BY", chain[2].Id);
        }

        [Fact]
        public void TestJurisdictionRegistry_GetChildren()
        {
            var registry = CreateThreeLevelHierarchy();
            var children = registry.GetChildren("EPO").ToList();

            Assert.Equal(2, children.Count);
            Assert.Contains(children, j => j.Id == "EPO-DE");
            Assert.Contains(children, j => j.Id == "EPO-FR");
        }

        [Fact]
        public void TestJurisdictionRegistry_GetRootJurisdictions()
        {
            var registry = CreateThreeLevelHierarchy();
            var roots = registry.GetRootJurisdictions().ToList();

            Assert.Single(roots);
            Assert.Equal("EPO", roots[0].Id);
        }

        [Fact]
        public void TestJurisdictionComposer_SimpleComposition()
        {
            var registry = new JurisdictionRegistry();
            var parent = new Jurisdiction("EPO", "European Patent Office", CreateScriptWithFees("FilingFee"));
            registry.Register(parent);

            var composer = new JurisdictionComposer(registry);
            var composed = composer.Compose("EPO");

            Assert.Equal("EPO", composed.JurisdictionId);
            Assert.Single(composed.AppliedJurisdictions);
            Assert.Equal(0, composed.InheritanceLevels);
            Assert.Single(composed.Script.Fees);
        }

        [Fact]
        public void TestJurisdictionComposer_InheritFromParent()
        {
            var registry = new JurisdictionRegistry();
            
            var parent = new Jurisdiction("EPO", "European Patent Office", 
                CreateScriptWithFees("FilingFee", "SearchFee"));
            registry.Register(parent);

            var child = new Jurisdiction("EPO-DE", "Germany", 
                CreateScriptWithFees("ExaminationFee"), 
                parentJurisdictionId: "EPO");
            registry.Register(child);

            var composer = new JurisdictionComposer(registry);
            var composed = composer.Compose("EPO-DE");

            Assert.Equal(2, composed.AppliedJurisdictions.Count);
            Assert.Equal(1, composed.InheritanceLevels);
            Assert.Equal(3, composed.Script.Fees.Count()); // FilingFee + SearchFee + ExaminationFee
        }

        [Fact]
        public void TestJurisdictionComposer_ChildOverridesParent()
        {
            var registry = new JurisdictionRegistry();
            
            var parentFees = new List<DslFee>
            {
                CreateFee("FilingFee", 100m),
                CreateFee("SearchFee", 500m)
            };
            var parent = new Jurisdiction("EPO", "European Patent Office", 
                new ParsedScript(Enumerable.Empty<DslInput>(), parentFees, 
                    Enumerable.Empty<DslReturn>(), Enumerable.Empty<DslGroup>(), Enumerable.Empty<DslVerify>()));
            registry.Register(parent);

            var childFees = new List<DslFee>
            {
                CreateFee("FilingFee", 120m) // Override with higher amount
            };
            var child = new Jurisdiction("EPO-DE", "Germany", 
                new ParsedScript(Enumerable.Empty<DslInput>(), childFees,
                    Enumerable.Empty<DslReturn>(), Enumerable.Empty<DslGroup>(), Enumerable.Empty<DslVerify>()),
                parentJurisdictionId: "EPO");
            registry.Register(child);

            var composer = new JurisdictionComposer(registry);
            var composed = composer.Compose("EPO-DE");

            // Should have 2 fees total: overridden FilingFee + inherited SearchFee
            Assert.Equal(2, composed.Script.Fees.Count());
            
            var filingFee = composed.Script.Fees.First(f => f.Name == "FilingFee");
            Assert.NotNull(filingFee);
            // The child's version (120) should win
        }

        [Fact]
        public void TestJurisdictionComposer_ThreeLevelInheritance()
        {
            var registry = CreateThreeLevelHierarchy();
            var composer = new JurisdictionComposer(registry);
            var composed = composer.Compose("EPO-DE-BY");

            Assert.Equal(3, composed.AppliedJurisdictions.Count);
            Assert.Equal(2, composed.InheritanceLevels);
            Assert.Contains("EPO", composed.AppliedJurisdictions);
            Assert.Contains("EPO-DE", composed.AppliedJurisdictions);
            Assert.Contains("EPO-DE-BY", composed.AppliedJurisdictions);
        }

        [Fact]
        public void TestInheritanceAnalysis_NoInheritance()
        {
            var registry = new JurisdictionRegistry();
            var jurisdiction = new Jurisdiction("USPTO", "US Patent Office", 
                CreateScriptWithFees("FilingFee", "SearchFee"));
            registry.Register(jurisdiction);

            var composer = new JurisdictionComposer(registry);
            var analysis = composer.AnalyzeInheritance("USPTO");

            Assert.Equal(0, analysis.TotalInherited);
            Assert.Equal(2, analysis.TotalDefined);
            Assert.Equal(0, analysis.TotalOverridden);
        }

        [Fact]
        public void TestInheritanceAnalysis_WithInheritance()
        {
            var registry = new JurisdictionRegistry();
            
            var parent = new Jurisdiction("EPO", "European Patent Office", 
                CreateScriptWithFees("FilingFee", "SearchFee"));
            registry.Register(parent);

            var child = new Jurisdiction("EPO-DE", "Germany", 
                CreateScriptWithFees("ExaminationFee"), 
                parentJurisdictionId: "EPO");
            registry.Register(child);

            var composer = new JurisdictionComposer(registry);
            var analysis = composer.AnalyzeInheritance("EPO-DE");

            Assert.Equal(2, analysis.InheritedFees.Count); // FilingFee, SearchFee
            Assert.Equal(1, analysis.DefinedFees.Count);   // ExaminationFee
            Assert.Equal(0, analysis.OverriddenFees.Count);
            Assert.True(analysis.ReusePercentage > 50);
        }

        [Fact]
        public void TestInheritanceAnalysis_WithOverride()
        {
            var registry = new JurisdictionRegistry();
            
            var parent = new Jurisdiction("EPO", "European Patent Office", 
                CreateScriptWithFees("FilingFee", "SearchFee"));
            registry.Register(parent);

            var child = new Jurisdiction("EPO-DE", "Germany", 
                CreateScriptWithFees("FilingFee", "ExaminationFee"), // Override FilingFee
                parentJurisdictionId: "EPO");
            registry.Register(child);

            var composer = new JurisdictionComposer(registry);
            var analysis = composer.AnalyzeInheritance("EPO-DE");

            Assert.Equal(1, analysis.InheritedFees.Count);  // SearchFee
            Assert.Equal(1, analysis.OverriddenFees.Count); // FilingFee
            Assert.Equal(1, analysis.DefinedFees.Count);    // ExaminationFee
        }

        [Fact]
        public void TestCompositionMetrics_NoInheritance()
        {
            var registry = new JurisdictionRegistry();
            registry.Register(new Jurisdiction("USPTO", "USPTO", CreateScriptWithFees("Fee1", "Fee2")));
            registry.Register(new Jurisdiction("EPO", "EPO", CreateScriptWithFees("Fee3")));

            var composer = new JurisdictionComposer(registry);
            var metrics = composer.CalculateMetrics();

            Assert.Equal(2, metrics.TotalJurisdictions);
            Assert.Equal(0, metrics.JurisdictionsWithInheritance);
            Assert.Equal(0.0, metrics.InheritancePercentage);
        }

        [Fact]
        public void TestCompositionMetrics_WithInheritance()
        {
            var registry = CreateThreeLevelHierarchy();
            var composer = new JurisdictionComposer(registry);
            var metrics = composer.CalculateMetrics();

            Assert.Equal(5, metrics.TotalJurisdictions); // EPO, EPO-DE, EPO-FR, EPO-DE-BY, EPO-FR-AL
            Assert.Equal(4, metrics.JurisdictionsWithInheritance); // All except EPO
            Assert.Equal(80.0, metrics.InheritancePercentage); // 4/5 * 100
            Assert.True(metrics.TotalInheritedFees > 0);
        }

        [Fact]
        public void TestCompositionMetrics_CodeReuse()
        {
            var registry = new JurisdictionRegistry();
            
            // Parent with 3 fees
            var parent = new Jurisdiction("EPO", "EPO", 
                CreateScriptWithFees("FilingFee", "SearchFee", "ExaminationFee"));
            registry.Register(parent);

            // Child 1 inherits all 3, adds 1 new
            var child1 = new Jurisdiction("EPO-DE", "Germany", 
                CreateScriptWithFees("GrantFee"),
                parentJurisdictionId: "EPO");
            registry.Register(child1);

            // Child 2 inherits all 3, overrides 1, adds 1 new
            var child2 = new Jurisdiction("EPO-FR", "France", 
                CreateScriptWithFees("FilingFee", "TranslationFee"),
                parentJurisdictionId: "EPO");
            registry.Register(child2);

            var composer = new JurisdictionComposer(registry);
            var metrics = composer.CalculateMetrics();

            // Should show significant code reuse
            Assert.True(metrics.ReusePercentage > 40); // Reasonable code reuse
        }

        [Fact]
        public void TestRealWorldScenario_EPONationalPhases()
        {
            // Simulate EPO with national phases (DE, FR, UK, etc.)
            var registry = new JurisdictionRegistry();
            
            // Base EPO fees
            var epo = new Jurisdiction("EPO", "European Patent Office",
                CreateScriptWithFees("FilingFee", "SearchFee", "ExaminationFee", "DesignationFee"));
            registry.Register(epo);

            // Germany: inherits EPO, adds translation fee
            var germany = new Jurisdiction("EPO-DE", "Germany (EPO)", 
                CreateScriptWithFees("TranslationFee"),
                parentJurisdictionId: "EPO");
            registry.Register(germany);

            // France: inherits EPO, adds translation fee
            var france = new Jurisdiction("EPO-FR", "France (EPO)", 
                CreateScriptWithFees("TranslationFee"),
                parentJurisdictionId: "EPO");
            registry.Register(france);

            var composer = new JurisdictionComposer(registry);
            
            // Compose Germany
            var deComposed = composer.Compose("EPO-DE");
            Assert.Equal(5, deComposed.Script.Fees.Count()); // 4 EPO + 1 DE

            // Compose France
            var frComposed = composer.Compose("EPO-FR");
            Assert.Equal(5, frComposed.Script.Fees.Count()); // 4 EPO + 1 FR

            // Analyze code reuse
            var metrics = composer.CalculateMetrics();
            Assert.True(metrics.ReusePercentage > 40); // Significant reuse (4 inherited / 9 total)
        }

        // Helper methods
        private ParsedScript CreateSimpleScript()
        {
            return new ParsedScript(
                Enumerable.Empty<DslInput>(),
                Enumerable.Empty<DslFee>(),
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );
        }

        private ParsedScript CreateScriptWithFees(params string[] feeNames)
        {
            var fees = feeNames.Select(name => CreateFee(name, 100m)).ToList();
            return new ParsedScript(
                Enumerable.Empty<DslInput>(),
                fees,
                Enumerable.Empty<DslReturn>(),
                Enumerable.Empty<DslGroup>(),
                Enumerable.Empty<DslVerify>()
            );
        }

        private DslFee CreateFee(string name, decimal amount)
        {
            return new DslFee(name, false,
                new List<DslItem> {
                    new DslFeeCase(Array.Empty<string>(),
                        new List<DslFeeYield> {
                            new DslFeeYield(Array.Empty<string>(), new[] { amount.ToString() })
                        })
                },
                new List<DslFeeVar>());
        }

        private JurisdictionRegistry CreateThreeLevelHierarchy()
        {
            var registry = new JurisdictionRegistry();
            
            // Level 1: EPO (root)
            var epo = new Jurisdiction("EPO", "European Patent Office",
                CreateScriptWithFees("FilingFee", "SearchFee"));
            registry.Register(epo);

            // Level 2: National offices
            var germany = new Jurisdiction("EPO-DE", "Germany",
                CreateScriptWithFees("ExaminationFee"),
                parentJurisdictionId: "EPO");
            registry.Register(germany);

            var france = new Jurisdiction("EPO-FR", "France",
                CreateScriptWithFees("TranslationFee"),
                parentJurisdictionId: "EPO");
            registry.Register(france);

            // Level 3: Regional offices
            var bavaria = new Jurisdiction("EPO-DE-BY", "Bavaria",
                CreateScriptWithFees("RegionalFee"),
                parentJurisdictionId: "EPO-DE");
            registry.Register(bavaria);

            var alsace = new Jurisdiction("EPO-FR-AL", "Alsace",
                CreateScriptWithFees("RegionalFee"),
                parentJurisdictionId: "EPO-FR");
            registry.Register(alsace);

            return registry;
        }
    }
}
