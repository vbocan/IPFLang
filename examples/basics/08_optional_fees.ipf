# ============================================================================
# Optional Fees Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates the OPTIONAL keyword for fees that are not always charged.
#   Optional fees are reported separately from mandatory fees and can be
#   excluded from the total at the user's discretion.
#
# FEATURES DEMONSTRATED:
#   - OPTIONAL keyword on COMPUTE FEE
#   - Mandatory vs optional fee distinction
#   - BOOLEAN input for service selection
#   - LIST input for filing type
#   - Conditional optional fees
#
# MANDATORY VS OPTIONAL:
#   - Mandatory fees: Always included in total (FilingFee, ClaimFee)
#   - Optional fees: User can choose to include (SearchFee, ExpediteSurcharge)
#
# FEE STRUCTURE:
#   FilingFee (mandatory):
#     - Standard: 500
#     - Expedited: 800
#     - Provisional: 300
#
#   SearchFee (optional, if requested):
#     - Standard/Expedited: 800
#     - Provisional: 400
#
#   ExpediteSurcharge (optional):
#     - Only applies to expedited filings: 500
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/08_optional_fees.ipf
#   - Standard + no search: Mandatory=500, Optional=0
#   - Expedited + search: Mandatory=800, Optional=800+500=1,300
#
# ============================================================================

VERSION '1.0.0' EFFECTIVE 2024-01-01 DESCRIPTION 'Optional fees'

DEFINE NUMBER ClaimCount AS 'Number of claims'
BETWEEN 1 AND 100
DEFAULT 10
ENDDEFINE

DEFINE LIST FilingType AS 'Filing type'
CHOICE Filing_Standard AS 'Standard'
CHOICE Filing_Expedited AS 'Expedited'
CHOICE Filing_Provisional AS 'Provisional'
DEFAULT Filing_Standard
ENDDEFINE

DEFINE BOOLEAN RequestSearch AS 'Request search?'
DEFAULT FALSE
ENDDEFINE

# Mandatory filing fee
COMPUTE FEE FilingFee
YIELD 500 IF FilingType EQ Filing_Standard
YIELD 800 IF FilingType EQ Filing_Expedited
YIELD 300 IF FilingType EQ Filing_Provisional
ENDCOMPUTE

# Mandatory claim fee
COMPUTE FEE ClaimFee
LET ExcessClaims AS ClaimCount - 10
YIELD 50 * ExcessClaims IF ExcessClaims GT 0
ENDCOMPUTE

# Optional search fee
COMPUTE FEE SearchFee OPTIONAL
CASE RequestSearch EQ TRUE AS
  YIELD 800 IF FilingType EQ Filing_Standard
  YIELD 800 IF FilingType EQ Filing_Expedited
  YIELD 400 IF FilingType EQ Filing_Provisional
ENDCASE
CASE RequestSearch EQ FALSE AS
  YIELD 0
ENDCASE
ENDCOMPUTE

# Optional expedite surcharge
COMPUTE FEE ExpediteSurcharge OPTIONAL
CASE FilingType EQ Filing_Expedited AS
  YIELD 500
ENDCASE
YIELD 0 IF FilingType NEQ Filing_Expedited
ENDCOMPUTE

VERIFY COMPLETE FEE FilingFee
VERIFY COMPLETE FEE SearchFee
