# ============================================================================
# ERROR EXAMPLE: Non-Monotonic Fee
# ============================================================================
#
# PURPOSE:
#   Demonstrates the monotonicity checker catching a fee that decreases
#   when the input increases. This violates the principle that "more = more".
#
# THE ERROR:
#   When Quantity=50: fee = 50 * 10 = 500
#   When Quantity=51: fee = 200 (fixed)
#   The fee DECREASED from 500 to 200 when input increased!
#
# FEE STRUCTURE (FLAWED):
#   - Quantity 1-50: 10 per unit (linear increase)
#   - Quantity 51+: flat 200 (sudden drop!)
#
# WHY IT'S AN ERROR:
#   Non-monotonic fees are usually policy mistakes. Users would be
#   incentivized to manipulate their claims (e.g., add fake claims to
#   get a lower fee). The VERIFY MONOTONIC directive catches this.
#
# EXPECTED OUTPUT:
#   Run: dotnet run --project src/IPFLang.CLI -- verify examples/errors/err_03_non_monotonic.ipf
#   FAIL MONOTONIC NonMonotonicFee w.r.t. Quantity: NonDecreasing
#       Violation: At input=50, fee=500; at input=51, fee=200
#
# HOW TO FIX:
#   Ensure fees never decrease as input increases:
#     CASE Quantity GT 50 AS
#       YIELD 500 + 5 * (Quantity - 50)   # Continue increasing
#     ENDCASE
#
# SEE ALSO:
#   examples/05_verification.ipf (correct monotonicity)
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Non-monotonic fee demo'

DEFINE NUMBER Quantity AS 'Number of items'
BETWEEN 1 AND 100
DEFAULT 10
ENDDEFINE

# ERROR: Fee decreases when Quantity > 50 (violates monotonicity)
COMPUTE FEE NonMonotonicFee
CASE Quantity LTE 50 AS
  YIELD Quantity * 10
ENDCASE
CASE Quantity GT 50 AS
  YIELD 200
ENDCASE
ENDCOMPUTE

VERIFY MONOTONIC FEE NonMonotonicFee WITH RESPECT TO Quantity
