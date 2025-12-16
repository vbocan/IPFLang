# =============================================================================
# IPFLang Jurisdiction: PCT-EP
# =============================================================================
# Entry of regional phase in Europe
#
# Category: OfficialFees
# Currency: EUR
# Phase: Regional Phase
#
# Usage:
#   dotnet run --project ../src/IPFLang.CLI -- run ./PCT-EP.ipf
#
# =============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'Entry of regional phase in Europe'


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

DEFINE BOOLEAN Extension AS 'Request extension to additional states?'
GROUP PCTGroup
DEFAULT FALSE
ENDDEFINE

# Validation countries (states where EP patent can be validated)
DEFINE MULTILIST VAL_CTY AS 'EP: Select validation countries'
GROUP PCTGroup
CHOICE VAL_NONE AS 'None'
CHOICE VAL_KH AS 'Cambodia'
CHOICE VAL_MA AS 'Morocco'
CHOICE VAL_MD AS 'Republic of Moldova'
CHOICE VAL_TN AS 'Tunisia'
DEFAULT VAL_NONE
ENDDEFINE



# =============================================================================
# Official Fees
# =============================================================================

# File name: PCT-EP-OFF
# File content: Official fees for PCT regional phase: EPO
# Valid until 31/03/2023

COMPUTE FEE OFF_BasicNationalFee
YIELD 135<EUR>
ENDCOMPUTE

COMPUTE FEE OFF_DesignationFee
YIELD 660<EUR>
ENDCOMPUTE

COMPUTE FEE OFF_ExtensionFee OPTIONAL
YIELD 102<EUR> IF Extension EQ TRUE
ENDCOMPUTE

COMPUTE FEE OFF_SheetFee
YIELD 17*(SheetCount-35) IF SheetCount GT 35
ENDCOMPUTE

COMPUTE FEE OFF_ClaimFee
LET CF1 AS 265
LET CF2 AS 660
YIELD CF1*(ClaimCount-15) IF ClaimCount GT 15 AND ClaimCount LT 51
YIELD CF2*(ClaimCount-50) + CF1*(ClaimCount-15-(ClaimCount-50)) IF ClaimCount GT 50
ENDCOMPUTE

COMPUTE FEE OFF_SearchFee
LET SF1 AS 1460
LET SF2 AS 1185 #reduction in line with OJ EPO 2022-A2

YIELD 0 IF ISA EQ ISA_EPO
YIELD SF1-SF2 IF ISA EQ ISA_AT OR ISA EQ ISA_FI OR ISA EQ ISA_NPI OR ISA EQ ISA_ES OR ISA EQ ISA_SE OR ISA EQ ISA_TR OR ISA EQ ISA_VI
YIELD SF1 IF ISA NEQ ISA_AT AND ISA NEQ ISA_FI AND ISA NEQ ISA_NPI AND ISA NEQ ISA_ES AND ISA NEQ ISA_SE AND ISA NEQ ISA_TR AND ISA NEQ ISA_VI AND ISA NEQ ISA_NONE
ENDCOMPUTE

COMPUTE FEE OFF_ExaminationFee
LET ExFee1 AS 2055
LET ExFee2 AS 1840
LET Discount AS 0.75

CASE IPRP EQ IPRP_EPO AS # discount scheme if EPO has prepared IPRP (Chapter II)
YIELD ExFee1*Discount IF ISA EQ ISA_EPO 
YIELD ExFee2*Discount IF ISA NEQ ISA_EPO
ENDCASE

CASE IPRP NEQ IPRP_EPO AS # no discount scheme
YIELD ExFee1 IF ISA EQ ISA_EPO
YIELD ExFee2 IF ISA NEQ ISA_EPO
ENDCASE

ENDCOMPUTE

COMPUTE FEE OFF_ValidationFee_KH
YIELD 180<EUR> IF VAL_KH IN VAL_CTY
ENDCOMPUTE
COMPUTE FEE OFF_ValidationFee_MA
YIELD 240<EUR> IF VAL_MA IN VAL_CTY
ENDCOMPUTE
COMPUTE FEE OFF_ValidationFee_MD
YIELD 200<EUR> IF VAL_MD IN VAL_CTY
ENDCOMPUTE
COMPUTE FEE OFF_ValidationFee_TN
YIELD 180<EUR> IF VAL_TN IN VAL_CTY
ENDCOMPUTE

# need to check if 3rd annuity is due (>1.5 years after filing date)
COMPUTE FEE OFF_Renewal3
LET RenFee AS 530
LET RenMonth AS ROUND ( ApplicationDate!MONTHSTONOW_FROMLASTDAY ) #calculate difference in months to today
YIELD RenFee IF RenMonth GTE 18
ENDCOMPUTE

# =============================================================================
# Translation Fees (OPTIONAL)
# Language: English, French, German
# =============================================================================

DEFINE NUMBER NumberOfWords AS 'Number of words to translate'
GROUP ApplicationGroup
BETWEEN 1 AND 10000000
DEFAULT 5000
ENDDEFINE

COMPUTE FEE TranslationFee OPTIONAL
# Translation fee per word into English, French, German
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
VERIFY COMPLETE FEE OFF_DesignationFee
VERIFY COMPLETE FEE OFF_ExtensionFee
VERIFY COMPLETE FEE OFF_SheetFee
VERIFY COMPLETE FEE OFF_ClaimFee
VERIFY COMPLETE FEE OFF_SearchFee
VERIFY COMPLETE FEE OFF_ExaminationFee
VERIFY COMPLETE FEE OFF_ValidationFee_KH
VERIFY COMPLETE FEE OFF_ValidationFee_MA
VERIFY COMPLETE FEE OFF_ValidationFee_MD
VERIFY COMPLETE FEE OFF_ValidationFee_TN
VERIFY MONOTONIC FEE OFF_SheetFee WITH RESPECT TO SheetCount
VERIFY MONOTONIC FEE OFF_ClaimFee WITH RESPECT TO ClaimCount

# =============================================================================
# Returns
# =============================================================================

RETURN Currency AS 'EUR'
RETURN Jurisdiction AS 'EP'
RETURN JurisdictionName AS 'Entry of regional phase in Europe'
