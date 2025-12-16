# =============================================================================
# IPFLang Module: EP_Extension
# =============================================================================
# Define Extension as BOOLEAN w/ DEF=FALSE
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/ep_extension.ipf
# =============================================================================

DEFINE BOOLEAN Extension AS 'EP: Designate Bosnia and Herzegovina?'
GROUP G_EPO
DEFAULT FALSE
ENDDEFINE