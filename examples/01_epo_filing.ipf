# ============================================================================
# EPO Filing Fee Calculator
# ============================================================================
#
# DESCRIPTION:
#   Calculates European Patent Office (EPO) filing fees based on the official
#   fee schedule effective April 2024. This example demonstrates a real-world
#   patent fee calculation with multiple inputs and fee components.
#
# FEATURES DEMONSTRATED:
#   - VERSION directive with effective date and description
#   - GROUP definitions for organizing inputs in UI
#   - LIST inputs with multiple choices (entity type, ISA)
#   - NUMBER inputs with ranges (claims, pages)
#   - BOOLEAN inputs (IPRP status)
#   - Currency literals with <EUR> syntax
#   - LET variables for reusable constants
#   - Multi-tiered CASE blocks with conditions
#   - Conditional YIELD with IF clauses
#   - VERIFY COMPLETE for completeness checking
#   - VERIFY MONOTONIC for monotonicity checking
#   - RETURN statements for metadata
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/01_epo_filing.ipf
#   - With defaults (15 claims, 35 pages, no ISA): ~4,110 EUR total
#   - With 30 claims: adds excess claim fees (265 EUR x 15 = 3,975 EUR)
#   - With ISA_EPO: search fee is waived
#
# VERIFICATION:
#   Run: dotnet run --project src/IPFLang.CLI -- verify examples/01_epo_filing.ipf
#   - All fees should pass completeness verification
#   - ExcessClaimsFee and ExcessPagesFee are monotonic (never decrease)
#
# ============================================================================

VERSION '2024.1' EFFECTIVE 2024-04-01 DESCRIPTION 'EPO official fees 2024'

DEFINE GROUP General AS 'General Information' WITH WEIGHT 10
DEFINE GROUP Application AS 'Application Details' WITH WEIGHT 20
DEFINE GROUP PCT AS 'PCT Information' WITH WEIGHT 30

# ---- INPUT DEFINITIONS ----

DEFINE LIST EntityType AS 'Type of applicant'
GROUP General
CHOICE LargeEntity AS 'Large entity (company)'
CHOICE SME AS 'Small or Medium Enterprise'
CHOICE MicroEntity AS 'Micro entity / Natural person'
DEFAULT LargeEntity
ENDDEFINE

DEFINE NUMBER ClaimCount AS 'Total number of claims'
GROUP Application
BETWEEN 1 AND 500
DEFAULT 15
ENDDEFINE

DEFINE NUMBER SheetCount AS 'Number of pages in application'
GROUP Application
BETWEEN 1 AND 1000
DEFAULT 35
ENDDEFINE

DEFINE LIST ISA AS 'International Searching Authority'
GROUP PCT
CHOICE ISA_EPO AS 'European Patent Office'
CHOICE ISA_USPTO AS 'US Patent and Trademark Office'
CHOICE ISA_OTHER AS 'Other ISA'
CHOICE ISA_NONE AS 'Not applicable (direct EP filing)'
DEFAULT ISA_NONE
ENDDEFINE

DEFINE BOOLEAN HasIPRP AS 'EPO prepared IPRP (Chapter II)?'
GROUP PCT
DEFAULT FALSE
ENDDEFINE

# ---- FEE COMPUTATIONS ----

COMPUTE FEE FilingFee
YIELD 135<EUR>
ENDCOMPUTE

COMPUTE FEE DesignationFee
YIELD 660<EUR>
ENDCOMPUTE

COMPUTE FEE ExcessClaimsFee
LET ClaimFee1 AS 265
LET ClaimFee2 AS 660
CASE ClaimCount LTE 15 AS
  YIELD 0
ENDCASE
CASE ClaimCount GT 15 AND ClaimCount LTE 50 AS
  YIELD ClaimFee1 * (ClaimCount - 15)
ENDCASE
CASE ClaimCount GT 50 AS
  YIELD ClaimFee1 * 35 + ClaimFee2 * (ClaimCount - 50)
ENDCASE
ENDCOMPUTE

COMPUTE FEE ExcessPagesFee
CASE SheetCount LTE 35 AS
  YIELD 0
ENDCASE
CASE SheetCount GT 35 AS
  YIELD 17 * (SheetCount - 35)
ENDCASE
ENDCOMPUTE

COMPUTE FEE SearchFee
LET FullSearchFee AS 1460
LET ReducedSearchFee AS 1185
CASE ISA EQ ISA_EPO AS
  YIELD 0
ENDCASE
CASE ISA EQ ISA_USPTO OR ISA EQ ISA_OTHER AS
  YIELD FullSearchFee - ReducedSearchFee
ENDCASE
CASE ISA EQ ISA_NONE AS
  YIELD FullSearchFee
ENDCASE
ENDCOMPUTE

COMPUTE FEE ExaminationFee
LET ExamFeeISA AS 2055
LET ExamFeeOther AS 1840
LET IPRPDiscount AS 0.75
CASE HasIPRP EQ TRUE AS
  YIELD ExamFeeISA * IPRPDiscount IF ISA EQ ISA_EPO
  YIELD ExamFeeOther * IPRPDiscount IF ISA NEQ ISA_EPO
ENDCASE
CASE HasIPRP EQ FALSE AS
  YIELD ExamFeeISA IF ISA EQ ISA_EPO
  YIELD ExamFeeOther IF ISA NEQ ISA_EPO
ENDCASE
ENDCOMPUTE

# ---- VERIFICATION DIRECTIVES ----

VERIFY COMPLETE FEE FilingFee
VERIFY COMPLETE FEE ExcessClaimsFee
VERIFY COMPLETE FEE ExcessPagesFee
VERIFY COMPLETE FEE SearchFee
VERIFY COMPLETE FEE ExaminationFee
VERIFY MONOTONIC FEE ExcessClaimsFee WITH RESPECT TO ClaimCount
VERIFY MONOTONIC FEE ExcessPagesFee WITH RESPECT TO SheetCount

RETURN Currency AS 'EUR'
RETURN TotalFees AS 'Total EPO Filing Fees'
