# =============================================================================
# IPFLang Module: ISA
# =============================================================================
# Define ISA as LIST w/ DEF=ISA_NONE
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/isa.ipf
# =============================================================================

# International Search Authority
DEFINE LIST ISA AS 'Which ISA searched the PCT application?'
GROUP G_PCT
CHOICE ISA_NONE AS 'NONE OF THE LIST'
CHOICE ISA_AT AS 'AT - ATPO'
CHOICE ISA_CA AS 'CA - CIPO'
CHOICE ISA_EPO AS 'EP - EPO'
CHOICE ISA_ES AS 'ES - OEPM'
CHOICE ISA_FI AS 'FI - PRH'
CHOICE ISA_JP AS 'JP - JPO'
CHOICE ISA_RU AS 'RU - ROSPAT'
CHOICE ISA_SE AS 'SE - PRV' 
CHOICE ISA_TR AS 'TR - Turkpatent'
CHOICE ISA_US AS 'US - USPTO'
CHOICE ISA_NPI AS 'XN - NPI'
CHOICE ISA_VI AS 'XV - Visegrad Patent Institute'
DEFAULT ISA_NONE
ENDDEFINE