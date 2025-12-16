# ============================================================================
# Version Control and Regulatory References Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates IPFLang's versioning capabilities for tracking fee schedule
#   changes over time. The VERSION directive captures version numbers,
#   effective dates, descriptions, and references to official publications.
#
# FEATURES DEMONSTRATED:
#   - VERSION directive with all fields:
#     - Version number ('2024.2')
#     - EFFECTIVE date (when fees become valid)
#     - DESCRIPTION (human-readable summary)
#     - REFERENCE (legal/official publication reference)
#   - RETURN statements for exposing version metadata
#   - Tiered excess claims fee structure
#   - Applicant-based fee differentiation
#
# VERSION MANAGEMENT:
#   Fee schedules change periodically (often annually or quarterly).
#   The VERSION directive helps track:
#   - Which fee schedule version is being used
#   - When it became effective
#   - What official publication defines the fees
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- info examples/09_versioning.ipf
#   - Shows version: 2024.2
#   - Shows effective date: 2024-07-01
#   - Shows reference: Official Journal 2024/A15
#
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/09_versioning.ipf
#   - Company with 20 claims: 1,600 + 275x5 = 2,975
#   - Individual with 15 claims: 800 + 0 = 800
#
# ============================================================================

VERSION '2024.2' EFFECTIVE 2024-07-01 DESCRIPTION 'Q3 2024 rates' REFERENCE 'Official Journal 2024/A15'

DEFINE NUMBER ClaimCount AS 'Total claims'
BETWEEN 1 AND 200
DEFAULT 15
ENDDEFINE

DEFINE LIST ApplicantType AS 'Applicant category'
CHOICE Type_Company AS 'Company'
CHOICE Type_SME AS 'SME'
CHOICE Type_Individual AS 'Individual'
DEFAULT Type_Company
ENDDEFINE

# Filing fee with Q3 2024 rates
COMPUTE FEE FilingFee
CASE ApplicantType EQ Type_Company AS
  YIELD 1600
ENDCASE
CASE ApplicantType EQ Type_SME AS
  YIELD 1200
ENDCASE
CASE ApplicantType EQ Type_Individual AS
  YIELD 800
ENDCASE
ENDCOMPUTE

# Excess claims fee - using direct input references
COMPUTE FEE ExcessClaimsFee
LET Rate1 AS 275
LET Rate2 AS 685
CASE ClaimCount LTE 15 AS
  YIELD 0
ENDCASE
CASE ClaimCount GT 15 AND ClaimCount LTE 50 AS
  YIELD Rate1 * (ClaimCount - 15)
ENDCASE
CASE ClaimCount GT 50 AS
  YIELD Rate1 * 35 + Rate2 * (ClaimCount - 50)
ENDCASE
ENDCOMPUTE

VERIFY COMPLETE FEE FilingFee
VERIFY COMPLETE FEE ExcessClaimsFee
VERIFY MONOTONIC FEE ExcessClaimsFee WITH RESPECT TO ClaimCount

RETURN Version AS '2024.2'
RETURN Reference AS 'Official Journal 2024/A15'
