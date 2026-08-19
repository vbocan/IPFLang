# =============================================================================
# IPFLang Jurisdiction: PCT-BY
# =============================================================================
# Entry of regional phase in Belarus
#
# Category: OfficialFees
# Currency: BYN
# Phase: National Phase
#
# Inherits from: bases/base_ea.ipf
# Usage:
#   dotnet run --project ../src/IPFLang.CLI -- compose ./bases/base_ea.ipf ./PCT-BY.ipf
#
# =============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'Entry of regional phase in Belarus'

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

# File name: PCT-BY-OFF
# File content: Official fees for PCT national phase: Belarus
# Valid until n/a

COMPUTE FEE OFF_BasicNationalFee
LET Fee AS 90.65
YIELD Fee
ENDCOMPUTE

COMPUTE FEE OFF_ClaimFee
LET CF AS 38.85
YIELD CF*(IndependentClaimCount-1) IF IndependentClaimCount GT 1
ENDCOMPUTE

COMPUTE FEE OFF_ExaminationFee
LET Fee1 AS 440.30 
LET Fee2 AS 0.5 * Fee1 # 50% Discount if ISR available
LET IndepClaimFee AS 259 # no discount seems to apply here

YIELD Fee1 IF ISR_AVAILABLE NEQ TRUE
YIELD Fee2 IF ISR_AVAILABLE EQ TRUE
YIELD IndepClaimFee*(IndependentClaimCount-1) IF IndependentClaimCount GT 1
ENDCOMPUTE

COMPUTE FEE OFF_GrantFee OPTIONAL
YIELD 181.30<BYN>
ENDCOMPUTE

# no annuities before grant

# =============================================================================
# Translation Fees (OPTIONAL)
# Language: Belarussian, Russian
# =============================================================================

DEFINE NUMBER NumberOfWords AS 'Number of words to translate'
GROUP ApplicationGroup
BETWEEN 1 AND 10000000
DEFAULT 5000
ENDDEFINE

COMPUTE FEE TranslationFee OPTIONAL
# Translation fee per word into Belarussian, Russian
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
VERIFY COMPLETE FEE OFF_ExaminationFee
VERIFY COMPLETE FEE OFF_GrantFee
VERIFY MONOTONIC FEE OFF_ClaimFee WITH RESPECT TO ClaimCount

# =============================================================================
# Returns
# =============================================================================

RETURN Currency AS 'BYN'
RETURN Jurisdiction AS 'BY'
RETURN JurisdictionName AS 'Entry of regional phase in Belarus'
