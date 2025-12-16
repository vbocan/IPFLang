# =============================================================================
# IPFLang Module: OfficeElected
# =============================================================================
# Define OfficeElected as BOOLEAN w/ DEF=FALSE
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/officeelected.ipf
# =============================================================================

#asks user if designated office is also elected office
DEFINE BOOLEAN OfficeElected AS 'Is designated office elected office?'
GROUP G_PCT
DEFAULT FALSE
ENDDEFINE