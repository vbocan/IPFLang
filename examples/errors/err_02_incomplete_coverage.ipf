# ============================================================================
# ERROR EXAMPLE: Incomplete Fee Coverage
# ============================================================================
#
# PURPOSE:
#   Demonstrates the completeness checker identifying missing CASE coverage.
#   This is a common error when not all input combinations are handled.
#
# THE ERROR:
#   The fee only handles Cat_A and Cat_B, but Cat_C has no CASE block.
#   When Category=Cat_C, no YIELD will execute.
#
# STRUCTURE:
#   Category has 3 values: Cat_A, Cat_B, Cat_C
#   IncompleteFee only covers: Cat_A, Cat_B
#   Missing: Cat_C
#
# WHY IT'S AN ERROR:
#   If a user selects Cat_C, the fee computation has undefined behavior.
#   The VERIFY COMPLETE directive catches this at verification time.
#
# EXPECTED OUTPUT:
#   Run: dotnet run --project src/IPFLang.CLI -- verify examples/errors/err_02_incomplete_coverage.ipf
#   FAIL COMPLETE IncompleteFee: 3 combinations checked
#       Gap: {Category=Cat_C}
#
# HOW TO FIX:
#   Add a CASE block for Cat_C:
#     CASE Category EQ Cat_C AS
#       YIELD 300
#     ENDCASE
#
# SEE ALSO:
#   examples/05_verification.ipf (correct completeness)
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Incomplete coverage demo'

DEFINE LIST Category AS 'Item category'
CHOICE Cat_A AS 'Category A'
CHOICE Cat_B AS 'Category B'
CHOICE Cat_C AS 'Category C'
DEFAULT Cat_A
ENDDEFINE

# ERROR: Missing case for Cat_C
COMPUTE FEE IncompleteFee
CASE Category EQ Cat_A AS
  YIELD 100
ENDCASE
CASE Category EQ Cat_B AS
  YIELD 200
ENDCASE
# Cat_C is not handled - completeness verification will fail
ENDCOMPUTE

VERIFY COMPLETE FEE IncompleteFee
