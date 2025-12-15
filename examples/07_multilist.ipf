# ============================================================================
# MULTILIST Multi-Selection Input Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates the MULTILIST input type for scenarios where users can
#   select multiple options from a list. Common use case: selecting
#   multiple validation countries for a European patent.
#
# FEATURES DEMONSTRATED:
#   - MULTILIST input type (multiple selections allowed)
#   - IN operator for checking if value is in selected set
#   - Conditional YIELD based on selection membership
#   - Individual fees per selected option
#
# HOW MULTILIST WORKS:
#   Unlike LIST (single selection), MULTILIST allows selecting multiple
#   values. Use the IN operator to check membership:
#     YIELD 180 IF VAL_DE IN SelectedCountries
#
# VALIDATION COUNTRY FEES:
#   - Germany (DE): 180 EUR
#   - France (FR): 200 EUR
#   - United Kingdom (GB): 250 EUR
#   - Italy (IT): 175 EUR
#   - Spain (ES): 160 EUR
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/07_multilist.ipf
#   - Selecting DE + FR: 1,200 + 180 + 200 = 1,580 EUR
#   - Selecting all countries: 1,200 + 180 + 200 + 250 + 175 + 160 = 2,165 EUR
#   - Selecting none: 1,200 EUR (base filing only)
#
# ============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'Multi-selection'

DEFINE MULTILIST SelectedCountries AS 'Select validation countries'
CHOICE VAL_NONE AS 'None'
CHOICE VAL_DE AS 'Germany'
CHOICE VAL_FR AS 'France'
CHOICE VAL_GB AS 'United Kingdom'
CHOICE VAL_IT AS 'Italy'
CHOICE VAL_ES AS 'Spain'
DEFAULT VAL_NONE
ENDDEFINE

# Base filing fee
COMPUTE FEE FilingFee
YIELD 1200
ENDCOMPUTE

# Validation fees per country
COMPUTE FEE ValidationFee_DE
YIELD 180 IF VAL_DE IN SelectedCountries
ENDCOMPUTE

COMPUTE FEE ValidationFee_FR
YIELD 200 IF VAL_FR IN SelectedCountries
ENDCOMPUTE

COMPUTE FEE ValidationFee_GB
YIELD 250 IF VAL_GB IN SelectedCountries
ENDCOMPUTE

COMPUTE FEE ValidationFee_IT
YIELD 175 IF VAL_IT IN SelectedCountries
ENDCOMPUTE

COMPUTE FEE ValidationFee_ES
YIELD 160 IF VAL_ES IN SelectedCountries
ENDCOMPUTE

RETURN Currency AS 'EUR'
RETURN Note AS 'Select multiple countries for validation'
