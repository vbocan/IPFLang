using IPFLang.Composition;
using IPFLang.Corpus;
using IPFLang.Evaluator;
using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    /// <summary>
    /// The corpus ships inside the assembly, so these tests double as a corpus-wide
    /// well-formedness check: every jurisdiction must parse, type-check, compose with its
    /// regional base where it declares one, and execute under its own default inputs.
    /// </summary>
    public class JurisdictionCorpusTests
    {
        public static TheoryData<string> AllCodes()
        {
            var data = new TheoryData<string>();
            foreach (var code in JurisdictionCorpus.Codes) data.Add(code);
            return data;
        }

        [Fact]
        public void CorpusIsEmbeddedAndDiscoverable()
        {
            Assert.Equal(119, JurisdictionCorpus.Codes.Count);
            Assert.Equal(4, JurisdictionCorpus.BaseNames.Count);
            Assert.Contains("RO", JurisdictionCorpus.Codes);
            Assert.Contains("US", JurisdictionCorpus.Codes);
            Assert.Contains("MY", JurisdictionCorpus.Codes);
            Assert.Contains("BASE_EP", JurisdictionCorpus.BaseNames);
        }

        [Fact]
        public void RegionalMembersDeclareTheirBase()
        {
            Assert.Equal("BASE_EP", JurisdictionCorpus.GetBaseNameFor("RO"));
            Assert.Equal("BASE_EA", JurisdictionCorpus.GetBaseNameFor("RU"));
            Assert.Equal("BASE_AP", JurisdictionCorpus.GetBaseNameFor("KE"));

            // The United States belongs to no regional system.
            Assert.Null(JurisdictionCorpus.GetBaseNameFor("US"));
        }

        [Fact]
        public void UnknownJurisdictionIsReportedClearly()
        {
            Assert.False(JurisdictionCorpus.Contains("ZZ"));
            var ex = Assert.Throws<KeyNotFoundException>(() => JurisdictionCorpus.GetJurisdiction("ZZ"));
            Assert.Contains("ZZ", ex.Message);
        }

        [Theory]
        [MemberData(nameof(AllCodes))]
        public void EveryJurisdictionParsesComposesAndExecutes(string code)
        {
            var parser = new DslParser();
            var registry = new JurisdictionRegistry();
            string? parent = null, leaf = null;

            foreach (var (id, source) in JurisdictionCorpus.GetCompositionChain(code))
            {
                var (parsed, errors) = parser.Parse(source, returnParsedScript: true);
                Assert.True(parsed is not null, $"{code}: '{id}' failed to parse: {string.Join(" | ", errors)}");

                registry.Register(new Jurisdiction(id, id, parsed!, parent));
                parent = id;
                leaf = id;
            }

            var composed = new JurisdictionComposer(registry).Compose(leaf!);

            var calculator = new DslCalculator(new DslParser());
            calculator.LoadParsedScript(composed.Script);

            Assert.Empty(calculator.GetTypeErrors());
            Assert.NotEmpty(calculator.GetFees());

            var (mandatory, optional, _, _) = calculator.Compute(DefaultsFor(calculator.GetInputs()));
            Assert.True(mandatory >= 0, $"{code}: mandatory total went negative");
            Assert.True(optional >= 0, $"{code}: optional total went negative");
        }

        /// <summary>
        /// Inheritance has to be load-bearing, not decorative. A member of a regional system must
        /// take the shared parameters from its base rather than carrying its own copy: a member
        /// that redeclares everything composes to exactly what it would produce alone, and the
        /// base becomes a file that looks like a single point of maintenance without being one.
        /// </summary>
        [Theory]
        [InlineData("RO", "BASE_EP")]
        [InlineData("DE", "BASE_EP")]
        [InlineData("RU", "BASE_EA")]
        [InlineData("KE", "BASE_AP")]
        public void AMemberInheritsTheSharedParametersRatherThanRedeclaringThem(string code, string expectedBase)
        {
            Assert.Equal(expectedBase, JurisdictionCorpus.GetBaseNameFor(code));

            var parser = new DslParser();
            var (alone, _) = parser.Parse(JurisdictionCorpus.GetJurisdiction(code), returnParsedScript: true);
            Assert.NotNull(alone);

            var chain = JurisdictionCorpus.GetCompositionChain(code);
            var registry = new JurisdictionRegistry();
            string? parent = null, leaf = null;
            foreach (var (id, source) in chain)
            {
                var (parsed, _) = parser.Parse(source, returnParsedScript: true);
                registry.Register(new Jurisdiction(id, id, parsed!, parent));
                parent = id;
                leaf = id;
            }
            var composed = new JurisdictionComposer(registry).Compose(leaf!);

            // Composing must add something. If it does not, the member is redeclaring the base.
            Assert.True(composed.Script.Inputs.Count() > alone!.Inputs.Count(),
                $"{code} composes to no more than it declares alone, so its base contributes nothing");

            // The shared parameters must arrive from the base, not from the member.
            var declaredLocally = alone.Inputs.Select(i => i.Name).ToHashSet();
            foreach (var shared in new[] { "EntitySize", "ClaimCount", "SheetCount", "ApplicationDate" })
            {
                Assert.DoesNotContain(shared, declaredLocally);
                Assert.Contains(composed.Script.Inputs, i => i.Name == shared);
            }
        }

        /// <summary>
        /// The consequence of the property above: an edit to a base reaches its members.
        /// </summary>
        [Fact]
        public void EditingABaseReachesTheJurisdictionsThatExtendIt()
        {
            var parser = new DslParser();

            // Widen the claim range in the base only.
            var edited = JurisdictionCorpus.GetBase("BASE_EP")
                .Replace("DEFINE NUMBER ClaimCount AS 'Number of claims'", "DEFINE NUMBER ClaimCount AS 'Claims, revised'");

            var (baseScript, _) = parser.Parse(edited, returnParsedScript: true);
            var (member, _) = parser.Parse(JurisdictionCorpus.GetJurisdiction("RO"), returnParsedScript: true);

            var registry = new JurisdictionRegistry();
            registry.Register(new Jurisdiction("BASE_EP", "BASE_EP", baseScript!, null));
            registry.Register(new Jurisdiction("RO", "RO", member!, "BASE_EP"));

            var composed = new JurisdictionComposer(registry).Compose("RO");
            var claims = Assert.Single(composed.Script.Inputs.Where(i => i.Name == "ClaimCount"));

            Assert.Equal("Claims, revised", claims.Text);
        }

        private static List<IPFValue> DefaultsFor(IEnumerable<DslInput> inputs)
        {
            var values = new List<IPFValue>();
            foreach (var input in inputs)
            {
                switch (input)
                {
                    case DslInputNumber n: values.Add(new IPFValueNumber(input.Name, n.DefaultValue)); break;
                    case DslInputBoolean b: values.Add(new IPFValueBoolean(input.Name, b.DefaultValue)); break;
                    case DslInputList l: values.Add(new IPFValueString(input.Name, l.DefaultSymbol)); break;
                    case DslInputListMultiple m: values.Add(new IPFValueStringList(input.Name, m.DefaultSymbols.ToList())); break;
                    case DslInputDate d: values.Add(new IPFValueDate(input.Name, d.DefaultValue)); break;
                    case DslInputAmount a: values.Add(new IPFValueNumber(input.Name, a.DefaultValue)); break;
                }
            }
            return values;
        }
    }
}
