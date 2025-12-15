# ============================================================================
# ERROR EXAMPLE: Mixed Currency Arithmetic
# ============================================================================
#
# PURPOSE:
#   Demonstrates the type checker catching cross-currency operations.
#   IPFLang enforces currency type safety at compile time.
#
# THE ERROR:
#   Line 29: YIELD 100<EUR> + 50<USD>
#   This attempts to add EUR and USD amounts directly, which is invalid.
#
# WHY IT'S AN ERROR:
#   Adding different currencies without conversion produces meaningless
#   results. IPFLang's type system prevents this at parse time, before
#   any computation occurs.
#
# EXPECTED OUTPUT:
#   Run: dotnet run --project src/IPFLang.CLI -- parse examples/errors/err_01_mixed_currency.ipf
#   Error: Type error: Cannot mix currencies in arithmetic: 'EUR' and 'USD'
#
# HOW TO FIX:
#   Use a currency converter or ensure all amounts are in the same currency:
#     YIELD 100<EUR> + CONVERT(50<USD> TO EUR)
#   Or simply:
#     YIELD 100<EUR> + 45<EUR>
#
# SEE ALSO:
#   examples/02_currency_types.ipf (correct currency usage)
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Mixed currency error demo'

DEFINE NUMBER Quantity AS 'Number of items'
BETWEEN 1 AND 100
DEFAULT 10
ENDDEFINE

# ERROR: Adding EUR and USD amounts directly
COMPUTE FEE MixedCurrencyFee
YIELD 100<EUR> + 50<USD>
ENDCOMPUTE
