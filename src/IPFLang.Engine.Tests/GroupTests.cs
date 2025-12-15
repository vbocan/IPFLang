using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class GroupTests
    {
        [Fact]
        public void TestGroupList()
        {
            string text =
            """
            # Define a list variable
            DEFINE LIST EntityType AS 'Entity type'
            GROUP G1
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
            Assert.Equal("G1", result.Group);
        }

        [Fact]
        public void TestGroupListMultiple()
        {
            string text =
            """
            # Define a multiple-selection list variable
            DEFINE MULTILIST EntityType AS 'Entity type'
            GROUP G2
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
            Assert.Equal("G2", result.Group);
        }

        [Fact]
        public void TestCommentInString()
        {
            string text =
            """
            # Define a list variable
            DEFINE LIST L1 AS 'List #1'
            GROUP G3
            CHOICE C1 AS 'Choice #1'
            DEFAULT C1
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputList?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("G3", result.Group);
        }

        [Fact]
        public void TestGroupNumber()
        {
            string text =
            """
            # Define a number variable
            DEFINE NUMBER ClaimCount AS 'Number of claims'
            GROUP G4
            BETWEEN 0 AND 1000
            DEFAULT 0
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputNumber?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("G4", result.Group);
        }

        [Fact]
        public void TestGroupBoolean()
        {
            string text =
            """
            # Define a boolean variable
            DEFINE BOOLEAN ContainsDependentClaims AS 'Contains dependent claims'
            GROUP G5
            DEFAULT TRUE
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputBoolean?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("G5", result.Group);
        }

        [Fact]
        public void TestGroupDate()
        {
            string text =
            """
            # Define a date variable
            DEFINE DATE ApplicationDate AS 'Application date'
            GROUP G6
            BETWEEN 01.01.2023 AND TODAY
            DEFAULT 01.03.2023
            ENDDEFINE
            """;
            var p = new DslParser();
            var _ = p.Parse(text);
            var result = (DslInputDate?)p.GetInputs().SingleOrDefault();
            Assert.NotNull(result);
            Assert.Equal("G6", result.Group);
        }

        [Fact]
        public void TestMissingGroupDate()
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
            Assert.Equal(string.Empty, result.Group);
        }
    }
}
