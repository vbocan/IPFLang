# ============================================================================
# ERROR EXAMPLE: Undefined Variable Reference
# ============================================================================
#
# PURPOSE:
#   Demonstrates the type checker catching references to undefined variables.
#   All variables must be defined as inputs or LET bindings before use.
#
# THE ERROR:
#   Line 44: YIELD Quantity * UndefinedVar
#   'UndefinedVar' is never defined anywhere in the script.
#
# DEFINED VARIABLES IN THIS SCRIPT:
#   - Quantity (defined as NUMBER input)
#   - UndefinedVar (NOT defined anywhere - error!)
#
# WHY IT'S AN ERROR:
#   Referencing undefined variables would cause runtime failures.
#   The type checker catches this at parse time for safety.
#
# EXPECTED OUTPUT:
#   Run: dotnet run --project src/IPFLang.CLI -- parse examples/errors/err_05_undefined_variable.ipf
#   Error: Type error: Cannot perform arithmetic on non-numeric type: String
#   (The unknown variable is treated as a string literal, causing type error)
#
# HOW TO FIX:
#   Either define the variable as an input:
#     DEFINE NUMBER UndefinedVar AS 'Multiplier'
#     BETWEEN 1 AND 10
#     DEFAULT 1
#     ENDDEFINE
#
#   Or as a LET binding:
#     LET UndefinedVar AS 5
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Undefined variable demo'

DEFINE NUMBER Quantity AS 'Number of items'
BETWEEN 1 AND 100
DEFAULT 10
ENDDEFINE

# ERROR: UndefinedVar is never defined
COMPUTE FEE UndefinedVarFee
YIELD Quantity * UndefinedVar
ENDCOMPUTE
