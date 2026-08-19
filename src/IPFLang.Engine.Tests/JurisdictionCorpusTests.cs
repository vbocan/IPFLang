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
