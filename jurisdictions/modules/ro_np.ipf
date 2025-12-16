# =============================================================================
# IPFLang Module: RO_NP
# =============================================================================
# Define RO_NP as BOOLEAN; if PCT RO = national entry jurisdiction w/ DEF=FALSE
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/ro_np.ipf
# =============================================================================

#asks user if RO of PCT application is the same as the jurisdiction in which national phase entry is requested (RO_NP)
DEFINE BOOLEAN RO_NP AS 'Is requested jurisdiction same as PCT/RO?'
GROUP G_PCT
DEFAULT FALSE
ENDDEFINE