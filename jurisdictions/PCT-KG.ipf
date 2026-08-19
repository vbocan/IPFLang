# =============================================================================
# IPFLang Jurisdiction: PCT-KG
# =============================================================================
# Entry of national phase in Kyrgyzstan
#
# Category: OfficialFees
# Currency: KGS
# Phase: National Phase
#
# Inherits from: bases/base_ea.ipf
# Usage:
#   dotnet run --project ../src/IPFLang.CLI -- compose ./bases/base_ea.ipf ./PCT-KG.ipf
#
# =============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'Entry of national phase in Kyrgyzstan'

# Inputs and groups shared across this regional system are declared once in the base
# and inherited here. Redeclaring one below overrides the base for this jurisdiction only.


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

# File name: PCT-KG-OFF
# File content: Official fees for PCT national phase: Kyrgyzstan
# Valid until n/a
# Is newer than 30 days: False
# Fees keywords: ['claim']

COMPUTE FEE OFF_BasicNationalFee
LET Fee1 AS 8000
YIELD Fee1
ENDCOMPUTE

COMPUTE FEE OFF_IndependentClaimFee
LET CF1 AS 1500
YIELD CF1*(IndependentClaimCount-1) IF IndependentClaimCount GT 1 
ENDCOMPUTE

COMPUTE FEE OFF_ExaminationFee OPTIONAL #30months as of NPE
#The examination fee is reduced by 25% where an international search report has been established and by 50% where an international preliminary examination report has been established
LET Fee1 AS 15000
LET ICFee AS 7500
LET DiscountISR AS 0.25
LET DiscountIPRP AS 0.5
YIELD Fee1 + (IndependentClaimCount-1)*ICFee
YIELD -Fee1*DiscountIPRP IF IPRP NEQ IPRP_NONE
YIELD -Fee1*DiscountISR IF IPRP EQ IPRP_NONE AND ISR_AVAILABLE EQ TRUE
ENDCOMPUTE

#annuities after grant

# =============================================================================
# Translation Fees (OPTIONAL)
# Language: Kyrgyz, Russian
# =============================================================================

DEFINE NUMBER NumberOfWords AS 'Number of words to translate'
GROUP ApplicationGroup
BETWEEN 1 AND 10000000
DEFAULT 5000
ENDDEFINE

COMPUTE FEE TranslationFee OPTIONAL
# Translation fee per word into Kyrgyz, Russian
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
VERIFY COMPLETE FEE OFF_IndependentClaimFee
VERIFY COMPLETE FEE OFF_ExaminationFee
VERIFY MONOTONIC FEE OFF_IndependentClaimFee WITH RESPECT TO ClaimCount

# =============================================================================
# Returns
# =============================================================================

RETURN Currency AS 'KGS'
RETURN Jurisdiction AS 'KG'
RETURN JurisdictionName AS 'Entry of national phase in Kyrgyzstan'
