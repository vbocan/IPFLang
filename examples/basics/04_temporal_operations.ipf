# ============================================================================
# Temporal Operations and Renewal Fees Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates date-based inputs and renewal fee schedules. Patent renewal
#   fees typically increase over time. This example models a simplified
#   renewal fee schedule based on the patent's age.
#
# FEATURES DEMONSTRATED:
#   - DATE input with BETWEEN constraints
#   - TODAY keyword for current date
#   - LIST input for renewal year selection
#   - Cross-product CASE conditions (year AND entity type)
#   - Complete coverage verification
#
# FEE SCHEDULE:
#   - Year 1: No renewal fee
#   - Year 2: 200 (large) / 100 (small)
#   - Year 3: 300 (large) / 150 (small)
#   - Year 4+: 400 (large) / 200 (small)
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/04_temporal_operations.ipf
#   - Year 1 renewal: 500 (filing) + 0 (renewal) = 500
#   - Year 3 large entity: 500 (filing) + 300 (renewal) = 800
#   - Year 4+ small entity: 250 (filing) + 200 (renewal) = 450
#
# NOTE:
#   For complex temporal calculations (e.g., MONTHSTONOW), use LET variables
#   to compute derived values from date inputs.
#
# ============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'Temporal operations'

DEFINE DATE ApplicationDate AS 'Filing date'
BETWEEN 01.01.2000 AND TODAY
DEFAULT TODAY
ENDDEFINE

DEFINE LIST EntitySize AS 'Entity type'
CHOICE Entity_Large AS 'Large entity'
CHOICE Entity_Small AS 'Small entity'
DEFAULT Entity_Large
ENDDEFINE

DEFINE LIST RenewalYear AS 'Renewal year'
CHOICE Year_1 AS 'Year 1'
CHOICE Year_2 AS 'Year 2'
CHOICE Year_3 AS 'Year 3'
CHOICE Year_4Plus AS 'Year 4+'
DEFAULT Year_1
ENDDEFINE

# Basic filing fee - complete coverage of EntitySize
COMPUTE FEE FilingFee
CASE EntitySize EQ Entity_Large AS
  YIELD 500
ENDCASE
CASE EntitySize EQ Entity_Small AS
  YIELD 250
ENDCASE
ENDCOMPUTE

# Renewal fees based on year and entity size
COMPUTE FEE RenewalFee
CASE RenewalYear EQ Year_1 AND EntitySize EQ Entity_Large AS
  YIELD 0
ENDCASE
CASE RenewalYear EQ Year_1 AND EntitySize EQ Entity_Small AS
  YIELD 0
ENDCASE
CASE RenewalYear EQ Year_2 AND EntitySize EQ Entity_Large AS
  YIELD 200
ENDCASE
CASE RenewalYear EQ Year_2 AND EntitySize EQ Entity_Small AS
  YIELD 100
ENDCASE
CASE RenewalYear EQ Year_3 AND EntitySize EQ Entity_Large AS
  YIELD 300
ENDCASE
CASE RenewalYear EQ Year_3 AND EntitySize EQ Entity_Small AS
  YIELD 150
ENDCASE
CASE RenewalYear EQ Year_4Plus AND EntitySize EQ Entity_Large AS
  YIELD 400
ENDCASE
CASE RenewalYear EQ Year_4Plus AND EntitySize EQ Entity_Small AS
  YIELD 200
ENDCASE
ENDCOMPUTE

VERIFY COMPLETE FEE FilingFee
VERIFY COMPLETE FEE RenewalFee
