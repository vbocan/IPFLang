# =============================================================================
# IPFLang Module: PCT Input Definitions  
# =============================================================================
# Input definitions specific to PCT national/regional phase entry.
#
# Usage: COMPOSE modules/pct.ipf
# =============================================================================

# International Search Authority
DEFINE LIST ISA AS 'Which ISA searched the PCT application?'
GROUP G_PCT
CHOICE ISA_NONE AS 'None of the listed'
CHOICE ISA_EPO AS 'EP - European Patent Office'
CHOICE ISA_USPTO AS 'US - USPTO'
CHOICE ISA_JPO AS 'JP - Japan Patent Office'
CHOICE ISA_KIPO AS 'KR - Korean Intellectual Property Office'
CHOICE ISA_CNIPA AS 'CN - China National IP Administration'
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

# International Preliminary Report on Patentability (Chapter II)
DEFINE LIST IPRP AS 'Which office prepared the IPRP (Chapter II)?'
GROUP G_PCT
CHOICE IPRP_NONE AS 'No IPRP / Chapter I only'
CHOICE IPRP_EPO AS 'EP - European Patent Office'
CHOICE IPRP_USPTO AS 'US - USPTO'
CHOICE IPRP_JPO AS 'JP - Japan Patent Office'
CHOICE IPRP_KIPO AS 'KR - Korean IP Office'
CHOICE IPRP_CNIPA AS 'CN - CNIPA'
DEFAULT IPRP_NONE
ENDDEFINE

# ISR availability
DEFINE BOOLEAN ISR_AVAILABLE AS 'Is the International Search Report available?'
GROUP G_PCT
DEFAULT TRUE
ENDDEFINE

# RO same as designated office
DEFINE BOOLEAN RO_NP AS 'Is receiving office same as national phase office?'
GROUP G_PCT
DEFAULT FALSE
ENDDEFINE

# Office elected (Chapter II)
DEFINE BOOLEAN OfficeElected AS 'Is the designated office an elected office?'
GROUP G_PCT
DEFAULT FALSE
ENDDEFINE
