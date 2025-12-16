# =============================================================================
# IPFLang Module: Common Input Definitions
# =============================================================================
# Shared input definitions used across all jurisdictions.
#
# Usage: COMPOSE modules/common.ipf
# =============================================================================

# Group definitions with weights for UI ordering
DEFINE GROUP G_APP AS 'Application Details' WITH WEIGHT 10
DEFINE GROUP G_APL AS 'Applicant Details' WITH WEIGHT 20
DEFINE GROUP G_PCT AS 'PCT Details' WITH WEIGHT 30

# Entity size definition
DEFINE LIST EntitySize AS 'Entity size of applicant'
GROUP G_APL
CHOICE Entity_Person AS 'Natural person'
CHOICE Entity_Less25 AS 'Less than 25 employees'
CHOICE Entity_Less100 AS 'Less than 100 employees'
CHOICE Entity_Small AS 'Small entity'
CHOICE Entity_Micro AS 'Micro entity'
CHOICE Entity_Large AS 'Large entity (default)'
DEFAULT Entity_Large
ENDDEFINE

# Application date
DEFINE DATE ApplicationDate AS 'Application date (international filing date)'
GROUP G_APP
BETWEEN 01.01.1900 AND TODAY
DEFAULT TODAY
ENDDEFINE

# Claim counts
DEFINE NUMBER ClaimCount AS 'Total number of claims'
GROUP G_APP
BETWEEN 1 AND 1000
DEFAULT 10
ENDDEFINE

DEFINE NUMBER IndependentClaimCount AS 'Number of independent claims'
GROUP G_APP
BETWEEN 1 AND 100
DEFAULT 3
ENDDEFINE

DEFINE NUMBER DependentClaimCount AS 'Number of dependent claims'
GROUP G_APP
BETWEEN 0 AND 100
DEFAULT 7
ENDDEFINE

# Sheet/page count
DEFINE NUMBER SheetCount AS 'Number of pages/sheets'
GROUP G_APP
BETWEEN 1 AND 10000
DEFAULT 30
ENDDEFINE

# Priority count
DEFINE NUMBER PriorityCount AS 'Number of priority claims'
GROUP G_APP
BETWEEN 0 AND 100
DEFAULT 1
ENDDEFINE

# Translation word count
DEFINE NUMBER NumberOfWords AS 'Number of words for translation'
GROUP G_APP
BETWEEN 1 AND 10000000
DEFAULT 5000
ENDDEFINE
