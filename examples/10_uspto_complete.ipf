# ============================================================================
# USPTO Complete Fee Calculator
# ============================================================================
#
# DESCRIPTION:
#   A comprehensive real-world implementation of the United States Patent and
#   Trademark Office (USPTO) fee schedule. This example demonstrates how
#   IPFLang can model complex regulatory fee structures with multiple
#   interacting variables.
#
# FEATURES DEMONSTRATED:
#   - GROUP definitions for UI organization
#   - Multiple LIST inputs (entity status, application type)
#   - Multiple NUMBER inputs (total claims, independent claims)
#   - BOOLEAN input (electronic vs paper filing)
#   - Entity-based fee reductions (50% small, 75% micro)
#   - Application type variations (utility, design, provisional)
#   - Complex excess claims calculations
#   - Cross-product verification
#
# ENTITY FEE STRUCTURE (per 37 CFR):
#   - Large Entity: 100% (full fee)
#   - Small Entity: 50% reduction
#   - Micro Entity: 75% reduction
#
# BASE FEES (Large Entity):
#   Application Type | Filing | Search
#   -----------------|--------|--------
#   Utility          | $1,820 | $720
#   Design           | $1,060 | $180
#   Provisional      | $350   | $0
#
# EXCESS CLAIMS (Utility only):
#   - Claims 21+: $100 each (reduced for small/micro)
#   - Independent claims 4+: $480 each (reduced for small/micro)
#
# PAPER FILING SURCHARGE:
#   - $400 for non-electronic filing
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/10_uspto_complete.ipf
#   - Large utility, 20 claims, 3 indep, electronic: $1,820 + $720 + $0 + $0 = $2,540
#   - Micro utility, 30 claims, 5 indep, electronic: $455 + $180 + $250 + $240 = $1,125
#   - Large design, paper filing: $1,060 + $180 + $0 + $400 = $1,640
#
# VERIFICATION:
#   Run: dotnet run --project src/IPFLang.CLI -- verify examples/10_uspto_complete.ipf
#   - All fees pass completeness verification
#   - Tests 3 app types x 3 entity statuses x 2 filing methods = 18+ combinations
#
# ============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-17 DESCRIPTION 'USPTO fees' REFERENCE '37 CFR 1.16-1.18'

DEFINE GROUP Applicant AS 'Applicant' WITH WEIGHT 10
DEFINE GROUP Application AS 'Application' WITH WEIGHT 20
DEFINE GROUP Filing AS 'Filing Options' WITH WEIGHT 30

DEFINE LIST EntityStatus AS 'Entity status'
GROUP Applicant
CHOICE Entity_Large AS 'Large Entity'
CHOICE Entity_Small AS 'Small Entity (50%)'
CHOICE Entity_Micro AS 'Micro Entity (75%)'
DEFAULT Entity_Large
ENDDEFINE

DEFINE LIST ApplicationType AS 'Application type'
GROUP Application
CHOICE App_Utility AS 'Utility Patent'
CHOICE App_Design AS 'Design Patent'
CHOICE App_Provisional AS 'Provisional'
DEFAULT App_Utility
ENDDEFINE

DEFINE NUMBER TotalClaims AS 'Total claims'
GROUP Application
BETWEEN 1 AND 500
DEFAULT 20
ENDDEFINE

DEFINE NUMBER IndependentClaims AS 'Independent claims'
GROUP Application
BETWEEN 1 AND 100
DEFAULT 3
ENDDEFINE

DEFINE BOOLEAN ElectronicFiling AS 'Electronic filing?'
GROUP Filing
DEFAULT TRUE
ENDDEFINE

# Basic filing fee
COMPUTE FEE BasicFilingFee
LET UtilityFee AS 1820
LET DesignFee AS 1060
LET ProvisionalFee AS 350

CASE ApplicationType EQ App_Utility AND EntityStatus EQ Entity_Large AS
  YIELD UtilityFee
ENDCASE
CASE ApplicationType EQ App_Utility AND EntityStatus EQ Entity_Small AS
  YIELD UtilityFee * 0.50
ENDCASE
CASE ApplicationType EQ App_Utility AND EntityStatus EQ Entity_Micro AS
  YIELD UtilityFee * 0.25
ENDCASE

CASE ApplicationType EQ App_Design AND EntityStatus EQ Entity_Large AS
  YIELD DesignFee
ENDCASE
CASE ApplicationType EQ App_Design AND EntityStatus EQ Entity_Small AS
  YIELD DesignFee * 0.50
ENDCASE
CASE ApplicationType EQ App_Design AND EntityStatus EQ Entity_Micro AS
  YIELD DesignFee * 0.25
ENDCASE

CASE ApplicationType EQ App_Provisional AND EntityStatus EQ Entity_Large AS
  YIELD ProvisionalFee
ENDCASE
CASE ApplicationType EQ App_Provisional AND EntityStatus EQ Entity_Small AS
  YIELD ProvisionalFee * 0.50
ENDCASE
CASE ApplicationType EQ App_Provisional AND EntityStatus EQ Entity_Micro AS
  YIELD ProvisionalFee * 0.25
ENDCASE
ENDCOMPUTE

# Search fee
COMPUTE FEE SearchFee
LET UtilitySearch AS 720
LET DesignSearch AS 180

CASE ApplicationType EQ App_Utility AND EntityStatus EQ Entity_Large AS
  YIELD UtilitySearch
ENDCASE
CASE ApplicationType EQ App_Utility AND EntityStatus EQ Entity_Small AS
  YIELD UtilitySearch * 0.50
ENDCASE
CASE ApplicationType EQ App_Utility AND EntityStatus EQ Entity_Micro AS
  YIELD UtilitySearch * 0.25
ENDCASE

CASE ApplicationType EQ App_Design AND EntityStatus EQ Entity_Large AS
  YIELD DesignSearch
ENDCASE
CASE ApplicationType EQ App_Design AND EntityStatus EQ Entity_Small AS
  YIELD DesignSearch * 0.50
ENDCASE
CASE ApplicationType EQ App_Design AND EntityStatus EQ Entity_Micro AS
  YIELD DesignSearch * 0.25
ENDCASE

CASE ApplicationType EQ App_Provisional AS
  YIELD 0
ENDCASE
ENDCOMPUTE

# Excess claims fee - using direct input references
COMPUTE FEE ExcessClaimsFee
LET TotalRate AS 100
LET IndepRate AS 480

CASE ApplicationType NEQ App_Utility AS
  YIELD 0
ENDCASE

CASE ApplicationType EQ App_Utility AND TotalClaims LTE 20 AND IndependentClaims LTE 3 AS
  YIELD 0
ENDCASE

CASE ApplicationType EQ App_Utility AND TotalClaims GT 20 AND IndependentClaims LTE 3 AND EntityStatus EQ Entity_Large AS
  YIELD TotalRate * (TotalClaims - 20)
ENDCASE
CASE ApplicationType EQ App_Utility AND TotalClaims GT 20 AND IndependentClaims LTE 3 AND EntityStatus EQ Entity_Small AS
  YIELD TotalRate * (TotalClaims - 20) * 0.50
ENDCASE
CASE ApplicationType EQ App_Utility AND TotalClaims GT 20 AND IndependentClaims LTE 3 AND EntityStatus EQ Entity_Micro AS
  YIELD TotalRate * (TotalClaims - 20) * 0.25
ENDCASE

CASE ApplicationType EQ App_Utility AND IndependentClaims GT 3 AND TotalClaims LTE 20 AND EntityStatus EQ Entity_Large AS
  YIELD IndepRate * (IndependentClaims - 3)
ENDCASE
CASE ApplicationType EQ App_Utility AND IndependentClaims GT 3 AND TotalClaims LTE 20 AND EntityStatus EQ Entity_Small AS
  YIELD IndepRate * (IndependentClaims - 3) * 0.50
ENDCASE
CASE ApplicationType EQ App_Utility AND IndependentClaims GT 3 AND TotalClaims LTE 20 AND EntityStatus EQ Entity_Micro AS
  YIELD IndepRate * (IndependentClaims - 3) * 0.25
ENDCASE

CASE ApplicationType EQ App_Utility AND TotalClaims GT 20 AND IndependentClaims GT 3 AND EntityStatus EQ Entity_Large AS
  YIELD TotalRate * (TotalClaims - 20) + IndepRate * (IndependentClaims - 3)
ENDCASE
CASE ApplicationType EQ App_Utility AND TotalClaims GT 20 AND IndependentClaims GT 3 AND EntityStatus EQ Entity_Small AS
  YIELD (TotalRate * (TotalClaims - 20) + IndepRate * (IndependentClaims - 3)) * 0.50
ENDCASE
CASE ApplicationType EQ App_Utility AND TotalClaims GT 20 AND IndependentClaims GT 3 AND EntityStatus EQ Entity_Micro AS
  YIELD (TotalRate * (TotalClaims - 20) + IndepRate * (IndependentClaims - 3)) * 0.25
ENDCASE
ENDCOMPUTE

# Paper filing surcharge
COMPUTE FEE PaperFilingSurcharge
CASE ElectronicFiling EQ FALSE AS
  YIELD 400
ENDCASE
CASE ElectronicFiling EQ TRUE AS
  YIELD 0
ENDCASE
ENDCOMPUTE

VERIFY COMPLETE FEE BasicFilingFee
VERIFY COMPLETE FEE SearchFee
VERIFY COMPLETE FEE ExcessClaimsFee
VERIFY COMPLETE FEE PaperFilingSurcharge

RETURN Currency AS 'USD'
RETURN Version AS '2024.1'
RETURN Reference AS '37 CFR 1.16-1.18'
