# =============================================================================
# IPFLang Module: PriorityCount
# =============================================================================
# Define PriorityCount as NUMBER w/ DEF=0
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/prioritycount.ipf
# =============================================================================

DEFINE NUMBER PriorityCount AS 'Number of priorities?'
GROUP G_APP
BETWEEN 0 AND 100
DEFAULT 0
ENDDEFINE
