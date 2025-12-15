using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class InputTests
    {
        [Fact]
        public void TestInputList()
        {
            string text =
            """
            # Define a list variable
            DEFINE LIST EntityType AS 'Entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE SmallEntity AS 'Small'
            CHOICE MicroEntity AS 'Micro'
            DEFAULT NormalEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputList?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("EntityType", result.Name);
            Assert.Equal("Entity type", result.Text);
            Assert.Equal("NormalEntity", result.DefaultSymbol);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new DslListItem("NormalEntity", "Normal"), result.Items[0]);
            Assert.Equal(new DslListItem("SmallEntity", "Small"), result.Items[1]);
            Assert.Equal(new DslListItem("MicroEntity", "Micro"), result.Items[2]);
        }

        [Fact]
        public void TestInputListMultiple()
        {
            string text =
            """
            # Define a multiple-selection list variable
            DEFINE MULTILIST EntityType AS 'Entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE SmallEntity AS 'Small'
            CHOICE MicroEntity AS 'Micro'
            DEFAULT SmallEntity,NormalEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputListMultiple?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("EntityType", result.Name);
            Assert.Equal("Entity type", result.Text);
            Assert.Equal("SmallEntity", result.DefaultSymbols[0]);
            Assert.Equal("NormalEntity", result.DefaultSymbols[1]);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new DslListItem("NormalEntity", "Normal"), result.Items[0]);
            Assert.Equal(new DslListItem("SmallEntity", "Small"), result.Items[1]);
            Assert.Equal(new DslListItem("MicroEntity", "Micro"), result.Items[2]);
        }

        [Fact]
        public void TestCommentInString()
        {
            string text =
            """
            # Define a list variable
            DEFINE LIST L1 AS 'List #1'
            CHOICE C1 AS 'Choice #1'
            DEFAULT C1
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputList?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("L1", result.Name);
            Assert.Equal("List #1", result.Text);
            Assert.Single(result.Items);
            Assert.Equal(new DslListItem("C1", "Choice #1"), result.Items[0]);
        }

        [Fact]
        public void TestInputNumber()
        {
            string text =
            """
            # Define a number variable
            DEFINE NUMBER ClaimCount AS 'Number of claims'
            BETWEEN 0 AND 1000
            DEFAULT 0
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputNumber?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("ClaimCount", result.Name);
            Assert.Equal("Number of claims", result.Text);
            Assert.Equal(0, result.MinValue);
            Assert.Equal(1000, result.MaxValue);
            Assert.Equal(0, result.DefaultValue);
        }

        [Fact]
        public void TestInputBoolean()
        {
            string text =
            """
            # Define a boolean variable
            DEFINE BOOLEAN ContainsDependentClaims AS 'Contains dependent claims'
            DEFAULT TRUE
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputBoolean?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("ContainsDependentClaims", result.Name);
            Assert.Equal("Contains dependent claims", result.Text);
            Assert.True(result.DefaultValue);
        }

        [Fact]
        public void TestInputDate()
        {
            string text =
            """
            # Define a date variable
            DEFINE DATE ApplicationDate AS 'Application date'
            BETWEEN 01.01.2023 AND TODAY
            DEFAULT 01.03.2023
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputDate?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("ApplicationDate", result.Name);
            Assert.Equal("Application date", result.Text);
            Assert.Equal(DateOnly.ParseExact("01.03.2023", "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None), result.DefaultValue);
            Assert.Equal(DateOnly.ParseExact("01.01.2023", "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None), result.MinValue);
            Assert.Equal(DateOnly.FromDateTime(DateTime.Now), result.MaxValue);
        }
    }
}
