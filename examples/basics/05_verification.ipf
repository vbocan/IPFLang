# ============================================================================
# Verification Directives Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates IPFLang's built-in verification capabilities for ensuring
#   fee definitions are mathematically sound. Verification catches common
#   errors like missing cases or non-monotonic fee structures.
#
# FEATURES DEMONSTRATED:
#   - VERIFY COMPLETE: Ensures all input combinations produce a result
#   - VERIFY MONOTONIC: Ensures fees never decrease as input increases
#   - LIST, NUMBER, and BOOLEAN inputs
#   - Unconditional YIELD (always produces output)
#
# VERIFICATION TYPES:
#   1. COMPLETE - Checks that every possible combination of inputs has
#      at least one YIELD that matches. Missing cases cause verification
#      to fail with a gap report.
#
#   2. MONOTONIC - Checks that a fee is non-decreasing with respect to
#      a numeric input. Useful for ensuring "more claims = more fees".
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- verify examples/05_verification.ipf
#   - CategoryFee: PASS (all 3 categories covered)
#   - UrgencyFee: PASS (both TRUE and FALSE covered)
#   - QuantityFee: PASS (unconditional, always yields)
#   - QuantityFee monotonicity: PASS (linear, always increases)
#
# SEE ALSO:
#   - examples/errors/err_02_incomplete_coverage.ipf (failing completeness)
#   - examples/errors/err_03_non_monotonic.ipf (failing monotonicity)
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Verification examples'

DEFINE LIST Category AS 'Fee category'
CHOICE Cat_A AS 'Category A'
CHOICE Cat_B AS 'Category B'
CHOICE Cat_C AS 'Category C'
DEFAULT Cat_A
ENDDEFINE

DEFINE NUMBER Quantity AS 'Number of items'
BETWEEN 1 AND 100
DEFAULT 1
ENDDEFINE

DEFINE BOOLEAN IsUrgent AS 'Urgent processing?'
DEFAULT FALSE
ENDDEFINE

# Complete fee - all Category values covered
COMPUTE FEE CategoryFee
CASE Category EQ Cat_A AS
  YIELD 100
ENDCASE
CASE Category EQ Cat_B AS
  YIELD 200
ENDCASE
CASE Category EQ Cat_C AS
  YIELD 50
ENDCASE
ENDCOMPUTE

# Monotonic fee - increases with Quantity
COMPUTE FEE QuantityFee
LET UnitPrice AS 25
YIELD UnitPrice * Quantity
ENDCOMPUTE

# Complete coverage of boolean input
COMPUTE FEE UrgencyFee
CASE IsUrgent EQ TRUE AS
  YIELD 150
ENDCASE
CASE IsUrgent EQ FALSE AS
  YIELD 0
ENDCASE
ENDCOMPUTE

VERIFY COMPLETE FEE CategoryFee
VERIFY COMPLETE FEE UrgencyFee
VERIFY COMPLETE FEE QuantityFee
VERIFY MONOTONIC FEE QuantityFee WITH RESPECT TO Quantity
