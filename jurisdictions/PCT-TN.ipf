# =============================================================================
# IPFLang Jurisdiction: PCT-TN
# =============================================================================
# Entry of national phase in Tunisia
#
# Category: OfficialFees
# Currency: TND
# Phase: National Phase
#
# Usage:
#   dotnet run --project ../src/IPFLang.CLI -- run ./PCT-TN.ipf
#
# =============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'Entry of national phase in Tunisia'


# Groups
DEFINE GROUP GeneralGroup AS 'General Settings' WITH WEIGHT 1
DEFINE GROUP ApplicationGroup AS 'Application Details' WITH WEIGHT 2
DEFINE GROUP PCTGroup AS 'PCT Information' WITH WEIGHT 3

# Entity size (includes all entity classifications used globally)
DEFINE LIST EntitySize AS 'Entity size of applicant'
GROUP GeneralGroup
CHOICE Entity_Person AS 'Natural person'
CHOICE Entity_Less25 AS 'Less than 25 employees'
CHOICE Entity_Less100 AS 'Less than 100 employees'
CHOICE Entity_Small AS 'Small entity'
CHOICE Entity_Micro AS 'Micro entity'
CHOICE Entity_Large AS 'Large entity (default)'
DEFAULT Entity_Large
ENDDEFINE

# Application date
DEFINE DATE ApplicationDate AS 'Application date'
GROUP ApplicationGroup
BETWEEN 01.01.2000 AND TODAY
DEFAULT TODAY
ENDDEFINE

# Claims
DEFINE NUMBER ClaimCount AS 'Number of claims'
GROUP ApplicationGroup
BETWEEN 1 AND 1000
DEFAULT 10
ENDDEFINE

# Sheets/Pages
DEFINE NUMBER SheetCount AS 'Number of pages'
GROUP ApplicationGroup
BETWEEN 1 AND 10000
DEFAULT 30
ENDDEFINE

# Priority count
DEFINE NUMBER PriorityCount AS 'Number of priorities claimed'
GROUP ApplicationGroup
BETWEEN 0 AND 100
DEFAULT 1
ENDDEFINE

# Independent claim count
DEFINE NUMBER IndependentClaimCount AS 'Number of independent claims'
GROUP ApplicationGroup
BETWEEN 1 AND 100
DEFAULT 3
ENDDEFINE

# Multiple dependent claim count
DEFINE NUMBER MultipleDependentClaimCount AS 'Number of multiple dependent claims'
GROUP ApplicationGroup
BETWEEN 0 AND 100
DEFAULT 0
ENDDEFINE

# Dependent claim count (for jurisdictions that charge per dependent claim)
DEFINE NUMBER DependentClaimCount AS 'Number of dependent claims'
GROUP ApplicationGroup
BETWEEN 0 AND 1000
DEFAULT 7
ENDDEFINE

# Country count (for regional organizations like ARIPO)
DEFINE NUMBER CountryCount AS 'Number of designated countries'
GROUP ApplicationGroup
BETWEEN 1 AND 50
DEFAULT 1
ENDDEFINE

# PCT-specific inputs
DEFINE LIST ISA AS 'Which ISA searched the PCT application?'
GROUP PCTGroup
CHOICE ISA_NONE AS 'None of the listed'
CHOICE ISA_EPO AS 'EP - European Patent Office'
CHOICE ISA_US AS 'US - USPTO'
CHOICE ISA_JP AS 'JP - Japan Patent Office'
CHOICE ISA_KR AS 'KR - Korean IP Office'
CHOICE ISA_CN AS 'CN - CNIPA'
CHOICE ISA_AT AS 'AT - Austrian Patent Office'
CHOICE ISA_AU AS 'AU - IP Australia'
CHOICE ISA_CA AS 'CA - Canadian IP Office'
CHOICE ISA_ES AS 'ES - Spanish Patent Office'
CHOICE ISA_FI AS 'FI - Finnish Patent Office'
CHOICE ISA_RU AS 'RU - Rospatent'
CHOICE ISA_SE AS 'SE - Swedish Patent Office'
CHOICE ISA_TR AS 'TR - Turkish Patent Office'
CHOICE ISA_NPI AS 'XN - Nordic Patent Institute'
CHOICE ISA_VI AS 'XV - Visegrad Patent Institute'
DEFAULT ISA_NONE
ENDDEFINE

DEFINE LIST IPRP AS 'Which office provided an IPRP (Chapter II)?'
GROUP PCTGroup
CHOICE IPRP_NONE AS 'No IPRP / Chapter I only'
CHOICE IPRP_EPO AS 'EP - European Patent Office'
CHOICE IPRP_US AS 'US - USPTO'
CHOICE IPRP_JP AS 'JP - Japan Patent Office'
CHOICE IPRP_KR AS 'KR - Korean IP Office'
CHOICE IPRP_CN AS 'CN - CNIPA'
CHOICE IPRP_AT AS 'AT - Austrian Patent Office'
CHOICE IPRP_AU AS 'AU - IP Australia'
CHOICE IPRP_RU AS 'RU - Rospatent'
DEFAULT IPRP_NONE
ENDDEFINE

DEFINE BOOLEAN ISR_AVAILABLE AS 'Is International Search Report available?'
GROUP PCTGroup
DEFAULT TRUE
ENDDEFINE

DEFINE BOOLEAN RO_NP AS 'Is receiving office same as national phase office?'
GROUP PCTGroup
DEFAULT FALSE
ENDDEFINE

DEFINE BOOLEAN OfficeElected AS 'Is the designated office an elected office?'
GROUP PCTGroup
DEFAULT FALSE
ENDDEFINE

DEFINE BOOLEAN Extension AS 'Request extension to additional states?'
GROUP PCTGroup
DEFAULT FALSE
ENDDEFINE

# =============================================================================
# Official Fees
# =============================================================================

# File name: PCT-TN-OFF
# File content: Official fees for PCT national phase: Tunisia
# Valid until n/a
# Is newer than 30 days: False
# Fees keywords: ['claim']

COMPUTE FEE OFF_BasicNationalFee
LET Fee1 AS 140
YIELD Fee1
ENDCOMPUTE

COMPUTE FEE OFF_ClaimFee
LET Fee1 AS 30
YIELD Fee1*(ClaimCount-10) IF ClaimCount GT 10
ENDCOMPUTE

COMPUTE FEE OFF_Renewal #as of 2nd year
LET RenFee AS 50
LET RenMonth AS ROUND ( ApplicationDate!MONTHSTONOW_FROMLASTDAY ) #calculate difference in months to today
YIELD RenFee IF RenMonth GTE 12
YIELD RenFee IF RenMonth GTE 24
ENDCOMPUTE

# =============================================================================
# Translation Fees (OPTIONAL)
# Language: Arabic, English, French
# =============================================================================

DEFINE NUMBER NumberOfWords AS 'Number of words to translate'
GROUP ApplicationGroup
BETWEEN 1 AND 10000000
DEFAULT 5000
ENDDEFINE

COMPUTE FEE TranslationFee OPTIONAL
# Translation fee per word into Arabic, English, French
# Translation fee into local language per 100 words; source language is English
YIELD 12 * NumberOfWords / 100
ENDCOMPUTE

# =============================================================================
# Service Fees (OPTIONAL)
# =============================================================================

COMPUTE FEE AgentServiceFee OPTIONAL
YIELD 100<EUR>
ENDCOMPUTE

# =============================================================================
# Verification Directives
# =============================================================================

VERIFY COMPLETE FEE OFF_BasicNationalFee
VERIFY COMPLETE FEE OFF_ClaimFee
VERIFY MONOTONIC FEE OFF_ClaimFee WITH RESPECT TO ClaimCount

# =============================================================================
# Returns
# =============================================================================

RETURN Currency AS 'TND'
RETURN Jurisdiction AS 'TN'
RETURN JurisdictionName AS 'Entry of national phase in Tunisia'
