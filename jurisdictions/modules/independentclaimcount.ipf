# =============================================================================
# IPFLang Module: IndependentClaimCount
# =============================================================================
# Define IndependentClaimCount as NUMBER w/ DEF=1
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/independentclaimcount.ipf
# =============================================================================

DEFINE NUMBER IndependentClaimCount AS 'Number of independent claims?'
GROUP G_APP
BETWEEN 1 AND 100
DEFAULT 1
ENDDEFINE