# ============================================================================
# Multi-Level CASE Conditions Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates complex decision trees with multiple inputs. When fees
#   depend on combinations of several variables, CASE conditions can use
#   AND/OR operators to specify precise matching criteria.
#
# FEATURES DEMONSTRATED:
#   - Multiple LIST inputs (ServiceType, Region)
#   - NUMBER input with tiered pricing
#   - AND operator for compound conditions
#   - Cross-product fee matrix (3 services x 3 regions)
#   - Tiered pricing with volume discounts
#   - LET variables for rate constants
#
# FEE MATRIX:
#   ServiceType  | EU    | US    | ASIA
#   -------------|-------|-------|------
#   Filing       | 800   | 1000  | 600
#   Search       | 400   | 500   | 350
#   Examination  | 600   | 600   | 600
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/06_multilevel_cases.ipf
#   - Filing + EU + 20 docs: 800 + 300 = 1,100
#   - Search + US + 50 docs: 500 + (15x20 + 12x30) = 500 + 660 = 1,160
#
# NOTE:
#   Examination fee is region-independent (same for all regions).
#   The completeness checker verifies all combinations are covered.
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Multi-level cases'

DEFINE LIST ServiceType AS 'Type of service'
CHOICE Svc_Filing AS 'Patent Filing'
CHOICE Svc_Search AS 'Prior Art Search'
CHOICE Svc_Exam AS 'Examination'
DEFAULT Svc_Filing
ENDDEFINE

DEFINE LIST Region AS 'Target region'
CHOICE Reg_EU AS 'Europe'
CHOICE Reg_US AS 'United States'
CHOICE Reg_ASIA AS 'Asia Pacific'
DEFAULT Reg_EU
ENDDEFINE

DEFINE NUMBER DocumentCount AS 'Number of documents'
BETWEEN 1 AND 500
DEFAULT 20
ENDDEFINE

# Service fee varies by type and region
COMPUTE FEE ServiceFee
CASE ServiceType EQ Svc_Filing AND Region EQ Reg_EU AS
  YIELD 800
ENDCASE
CASE ServiceType EQ Svc_Filing AND Region EQ Reg_US AS
  YIELD 1000
ENDCASE
CASE ServiceType EQ Svc_Filing AND Region EQ Reg_ASIA AS
  YIELD 600
ENDCASE
CASE ServiceType EQ Svc_Search AND Region EQ Reg_EU AS
  YIELD 400
ENDCASE
CASE ServiceType EQ Svc_Search AND Region EQ Reg_US AS
  YIELD 500
ENDCASE
CASE ServiceType EQ Svc_Search AND Region EQ Reg_ASIA AS
  YIELD 350
ENDCASE
CASE ServiceType EQ Svc_Exam AS
  YIELD 600
ENDCASE
ENDCOMPUTE

# Document fee scales with count
COMPUTE FEE DocumentFee
LET BaseDocPrice AS 15
CASE DocumentCount LTE 20 AS
  YIELD BaseDocPrice * DocumentCount
ENDCASE
CASE DocumentCount GT 20 AS
  YIELD BaseDocPrice * 20 + 12 * (DocumentCount - 20)
ENDCASE
ENDCOMPUTE

VERIFY COMPLETE FEE ServiceFee
VERIFY COMPLETE FEE DocumentFee
VERIFY MONOTONIC FEE DocumentFee WITH RESPECT TO DocumentCount
