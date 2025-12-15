# ============================================================================
# ERROR EXAMPLE: Amount Input Missing Currency
# ============================================================================
#
# PURPOSE:
#   Demonstrates the parser requiring CURRENCY declaration for AMOUNT inputs.
#   AMOUNT is a currency-typed input that must specify its currency.
#
# THE ERROR:
#   The DEFINE AMOUNT block is missing the required CURRENCY declaration.
#   AMOUNT inputs must always specify what currency they represent.
#
# CORRECT STRUCTURE:
#   DEFINE AMOUNT BaseFee AS 'Base fee amount'
#   CURRENCY EUR           <- This line is missing!
#   DEFAULT 100
#   ENDDEFINE
#
# WHY IT'S AN ERROR:
#   AMOUNT inputs represent monetary values. Without a currency, the
#   type system cannot verify currency safety in expressions.
#
# EXPECTED OUTPUT:
#   Run: dotnet run --project src/IPFLang.CLI -- parse examples/errors/err_06_missing_currency.ipf
#   Error: [Line 40] Error: Amount definition requires a CURRENCY declaration
#
# HOW TO FIX:
#   Add the CURRENCY declaration:
#     DEFINE AMOUNT BaseFee AS 'Base fee amount'
#     CURRENCY EUR
#     DEFAULT 100
#     ENDDEFINE
#
# SEE ALSO:
#   examples/02_currency_types.ipf (correct currency usage)
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Missing currency demo'

# ERROR: AMOUNT input requires CURRENCY declaration
DEFINE AMOUNT BaseFee AS 'Base fee amount'
DEFAULT 100
ENDDEFINE

COMPUTE FEE MissingCurrencyFee
YIELD BaseFee
ENDCOMPUTE
