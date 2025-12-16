# =============================================================================
# IPFLang Module: MultipleDependentClaimCount
# =============================================================================
# Define MultipleDependentClaimCount as NUMBER w/ DEF=0
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/multipledependentclaimcount.ipf
# =============================================================================

DEFINE NUMBER MultipleDependentClaimCount AS 'Number of multiple dependent claims?'
GROUP G_APP
BETWEEN 0 AND 100
DEFAULT 0
ENDDEFINE