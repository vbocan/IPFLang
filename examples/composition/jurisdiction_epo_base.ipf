# =============================================================================
# IPFLang Example: European Patent Office (EPO) - Base Fee Schedule
# =============================================================================
# This file defines the base fee structure for EPO patent filings.
# It serves as the ROOT jurisdiction in a composition hierarchy.
#
# Jurisdiction Hierarchy:
#   [EPO Base] (this file)
#       └── EPO-DE (Germany - adds translation and validation fees)
#
# How composition works:
#   1. Child jurisdictions INHERIT all inputs, fees, and groups from parent
#   2. Child jurisdictions can OVERRIDE fees by defining them with the same name
#   3. Child jurisdictions can ADD new fees not present in parent
#   4. VERIFY directives from all jurisdictions in the chain are combined
#
# Usage:
#   As standalone:  dotnet run --project src/IPFLang.CLI -- run examples/jurisdiction_epo_base.ipf
#   With child:     dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_de.ipf
#
# =============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'EPO Base Fee Schedule 2024'

# Groups for organizing inputs
DEFINE GROUP ApplicationGroup AS 'Application Settings' WITH WEIGHT 1
DEFINE GROUP ClaimsGroup AS 'Claims and Pages' WITH WEIGHT 2

# Application type selection
DEFINE LIST ApplicationType AS 'Type of patent application'
GROUP ApplicationGroup
CHOICE STANDARD AS 'Standard application'
CHOICE DIVISIONAL AS 'Divisional application'
CHOICE PCT_ENTRY AS 'PCT national phase entry'
DEFAULT STANDARD
ENDDEFINE

# Applicant type for fee reductions
DEFINE LIST ApplicantType AS 'Type of applicant'
GROUP ApplicationGroup
CHOICE LARGE AS 'Large entity (full fees)'
CHOICE SME AS 'SME (30% reduction)'
CHOICE MICRO AS 'Micro entity (50% reduction)'
DEFAULT LARGE
ENDDEFINE

# Number of claims
DEFINE NUMBER ClaimCount AS 'Number of claims'
GROUP ClaimsGroup
BETWEEN 1 AND 100
DEFAULT 10
ENDDEFINE

# Number of pages
DEFINE NUMBER PageCount AS 'Number of pages'
GROUP ClaimsGroup
BETWEEN 1 AND 500
DEFAULT 30
ENDDEFINE

# Request examination?
DEFINE BOOLEAN RequestExamination AS 'Request examination at filing'
GROUP ApplicationGroup
DEFAULT TRUE
ENDDEFINE

# Request search?
DEFINE BOOLEAN RequestSearch AS 'Request search at filing'
GROUP ApplicationGroup
DEFAULT TRUE
ENDDEFINE

# -----------------------------------------------------------------------------
# Fee Definitions - Core EPO fees
# Filing fee varies by applicant type
# -----------------------------------------------------------------------------

COMPUTE FEE FilingFee
CASE ApplicantType EQ LARGE AS
  YIELD 140<EUR>
ENDCASE
CASE ApplicantType EQ SME AS
  YIELD 98<EUR>
ENDCASE
CASE ApplicantType EQ MICRO AS
  YIELD 70<EUR>
ENDCASE
ENDCOMPUTE

COMPUTE FEE SearchFee
CASE RequestSearch EQ TRUE AND ApplicantType EQ LARGE AS
  YIELD 1520<EUR>
ENDCASE
CASE RequestSearch EQ TRUE AND ApplicantType EQ SME AS
  YIELD 1064<EUR>
ENDCASE
CASE RequestSearch EQ TRUE AND ApplicantType EQ MICRO AS
  YIELD 760<EUR>
ENDCASE
CASE RequestSearch EQ FALSE AS
  YIELD 0
ENDCASE
ENDCOMPUTE

COMPUTE FEE ExaminationFee
CASE RequestExamination EQ TRUE AND ApplicantType EQ LARGE AS
  YIELD 1885<EUR>
ENDCASE
CASE RequestExamination EQ TRUE AND ApplicantType EQ SME AS
  YIELD 1320<EUR>
ENDCASE
CASE RequestExamination EQ TRUE AND ApplicantType EQ MICRO AS
  YIELD 943<EUR>
ENDCASE
CASE RequestExamination EQ FALSE AS
  YIELD 0
ENDCASE
ENDCOMPUTE

COMPUTE FEE ClaimsFee
LET ClaimFeeBase AS 275
LET ClaimFeeHigh AS 660
CASE ClaimCount LTE 15 AS
  YIELD 0
ENDCASE
CASE ClaimCount GT 15 AND ClaimCount LTE 50 AS
  YIELD ClaimFeeBase * (ClaimCount - 15)
ENDCASE
CASE ClaimCount GT 50 AS
  YIELD ClaimFeeBase * 35 + ClaimFeeHigh * (ClaimCount - 50)
ENDCASE
ENDCOMPUTE

COMPUTE FEE PageFee
CASE PageCount LTE 35 AS
  YIELD 0
ENDCASE
CASE PageCount GT 35 AS
  YIELD 17 * (PageCount - 35)
ENDCASE
ENDCOMPUTE

COMPUTE FEE DesignationFee
CASE ApplicantType EQ LARGE AS
  YIELD 680<EUR>
ENDCASE
CASE ApplicantType EQ SME AS
  YIELD 476<EUR>
ENDCASE
CASE ApplicantType EQ MICRO AS
  YIELD 340<EUR>
ENDCASE
ENDCOMPUTE

# Verification directives
VERIFY COMPLETE FEE FilingFee
VERIFY COMPLETE FEE SearchFee
VERIFY COMPLETE FEE ExaminationFee
VERIFY COMPLETE FEE ClaimsFee
VERIFY COMPLETE FEE PageFee
VERIFY COMPLETE FEE DesignationFee
VERIFY MONOTONIC FEE ClaimsFee WITH RESPECT TO ClaimCount
VERIFY MONOTONIC FEE PageFee WITH RESPECT TO PageCount

# Returns
RETURN Currency AS 'EUR'
RETURN JurisdictionLevel AS 'EPO Regional Phase'
RETURN TotalFees AS 'Total EPO Filing Fees'
