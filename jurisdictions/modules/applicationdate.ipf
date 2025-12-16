# =============================================================================
# IPFLang Module: ApplicationDate
# =============================================================================
# Define ApplicationDate as DATE w/ DEF=TODAY
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/applicationdate.ipf
# =============================================================================

DEFINE DATE ApplicationDate AS 'Application date'
GROUP G_APP
BETWEEN 01.01.1900 AND TODAY
DEFAULT TODAY
ENDDEFINE