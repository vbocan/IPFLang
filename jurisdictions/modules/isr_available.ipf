# =============================================================================
# IPFLang Module: ISR_AVAILABLE
# =============================================================================
# Define ISR_AVAILABLE (International Search Report) as BOOLEAN w/ DEF=TRUE
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/isr_available.ipf
# =============================================================================

DEFINE BOOLEAN ISR_AVAILABLE AS 'Is ISR available?'
GROUP G_PCT
DEFAULT TRUE
ENDDEFINE