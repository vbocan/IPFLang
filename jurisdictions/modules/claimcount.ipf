# =============================================================================
# IPFLang Module: ClaimCount
# =============================================================================
# Define ClaimCount as NUMBER w/ DEF=1
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/claimcount.ipf
# =============================================================================

DEFINE NUMBER ClaimCount AS 'Number of claims?'
GROUP G_APP
BETWEEN 1 AND 1000
DEFAULT 1
ENDDEFINE