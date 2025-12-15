# ============================================================================
# ERROR EXAMPLE: Invalid Currency Code
# ============================================================================
#
# PURPOSE:
#   Demonstrates the parser rejecting invalid ISO 4217 currency codes.
#   IPFLang validates currency codes against the ISO 4217 standard.
#
# THE ERROR:
#   Line 38: CURRENCY XXX
#   'XXX' is not a valid ISO 4217 currency code.
#
# VALID CURRENCY CODES (examples):
#   - EUR (Euro)
#   - USD (US Dollar)
#   - GBP (British Pound)
#   - JPY (Japanese Yen)
#   - CHF (Swiss Franc)
#
# WHY IT'S AN ERROR:
#   Using invalid currency codes could lead to confusion and errors in
#   financial calculations. The parser validates codes at parse time.
#
# EXPECTED OUTPUT:
#   Run: dotnet run --project src/IPFLang.CLI -- parse examples/errors/err_04_invalid_currency.ipf
#   Error: [Line 38] Error: Invalid ISO 4217 currency code: 'XXX'
#
# HOW TO FIX:
#   Use a valid ISO 4217 code:
#     CURRENCY EUR
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Invalid currency demo'

DEFINE AMOUNT BaseFee AS 'Base fee amount'
CURRENCY XXX
DEFAULT 100
ENDDEFINE

COMPUTE FEE InvalidCurrencyFee
YIELD BaseFee * 2
ENDCOMPUTE
