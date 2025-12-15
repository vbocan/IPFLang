using IPFLang.Parser;

namespace IPFLang.Engine.Tests
{
    public class SemanticTests
    {
        #region Single Selection List Tests
        [Fact]
        public void TestListNoError()
        {
            string text =
            """
            DEFINE LIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE SmallEntity AS 'Small'
            CHOICE MicroEntity AS 'Micro'
            DEFAULT NormalEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var errors = p.GetErrors();
            Assert.Empty(errors);
        }

        [Fact]
        public void TestListDuplicateChoice()
        {
            string text =
            """
            DEFINE LIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE NormalEntity AS 'Small'
            DEFAULT NormalEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableDuplicateChoices);
        }

        [Fact]
        public void TestListWrongDefaultChoice()
        {
            string text =
            """
            DEFINE LIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE SmallEntity AS 'Small'
            DEFAULT TinyEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableInvalidDefaultChoice);
        }

        [Fact]
        public void TestListWrongDefaultAndDuplicateChoice()
        {
            string text =
            """
            DEFINE LIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE NormalEntity AS 'Normal'
            DEFAULT TinyEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 is DslError.VariableDuplicateChoices or DslError.VariableInvalidDefaultChoice);
        }

        [Fact]
        public void TestListNoChoices()
        {
            string text =
            """
            DEFINE LIST EntityType AS 'Select the desired entity type'
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableNoChoice);
        }

        [Fact]
        public void TestListDuplicateChoicesInMultipleVariables()
        {
            string text =
            """
            DEFINE LIST EntityType1 AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            DEFAULT NormalEntity
            ENDDEFINE

            DEFINE LIST EntityType2 AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            DEFAULT NormalEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.ChoiceDefinedInMultipleVariables);
        }
        #endregion

        #region Multiple Selection List Tests
        [Fact]
        public void TestListMultipleNoError()
        {
            string text =
            """
            DEFINE MULTILIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE SmallEntity AS 'Small'
            CHOICE MicroEntity AS 'Micro'
            DEFAULT NormalEntity,SmallEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
            var errors = p.GetErrors();
            Assert.Empty(errors);
        }

        [Fact]
        public void TestListMultipleDuplicateChoice()
        {
            string text =
            """
            DEFINE MULTILIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE NormalEntity AS 'Small'
            DEFAULT NormalEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableDuplicateChoices);
        }

        [Fact]
        public void TestListMultipleWrongDefaultChoice()
        {
            string text =
            """
            DEFINE MULTILIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE SmallEntity AS 'Small'
            DEFAULT TinyEntity,ExtremEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableInvalidDefaultChoice);
        }

        [Fact]
        public void TestListMultipleWrongDefaultAndDuplicateChoice()
        {
            string text =
            """
            DEFINE MULTILIST EntityType AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            CHOICE NormalEntity AS 'Normal'
            DEFAULT TinyEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 is DslError.VariableDuplicateChoices or DslError.VariableInvalidDefaultChoice);
        }

        [Fact]
        public void TestListMultipleNoChoices()
        {
            string text =
            """
            DEFINE MULTILIST EntityType AS 'Select the desired entity type'
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableNoChoice);
        }

        [Fact]
        public void TestListMultipleDuplicateChoicesInMultipleVariables()
        {
            string text =
            """
            DEFINE MULTILIST EntityType1 AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            DEFAULT NormalEntity
            ENDDEFINE

            DEFINE MULTILIST EntityType2 AS 'Select the desired entity type'
            CHOICE NormalEntity AS 'Normal'
            DEFAULT NormalEntity
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.ChoiceDefinedInMultipleVariables);
        }
        #endregion

        #region Number Tests
        [Fact]
        public void TestNumberMinMax()
        {
            string text =
            """
            DEFINE NUMBER SheetCount AS 'Enter the number of sheets'
            BETWEEN 10 AND 1000
            DEFAULT 15
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
        }

        [Fact]
        public void TestNumberMinOverMax()
        {
            string text =
            """
            DEFINE NUMBER SheetCount AS 'Enter the number of sheets'
            BETWEEN 20 AND 10
            DEFAULT 15
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.InvalidMinMaxDefault);
        }

        [Fact]
        public void TestNumberDefaultOutOfRange()
        {
            string text =
            """
            DEFINE NUMBER SheetCount AS 'Enter the number of sheets'
            BETWEEN 10 AND 20
            DEFAULT 21
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.InvalidMinMaxDefault);
        }

        [Fact]
        public void TestVariableListDefinedMultipleTimes()
        {
            string text =
            """
            DEFINE NUMBER SheetCount AS 'Enter the number of sheets'
            BETWEEN 10 AND 20
            DEFAULT 11
            ENDDEFINE
            DEFINE NUMBER SheetCount AS 'Enter the number of sheets'
            BETWEEN 10 AND 20
            DEFAULT 11
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableDefinedMultipleTimes);
        }
        #endregion

        #region Fee Tests
        [Fact]
        public void TestFeeDefinedMultipleTimes()
        {
            string text =
            """
            COMPUTE FEE SheetFee
            YIELD 420 * SheetCount / 50 IF SheetCount OVER 100 AND EntityType IS NormalEntity
            YIELD 168 * SheetCount / 50 IF SheetCount OVER 100 AND EntityType IS SmallEntity
            YIELD 84 * SheetCount / 50 IF SheetCount OVER 100 AND EntityType IS MicroEntity
            ENDCOMPUTE
            COMPUTE FEE SheetFee
            YIELD 480 * ClaimCount IF ClaimCount OVER 3 AND EntityType IS NormalEntity
            YIELD 192 IF ClaimCount OVER 3 AND EntityType IS SmallEntity
            YIELD 96 IF ClaimCount OVER 3 AND EntityType IS MicroEntity
            ENDCOMPUTE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.FeeDefinedMultipleTimes);
        }
        #endregion

        #region Date Tests
        [Fact]
        public void TestDateMinMax()
        {
            string text =
            """
            DEFINE DATE SubmissionDate AS 'Pick the submission date'
            BETWEEN 01.01.2023 AND 31.12.2023
            DEFAULT 01.07.2023
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.True(result);
        }

        [Fact]
        public void TestDateMinOverMax()
        {
            string text =
            """
            DEFINE DATE SubmissionDate AS 'Pick the submission date'
            BETWEEN 31.12.2023 AND 01.01.2023
            DEFAULT 01.07.2023
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.InvalidMinMaxDefault);
        }

        [Fact]
        public void TestDateDefaultOutOfRange()
        {
            string text =
            """
            DEFINE DATE SubmissionDate AS 'Pick the submission date'
            BETWEEN 01.01.2023 AND 31.12.2023
            DEFAULT 01.07.2024
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.InvalidMinMaxDefault);
        }

        [Fact]
        public void TestVariableDateDefinedMultipleTimes()
        {
            string text =
            """
            DEFINE DATE SubmissionDate AS 'Pick the submission date'
            BETWEEN 01.01.2023 AND 31.12.2023
            DEFAULT 01.07.2023
            ENDDEFINE
            DEFINE DATE SubmissionDate AS 'Pick the submission date'
            BETWEEN 01.01.2023 AND 31.12.2023
            DEFAULT 01.07.2023
            ENDDEFINE
            """;
            var p = new DslParser();
            var result = p.Parse(text);
            Assert.False(result);
            var errors = p.GetErrors();
            Assert.Contains(errors, a => a.Item1 == DslError.VariableDefinedMultipleTimes);
        }
        #endregion
    }
}
