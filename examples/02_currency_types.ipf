# ============================================================================
# Currency Literals and Type Safety Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates IPFLang's currency literal syntax and type-safe arithmetic.
#   Currency amounts are specified using the <CURRENCY> suffix (e.g., 500<EUR>).
#   The type checker prevents mixing currencies in arithmetic operations.
#
# FEATURES DEMONSTRATED:
#   - Currency literals: 500<EUR>, 10<EUR>, 600<USD>
#   - Arithmetic with currency amounts
#   - Tiered pricing with LET variables
#   - Monotonicity verification for scaling fees
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/02_currency_types.ipf
#   - EuroFee: base 500 EUR + (Quantity x 10 EUR)
#   - DollarFee: fixed 600 USD
#   - TieredFee: first 10 items at 30/unit, remainder at 25/unit
#
# TYPE SAFETY NOTE:
#   Attempting to add EUR and USD amounts would cause a type error.
#   See examples/errors/err_01_mixed_currency.ipf for an example.
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Currency examples'

DEFINE NUMBER Quantity AS 'Number of items'
BETWEEN 1 AND 100
DEFAULT 10
ENDDEFINE

# Fee using currency literals
COMPUTE FEE EuroFee
YIELD 500<EUR>
YIELD Quantity * 10<EUR>
ENDCOMPUTE

COMPUTE FEE DollarFee
YIELD 600<USD>
ENDCOMPUTE

# Tiered pricing
COMPUTE FEE TieredFee
LET Tier1Rate AS 30
LET Tier2Rate AS 25
CASE Quantity LTE 10 AS
  YIELD Tier1Rate * Quantity
ENDCASE
CASE Quantity GT 10 AS
  YIELD Tier1Rate * 10 + Tier2Rate * (Quantity - 10)
ENDCASE
ENDCOMPUTE

VERIFY COMPLETE FEE TieredFee
VERIFY MONOTONIC FEE TieredFee WITH RESPECT TO Quantity
