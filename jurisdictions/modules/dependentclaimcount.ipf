# =============================================================================
# IPFLang Module: DependentClaimCount
# =============================================================================
# Define DependentClaimCount as NUMBER w/ DEF=0
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/dependentclaimcount.ipf
# =============================================================================

DEFINE NUMBER DependentClaimCount AS 'Number of dependent claims?'
GROUP G_APP
BETWEEN 0 AND 100
DEFAULT 0
ENDDEFINE